using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Population
{
    /// <summary>population 插件存档段：序列化连续人口分层数组（与运行时同布局）。</summary>
    [Serializable]
    public sealed class PopulationSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.population"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        /// <summary>势力数量。</summary>
        public int FactionCount;

        /// <summary>连续人口分层数组 [faction*LayerCount + layer]。</summary>
        public int[] Layers;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
