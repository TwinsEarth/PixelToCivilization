using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Victory
{
    /// <summary>胜利插件存档段。</summary>
    [Serializable]
    public sealed class VictorySaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.victory"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        public int AchievedType;
        public int AchievedYear;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
