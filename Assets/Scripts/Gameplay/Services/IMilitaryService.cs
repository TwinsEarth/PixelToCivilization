namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 军事总控服务（military 插件实现）。维护势力的聚合兵力、火力与征兵，
    /// 具体可移动作战单位（舰船/地面单位）由 naval / groundforce 持有并登记到 UnitRegistry。
    /// </summary>
    public interface IMilitaryService
    {
        /// <summary>总兵力（士兵 + 作战单位折算）。</summary>
        int TotalSoldiers(int faction);

        /// <summary>火力评分（决定攻防强度，受软上限约束）。</summary>
        int Firepower(int faction);

        /// <summary>尝试征兵 count 名（消耗人口与粮食，失败返回 false）。</summary>
        bool TryRecruit(int faction, int count);

        /// <summary>两势力是否处于交战状态。</summary>
        bool IsAtWar(int faction, int otherFaction);
    }
}
