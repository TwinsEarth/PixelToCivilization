using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;

namespace PxC.Content.Towers
{
    /// <summary>
    /// 塔防视觉内容包（Tier-2）。提供五型塔多层收分视觉数据，注册 ITowerVisualService。
    /// 纯静态数据，无需存档。
    /// </summary>
    public sealed class TowersPack : IContentPlugin, ITowerVisualService
    {
        public const string PluginIdValue = "pxc.content.towers";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(0);
        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);
        private static readonly IReadOnlyList<string> _groups =
            new List<string>(1) { "Content.Towers" };
        private static readonly IReadOnlyList<string> _addresses =
            new List<string>(0);

        private IPluginContext _ctx;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "塔防视觉内容包";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Content2;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;
        public IReadOnlyList<string> DefinitionAddresses => _addresses;
        public IReadOnlyList<string> ContentGroups => _groups;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            _ctx.Services.Register<ITowerVisualService>(this);
            _initialized = true;
        }

        public void OnContentLoaded()
        {
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<ITowerVisualService>();
            _initialized = false;
        }

        public TowerVisualProfile ForType(int typeId) => TowerVisualContent.ForType(typeId);

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
