using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;

namespace PxC.Plugins.Military
{
    /// <summary>
    /// 军事总控插件（Tier-0）。聚合兵力/火力、征兵、战争状态，注册 IMilitaryService。
    /// 订阅 WarDeclaredEvent 维护战争矩阵（记录开始年份）。依赖 groundforce/naval/population。
    /// </summary>
    public sealed class MilitaryPlugin : IPlugin
    {
        public const string PluginIdValue = "pxc.military";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(3)
            {
                new PluginDependency("pxc.groundforce", 1),
                new PluginDependency("pxc.naval", 1),
                new PluginDependency("pxc.population", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;
        private MilitaryService _service;

        public string Id => PluginIdValue;
        public string DisplayName => "军事总控";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize()
        {
            var ground = _ctx.Services.Resolve<GroundForceService>();
            var naval = _ctx.Services.Resolve<NavalService>();
            var pop = _ctx.Services.Resolve<IPopulationService>();

            _service = new MilitaryService(ground, naval, pop);
            _ctx.Services.Register<IMilitaryService>(_service);
            _ctx.Events.Subscribe<WarDeclaredEvent>(OnWarDeclared);
        }

        private void OnWarDeclared(WarDeclaredEvent evt)
        {
            int year = CurrentYear();
            _service?.DeclareWar(evt.AttackerFaction, evt.DefenderFaction, year);
        }

        private int CurrentYear()
        {
            try
            {
                var clock = _ctx.Services.Resolve<IClock>();
                if (clock != null) return clock.GameYear;
            }
            catch { /* 测试/无时钟环境：年份从 1 起 */ }
            return 1;
        }

        public void OnShutdown()
        {
            _ctx.Events.Unsubscribe<WarDeclaredEvent>(OnWarDeclared);
            _ctx.Services.Unregister<IMilitaryService>();
        }

        public ISaveSegment GetSaveSegment()
        {
            int[] years;
            int[] pairs = _service.GetWarPairs(out years);
            return new MilitarySaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                PairCount = pairs.Length / 2,
                WarPairs = pairs,
                StartYears = years,
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is MilitarySaveSegment s)) return;
            _service.RestoreWarPairs(s.WarPairs, s.StartYears);
        }
    }
}
