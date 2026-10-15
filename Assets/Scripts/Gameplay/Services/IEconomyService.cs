using PxC.Gameplay.Resources;

namespace PxC.Gameplay.Services
{
    /// <summary>一项资源花费（建造/研究/征兵的成本项）。</summary>
    public struct ResourceCost
    {
        public ResourceType Type;
        public float Amount;

        public ResourceCost(ResourceType type, float amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    /// <summary>
    /// 经济服务（economy 插件实现）。资源以 SoA 连续存储 [faction, resource]，
    /// 任何系统的扣费都必须走 TrySpend（禁止负资源），保证资源口径唯一。
    /// </summary>
    public interface IEconomyService
    {
        /// <summary>查询某势力某资源当前余额。</summary>
        float Get(int faction, ResourceType type);

        /// <summary>直接增减资源（产出/消耗结算，可负；玩家花费请用 TrySpend）。</summary>
        void Add(int faction, ResourceType type, float amount);

        /// <summary>余额是否足够。</summary>
        bool CanAfford(int faction, ResourceType type, float amount);

        /// <summary>尝试扣费：足够则扣减并返回 true，否则不动并返回 false。</summary>
        bool TrySpend(int faction, ResourceType type, float amount);

        /// <summary>能否一次性支付一组成本（原子判定）。</summary>
        bool CanAffordBundle(int faction, ResourceCost[] costs);

        /// <summary>尝试一次性扣减一组成本：全部足够才扣（原子），否则全不扣。</summary>
        bool TrySpendBundle(int faction, ResourceCost[] costs);
    }
}
