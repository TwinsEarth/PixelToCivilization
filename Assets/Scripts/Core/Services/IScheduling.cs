using PxC.Core.Contracts;

namespace PxC.Core.Services
{
    /// <summary>
    /// 帧预算：WebGL 6ms / 桌面 10ms 的模拟预算。系统在 Tick 前检查剩余预算，
    /// 超限则把工作推迟到下一帧（配合自适应画质）。
    /// </summary>
    public interface IFrameBudget
    {
        float FrameBudgetMs { get; }
        float UsedMs { get; }
        float RemainingMs { get; }
        bool HasBudget(float needMs = 0f);
        void BeginFrame();
        void Consume(float ms);
    }

    /// <summary>
    /// 算力调度器：按 SimulationLayer 与画质档位决定本帧是否 Tick。
    /// P0Core 永不节流；P1Tactical 默认 1Hz（Low 2s）；P2Ambient 按 4/2/1Hz（Ultra/High/Low）。
    /// </summary>
    public interface ISimScheduler
    {
        /// <summary>本帧该层是否应执行。hz 为目标频率，内部累计时间分片。</summary>
        bool ShouldTick(SimulationLayer layer, float realDt, float hz);

        /// <summary>获取某层当前生效频率（受画质档位影响）。</summary>
        float EffectiveHz(SimulationLayer layer);

        /// <summary>重置时间分片（场景切换/恢复后）。</summary>
        void Reset();
    }
}
