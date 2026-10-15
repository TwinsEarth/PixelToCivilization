using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Population
{
    /// <summary>
    /// population 插件存档段（v10.4.0，SegmentVersion=2）。
    /// 序列化连续人口分层数组（settlement × layer，布局 [slot*LayerCount + layer]）
    /// 及槽位容量。与运行时同布局，读档直接恢复。
    /// </summary>
    [Serializable]
    public sealed class PopulationSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.population"）。</summary>
        public string PluginId;

        /// <summary>段版本（v10.4.0 起为 2：聚落 × 分层）。</summary>
        public int SegmentVersion = 2;

        /// <summary>聚落槽位容量。</summary>
        public int SlotCapacity;

        /// <summary>连续人口分层数组 [slot*LayerCount + layer]。</summary>
        public int[] Layers;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
