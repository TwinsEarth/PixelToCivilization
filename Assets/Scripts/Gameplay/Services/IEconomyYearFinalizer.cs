namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 年度后置结算器（v10.4.2 治本）。
    /// 在所有模拟插件 OnYear（含建筑产出）完成之后，由 GameLoop 年度钩子统一调用
    /// <see cref="FinalizeYear"/>，从已收集齐全部产出（劳力 + 农田/市集等建筑产出）的
    /// 库存中扣除粮食消耗并判定饥荒。
    /// 修复旧版 EconomyPlugin 在 OnYear（建筑产出到账前）立即判定饥荒、导致有农田也饿死的问题。
    /// </summary>
    public interface IEconomyYearFinalizer
    {
        /// <summary>在该年度全部产出到账后，统一结算粮食消耗/饥荒。</summary>
        void FinalizeYear(int gameYear);
    }
}
