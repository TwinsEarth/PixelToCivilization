namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 人口分层（职业/用途）。决定产出、兵源与消耗：
    /// 劳力产粮、工匠产商品、学者产研究文化、士兵消耗更多粮食。
    /// </summary>
    public enum PopulationLayer
    {
        /// <summary>少儿（非劳力，未来增长）。</summary>
        Children = 0,

        /// <summary>劳力：农夫/采集（产粮/木/石）。</summary>
        Laborer = 1,

        /// <summary>工匠：作坊/工厂（产商品、参与建造）。</summary>
        Artisan = 2,

        /// <summary>士兵：战斗力（消耗更多粮食）。</summary>
        Soldier = 3,

        /// <summary>学者：研究/文化产出。</summary>
        Scholar = 4,

        /// <summary>老人（非劳力）。</summary>
        Elder = 5,
    }

    /// <summary>
    /// 人口服务（population 插件实现）。v10.4.0 起人口唯一存储为
    /// <b>聚落 × 分层 [settlement, layer]</b>：出生/死亡/成年/职业分配都在聚落粒度结算，
    /// 势力人口为该势力所有聚落的聚合。移民是聚落间的空间转移（总量守恒）。
    /// </summary>
    public interface IPopulationService
    {
        // ----------------------------------------------------------------
        // 势力聚合口径（faction = Σ该势力聚落）
        // ----------------------------------------------------------------

        /// <summary>势力总人口（Σ该势力所有聚落各层）。</summary>
        int Total(int faction);

        /// <summary>势力某分层人口（Σ该势力所有聚落该层）。</summary>
        int ByLayer(int faction, PopulationLayer layer);

        /// <summary>粗出生率（每人每年）。</summary>
        float BirthRate(int faction);

        /// <summary>粗死亡率（每人每年，含饥荒）。</summary>
        float DeathRate(int faction);

        /// <summary>当前饥荒风险 [0,1]（粮食缺口/需求）。</summary>
        float StarvationRisk(int faction);

        /// <summary>
        /// 尝试在势力内把 count 人从 from 调配到 to（在该势力各聚落间凑齐来源，
        /// 优先在同一聚落内转换）。来源不足返回 false。
        /// </summary>
        bool TryAssign(int faction, PopulationLayer from, PopulationLayer to, int count);

        /// <summary>
        /// 灾害/瘟疫造成的额外死亡：按灾害顺序从该势力各聚落扣减，返回实际死亡数。
        /// 供 disaster 插件调用（瘟疫/寒潮），不影响正常出生/死亡口径。
        /// </summary>
        int ApplyExcessDeaths(int faction, int count);

        // ----------------------------------------------------------------
        // 聚落粒度口径（settlement，人口的家）
        // ----------------------------------------------------------------

        /// <summary>某聚落总人口（Σ该聚落各层）。</summary>
        int SettlementTotal(int slot);

        /// <summary>某聚落某分层人口。</summary>
        int SettlementByLayer(int slot, PopulationLayer layer);

        /// <summary>
        /// 清空某聚落全部人口（聚落灭城/废弃、殖民失败回滚时调用）。
        /// 人口随聚落消亡；势力总人口相应减少。
        /// </summary>
        void ClearSettlement(int slot);

        /// <summary>
        /// 首都建立时初始化人口：按权威分层写入首都槽位（20/55/8/8/4/5，共 100）。
        /// 由 settlement 插件 FoundInitialCapitals 调用；仅用于开局首都，不凭空增加其他人口。
        /// </summary>
        void SeedCapital(int slot);

        /// <summary>
        /// 移民聚落间转移：从源聚落 fromSlot 的 layer 层转移 count 人到目标聚落 toSlot 同层，
        /// 势力总人口不变（空间转移、总量守恒）。来源不足返回 false（不改数据）。
        /// 供 colonization 插件建立新聚落时调用（从首都/源聚落转移到新聚落）。
        /// </summary>
        bool SendMigrants(int fromSlot, int toSlot, PopulationLayer layer, int count);
    }
}
