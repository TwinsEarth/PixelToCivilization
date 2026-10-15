using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Military
{
    /// <summary>
    /// 军事插件存档段：交战关系对（WarPairs 长度 = 2*交战对数，
    /// 相邻两项为交战双方势力 id）及每对战争的开始年份（StartYears，长度 = 对数）。
    /// </summary>
    [Serializable]
    public sealed class MilitarySaveSegment : ISaveSegment
    {
        public string PluginId;
        public int SegmentVersion = 1;
        public int PairCount;
        public int[] WarPairs;

        /// <summary>每对交战的开始游戏年份（与 WarPairs 顺序对应，长度 = 对数）。</summary>
        public int[] StartYears;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
