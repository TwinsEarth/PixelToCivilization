using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Plugins.Naval;

namespace PxC.Plugins.Economy
{
    /// <summary>
    /// 经济核心插件（Tier-0）。维护 15 资源的连续 SoA 与年度收支结算，
    /// 注册 IEconomyService 作为全游戏唯一扣费口径。
    ///
    /// 年度结算分两阶段（v10.4.2 治本）：
    /// 1) OnYear（在 building 之前）：结算劳力基础产出 + 基础采集 + 人头税 + 学者产出，
    ///    并按年初人口缓存本年度粮食需求；此时不判定饥荒。
    /// 2) FinalizeYear（所有 sim OnYear 之后，含建筑产出到账）：从库存（劳力 + 农田等
    ///    建筑产出）统一扣除粮食需求，缺口发布 FamineEvent。
    /// 修复旧版在建筑产出到账前判定饥荒、导致有农田也饿死的时序问题。
    /// </summary>
    public sealed class EconomyPlugin : ISimPlugin, IEconomyYearFinalizer
    {
        public const string PluginIdValue = "pxc.economy";

        // ---- 年度经济口径（资源单位/年）----
        private const float FoodPerLabor = 2.5f;       // 每名劳力年产粮
        private const float FoodPerCapita = 1.0f;      // 每人年基础口粮
        private const float FoodPerSoldier = 0.6f;     // 每名士兵额外年口粮
        private const float WoodPerCapita = 0.06f;     // 每人年基础伐木
        private const float StonePerCapita = 0.03f;    // 每人年基础采石
        private const float ResearchPerScholar = 2.0f; // 每名学者年研究
        private const float CulturePerScholar = 1.6f;  // 每名学者年文化
        private const float GoldTaxPerCapita = 0.03f;  // 每人年基础人头税（金）

        // 船艇经济产出（资源单位/年；Naval 为可选服务，缺失时按 0 计）
        private const float FoodPerFishingBoat = 8f;   // 每条渔船年产鱼
        private const float FoodPerRaft = 2f;          // 每条木筏年产少量食物
        private const float GoldPerSailingShip = 5f;   // 每艘帆船贸易年产金

