using System;

namespace PxC.Plugins.Settlement
{
    /// <summary>
    /// 聚落数值配置（权威）。聚落人口年增长、发展度、升级阈值、领土半径。
    /// 所有数值集中于此，便于平衡与复算。
    /// </summary>
    public static class SettlementProfile
    {
        /// <summary>聚落人口基础年增长率（每人每年）。</summary>
        public const float BaseGrowthRate = 0.025f;

        /// <summary>每名居民每年贡献的发展度。</summary>
        public const float ProsperityPerCapita = 0.1f;

        /// <summary>聚落每年基础发展度（行政/积累）。</summary>
        public const float ProsperityBase = 2f;

        /// <summary>升级所需发展度基数（× 当前等级）。</summary>
        public const float UpgradeProsperityBase = 100f;

        /// <summary>聚落最高等级。</summary>
        public const int MaxLevel = 10;

        /// <summary>首都初始规模人口。</summary>
        public const int InitialCapitalPopulation = 50;

        /// <summary>首都初始发展度。</summary>
        public const float InitialCapitalProsperity = 50f;

        /// <summary>聚落基础领土半径（1 级）。</summary>
        public const float BaseTerritoryRadius = 30f;

        /// <summary>每升一级领土半径增量。</summary>
        public const float RadiusPerLevel = 5f;

        /// <summary>领土半径上限。</summary>
        public const float MaxTerritoryRadius = 80f;

        /// <summary>当前等级聚落的领土半径。</summary>
        public static float TerritoryRadius(int level)
        {
            int lv = Math.Max(1, level);
            float r = BaseTerritoryRadius + (lv - 1) * RadiusPerLevel;
            return Math.Min(MaxTerritoryRadius, r);
        }

        /// <summary>从当前等级升到下一级所需的发展度。</summary>
        public static float UpgradeRequirement(int currentLevel) =>
            UpgradeProsperityBase * Math.Max(1, currentLevel);
    }
}
