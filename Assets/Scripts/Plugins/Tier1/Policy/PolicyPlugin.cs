using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Policy
{
    /// <summary>
    /// 政策插件（Tier-1）。提供治理系数（粮食/金币/出生/死亡/文化/科研），
    /// 注册 IPolicyService，支持国策切换（带冷却）。
    /// </summary>
    public sealed class PolicyPlugin : IPlugin
    {
        public const string PluginIdValue = "pxc.policy";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(0);

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.Save),
            };

        private IPluginContext _ctx;
        private PolicyState _state;
        private PolicyService _service;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "政策治理";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            int factionCount = ResolveFactionCount();
            _state = new PolicyState();
            _state.Bind(factionCount);
            _service = new PolicyService(_state, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<IPolicyService>(_service);
            _initialized = true;
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<IPolicyService>();
            _initialized = false;
        }

        public ISaveSegment GetSaveSegment()
        {
            if (_state == null) return null;
            int n = _state.FactionCount;
            var current = new int[n];
            var lastSwitch = new int[n];
            for (int i = 0; i < n; i++)
            {
                current[i] = _state.Current(i);
                lastSwitch[i] = _state.LastSwitch(i);
            }
            return new PolicySaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                Current = current,
                LastSwitch = lastSwitch,
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is PolicySaveSegment s)) return;
            int factionCount = ResolveFactionCount();
            _state = new PolicyState();
            _state.Restore(s.Current, s.LastSwitch, factionCount);
            _service = new PolicyService(_state, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<IPolicyService>(_service);
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
