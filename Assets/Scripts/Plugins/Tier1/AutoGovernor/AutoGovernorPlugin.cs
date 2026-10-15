using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;
using PxC.Plugins.Building;
using UnityEngine;

namespace PxC.Plugins.AutoGovernor
{
    /// <summary>
    /// 自动发展 / 自动建造战略 AI（Tier-1）。年度对每个存活势力按"发展阶段"主动建设：
    /// 粮食安全 → 基建覆盖（伐木/采石/兵营/市集/工坊，按时代解锁）→ 储备安全 → 工业扩展
    /// → 持续发展（农田随人口）。每势力每年最多建成 1 栋；候选按优先级排序，
    /// 选第一个通过 <see cref="IBuildingService.CanBuild"/> 的类型，避免"想建但建不了就空过一年"。
    /// 玩家势力默认跳过（保留玩家控制权）；可通过 <see cref="EnablePlayerAutomation"/> 开启。
    /// 纯决策、无运行时状态（每年根据当前世界状态决策），不产生存档段。
    /// 依赖 building（建造）、economy（资源）、population（人口）、tech（时代）。
    /// </summary>
    public sealed class AutoGovernorPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.autogovernor";

        // ---- 决策阈值（平衡：每年每势力 1 栋，资源/时代约束自然限速）----
        /// <summary>每势力每年最多自动建造数。</summary>
        public const int AutoBuildsPerYear = 1;

        /// <summary>粮食安全：库存低于该年数 × 总人口（口粮）时优先建农田。</summary>
        public const float FoodReserveYears = 1.5f;

        /// <summary>木材安全库存。</summary>
        public const float WoodReserve = 40f;

        /// <summary>石料安全库存。</summary>
        public const float StoneReserve = 30f;

        /// <summary>金币安全库存。</summary>
        public const float GoldReserve = 50f;

        /// <summary>每多少人口需要 1 个农田（农田目标 = 1 + 总人口/该值）。</summary>
        public const int PopulationPerFarm = 150;

        // ---- 首府周边环布位置 ----
        private const float SiteInnerRadius = 8f;
        private const int SiteRingCount = 6;
        private const float SiteRingSpacing = 3f;
        private const float GoldenAngle = 2.399963f; // 黄金角（弧度）

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(4)
            {
                new PluginDependency("pxc.economy", 1),
                new PluginDependency("pxc.population", 1),
                new PluginDependency("pxc.building", 1),
                new PluginDependency("pxc.tech", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        /// <summary>复用的候选类型缓冲（避免每年 GC）。</summary>
        private readonly List<int> _candidates = new List<int>(12);

        /// <summary>是否对玩家势力也自动建造（默认 false=保留玩家控制权）。</summary>
        public bool EnablePlayerAutomation;

        public string Id => PluginIdValue;
        public string DisplayName => "自动发展（自动建造）";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;
        public SimulationLayer Layer => SimulationLayer.P1Tactical;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize()
        {
            // 纯决策插件：无状态、无服务注册。
        }

        public void OnTick(float simDt)
        {
            // 战略决策按年结算（OnYear）。
        }

        public void OnYear(int gameYear)
        {
            if (!_ctx.Services.TryResolve<IBuildingService>(out var buildings)) return;
            if (!_ctx.Services.TryResolve<IEconomyService>(out var economy)) return;
            if (!_ctx.Services.TryResolve<IPopulationService>(out var population)) return;
            if (!_ctx.Services.TryResolve<ITechService>(out var tech)) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            for (int f = 0; f < factions.Count; f++)
            {
                FactionInfo info = factions.Get(f);
                if (!info.Alive) continue;
                if (info.IsPlayer && !EnablePlayerAutomation) continue;

                BuildCandidates(f, buildings, economy, population, tech, _candidates);

                // 候选按优先级排序，选第一个可建造的；每年最多 1 栋。
                for (int i = 0; i < _candidates.Count; i++)
                {
                    int typeId = _candidates[i];
                    if (!buildings.CanBuild(f, typeId)) continue;

                    Vector3 position = NextSitePosition(info, buildings.TotalCount(f));
                    int slot = buildings.TryBuild(f, typeId, position);
                    if (slot >= 0)
                    {
                        _ctx.Log($"[自动发展] 势力 {f}（{info.Name}）自动建造 " +
                                 $"{BuildingName(typeId)}（slot {slot}）");
                        break;
                    }
                }
            }
        }

        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }

        public void OnShutdown()
        {
            // 无资源/服务需要释放。
        }

        // 无存档状态：决策完全由当前世界状态决定，不产生存档段。
        public ISaveSegment GetSaveSegment() => null;

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            // 无存档段。
        }

