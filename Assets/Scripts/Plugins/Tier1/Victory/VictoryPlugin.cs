using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Victory
{
    /// <summary>
    /// 胜利插件（Tier-1）。每年（在所有结算之后）检测征服/科技/文化/发展四种胜利，
    /// 注册 IVictoryService，发布 VictoryEvent。
    /// </summary>
    public sealed class VictoryPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.victory";

        // 依赖全部结算类插件，确保 OnYear 拓扑中最后执行（读到当年最终状态）。
        // warresolution：灭国先结算，胜利随后检测（当年灭国→当年触发征服胜利）。
        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(6)
            {
                new PluginDependency("pxc.economy"),
                new PluginDependency("pxc.population"),
                new PluginDependency("pxc.building"),
                new PluginDependency("pxc.tech"),
                new PluginDependency("pxc.culture"),
                new PluginDependency("pxc.warresolution"),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.Save),
            };

        private IPluginContext _ctx;
        private VictoryState _state;
        private VictoryService _service;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "胜利目标";
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
            _state = new VictoryState();
            _service = new VictoryService(_state, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<IVictoryService>(_service);
            _initialized = true;
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<IVictoryService>();
            _initialized = false;
        }

        public void OnTick(float simDt)
        {
        }

        public void OnYear(int gameYear)
        {
            if (!_initialized) return;
            _service.Check(gameYear);
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
            return new VictorySaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                AchievedType = _state.AchievedType,
                AchievedYear = _state.AchievedYear,
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is VictorySaveSegment s)) return;
            _state = new VictoryState();
            _state.Restore(s.AchievedType, s.AchievedYear);
            _service = new VictoryService(_state, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<IVictoryService>(_service);
            _initialized = true;
        }
    }
}
