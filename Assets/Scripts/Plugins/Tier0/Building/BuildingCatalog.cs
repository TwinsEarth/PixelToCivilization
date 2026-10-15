using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;

namespace PxC.Plugins.Building
{
    /// <summary>
    /// 建筑按时代解锁的副产出（隐式产业链：建筑本身代表"采集→冶炼→加工"完整链路）。
    /// 势力时代达到 <see cref="MinEra"/> 后，每年额外产出 <see cref="Amount"/> 资源。
    /// 不逐笔消耗原料（建筑成本与时代门槛是主要成本，产出受建筑数量自然限速）。
    /// </summary>
    public sealed class BuildingEraOutput
    {
        public ResourceType Resource;
        public float Amount;
        public Era MinEra;
    }

    /// <summary>
    /// 建筑类型目录条目：名称、建造成本、耐久、年度产出、解锁时代、是否塔防。
    /// 数据驱动（后续可迁移为 ScriptableObject/Addressables），数值可在此统一平衡。
    /// </summary>
    public sealed class BuildingCatalogEntry
    {
        public BuildingType Type;
        public string DisplayName;
        public ResourceCost[] Cost;
        public float MaxHp;
        public bool IsTower;

        // 年度产出（主资源；None=无直接产出）
        public ResourceType Produces;
        public float ProducePerYear;

        // 按时代解锁的副产出（青铜/铁/煤/油/钢/电/数据/资本等高级资源链）
        public BuildingEraOutput[] EraOutputs;

        // 解锁所需时代（Era 枚举，Primitive=0 原始即可）
        public Era RequiredEra;

        /// <summary>拆除返还比例（返还成本的该比例）。</summary>
        public float RefundRatio = 0.5f;
    }

    /// <summary>建筑类型目录（按类型索引的权威表）。</summary>
    public static class BuildingCatalog
    {
        private static readonly BuildingCatalogEntry[] _entries = Build();

        public static BuildingCatalogEntry Get(BuildingType type) =>
            _entries[(int)type];

        public static int Count => _entries.Length;

