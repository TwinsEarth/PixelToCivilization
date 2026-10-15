using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Services;

namespace PxC.Infra.Budget
{
    /// <summary>画质档位（决定 P1/P2 层频率）。</summary>
    public enum QualityTier
    {
        Ultra = 0,
        High = 1,
        Low = 2
    }

    /// <summary>
    /// 算力调度器：P0Core 不节流；P1Tactical 按 1Hz（Low 0.5Hz）；
    /// P2Ambient 按 4/2/1Hz（Ultra/High/Low）。时间分片累计"模拟时间"，
    /// 到点执行并通过 stepDt 传回自上次 tick 以来累计的模拟时间（保留余量）。
    /// 铁律：单步最大模拟时间 <see cref="MaxStepSeconds"/>，超出部分结转下帧
    /// （快进/卡顿下防止单位单步跳过目标），属于自适应降级。
    /// </summary>
    public sealed class SimScheduler : ISimScheduler
    {
        /// <summary>单步最大模拟时间（秒），超出结转，防止跳变。</summary>
        public const float MaxStepSeconds = 2f;

        private readonly Dictionary<SimulationLayer, float> _acc;
        private QualityTier _quality;

        public SimScheduler(QualityTier quality)
        {
            _quality = quality;
            _acc = new Dictionary<SimulationLayer, float>(4);
        }

        public void SetQuality(QualityTier quality) => _quality = quality;

        public bool ShouldTick(SimulationLayer layer, float simDt, float hz, out float stepDt)
        {
            stepDt = 0f;

            if (layer == SimulationLayer.P0Core)
            {
                // 不节流：消费本帧全部模拟时间（极端情况仍受 MaxStep 限制，余量结转）。
                stepDt = simDt <= MaxStepSeconds ? simDt : MaxStepSeconds;
                if (simDt > MaxStepSeconds)
                {
                    _acc.TryGetValue(layer, out float a0);
                    _acc[layer] = a0 + (simDt - MaxStepSeconds);
                }
                return true;
            }

            _acc.TryGetValue(layer, out float a);
            a += simDt;
            float interval = 1f / hz;
            if (a >= interval)
            {
                // 传回"自上次 tick 累计的模拟时间"（封顶 MaxStep），扣减后保留余量。
                stepDt = a <= MaxStepSeconds ? a : MaxStepSeconds;
                _acc[layer] = a - stepDt;
                return true;
            }
            _acc[layer] = a;
            return false;
        }

        public float EffectiveHz(SimulationLayer layer)
        {
            switch (layer)
            {
                case SimulationLayer.P0Core:
                    return 9999f;
                case SimulationLayer.P1Tactical:
                    return _quality == QualityTier.Low ? 0.5f : 1f;
                case SimulationLayer.P2Ambient:
                    switch (_quality)
                    {
                        case QualityTier.Ultra: return 4f;
                        case QualityTier.Low: return 1f;
                        default: return 2f;
                    }
                default:
                    return 1f;
            }
        }

        public void Reset() => _acc.Clear();
    }
}
