using System;
using PxC.Core.Persistence;
using PxC.Gameplay.Services;

namespace PxC.Plugins.History
{
    /// <summary>历史插件存档段。</summary>
    [Serializable]
    public sealed class HistorySaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.history"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        /// <summary>历史条目（按时间正序）。</summary>
        public HistoryEntry[] Entries;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
