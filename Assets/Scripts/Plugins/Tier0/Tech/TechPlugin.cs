using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Tech
{
    /// <summary>
    /// 科技插件（Tier-0）。创建并注册 ITechService，年度消费 Research 推进时代，
    /// 时代切换发布 EraChangedEvent。依赖 economy。
    /// </summary>
    public sealed class TechPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.tech";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(1)
            {
                new PluginDependency("pxc.economy", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;
        private TechState _state;
        private TechService _service;

        public string Id => PluginIdValue;
        public string DisplayName => "科技系统";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;
        public SimulationLayer Layer => SimulationLayer.P0Core;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize()
        {
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            _state = new TechState();
            _state.Bind(factions.Count);
            _service = new TechService(_state);
            _ctx.Services.Register<ITechService>(_service);
        }

        public void OnTick(float simDt)
        {
            // 科技按年结算（Research 由 economy OnYear 产出，本插件随后消费）。
        }

        public void OnYear(int gameYear)
        {
            if (!_ctx.Services.TryResolve<IEconomyService>(out var economy)) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            for (int f = 0; f < factions.Count; f++)
            {
                if (!factions.Get(f).Alive) continue;
                float research = economy.Get(f, ResourceType.Research);
                if (research <= 0f) continue;

                int oldEra = _state.GetEra(f);
                economy.TrySpend(f, ResourceType.Research, research);
                int levels = _service.AdvanceResearch(f, research);

                if (levels > 0)
                {
                    int newEra = _state.GetEra(f);
                    _ctx.Events.Publish(new EraChangedEvent
                    {
                        NewEra = newEra,
                        OldEra = oldEra,
                        GameYear = gameYear,
                    });
                }
            }
        }

        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }

        public void OnShutdown()
        {
            _ctx.Services.Unregister<ITechService>();
        }

        public ISaveSegment GetSaveSegment()
        {
            return new TechSaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                FactionCount = _state.FactionCount,
                Era = _state.EraArray,
                Progress = _state.ProgressArray,
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is TechSaveSegment s)) return;
            _state.Restore(s.Era, s.Progress, s.FactionCount);
            _service = new TechService(_state);
            _ctx.Services.Register<ITechService>(_service);
        }
    }
}
