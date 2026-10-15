using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Colonization
{
    /// <summary>
    /// 殖民/扩张插件（Tier-1）。年度驱动各势力自动扩张建立新聚落，
    /// 注册 IColonizationService（手动殖民/领土查询）。
    /// 本身无独立状态（聚落数据由 settlement 插件持有），故无存档段。
    /// 依赖 economy/population/settlement，保证扩张在年度结算与聚落发展之后。
    /// </summary>
    public sealed class ColonizationPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.colonization";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(3)
            {
                new PluginDependency("pxc.economy"),
                new PluginDependency("pxc.population"),
                new PluginDependency("pxc.settlement"),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;
        private ColonizationService _service;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "殖民扩张";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;
        public SimulationLayer Layer => SimulationLayer.P1Tactical;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            var reg = _ctx.Services.Resolve<SettlementRegistry>();
            _service = new ColonizationService(reg, _ctx.Services, _ctx.Events, _ctx.Log);
            _ctx.Services.Register<IColonizationService>(_service);
            _initialized = true;
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<IColonizationService>();
            _initialized = false;
        }

        public void OnTick(float simDt)
        {
        }

        public void OnYear(int gameYear)
        {
            if (!_initialized) return;
            int founded = _service.AutoExpand(gameYear);
            if (founded > 0)
                _ctx.Log($"[殖民] {gameYear} 年各势力共新建 {founded} 个聚落");
        }

        public void OnEra(int newEra, int oldEra)
        {
        }

        public void OnDynasty(int dynastyIndex, int gameYear)
        {
        }

        public ISaveSegment GetSaveSegment()
        {
            // 殖民插件无独立状态：聚落数据由 settlement 存档段保存。
            return null;
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            // 无状态需恢复。
        }
    }
}