        private static BuildingCatalogEntry[] Build()
        {
            var e = new BuildingCatalogEntry[BuildingTypes.Count];

            e[(int)BuildingType.House] = new BuildingCatalogEntry
            {
                Type = BuildingType.House,
                DisplayName = "民居",
                Cost = new[] { new ResourceCost(ResourceType.Wood, 20f) },
                MaxHp = 60f,
                IsTower = false,
                Produces = ResourceType.None,
                ProducePerYear = 0f,
                RequiredEra = 0,
            };

            e[(int)BuildingType.Farm] = new BuildingCatalogEntry
            {
                Type = BuildingType.Farm,
                DisplayName = "农田",
                Cost = new[] { new ResourceCost(ResourceType.Wood, 15f) },
                MaxHp = 50f,
                IsTower = false,
                Produces = ResourceType.Food,
                ProducePerYear = 12f,
                RequiredEra = 0,
            };

            e[(int)BuildingType.LumberMill] = new BuildingCatalogEntry
            {
                Type = BuildingType.LumberMill,
                DisplayName = "伐木场",
                Cost = new[] { new ResourceCost(ResourceType.Wood, 25f) },
                MaxHp = 70f,
                IsTower = false,
                Produces = ResourceType.Wood,
                ProducePerYear = 8f,
                RequiredEra = 0,
            };

            e[(int)BuildingType.Quarry] = new BuildingCatalogEntry
            {
                Type = BuildingType.Quarry,
                DisplayName = "采石场",
                Cost = new[] { new ResourceCost(ResourceType.Wood, 20f) },
                MaxHp = 90f,
                IsTower = false,
                Produces = ResourceType.Stone,
                ProducePerYear = 5f,
                // 采矿场随时代产出矿脉：青铜（铜）→铁→煤/油（工业露天矿）
                EraOutputs = new[]
                {
                    new BuildingEraOutput { Resource = ResourceType.Bronze, Amount = 3f, MinEra = Era.BronzeAge },
                    new BuildingEraOutput { Resource = ResourceType.Iron, Amount = 2f, MinEra = Era.IronAge },
                    new BuildingEraOutput { Resource = ResourceType.Coal, Amount = 4f, MinEra = Era.Industrial },
                    new BuildingEraOutput { Resource = ResourceType.Oil, Amount = 2f, MinEra = Era.Industrial },
                },
                RequiredEra = Era.BronzeAge,
            };

            e[(int)BuildingType.Market] = new BuildingCatalogEntry
            {
                Type = BuildingType.Market,
                DisplayName = "市集",
                Cost = new[]
                {
                    new ResourceCost(ResourceType.Wood, 30f),
                    new ResourceCost(ResourceType.Gold, 10f),
                },
                MaxHp = 80f,
                IsTower = false,
                Produces = ResourceType.Gold,
                ProducePerYear = 10f,
                // 商贸随时代形成金融资本（工业→信息，资本积累加速）
                EraOutputs = new[]
                {
                    new BuildingEraOutput { Resource = ResourceType.Capital, Amount = 3f, MinEra = Era.Industrial },
                    new BuildingEraOutput { Resource = ResourceType.Capital, Amount = 5f, MinEra = Era.Information },
                },
                RequiredEra = Era.IronAge,
            };

            e[(int)BuildingType.Workshop] = new BuildingCatalogEntry
            {
                Type = BuildingType.Workshop,
                DisplayName = "工坊",
                Cost = new[]
                {
                    new ResourceCost(ResourceType.Wood, 30f),
                    new ResourceCost(ResourceType.Stone, 20f),
                },
                MaxHp = 100f,
                IsTower = false,
                Produces = ResourceType.Goods,
                ProducePerYear = 6f,
                // 加工/冶炼随时代升级：铁器锻造→炼钢/发电→信息电子
                EraOutputs = new[]
                {
                    new BuildingEraOutput { Resource = ResourceType.Iron, Amount = 3f, MinEra = Era.IronAge },
                    new BuildingEraOutput { Resource = ResourceType.Steel, Amount = 3f, MinEra = Era.Industrial },
                    new BuildingEraOutput { Resource = ResourceType.Electricity, Amount = 2f, MinEra = Era.Industrial },
                    new BuildingEraOutput { Resource = ResourceType.Data, Amount = 3f, MinEra = Era.Information },
                },
                RequiredEra = Era.IronAge,
            };

            e[(int)BuildingType.Barracks] = new BuildingCatalogEntry
            {
                Type = BuildingType.Barracks,
                DisplayName = "兵营",
                Cost = new[]
                {
                    new ResourceCost(ResourceType.Wood, 25f),
                    new ResourceCost(ResourceType.Stone, 10f),
                },
                MaxHp = 120f,
                IsTower = false,
                Produces = ResourceType.None,
                ProducePerYear = 0f,
                RequiredEra = Era.BronzeAge,
            };

            e[(int)BuildingType.ArrowTower] = new BuildingCatalogEntry
            {
                Type = BuildingType.ArrowTower,
                DisplayName = "箭塔",
                Cost = new[]
                {
                    new ResourceCost(ResourceType.Wood, 30f),
                    new ResourceCost(ResourceType.Stone, 10f),
                },
                MaxHp = 140f,
                IsTower = true,
                Produces = ResourceType.None,
                ProducePerYear = 0f,
                RequiredEra = Era.BronzeAge,
            };

            e[(int)BuildingType.FireTower] = new BuildingCatalogEntry
            {
                Type = BuildingType.FireTower,
                DisplayName = "火塔",
                Cost = new[]
                {
                    new ResourceCost(ResourceType.Wood, 20f),
                    new ResourceCost(ResourceType.Stone, 25f),
                },
                MaxHp = 160f,
                IsTower = true,
                Produces = ResourceType.None,
                ProducePerYear = 0f,
                RequiredEra = Era.BronzeAge,
            };

            e[(int)BuildingType.CannonTower] = new BuildingCatalogEntry
            {
                Type = BuildingType.CannonTower,
                DisplayName = "炮塔",
                Cost = new[]
                {
                    new ResourceCost(ResourceType.Stone, 40f),
                    new ResourceCost(ResourceType.Bronze, 15f),
                },
                MaxHp = 200f,
                IsTower = true,
                Produces = ResourceType.None,
                ProducePerYear = 0f,
                RequiredEra = Era.IronAge,
            };

            e[(int)BuildingType.Beacon] = new BuildingCatalogEntry
            {
                Type = BuildingType.Beacon,
                DisplayName = "烽火台",
                Cost = new[] { new ResourceCost(ResourceType.Wood, 25f) },
                MaxHp = 90f,
                IsTower = true,
                Produces = ResourceType.None,
                ProducePerYear = 0f,
                RequiredEra = 0,
            };

            e[(int)BuildingType.Bunker] = new BuildingCatalogEntry
            {
                Type = BuildingType.Bunker,
                DisplayName = "碉堡",
                Cost = new[]
                {
                    new ResourceCost(ResourceType.Stone, 50f),
                    new ResourceCost(ResourceType.Steel, 10f),
                },
                MaxHp = 320f,
                IsTower = true,
                Produces = ResourceType.None,
                ProducePerYear = 0f,
                RequiredEra = Era.Industrial,
            };

            return e;
        }
    }
}
