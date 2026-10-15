using System.Collections.Generic;

namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 一条朝代记录（DynastiesPack 内容数据）。
    /// 公历年份：负数=公元前，正数=公元后。
    /// </summary>
    public struct DynastyInfo
    {
        /// <summary>朝代名称（如"唐"）。</summary>
        public string Name;

        /// <summary>起始公历年（前2070=-2070）。</summary>
        public int StartAd;

        /// <summary>结束公历年。</summary>
        public int EndAd;

        /// <summary>关联时代（Era）。</summary>
        public int Era;

        /// <summary>特征简述（如"盛世开放"）。</summary>
        public string Note;
    }

    /// <summary>
    /// 朝代内容服务（dynasties 内容包实现，Tier-2）。
    /// 提供朝代序列数据与按年份查询。游戏年与公历换算：
    /// 公元 0 = 游戏年 3000（GameYear - 3000 = 公历年）。
    /// </summary>
    public interface IDynastyService
    {
        /// <summary>朝代序列（按时间正序，只读）。</summary>
        IReadOnlyList<DynastyInfo> Dynasties { get; }

        /// <summary>游戏年 → 公历。</summary>
        int ToAdYear(int gameYear);

        /// <summary>公历 → 游戏年。</summary>
        int ToGameYear(int adYear);

        /// <summary>按公历取朝代索引（无匹配 -1）。</summary>
        int IndexForAdYear(int adYear);

        /// <summary>按游戏年取朝代（无匹配 default）。</summary>
        DynastyInfo ForGameYear(int gameYear);
    }
}
