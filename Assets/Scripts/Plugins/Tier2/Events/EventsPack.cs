using System;
using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Services;

namespace PxC.Content.Events
{
    /// <summary>
    /// 重大历史事件内容包（Tier-2）。订阅年份推进，按公历年份匹配历史事件，
    /// 发布 HistoricalEventEvent（并写入历史）。纯静态数据，无需存档。
    /// </summary>
    public sealed class EventsPack : IContentPlugin
    {
        public const string PluginIdValue = "pxc.content.events";

        public const int GameYearAtAdZero = 3000;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(0);
        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);
        private static readonly IReadOnlyList<string> _groups =
            new List<string>(1) { "Content.Events" };
        private static readonly IReadOnlyList<string> _addresses =
            new List<string>(0);

        private IPluginContext _ctx;
        private Action<YearAdvancedEvent> _onYear;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "重大历史事件包";
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
            _onYear = OnYearAdvanced;
            _ctx.Events.Subscribe(_onYear);
            _initialized = true;
        }

        public void OnContentLoaded()
        {
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Events.Unsubscribe(_onYear);
            _initialized = false;
        }

        private void OnYearAdvanced(YearAdvancedEvent e)
        {
            int ad = e.GameYear - GameYearAtAdZero;
            for (int i = 0; i < HistoricalEventContent.Sequence.Length; i++)
            {
                var info = HistoricalEventContent.Sequence[i];
                if (info.AdYear != ad) continue;

                _ctx.Events.Publish(new HistoricalEventEvent
                {
                    GameYear = e.GameYear,
                    AdYear = ad,
                    Kind = info.Kind,
                    EventName = info.Name,
                    Description = info.Description,
                });
                _ctx.Log($"[史事] {info.Name}（公元 {ad}）：{info.Description}");
            }
        }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
