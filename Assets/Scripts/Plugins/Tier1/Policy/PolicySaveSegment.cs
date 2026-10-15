using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Policy
{
    /// <summary>政策插件存档段。</summary>
    [Serializable]
    public sealed class PolicySaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.policy"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        /// <summary>每势力当前国策（int）。</summary>
        public int[] Current;

        /// <summary>每势力上次切换年份。</summary>
        public int[] LastSwitch;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
