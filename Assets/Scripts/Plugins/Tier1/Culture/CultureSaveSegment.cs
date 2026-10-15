using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Culture
{
    /// <summary>文化插件存档段。</summary>
    [Serializable]
    public sealed class CultureSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.culture"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        /// <summary>每势力文化等级。</summary>
        public int[] Levels;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
