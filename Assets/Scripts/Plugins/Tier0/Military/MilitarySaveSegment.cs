using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Military
{
    /// <summary>
    /// 军事插件存档段：交战关系对（WarPairs 长度 = 2*交战对数，
    /// 相邻两项为交战双方势力 id）。
    /// </summary>
    [Serializable]
    public sealed class MilitarySaveSegment : ISaveSegment
    {
        public string PluginId;
        public int SegmentVersion = 1;
        public int PairCount;
        public int[] WarPairs;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
