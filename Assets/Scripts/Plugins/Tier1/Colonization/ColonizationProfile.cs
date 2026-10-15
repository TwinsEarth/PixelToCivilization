using System;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Colonization
{
    /// <summary>
    /// 殖民数值配置（权威）。各聚落类型的移民人口、资源成本、时代门控；
    /// 自动扩张门槛（指数曲线）、聚落间距与选址参数。
    /// v10.3.1：扩张门槛由线性（80+60×n）改为指数（115×1.40^n），
    /// 使 12 个聚落均匀分布到约第 300 年，开局有立足积累期，不再前紧后松。
    /// </summary>
    public static class ColonizationProfile
    {
        /// <summary>每势力聚落数量上限。</summary>
        public const int MaxSettlementsPerFaction = 12;

        /// <summary>全局聚落数量上限（受 SoA 容量约束）。</summary>
        public const int MaxSettlementsTotal = 96;

        /// <summary>
        /// 第二个聚落的势力总人口门槛（只含一个首都时）。
        /// 初始人口 100 &lt; 115，需繁衍积累约 15 年才首次扩张。
        /// </summary>
        public const int BasePopulationForExpansion = 115;

        /// <summary>
        /// 门槛指数比：每多一个聚落，所需总人口相对上一档放大 1.40 倍。
        /// 与人口约 2% 的自然增速匹配，使聚落间隔稳定在 20-25 年。
        /// </summary>
        public const float ExpansionThresholdRatio = 1.40f;

        /// <summary>新聚落与最近聚落最小间距倍数（× 该聚落领土半径）。</summary>
        public const float MinSpacingFactor = 1.6f;

        /// <summary>自动选址目标距离倍数（× 现有聚落领土半径）。</summary>
        public const float SiteDistanceFactor = 2.0f;

        /// <summary>每势力每年自动扩张最多新建聚落数。</summary>
        public const int MaxNewPerFactionPerYear = 1;

        /// <summary>
        /// 年度自动扩张的势力总人口门槛。
        /// </summary>
        /// <param name="existingCount">势力当前已有的聚落数（含首都）。</param>
        /// <returns>再建立一个新聚落所需的总人口。</returns>
        public static int ExpansionThreshold(int existingCount)
        {
            // 要建的是第 (existingCount+1) 个聚落；门槛 = 115 × 1.40^(existingCount−1)。
            // existingCount 至少为 1（首都），指数从 0 起。
            int n = Math.Max(1, existingCount);
            double value = BasePopulationForExpansion *
                Math.Pow(ExpansionThresholdRatio, n - 1);
            return (int)Math.Round(value);
        }

        /// <summary>建立某类型聚落所需的移民人口。</summary>
        public static int Migrants(SettlementKind kind)
        {
            switch (kind)
            {
                case SettlementKind.Outpost: return 5;
                case SettlementKind.Village: return 8;
                case SettlementKind.Coastal: return 10;
                case SettlementKind.Island: return 12;
                case SettlementKind.Town: return 15;
                case SettlementKind.Planet: return 20;
                default: return 10; // Capital（不会走殖民）
            }
        }

        /// <summary>建立某类型聚落所需的最小时代（Era 索引）。</summary>
        public static int RequiredEra(SettlementKind kind)
        {
            switch (kind)
            {
                case SettlementKind.Town: return 1;     // BronzeAge
                case SettlementKind.Coastal: return 3;  // Medieval
                case SettlementKind.Island: return 5;   // AgeOfSail
                case SettlementKind.Planet: return 9;   // Space
                default: return 0;                      // Village/Outpost/Capital
            }
        }

        /// <summary>建立某类型聚落所需的资源成本（返回新数组）。</summary>
        public static ResourceCost[] Costs(SettlementKind kind)
        {
            switch (kind)
            {
                case SettlementKind.Outpost:
                    return new ResourceCost[]
                    {
                        new ResourceCost(ResourceType.Food, 20f),
                        new ResourceCost(ResourceType.Wood, 10f),
                    };
                case SettlementKind.Village:
                    return new ResourceCost[]
                    {
                        new ResourceCost(ResourceType.Food, 30f),
                        new ResourceCost(ResourceType.Wood, 20f),
                    };
                case SettlementKind.Coastal:
                    return new ResourceCost[]
                    {
                        new ResourceCost(ResourceType.Food, 40f),
                        new ResourceCost(ResourceType.Wood, 30f),
                        new ResourceCost(ResourceType.Gold, 20f),
                    };
                case SettlementKind.Island:
                    return new ResourceCost[]
                    {
                        new ResourceCost(ResourceType.Food, 60f),
                        new ResourceCost(ResourceType.Wood, 40f),
                        new ResourceCost(ResourceType.Gold, 40f),
                    };
                case SettlementKind.Town:
                    return new ResourceCost[]
                    {
                        new ResourceCost(ResourceType.Food, 60f),
                        new ResourceCost(ResourceType.Wood, 40f),
                        new ResourceCost(ResourceType.Gold, 30f),
                    };
                case SettlementKind.Planet:
                    return new ResourceCost[]
                    {
                        new ResourceCost(ResourceType.Food, 100f),
                        new ResourceCost(ResourceType.Steel, 100f),
                        new ResourceCost(ResourceType.Data, 100f),
                    };
                default:
                    return new ResourceCost[0]; // Capital
            }
        }
    }
}
