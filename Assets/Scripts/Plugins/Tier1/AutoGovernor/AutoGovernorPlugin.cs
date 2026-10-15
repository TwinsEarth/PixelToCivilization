using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Plugins.Building;
using UnityEngine;

namespace PxC.Plugins.AutoGovernor
{
    /// <summary>
    /// 自动发展 / 自动建造战略 AI（Tier-1）。年度对每个存活势力按固定优先级
    /// （粮食安全 → 木材储备 → 石料储备 → 金币储备）自动建造最多 1 栋建筑。
    /// 玩家势力默认跳过（保留玩家控制权）；可通过 <see cref="EnablePlayerAutomation"/> 开启。
    /// 本插件为纯决策、无运行时状态（每年根据当前世界状态决策），不产生存档段。
    /// 依赖 building（调用建造）、economy（读资源）、population（读人口）。
    /// </summary>
    public sealed class AutoGovernorPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.autogovernor";

        // ---- 决策阈值（平衡：避免一帧建满，资源约束自然限速）----
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

        // ---- 首府周边环布位置 ----
        private const float SiteInnerRadius = 8f;
        private const int SiteRingCount = 6;
        private const float SiteRingSpacing = 3f;
        private const float GoldenAngle = 2.399963f; // 黄金角（弧度）

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(3)
            {
                new PluginDependency("pxc.economy", 1),
                new PluginDependency("pxc.population", 1),
                new PluginDependency("pxc.building", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

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
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            for (int f = 0; f < factions.Count; f++)
            {
                FactionInfo info = factions.Get(f);
                if (!info.Alive) continue;
                if (info.IsPlayer && !EnablePlayerAutomation) continue;

                int typeId = ChooseBuilding(f, economy, population);
                if (typeId < 0) continue;
                if (!buildings.CanBuild(f, typeId)) continue;

                Vector3 position = NextSitePosition(info, buildings.TotalCount(f));
                int slot = buildings.TryBuild(f, typeId, position);
                if (slot >= 0)
                    _ctx.Log($"[自动发展] 势力 {f}（{info.Name}）自动建造 " +
                             $"{BuildingName(typeId)}（slot {slot}）");
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
        // 决策
        // ============================================================
        /// <summary>
        /// 按优先级选择要建造的建筑类型；返回 -1 表示当前无需建造。
        /// 建筑的时代门控/成本由 IBuildingService.CanBuild 统一判定，这里只决定"想建什么"。
        /// </summary>
        private static int ChooseBuilding(
            int faction, IEconomyService economy, IPopulationService population)
        {
            int total = population.Total(faction);
            float food = economy.Get(faction, ResourceType.Food);

            // 1. 粮食安全：库存低于 1.5 年口粮 → 农田（配合 v10.4.2 两阶段年结，农田当年即可防饥荒）。
            if (food < total * FoodReserveYears)
                return (int)BuildingType.Farm;

            // 2. 木材储备不足 → 伐木场。
            if (economy.Get(faction, ResourceType.Wood) < WoodReserve)
                return (int)BuildingType.LumberMill;

            // 3. 石料储备不足 → 采石场（青铜时代后方可建，CanBuild 门控）。
            if (economy.Get(faction, ResourceType.Stone) < StoneReserve)
                return (int)BuildingType.Quarry;

            // 4. 金币储备不足 → 市集（铁器时代后方可建，CanBuild 门控）。
            if (economy.Get(faction, ResourceType.Gold) < GoldReserve)
                return (int)BuildingType.Market;

            return -1;
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
