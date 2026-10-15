using System;

namespace PxC.Plugins.Settlement
{
    /// <summary>
    /// 聚落数值配置（权威，v10.4.0 起只管发展度/领土）。
    /// 人口出生/死亡/容量已统一到 population 插件（聚落 × 分层），本类不再有
    /// 人口增长率/软上限，避免两套口径。
    /// </summary>
    public static class SettlementProfile
    {
        /// <summary>每名居民每年贡献的发展度。</summary>
        public const float ProsperityPerCapita = 0.1f;

        /// <summary>聚落每年基础发展度（行政/积累）。</summary>
        public const float ProsperityBase = 2f;

        /// <summary>升级所需发展度基数（× 当前等级）。</summary>
        public const float UpgradeProsperityBase = 100f;

        /// <summary>聚落最高等级。</summary>
        public const int MaxLevel = 10;

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
