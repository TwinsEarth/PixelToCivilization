using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Culture
{
    /// <summary>
    /// 文化插件（Tier-1）。依据累积文化值刷新文化等级，提供影响力与文化胜利进度，
    /// 注册 ICultureService。OnYear 在 Economy 之后（读取当年产出的文化值）。
    /// </summary>
    public sealed class CulturePlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.culture";

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
        private CultureState _state;
        private CultureService _service;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "文化系统";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;
        public SimulationLayer Layer => SimulationLayer.P2Ambient;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            int factionCount = ResolveFactionCount();
            _state = new CultureState();
            _state.Bind(factionCount);
            _service = new CultureService(_state, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<ICultureService>(_service);
            _initialized = true;
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<ICultureService>();
            _initialized = false;
        }

        public void OnTick(float simDt)
        {
        }

        public void OnYear(int gameYear)
        {
            if (!_initialized) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            for (int f = 0; f < factions.Count; f++)
            {
                if (factions.Get(f).Alive) _service.YearlyUpdate(f);
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
            int n = _state.FactionCount;
            var levels = new int[n];
            for (int i = 0; i < n; i++) levels[i] = _state.GetLevel(i);
            return new CultureSaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                Levels = levels,
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is CultureSaveSegment s)) return;
            int factionCount = ResolveFactionCount();
            _state = new CultureState();
            _state.Restore(s.Levels, factionCount);
            _service = new CultureService(_state, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<ICultureService>(_service);
            _initialized = true;
        }

        private int ResolveFactionCount()
        {
            if (_ctx.Services.TryResolve<FactionRegistry>(out var factions))
                return factions.Count;
            return 1;
        }
    }
}
