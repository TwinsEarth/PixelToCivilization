using PxC.Gameplay.Services;

namespace PxC.Plugins.Population
{
    /// <summary>
    /// IPopulationService 实现：人口查询、职业分配的唯一入口。
    /// 出生率/死亡率为基准值，饥荒死亡由 FamineEvent 单独结算。
    /// </summary>
    public sealed class PopulationService : IPopulationService
    {
        private const float BaseBirthRate = 0.035f;
        private const float BaseDeathRate = 0.015f;

        private readonly PopulationState _state;

        public PopulationService(PopulationState state)
        {
            _state = state;
        }

        public int Total(int faction) => _state.Total(faction);

        public int ByLayer(int faction, PopulationLayer layer) =>
            _state.Get(faction, layer);

        public float BirthRate(int faction) => BaseBirthRate;

        public float DeathRate(int faction) => BaseDeathRate;

        public float StarvationRisk(int faction)
        {
            // 实时饥荒风险在 economy 年结后以 FamineEvent 体现；此处基准为 0。
            return 0f;
        }

        public bool TryAssign(int faction, PopulationLayer from, PopulationLayer to, int count)
        {
            if (!_state.TryRemove(faction, from, count)) return false;
            _state.Add(faction, to, count);
            return true;
        }
    }
}
