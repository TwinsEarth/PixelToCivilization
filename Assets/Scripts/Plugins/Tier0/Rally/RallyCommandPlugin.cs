using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;
using UnityEngine;

namespace PxC.Plugins.Rally
{
    /// <summary>
    /// 集结令接线插件（Tier-0）。订阅 <see cref="RallyOrderedEvent"/>，
    /// 按旗帜点是陆地还是水域，分别调用 GroundForce/Naval 的 Rally；
    /// 不创建/销毁任何单位、不瞬时搬运、不分配大数组——连续插旗只更新命令字段，
    /// 从根上消除旧版"连续插旗 5 次崩溃"。
    /// </summary>
    public sealed class RallyCommandPlugin : IPlugin
    {
        public const string PluginIdValue = "pxc.rally";

        /// <summary>集结半径（格）：半径内同势力单位响应。</summary>
        private const float RallyRadius = 60f;

        /// <summary>玩家势力（华夏=0）。</summary>
        private const int PlayerFaction = 0;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(2)
            {
                new PluginDependency("pxc.groundforce", 1),
                new PluginDependency("pxc.naval", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;
        private GroundForceService _ground;
        private NavalService _naval;
        private IGeographyQuery _geo;
        private int _lastSequence;

        public string Id => PluginIdValue;
        public string DisplayName => "集结令";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize()
        {
            _ground = _ctx.Services.Resolve<GroundForceService>();
            _naval = _ctx.Services.Resolve<NavalService>();
            _geo = _ctx.Services.Resolve<IGeographyQuery>();
            if (_geo == null) _geo = new PermissiveGeography();

            _ctx.Events.Subscribe<RallyOrderedEvent>(OnRallyOrdered);
            _ctx.Events.Subscribe<RallyCancelledEvent>(OnRallyCancelled);
        }

        private void OnRallyOrdered(RallyOrderedEvent evt)
        {
            // Sequence 幂等：丢弃旧/重复命令，不累积
            if (evt.Sequence <= _lastSequence) return;
            _lastSequence = evt.Sequence;

            var point = new Vector3(evt.X, 0f, evt.Y);
            int ordered;
            if (_geo.IsWater(evt.X, evt.Y))
            {
                ordered = _naval != null
                    ? _naval.Rally(PlayerFaction, point, RallyRadius, false)
                    : 0;
            }
            else
            {
                ordered = _ground != null
                    ? _ground.Rally(PlayerFaction, point, RallyRadius, false)
                    : 0;
            }
            _ctx.Log($"[集结令] seq={evt.Sequence} 点=({evt.X},{evt.Y}) 响应单位 {ordered}");
        }

        private void OnRallyCancelled(RallyCancelledEvent evt)
        {
            // 旧命令的失效由新 Sequence 自然覆盖；这里只记录，不做对象销毁
            if (_ctx != null)
                _ctx.Log($"[集结令] seq={evt.Sequence} 撤销");
        }

        public void OnShutdown()
        {
            _ctx.Events.Unsubscribe<RallyOrderedEvent>(OnRallyOrdered);
            _ctx.Events.Unsubscribe<RallyCancelledEvent>(OnRallyCancelled);
        }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
