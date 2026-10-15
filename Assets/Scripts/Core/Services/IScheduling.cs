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
    /// 铁律：传入的是"模拟时间"（realDt × 时钟倍率），节流 tick 必须通过
    /// <paramref name="stepDt"/> 传回"自上次 tick 以来累计的模拟时间"，
    /// 绝不允许节流后只传单帧 realDt（否则单位移动时间随节流倍率丢失）。
    /// </summary>
    public interface ISimScheduler
    {
        /// <summary>
        /// 本帧该层是否应执行。
        /// <paramref name="simDt"/> 为本帧模拟时间（realDt × 倍率）；
        /// hz 为目标频率；返回 true 时 <paramref name="stepDt"/> 为本次应推进的模拟时间。
        /// </summary>
        bool ShouldTick(SimulationLayer layer, float simDt, float hz, out float stepDt);

        /// <summary>获取某层当前生效频率（受画质档位影响）。</summary>
        float EffectiveHz(SimulationLayer layer);

        /// <summary>重置时间分片（场景切换/恢复后）。</summary>
        void Reset();
    }
}
