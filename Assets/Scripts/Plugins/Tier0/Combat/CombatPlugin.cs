using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;

namespace PxC.Plugins.Combat
{
    /// <summary>
    /// 战斗插件（Tier-0）：创建并注册 <see cref="CombatService"/>（ICombatService）。
    /// 不直接 spawn，只结算已存在单位之间的攻击。依赖 groundforce / naval。
    /// </summary>
    public sealed class CombatPlugin : IPlugin
    {
        public const string PluginIdValue = "pxc.combat";

        public string Id => PluginIdValue;
        public string DisplayName => "战斗结算";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        private static readonly PluginDependency[] _deps =
        {
            new PluginDependency("pxc.groundforce", 1),
            new PluginDependency("pxc.naval", 1),
        };

        private static readonly CapabilityClaim[] _caps =
            new CapabilityClaim[0];

        private IPluginContext _ctx;

        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize()
        {
            var units = _ctx.Services.Resolve<UnitRegistry>();
            var ground = _ctx.Services.Resolve<GroundForceService>();
            var naval = _ctx.Services.Resolve<NavalService>();
            var events = _ctx.Events;

            var service = new CombatService(units, ground, naval, events, _ctx.Log);
            _ctx.Services.Register<ICombatService>(service);
            _ctx.Log("[战斗] 统一伤害/护甲/经验结算已上线");
        }

        public void OnShutdown() { }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
