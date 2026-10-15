using System;
using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Population
{
    /// <summary>
    /// 人口核心插件（Tier-0）。维护人口分层连续 SoA、出生/死亡/周期律与职业分配，
    /// 注册 IPopulationService；订阅 FamineEvent 结算饥荒死亡。
    /// </summary>
    public sealed class PopulationPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.population";

        // ---- 年度人口口径 ----
        private const float BirthRate = 0.035f;        // 自然出生率
        private const float NaturalDeathRate = 0.015f; // 自然死亡率
        private const float MatureRatio = 0.12f;       // 每年儿童成年比例
        private const float FoodPerCapita = 1.0f;      // 每人年口粮（饥荒折算）

        // 新游戏初始人口（合计 100）
        private static readonly int[] InitialLayers = { 20, 55, 8, 8, 4, 5 };

        // 饥荒死亡顺序：老人 → 儿童 → 士兵 → 工匠 → 学者 → 劳力
        private static readonly PopulationLayer[] FamineOrder =
        {
            PopulationLayer.Elder,
            PopulationLayer.Children,
            PopulationLayer.Soldier,
            PopulationLayer.Artisan,
            PopulationLayer.Scholar,
            PopulationLayer.Laborer,
        };

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(1)
            {
                new PluginDependency("pxc.economy"),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.Save),
            };

        private IPluginContext _ctx;
        private PopulationState _state;
        private PopulationService _service;
        private int[] _famineDeaths;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "人口系统";
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
            _state = new PopulationState();
            _state.Bind(factions.Count);
            _famineDeaths = new int[factions.Count];
            SeedInitialPopulation(factions);
            _service = new PopulationService(_state);
            _ctx.Services.Register<IPopulationService>(_service);
            _ctx.Events.Subscribe<FamineEvent>(OnFamine);
            _initialized = true;
        }

        private void SeedInitialPopulation(FactionRegistry factions)
        {
            for (int f = 0; f < factions.Count; f++)
            {
                for (int layer = 0; layer < PopulationLayers.Count; layer++)
                {
                    _state.Set(f, (PopulationLayer)layer, InitialLayers[layer]);
                }
            }
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Events.Unsubscribe<FamineEvent>(OnFamine);
            _ctx.Services.Unregister<IPopulationService>();
            _initialized = false;
        }

        public void OnTick(float simDt)
        {
            // 人口按年结算。
        }

        public void OnYear(int gameYear)
        {
            if (!_initialized) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            for (int f = 0; f < factions.Count; f++)
            {
                if (!factions.Get(f).Alive) continue;

                int total = _state.Total(f);
                int children = _state.Get(f, PopulationLayer.Children);
                int labor = _state.Get(f, PopulationLayer.Laborer);
                int elders = _state.Get(f, PopulationLayer.Elder);

                // 政策为可选 Tier-1 服务：缺失时系数=1。
                float birthMod = 1f;
                float deathMod = 1f;
                if (_ctx.Services.TryResolve<IPolicyService>(out var pol))
                {
                    birthMod = pol.BirthModifier(f);
                    deathMod = pol.DeathModifier(f);
                }

                int births = (int)Math.Round(total * BirthRate * birthMod);
                int mature = (int)Math.Round(children * MatureRatio);
                int naturalDeaths = (int)Math.Round(total * NaturalDeathRate * deathMod);

                // 出生
                _state.Add(f, PopulationLayer.Children, births);

                // 成年：儿童 → 劳力
                mature = Math.Min(mature, _state.Get(f, PopulationLayer.Children));
                _state.Add(f, PopulationLayer.Children, -mature);
                _state.Add(f, PopulationLayer.Laborer, mature);

                // 自然死亡：优先老人，不足再从儿童
                int deadFromElders = Math.Min(naturalDeaths, elders);
                _state.Add(f, PopulationLayer.Elder, -deadFromElders);
                int remaining = naturalDeaths - deadFromElders;
                if (remaining > 0)
                    _state.RemoveInOrder(f, remaining,
                        new[] { PopulationLayer.Children, PopulationLayer.Laborer });

                int famine = _famineDeaths[f];
                _famineDeaths[f] = 0;

                _ctx.Events.Publish(new PopulationChangedEvent
                {
                    Faction = f,
                    Births = births,
                    Deaths = naturalDeaths + famine,
                    NewTotal = _state.Total(f),
                });
            }
        }

        /// <summary>饥荒事件（economy 年结时立即发布）：按粮食缺口折算死亡并扣减。</summary>
        private void OnFamine(FamineEvent evt)
        {
            if (!_initialized) return;
            int deaths = (int)Math.Ceiling(evt.Shortfall / FoodPerCapita);
            int removed = _state.RemoveInOrder(evt.Faction, deaths, FamineOrder);
            if (evt.Faction < _famineDeaths.Length)
                _famineDeaths[evt.Faction] += removed;
            _ctx.Log($"[饥荒] 势力 {evt.Faction} 粮食缺口 {evt.Shortfall:0.0}，饿死 {removed} 人");
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
            return new PopulationSaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                FactionCount = _state.FactionCount,
                Layers = _state.RawArray,
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is PopulationSaveSegment s)) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            _state = new PopulationState();
            _state.Restore(s.Layers, factions.Count);
            _famineDeaths = new int[factions.Count];
            _service = new PopulationService(_state);
            _ctx.Services.Register<IPopulationService>(_service);
            _ctx.Events.Subscribe<FamineEvent>(OnFamine);
            _initialized = true;
        }
    }
}
