using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Gameplay.Services;

namespace PxC.Content.Dynasties
{
    /// <summary>
    /// 朝代内容包（Tier-2）。提供中华朝代序列数据，注册 IDynastyService。
    /// 纯数据静态表，无 Addressables 依赖，无需存档。
    /// </summary>
    public sealed class DynastiesPack : IContentPlugin
    {
        public const string PluginIdValue = "pxc.content.dynasties";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(0);

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private static readonly IReadOnlyList<string> _groups =
            new List<string>(1) { "Content.Dynasties" };

        private static readonly IReadOnlyList<string> _addresses =
            new List<string>(0);

        private IPluginContext _ctx;
        private DynastyService _service;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "朝代内容包";
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
            _service = new DynastyService();
            _ctx.Services.Register<IDynastyService>(_service);
            _initialized = true;
        }

        public void OnContentLoaded()
        {
            // 纯静态数据，初始化即完成。
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<IDynastyService>();
            _initialized = false;
        }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
