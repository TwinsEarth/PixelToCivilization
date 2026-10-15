namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 聚落服务（settlement 插件实现）。管理城市/村落/据点/殖民地的发展：
    /// 聚落人口年度增长、发展度累积、自动升级；提供发展评分（发展胜利与势力评估）。
    /// 聚落人口是"居住规模"口径，与 PopulationState 的劳动力分层口径相互独立。
    /// </summary>
    public interface ISettlementService
    {
        /// <summary>某势力聚落总数。</summary>
        int Count(int faction);

        /// <summary>某势力首都 slot（无则 -1）。</summary>
        int GetCapital(int faction);

        /// <summary>聚落等级。</summary>
        int GetLevel(int slot);

        /// <summary>聚落规模人口。</summary>
        int GetPopulation(int slot);

        /// <summary>某势力所有聚落规模人口之和。</summary>
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
        /// 势力综合发展评分 = Σ(聚落等级 × 聚落人口 / 100)。
        /// 供发展胜利/势力评估使用。
        /// </summary>
        float DevelopmentScore(int faction);

        /// <summary>
        /// 年度发展：聚落人口增长、发展度累积、达到阈值自动升级。
        /// 由 settlement 插件在 OnYear 调用。
        /// </summary>
        void AdvanceYear(int gameYear);
    }
}
