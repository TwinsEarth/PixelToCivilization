using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Tech
{
    /// <summary>科技插件存档段：每势力当前时代与时代内进度。</summary>
    [Serializable]
    public sealed class TechSaveSegment : ISaveSegment
    {
        public string PluginId;
        public int SegmentVersion = 1;
        public int FactionCount;
        public int[] Era;
        public float[] Progress;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
