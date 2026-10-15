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
    /// P2Ambient 按 4/2/1Hz（Ultra/High/Low）。时间分片累计，到点执行并保留余量。
    /// </summary>
    public sealed class SimScheduler : ISimScheduler
    {
        private readonly Dictionary<SimulationLayer, float> _acc;
        private QualityTier _quality;

        public SimScheduler(QualityTier quality)
        {
            _quality = quality;
            _acc = new Dictionary<SimulationLayer, float>(4);
        }

        public void SetQuality(QualityTier quality) => _quality = quality;

        public bool ShouldTick(SimulationLayer layer, float realDt, float hz)
        {
            if (layer == SimulationLayer.P0Core) return true;
            _acc.TryGetValue(layer, out float a);
            a += realDt;
            float interval = 1f / hz;
            if (a >= interval)
            {
                _acc[layer] = a % interval;
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
