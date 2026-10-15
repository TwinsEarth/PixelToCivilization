namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 聚落服务（settlement 插件实现）。管理城市/村落/据点/殖民地的发展：
    /// 发展度累积、自动升级；提供发展评分（发展胜利与势力评估）。
    /// v10.4.0 起聚落人口由 population 插件统一（聚落 × 分层），GetPopulation 与
    /// TotalPopulation 都返回人口服务的聚合，不再有独立"居住规模"账本。
    /// </summary>
    public interface ISettlementService
    {
        /// <summary>某势力聚落总数。</summary>
        int Count(int faction);

        /// <summary>某势力首都 slot（无则 -1）。</summary>
        int GetCapital(int faction);

        /// <summary>聚落等级。</summary>
        int GetLevel(int slot);

        /// <summary>聚落总人口（来自人口服务，聚落 × 分层之和）。</summary>
        int GetPopulation(int slot);

        /// <summary>某势力所有聚落总人口之和（=人口服务的势力总人口）。</summary>
        int TotalPopulation(int faction);

        /// <summary>聚落发展度（当前累积，用于升级）。</summary>
        float GetProsperity(int slot);

        /// <summary>
        /// 聚落当前领土半径（由等级决定）。供殖民扩张做间距/领土判定。
        /// </summary>
        float GetTerritoryRadius(int slot);

        /// <summary>一级聚落的基础领土半径（无首都聚落时的兜底）。</summary>
        float BaseTerritoryRadius { get; }

        /// <summary>
        /// 势力综合发展评分 = 势力总人口（人口服务）+ 建筑加权。
        /// 供发展胜利/势力评估使用。
        /// </summary>
        float DevelopmentScore(int faction);

        /// <summary>
        /// 年度发展：发展度累积、达到阈值自动升级（人口已由 population 插件结算）。
        /// 由 settlement 插件在 OnYear 调用（在 population 之后）。
        /// </summary>
        void AdvanceYear(int gameYear);
    }
}
