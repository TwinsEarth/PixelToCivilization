using System;
using UnityEngine;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Colonization
{
    /// <summary>
    /// IColonizationService 实现。统一"建立新聚落"入口，校验时代/地理/资源/人口/间距，
    /// 年度自动扩张，提供领土归属查询。
    /// </summary>
    public sealed class ColonizationService : IColonizationService
    {
        private const float TwoPi = (float)Math.PI * 2f;

        // 复用的领土查询缓冲
        private readonly int[] _near = new int[32];

        private readonly SettlementRegistry _settlements;
        private readonly IServiceLocator _services;
        private readonly IEventBus _events;
        private readonly Action<string> _log;

        public ColonizationService(
            SettlementRegistry settlements,
            IServiceLocator services,
            IEventBus events,
            Action<string> log)
        {
            _settlements = settlements;
            _services = services;
            _events = events;
            _log = log;
        }

        // ------------------------------------------------------------
        // 条件检查
        // ------------------------------------------------------------
        public bool CanFound(int faction, SettlementKind kind, Vector3 position, out string reason)
        {
            reason = null;

            var factions = Resolve<FactionRegistry>();
            var factionInfo = factions != null ? factions.Get(faction) : null;
            if (factionInfo == null || !factionInfo.Alive)
            {
                reason = "势力不存在或已灭国";
                return false;
            }

            // 时代门控
            var tech = Resolve<ITechService>();
            if (tech != null)
            {
                int era = (int)tech.GetEra(faction);
                if (era < ColonizationProfile.RequiredEra(kind))
                {
                    reason = $"时代不足（需要时代 {ColonizationProfile.RequiredEra(kind)}，当前 {era}）";
                    return false;
                }
            }

            // 地理：聚落必须建立在陆地上
            var geo = Resolve<IGeographyQuery>();
            if (geo != null && !geo.IsLand(position.x, position.z))
            {
                reason = "目标位置是水域，聚落必须建立在陆地";
                return false;
            }

            // 全局/势力聚落上限
            if (_settlements.CountByFaction(faction) >= ColonizationProfile.MaxSettlementsPerFaction)
            {
                reason = "已达势力聚落数量上限";
                return false;
            }
            if (_settlements.Count >= ColonizationProfile.MaxSettlementsTotal)
            {
                reason = "已达全局聚落数量上限";
                return false;
            }

            // 间距：不能与任何现有聚落过近（领土不重叠）
            int nearest = FindNearestSettlement(position, out float nearestDist);
            if (nearest >= 0)
            {
                float required =
                    TerritoryRadius(nearest) * ColonizationProfile.MinSpacingFactor;
                if (nearestDist < required)
                {
                    ref var nr = ref _settlements.GetSlotRef(nearest);
                    reason = $"与现有聚落 {nr.Name} 过近（需相距 {required:0.#}，当前 {nearestDist:0.#}）";
                    return false;
                }
            }

            // 资源
            var economy = Resolve<IEconomyService>();
            var costs = ColonizationProfile.Costs(kind);
            if (economy != null && !economy.CanAffordBundle(faction, costs))
            {
                reason = "资源不足";
                return false;
            }

            // 移民劳力
            var population = Resolve<IPopulationService>();
            int migrants = ColonizationProfile.Migrants(kind);
            if (population != null && population.ByLayer(faction, PopulationLayer.Laborer) < migrants)
            {
                reason = $"劳力人口不足（需要 {migrants} 名劳力）";
                return false;
            }

            return true;
        }

        // ------------------------------------------------------------
        // 建立聚落
        // ------------------------------------------------------------
        public bool TryFoundSettlement(int faction, SettlementKind kind, Vector3 position, string name)
        {
            if (!CanFound(faction, kind, position, out string reason))
            {
                _log?.Invoke($"[殖民] 无法建立 {kind}：{reason}");
                return false;
            }

            var economy = Resolve<IEconomyService>();
            var population = Resolve<IPopulationService>();
            var costs = ColonizationProfile.Costs(kind);
            int migrants = ColonizationProfile.Migrants(kind);

            // 先原子扣资源
            if (economy != null && !economy.TrySpendBundle(faction, costs))
            {
                _log?.Invoke("[殖民] 资源扣减失败");
                return false;
            }

            // 再派遣移民（若失败，补偿回滚资源）
            if (population != null && !population.Emigrate(faction, migrants))
            {
                _log?.Invoke("[殖民] 移民派遣失败，回滚资源");
                if (economy != null)
                {
                    for (int i = 0; i < costs.Length; i++)
                        economy.Add(faction, costs[i].Type, costs[i].Amount);
                }
                return false;
            }

            int year = ResolveGameYear();
            string finalName = string.IsNullOrEmpty(name)
                ? $"{ResolveFactionName(faction)}聚落"
                : name;

            int slot = _settlements.Found(faction, kind, finalName, position, year, migrants);
            ref var r = ref _settlements.GetSlotRef(slot);

            // 贴地
            var geo = Resolve<IGeographyQuery>();
            if (geo != null) r.Position.y = geo.TerrainHeight(r.Position.x, r.Position.z);

            if (_events != null)
            {
                _events.Publish(new SettlementFoundedEvent
                {
                    Faction = faction,
                    Kind = (int)kind,
                    GameYear = year,
                    Position = r.Position,
                    SettlementName = finalName,
                });
            }
            _log?.Invoke($"[殖民] {finalName} 于 {year} 年建立（{kind}，移民 {migrants} 人）");
            return true;
        }

        // ------------------------------------------------------------
        // 年度自动扩张
        // ------------------------------------------------------------
        public int AutoExpand(int gameYear)
        {
            var factions = Resolve<FactionRegistry>();
            var population = Resolve<IPopulationService>();
            if (factions == null || population == null) return 0;

            int founded = 0;
            for (int f = 0; f < factions.Count; f++)
            {
                var info = factions.Get(f);
                if (info == null || !info.Alive) continue;

                int count = _settlements.CountByFaction(f);
                if (count >= ColonizationProfile.MaxSettlementsPerFaction) continue;
                if (_settlements.Count >= ColonizationProfile.MaxSettlementsTotal) break;

                // 扩张门槛：总人口随聚落数提高
                int total = population.Total(f);
                int threshold =
                    ColonizationProfile.MinPopulationForExpansion +
                    (count - 1) * ColonizationProfile.PopulationPerSettlement;
                if (total < threshold) continue;

                // 选址：从首都（或势力出生点）出发，沿确定性方向尝试
                Vector3 anchor;
                float anchorRadius;
                int capital = _settlements.FindCapital(f);
                if (capital >= 0)
                {
                    var cr = _settlements.GetSafe(capital);
                    anchor = cr.Position;
                    anchorRadius = TerritoryRadius(capital);
                }
                else
                {
                    anchor = new Vector3(info.HomeX, 0f, info.HomeZ);
                    var baseSvc = Resolve<ISettlementService>();
                    anchorRadius = baseSvc != null ? baseSvc.BaseTerritoryRadius : 30f;
                }

                float dist = anchorRadius * ColonizationProfile.SiteDistanceFactor;
                float baseAngle = f * 1.7f + (count - 1) * 1.3f;
                bool ok = false;
                for (int attempt = 0; attempt < 8 && !ok; attempt++)
                {
                    float a = baseAngle + attempt * (TwoPi / 8f);
                    var pos = new Vector3(
                        anchor.x + (float)Math.Cos(a) * dist,
                        0f,
                        anchor.z + (float)Math.Sin(a) * dist);

                    var kind = ChooseAutoKind(f);
                    if (CanFound(f, kind, pos, out _))
                    {
                        string nm = $"{info.Name}第{count + 1}聚落";
                        ok = TryFoundSettlement(f, kind, pos, nm);
                    }
                }
                if (ok) founded++;
            }
            return founded;
        }

        /// <summary>自动扩张选择聚落类型（太空时代星球聚落由玩家手动建立）。</summary>
        private SettlementKind ChooseAutoKind(int faction)
        {
            var tech = Resolve<ITechService>();
            if (tech != null)
            {
                int era = (int)tech.GetEra(faction);
                if (era >= 5) return SettlementKind.Island;   // 大航海时代
                if (era >= 3) return SettlementKind.Coastal;  // 中世纪
            }
            return SettlementKind.Village;
        }

        // ------------------------------------------------------------
        // 领土查询
        // ------------------------------------------------------------
        public int GetOwnerAt(Vector3 position)
        {
            int owner = -1;
            float best = float.MaxValue;
            int cap = _settlements.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_settlements.IsAlive(slot, _settlements.GetGeneration(slot))) continue;
                ref var r = ref _settlements.GetSlotRef(slot);
                float dx = r.Position.x - position.x;
                float dz = r.Position.z - position.z;
                float d = dx * dx + dz * dz;
                float radius = TerritoryRadius(slot);
                if (d <= radius * radius && d < best)
                {
                    best = d;
                    owner = r.Faction;
                }
            }
            return owner;
        }

        public int SettlementCount(int faction) => _settlements.CountByFaction(faction);

        // ------------------------------------------------------------
        // 辅助
        // ------------------------------------------------------------
        private int FindNearestSettlement(Vector3 position, out float nearestDist)
        {
            int best = -1;
            nearestDist = float.MaxValue;
            int cap = _settlements.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_settlements.IsAlive(slot, _settlements.GetGeneration(slot))) continue;
                ref var r = ref _settlements.GetSlotRef(slot);
                float dx = r.Position.x - position.x;
                float dz = r.Position.z - position.z;
                float d = (float)Math.Sqrt(dx * dx + dz * dz);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    best = slot;
                }
            }
            return best;
        }

        private T Resolve<T>() where T : class
        {
            if (_services == null) return null;
            return _services.TryResolve<T>(out T t) ? t : null;
        }

        /// <summary>
        /// 聚落领土半径（走 ISettlementService，避免跨插件直接读 SettlementProfile）。
        /// settlement 缺失的极端情况兜底为基础半径 30。
        /// </summary>
        private float TerritoryRadius(int slot)
        {
            var settlement = Resolve<ISettlementService>();
            if (settlement != null) return settlement.GetTerritoryRadius(slot);
            return 30f;
        }

        private int ResolveGameYear()
        {
            var clock = Resolve<IClock>();
            return clock != null ? clock.GameYear : 0;
        }

        private string ResolveFactionName(int faction)
        {
            var factions = Resolve<FactionRegistry>();
            var info = factions != null ? factions.Get(faction) : null;
            return info != null ? info.Name : "未知";
        }
    }
}
