using PxC.Gameplay.Resources;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Colonization
{
    /// <summary>
    /// 殖民数值配置（权威）。各聚落类型的移民人口、资源成本、时代门控；
    /// 自动扩张门槛、聚落间距与选址参数。
    /// </summary>
    public static class ColonizationProfile
    {
        /// <summary>每势力聚落数量上限。</summary>
        public const int MaxSettlementsPerFaction = 12;

        /// <summary>全局聚落数量上限（受 SoA 容量约束）。</summary>
        public const int MaxSettlementsTotal = 96;

        /// <summary>势力总人口达到此值才允许自动扩张（第二个聚落）。</summary>
        public const int MinPopulationForExpansion = 80;

        /// <summary>每多一个聚落所需的额外人口基数。</summary>
        public const int PopulationPerSettlement = 60;

        /// <summary>新聚落与最近聚落最小间距倍数（× 该聚落领土半径）。</summary>
        public const float MinSpacingFactor = 1.6f;

        /// <summary>自动选址目标距离倍数（× 现有聚落领土半径）。</summary>
        public const float SiteDistanceFactor = 2.0f;

        /// <summary>每势力每年自动扩张最多新建聚落数。</summary>
        public const int MaxNewPerFactionPerYear = 1;

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
