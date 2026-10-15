using System.Collections.Generic;
using PxC.Gameplay.Services;

namespace PxC.Content.Dynasties
{
    /// <summary>
    /// 朝代内容服务：提供朝代序列查询与游戏年/公历换算。
    /// </summary>
    public sealed class DynastyService : IDynastyService
    {
        /// <summary>公历 0 对应的游戏年（GameYear - 3000 = 公历）。</summary>
        public const int GameYearAtAdZero = 3000;

        public IReadOnlyList<DynastyInfo> Dynasties => DynastyContent.Sequence;

        public int ToAdYear(int gameYear) => gameYear - GameYearAtAdZero;

        public int ToGameYear(int adYear) => adYear + GameYearAtAdZero;

        public int IndexForAdYear(int adYear)
        {
            for (int i = 0; i < DynastyContent.Sequence.Length; i++)
            {
                var d = DynastyContent.Sequence[i];
                if (adYear >= d.StartAd && adYear <= d.EndAd) return i;
            }
            return -1;
        }

        public DynastyInfo ForGameYear(int gameYear)
        {
            int ad = ToAdYear(gameYear);
            int index = IndexForAdYear(ad);
            if (index < 0) return default;
            return DynastyContent.Sequence[index];
        }
    }
}