        // ============================================================
        // 决策：按发展阶段生成候选优先级列表
        // ============================================================
        /// <summary>
        /// 根据当前世界状态生成"想建造的建筑类型"候选（按优先级），并去重。
        /// 时代门控/成本由 <see cref="IBuildingService.CanBuild"/> 统一判定，这里只决定发展顺序。
        /// </summary>
        private static void BuildCandidates(
            int faction,
            IBuildingService buildings,
            IEconomyService economy,
            IPopulationService population,
            ITechService tech,
            List<int> candidates)
        {
            candidates.Clear();

            int total = population.Total(faction);
            int era = (int)tech.GetEra(faction);

            int nFarm = buildings.CountByType(faction, (int)BuildingType.Farm);
            int nLumber = buildings.CountByType(faction, (int)BuildingType.LumberMill);
            int nQuarry = buildings.CountByType(faction, (int)BuildingType.Quarry);
            int nBarracks = buildings.CountByType(faction, (int)BuildingType.Barracks);
            int nMarket = buildings.CountByType(faction, (int)BuildingType.Market);
            int nWorkshop = buildings.CountByType(faction, (int)BuildingType.Workshop);

            // 1. 粮食安全：库存低于 1.5 年口粮 → 农田（当年即可防饥荒）。
            if (economy.Get(faction, ResourceType.Food) < total * FoodReserveYears)
                Add(candidates, (int)BuildingType.Farm);

            // 2. 关键资源短缺：生产资源跌破安全线会卡住发展，优先补对应采集建筑
            //    （优先于"覆盖"，因为短缺是当前最紧急的瓶颈）。
            if (economy.Get(faction, ResourceType.Wood) < WoodReserve)
                Add(candidates, (int)BuildingType.LumberMill);
            if (economy.Get(faction, ResourceType.Stone) < StoneReserve)
                Add(candidates, (int)BuildingType.Quarry);
            if (economy.Get(faction, ResourceType.Gold) < GoldReserve)
                Add(candidates, (int)BuildingType.Market);

            // 3. 基建覆盖：每种关键建筑按时代至少 1 个（形成完整生产链）。
            if (nLumber < 1)
                Add(candidates, (int)BuildingType.LumberMill);
            if (era >= (int)Era.BronzeAge && nQuarry < 1)
                Add(candidates, (int)BuildingType.Quarry);
            if (era >= (int)Era.BronzeAge && nBarracks < 1)
                Add(candidates, (int)BuildingType.Barracks);
            if (era >= (int)Era.IronAge && nMarket < 1)
                Add(candidates, (int)BuildingType.Market);
            if (era >= (int)Era.IronAge && nWorkshop < 1)
                Add(candidates, (int)BuildingType.Workshop);

            // 4. 工业扩展：进入工业时代后建立第二套生产建筑，提升高级资源产出。
            if (era >= (int)Era.Industrial)
            {
                if (nQuarry < 2) Add(candidates, (int)BuildingType.Quarry);
                if (nWorkshop < 2) Add(candidates, (int)BuildingType.Workshop);
                if (nMarket < 2) Add(candidates, (int)BuildingType.Market);
            }

            // 5. 持续发展：农田随人口增长（每 150 人需 1 个农田），保证长期不饥荒。
            int farmTarget = 1 + total / PopulationPerFarm;
            if (nFarm < farmTarget)
                Add(candidates, (int)BuildingType.Farm);
        }

        /// <summary>去重追加候选。</summary>
        private static void Add(List<int> candidates, int typeId)
        {
            if (!candidates.Contains(typeId)) candidates.Add(typeId);
        }

        /// <summary>
        /// 首府周边环布位置：以该势力现有建筑数为索引，按黄金角散射、逐圈外扩，避免重叠。
        /// </summary>
        private static Vector3 NextSitePosition(FactionInfo info, int existingBuildings)
        {
            float angle = existingBuildings * GoldenAngle;
            float radius = SiteInnerRadius +
                (existingBuildings % SiteRingCount) * SiteRingSpacing;
            return new Vector3(
                info.HomeX + Mathf.Cos(angle) * radius,
                0f,
                info.HomeZ + Mathf.Sin(angle) * radius);
        }

        private static string BuildingName(int typeId)
        {
            return BuildingCatalog.Get((BuildingType)typeId).DisplayName;
        }
    }
}
