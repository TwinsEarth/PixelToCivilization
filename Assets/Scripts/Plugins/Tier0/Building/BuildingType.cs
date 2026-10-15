namespace PxC.Plugins.Building
{
    /// <summary>
    /// 建筑类型权威索引（BuildingRecord.TypeId 对应本枚举）。
    /// 经济建筑在前，塔防建筑在后。
    /// </summary>
    public enum BuildingType
    {
        // ---- 经济/民生建筑 ----
        House = 0,       // 民居（提升人口上限）
        Farm = 1,        // 农田（年产粮）
        LumberMill = 2,  // 伐木场（年产木）
        Quarry = 3,      // 采石场（年产石）
        Market = 4,      // 市集（年产金/商品）
        Workshop = 5,    // 工坊（年产商品/钢）
        Barracks = 6,    // 兵营（征兵）

        // ---- 塔防/防御建筑（多层收分塔，非高楼）----
        ArrowTower = 7,  // 箭塔：木制多层，射弩箭
        FireTower = 8,   // 火塔：土垒尖塔，喷火
        CannonTower = 9, // 炮塔：方形矮墩，旋转炮塔
        Beacon = 10,     // 烽火台：木架大锅
        Bunker = 11,     // 碉堡：圆形堡垒，四面机枪
    }

    /// <summary>建筑类型目录长度与分类辅助。</summary>
    public static class BuildingTypes
    {
        /// <summary>建筑类型总数。</summary>
        public const int Count = 12;

        /// <summary>第一个塔防类型的索引（>= 即塔防）。</summary>
        public const int FirstTowerType = 7;

        /// <summary>是否为塔防/防御建筑。</summary>
        public static bool IsTowerType(BuildingType type) => (int)type >= FirstTowerType;
    }
}
