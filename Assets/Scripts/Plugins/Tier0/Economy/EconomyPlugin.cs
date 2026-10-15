using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Economy
{
    /// <summary>
    /// 经济核心插件（Tier-0）。维护 15 资源的连续 SoA 与年度收支结算，
    /// 注册 IEconomyService 作为全游戏唯一扣费口径；粮食缺口发布 FamineEvent。
    /// 建筑/市场的产出由本插件在 OnYear 按聚合查询统一结算。
    /// </summary>
    public sealed class EconomyPlugin : ISimPlugin
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
            SeedInitialResources(factions);
            _service = new EconomyService(_state);
            _ctx.Services.Register<IEconomyService>(_service);
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
            _ctx.Services.Unregister<IEconomyService>();
            _initialized = false;
        }

        public void OnTick(float simDt)
        {
            // 经济按年结算，不在每帧累积（避免旧版每秒扣粮的口径漂移）。
        }

        public void OnYear(int gameYear)
        {
            if (!_initialized) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            if (!_ctx.Services.TryResolve<IPopulationService>(out var pop))
                return; // population 尚未就绪，本年度跳过（初始化顺序由依赖保证）

            for (int f = 0; f < factions.Count; f++)
            {
                if (!factions.Get(f).Alive) continue;

                int total = pop.Total(f);
                int labor = pop.ByLayer(f, PopulationLayer.Laborer);
                int soldiers = pop.ByLayer(f, PopulationLayer.Soldier);
                int scholars = pop.ByLayer(f, PopulationLayer.Scholar);

                // 粮食：产出 − 消耗；库存不足则钳制为 0 并发布饥荒
                // 天气为可选 Tier-1 服务：缺失时系数=1（不影响无天气插件的口径）。
                float cropMod = 1f;
                if (_ctx.Services.TryResolve<IWeatherService>(out var weather))
                    cropMod = weather.CropYieldModifier(f);
                float foodProduced = labor * FoodPerLabor * cropMod;
                // 政策为可选 Tier-1 服务：缺失时系数=1。
                if (_ctx.Services.TryResolve<IPolicyService>(out var policy))
                    foodProduced *= policy.FoodModifier(f);
                float foodDemanded = total * FoodPerCapita + soldiers * FoodPerSoldier;
                float foodStock = _state.Get(f, ResourceType.Food);
                float newFood = foodStock + foodProduced - foodDemanded;
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

                // 基础采集与学者产出（政策系数）
                _state.Add(f, ResourceType.Wood, total * WoodPerCapita);
                _state.Add(f, ResourceType.Stone, total * StonePerCapita);
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
            _service = new EconomyService(_state);
            _ctx.Services.Register<IEconomyService>(_service);
            _initialized = true;
        }
    }
}