        // 新游戏初始库存
        private const float InitialFood = 100f;
        private const float InitialGold = 50f;
        private const float InitialWood = 80f;
        private const float InitialStone = 40f;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(0);

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.Save),
            };

        private IPluginContext _ctx;
        private EconomyState _state;
        private EconomyService _service;
        private float[] _pendingFoodDemand;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "经济系统";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;
        public SimulationLayer Layer => SimulationLayer.P0Core;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            _state = new EconomyState();
            _state.Bind(factions.Count);
            _pendingFoodDemand = new float[factions.Count];
            SeedInitialResources(factions);
            _service = new EconomyService(_state);
            _ctx.Services.Register<IEconomyService>(_service);
            _ctx.Services.Register<IEconomyYearFinalizer>(this);
            _initialized = true;
        }

        private void SeedInitialResources(FactionRegistry factions)
        {
            for (int f = 0; f < factions.Count; f++)
            {
                _state.Set(f, ResourceType.Food, InitialFood);
                _state.Set(f, ResourceType.Gold, InitialGold);
                _state.Set(f, ResourceType.Wood, InitialWood);
                _state.Set(f, ResourceType.Stone, InitialStone);
            }
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<IEconomyYearFinalizer>();
            _ctx.Services.Unregister<IEconomyService>();
            _initialized = false;
        }

        public void OnTick(float simDt)
        {
            // 经济按年结算，不在每帧累积（避免旧版每秒扣粮的口径漂移）。
        }

        // 阶段 1：产出（在 building.OnYear 之前；粮食消耗/饥荒延后到 FinalizeYear）。
        public void OnYear(int gameYear)
        {
            if (!_initialized) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            if (!_ctx.Services.TryResolve<IPopulationService>(out var pop))
                return; // population 尚未就绪，本年度跳过（初始化顺序由依赖保证）

            for (int f = 0; f < factions.Count; f++)
            {
                if (!factions.Get(f).Alive)
                {
                    _pendingFoodDemand[f] = 0f;
                    continue;
                }

                int total = pop.Total(f);
                int labor = pop.ByLayer(f, PopulationLayer.Laborer);
                int soldiers = pop.ByLayer(f, PopulationLayer.Soldier);
                int scholars = pop.ByLayer(f, PopulationLayer.Scholar);

                // 粮食产出（劳力）；天气为可选 Tier-1 服务，缺失时系数=1。
                float cropMod = 1f;
                if (_ctx.Services.TryResolve<IWeatherService>(out var weather))
                    cropMod = weather.CropYieldModifier(f);
                float foodProduced = labor * FoodPerLabor * cropMod;
                // 政策为可选 Tier-1 服务，缺失时系数=1。
                if (_ctx.Services.TryResolve<IPolicyService>(out var policy))
                    foodProduced *= policy.FoodModifier(f);
                _state.Add(f, ResourceType.Food, foodProduced);

                // 缓存本年粮食需求（按年初人口，与产出同口径）；最终在 FinalizeYear 扣减。
                _pendingFoodDemand[f] = total * FoodPerCapita + soldiers * FoodPerSoldier;

                // 基础采集与学者产出（政策系数）
                _state.Add(f, ResourceType.Wood, total * WoodPerCapita);
                _state.Add(f, ResourceType.Stone, total * StonePerCapita);
                // 基础人头税（金）：金币闭环不依赖市集，避免沿海聚落耗尽金币后殖民永久卡死。
                _state.Add(f, ResourceType.Gold, total * GoldTaxPerCapita);
                float researchOut = scholars * ResearchPerScholar;
                float cultureOut = scholars * CulturePerScholar;
                if (policy != null)
                {
                    researchOut *= policy.ResearchModifier(f);
                    cultureOut *= policy.CultureModifier(f);
                }
                _state.Add(f, ResourceType.Research, researchOut);
                _state.Add(f, ResourceType.Culture, cultureOut);
            }
        }

        // 阶段 2：所有 sim OnYear（含建筑产出）完成后，统一扣粮食消耗、判定饥荒。
        public void FinalizeYear(int gameYear)
        {
            if (!_initialized) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            // Naval 为可选服务（不声明依赖，避免拓扑环）；有则计入船艇经济产出。
            _ctx.Services.TryResolve<NavalService>(out var naval);

            for (int f = 0; f < factions.Count; f++)
            {
                if (factions.Get(f).Alive && naval != null)
                {
                    // 渔船/木筏产食物，帆船贸易产金币（在扣口粮前到账，可养活人口）。
                    float fishFood =
                        naval.CountByModel(f, NavalModel.FishingBoat) * FoodPerFishingBoat +
                        naval.CountByModel(f, NavalModel.Raft) * FoodPerRaft;
                    if (fishFood > 0f)
                        _state.Add(f, ResourceType.Food, fishFood);

                    float tradeGold =
                        naval.CountByModel(f, NavalModel.SailingShip) * GoldPerSailingShip;
                    if (tradeGold > 0f)
                        _state.Add(f, ResourceType.Gold, tradeGold);
                }

                float demand = _pendingFoodDemand[f];
                _pendingFoodDemand[f] = 0f;

                if (!factions.Get(f).Alive || demand <= 0f) continue;

                float foodStock = _state.Get(f, ResourceType.Food);
                float newFood = foodStock - demand;
                if (newFood < 0f)
                {
                    _state.Set(f, ResourceType.Food, 0f);
                    _ctx.Events.Publish(new FamineEvent
                    {
                        Faction = f,
                        Shortfall = -newFood,
                        GameYear = gameYear,
                    });
                }
                else
                {
                    _state.Set(f, ResourceType.Food, newFood);
                }
            }
        }

        public void OnEra(int newEra, int oldEra)
        {
        }

        public void OnDynasty(int dynastyIndex, int gameYear)
        {
        }

        public ISaveSegment GetSaveSegment()
        {
            if (_state == null) return null;
            return new EconomySaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                FactionCount = _state.FactionCount,
                Resources = _state.RawArray,
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is EconomySaveSegment s)) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            _state = new EconomyState();
            _state.Restore(s.Resources, factions.Count);
            _pendingFoodDemand = new float[factions.Count];
            _service = new EconomyService(_state);
            _ctx.Services.Register<IEconomyService>(_service);
            _ctx.Services.Register<IEconomyYearFinalizer>(this);
            _initialized = true;
        }
    }
}
