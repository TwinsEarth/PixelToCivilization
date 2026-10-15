namespace PxC.Gameplay.Resources
{
    /// <summary>
    /// 全时代资源类型（15 种）。顺序即存档索引，禁止重排；新增只能追加到末尾。
    /// 前 9 种为农业社会核心，后续随工业/信息/星际时代依次解锁。
    /// </summary>
    public enum ResourceType
    {
        /// <summary>无：哨兵值，表示"无此资源类型/无产出"，不参与存档索引与数组下标。</summary>
        None = -1,

        /// <summary>粮食：养活人口与军队，饥荒核心。</summary>
        Food = 0,

        /// <summary>金币：贸易/税收与市场通用货币。</summary>
        Gold = 1,

        /// <summary>木材：早期建筑/燃料/器械。</summary>
        Wood = 2,

        /// <summary>石料：耐久建筑/城墙/塔。</summary>
        Stone = 3,

        /// <summary>青铜：先秦兵器/礼器。</summary>
        Bronze = 4,

        /// <summary>铁：铁器时代兵器/工具。</summary>
        Iron = 5,

        /// <summary>煤：工业革命燃料。</summary>
        Coal = 6,

        /// <summary>石油：内燃机/现代战争。</summary>
        Oil = 7,

        /// <summary>钢：工业脊梁。</summary>
        Steel = 8,

        /// <summary>商品：作坊/工厂产出的民生与贸易制成品。</summary>
        Goods = 9,

        /// <summary>研究点：科技研究。</summary>
        Research = 10,

        /// <summary>文化点：文化/哲学/制度。</summary>
        Culture = 11,

        /// <summary>电力：现代能源，驱动产能加成。</summary>
        Electricity = 12,

        /// <summary>数据：信息时代，驱动 AI/科研。</summary>
        Data = 13,

        /// <summary>资金/资本：现代金融与大规模建设。</summary>
        Capital = 14,
    }

    /// <summary>资源类型数量常量（=15）。</summary>
    public static class ResourceTypes
    {
        public const int Count = 15;

        /// <summary>资源在 UI 中的显示名（与 ResourceType 同序）。</summary>
        public static readonly string[] DisplayNames =
        {
            "粮食", "金币", "木材", "石料", "青铜", "铁",
            "煤", "石油", "钢", "商品", "研究", "文化",
            "电力", "数据", "资金"
        };
    }
}
