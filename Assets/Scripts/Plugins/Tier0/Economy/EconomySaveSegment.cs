using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Economy
{
    /// <summary>
    /// economy 插件存档段。直接序列化连续资源数组（与运行时同一布局，零拷贝恢复）。
    /// </summary>
    [Serializable]
    public sealed class EconomySaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.economy"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1；独立迁移用）。</summary>
        public int SegmentVersion = 1;

        /// <summary>势力数量。</summary>
        public int FactionCount;

        /// <summary>连续资源数组 [faction*ResourceCount + type]。</summary>
        public float[] Resources;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
