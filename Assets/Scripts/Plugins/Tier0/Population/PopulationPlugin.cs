using System;
using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Population
{
    /// <summary>
    /// 人口核心插件（Tier-0，v10.4.0 治本统一）。人口唯一存储为聚落 × 分层：
    /// 插件 OnInitialize 时聚落尚未建立，故不凭空 seed；首都人口由 settlement 插件
    /// FoundInitialCapitals 调用 SeedCapital 写入。年度 OnYear 遍历聚落结算出生/成年/
    /// 自然死亡，再按势力聚合发布；订阅 FamineEvent 结算饥荒死亡。
    /// </summary>
    public sealed class PopulationPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.population";

        private const float FoodPerCapita = 1.0f; // 每人年口粮（饥荒折算）

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
            var settlements = _ctx.Services.Resolve<SettlementRegistry>();

            _state = new PopulationState();
            _famineDeaths = new int[factions.Count];
            // 人口在聚落建立后由 SeedCapital 写入；此时 state 为空。
            _service = new PopulationService(_state, settlements, _ctx.Services);
            _ctx.Services.Register<IPopulationService>(_service);
            _ctx.Events.Subscribe<FamineEvent>(OnFamine);
            _initialized = true;
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
            int factionCount = factions.Count;

            // 聚落粒度结算（births/deaths 按势力累计）
            var births = new int[factionCount];
            var deaths = new int[factionCount];
            _service.SettlePopulationYear(factionCount, births, deaths);

            // 按势力发布（含本年度饥荒死亡）
            for (int f = 0; f < factionCount; f++)
            {
                if (!factions.Get(f).Alive) continue;

                int famine = _famineDeaths[f];
                _famineDeaths[f] = 0;

                _ctx.Events.Publish(new PopulationChangedEvent
                {
                    Faction = f,
                    Births = births[f],
                    Deaths = deaths[f] + famine,
                    NewTotal = _service.Total(f),
                });
            }
        }

        /// <summary>饥荒事件（economy 年结时立即发布）：按粮食缺口折算死亡并扣减。</summary>
        private void OnFamine(FamineEvent evt)
        {
            if (!_initialized) return;
            int deaths = (int)Math.Ceiling(evt.Shortfall / FoodPerCapita);
            int removed = _service.ApplyFamineDeaths(evt.Faction, deaths);
            if (evt.Faction >= 0 && evt.Faction < _famineDeaths.Length)
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
                SegmentVersion = 2,
                SlotCapacity = _state.SlotCapacity,
                Layers = _state.RawArray,
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is PopulationSaveSegment s)) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            var settlements = _ctx.Services.Resolve<SettlementRegistry>();

            _state = new PopulationState();
            _state.Restore(s.Layers, s.SlotCapacity);
            _famineDeaths = new int[factions.Count];
            _service = new PopulationService(_state, settlements, _ctx.Services);
            _ctx.Services.Register<IPopulationService>(_service);
            _ctx.Events.Subscribe<FamineEvent>(OnFamine);
            _initialized = true;
        }
    }
}
