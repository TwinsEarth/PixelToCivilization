using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Economy
{
    /// <summary>
    /// IEconomyService 实现：资源扣费的唯一入口。
    /// 单笔 TrySpend；Bundle 先全量校验再一次性扣减（原子，不会出现扣一半）。
    /// </summary>
    public sealed class EconomyService : IEconomyService
    {
        private readonly EconomyState _state;

        public EconomyService(EconomyState state)
        {
            _state = state;
        }

        public float Get(int faction, ResourceType type) => _state.Get(faction, type);

        public void Add(int faction, ResourceType type, float amount) =>
            _state.Add(faction, type, amount);

        public bool CanAfford(int faction, ResourceType type, float amount) =>
            _state.CanAfford(faction, type, amount);

        public bool TrySpend(int faction, ResourceType type, float amount)
        {
            if (!_state.CanAfford(faction, type, amount)) return false;
            _state.Add(faction, type, -amount);
            return true;
        }

        public bool CanAffordBundle(int faction, ResourceCost[] costs)
        {
            if (costs == null) return true;
            for (int i = 0; i < costs.Length; i++)
            {
                if (!_state.CanAfford(faction, costs[i].Type, costs[i].Amount))
                    return false;
            }
            return true;
        }

        public bool TrySpendBundle(int faction, ResourceCost[] costs)
        {
            if (!CanAffordBundle(faction, costs)) return false;
            for (int i = 0; i < costs.Length; i++)
                _state.Add(faction, costs[i].Type, -costs[i].Amount);
            return true;
        }
    }
}
