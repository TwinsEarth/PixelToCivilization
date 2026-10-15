using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Disaster
{
    /// <summary>灾害插件存档段。</summary>
    [Serializable]
    public sealed class DisasterSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.disaster"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        public int Active;
        public float Severity;
        public int ActiveYear;
        public int DryYears;
        public ulong RngState;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
