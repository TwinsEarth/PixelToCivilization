namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 文化服务（culture 插件实现，Tier-1）。
    /// 依据累积文化值（ResourceType.Culture）计算文化等级与文明影响力，
    /// 提供文化胜利进度（Victory 读取）。
    /// </summary>
    public interface ICultureService
    {
        /// <summary>累积文化值（= ResourceType.Culture 库存）。</summary>
        float Value(int faction);

        /// <summary>当前文化等级（里程碑）。</summary>
        int Level(int faction);

        /// <summary>文明影响力 [0,100]（文化辐射）。</summary>
        float Influence(int faction);

        /// <summary>文化胜利进度 [0,1]（达到最高文化等级）。</summary>
        float VictoryProgress(int faction);

        /// <summary>年度结算：刷新文化等级（返回是否升级）。</summary>
        bool YearlyUpdate(int faction);
    }
}
