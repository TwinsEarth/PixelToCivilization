using PxC.Core.Services;

namespace PxC.Infra.Budget
{
    /// <summary>帧预算实现（WebGL 6ms / 桌面 10ms）。</summary>
    public sealed class FrameBudget : IFrameBudget
    {
        private float _usedMs;

        public FrameBudget(float frameBudgetMs)
        {
            FrameBudgetMs = frameBudgetMs;
        }

        public float FrameBudgetMs { get; }
        public float UsedMs => _usedMs;
        public float RemainingMs => FrameBudgetMs - _usedMs;

        public bool HasBudget(float needMs = 0f) => RemainingMs >= needMs;

        public void BeginFrame() => _usedMs = 0f;

        public void Consume(float ms) => _usedMs += ms;
    }
}
