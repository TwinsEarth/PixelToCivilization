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
    /// 人口服务（population 插件实现）。人口以 SoA 存储 [faction, layer]，
    /// 处理出生/死亡/周期律与职业分配；饥荒由本服务按年结算（修复每帧重复扣人）。
    /// </summary>
    public interface IPopulationService
    {
        /// <summary>总人口。</summary>
        int Total(int faction);

        /// <summary>某分层人口。</summary>
        int ByLayer(int faction, PopulationLayer layer);

        /// <summary>粗出生率（每人每年）。</summary>
        float BirthRate(int faction);

        /// <summary>粗死亡率（每人每年，含饥荒）。</summary>
        float DeathRate(int faction);

        /// <summary>当前饥荒风险 [0,1]（粮食缺口/需求）。</summary>
        float StarvationRisk(int faction);

        /// <summary>尝试在分层间调配 count 人（来源不足返回 false）。</summary>
        bool TryAssign(int faction, PopulationLayer from, PopulationLayer to, int count);

        /// <summary>
        /// 灾害/瘟疫造成的额外死亡：按灾害顺序扣减，返回实际死亡数。
        /// 供 disaster 插件调用（瘟疫/寒潮），不影响正常出生/死亡口径。
        /// </summary>
        int ApplyExcessDeaths(int faction, int count);

        /// <summary>
        /// 派遣移民（殖民/扩张）：从劳力人口中派出 count 人，成功则扣减并返回 true。
        /// 供 colonization 插件建立新聚落时调用；劳力不足返回 false（不改数据）。
        /// </summary>
        bool Emigrate(int faction, int count);
    }
}
