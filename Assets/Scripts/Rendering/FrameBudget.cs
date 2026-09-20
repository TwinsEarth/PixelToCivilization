using UnityEngine;

namespace PixelToCivilization.Rendering
{
    /// <summary>画质档位（V7.0.3 性能神自适应降档）。</summary>
    public enum PerfLevel { High = 0, Mid = 1, Low = 2 }

    /// <summary>
    /// 每帧预算与自适应画质：统计平滑帧耗，掉帧时降阴影距离/粒子密度/鸟鱼全显名额，
    /// 持续富余时再逐级回升（降档即时、升档需连续若干秒达标，避免抖动）。
    /// 各系统只读静态系数，不新增每帧重算。
    /// </summary>
    public class FrameBudget : MonoBehaviour
    {
        public static FrameBudget I { get; private set; }
        public static PerfLevel Level { get; private set; } = PerfLevel.High;

        // 各档位缩放系数（供天气粒子、鸟鱼全显名额等读取）
        public static float ParticleScale => Level == PerfLevel.High ? 1f : Level == PerfLevel.Mid ? 0.6f : 0.32f;
        public static float WildlifeCapScale => Level == PerfLevel.High ? 1f : Level == PerfLevel.Mid ? 0.8f : 0.55f;
        public static float FarRefreshScale => Level == PerfLevel.High ? 1f : Level == PerfLevel.Mid ? 0.7f : 0.45f;

        const float EvalInterval = 1f;          // 每秒评估一次
        const float DownMidMs = 16.5f;          // >16.5ms（约<60fps）进中档
        const float DownLowMs = 24f;            // >24ms（约<42fps）进低档
        const float UpMs = 11.5f;               // <11.5ms（约>87fps）才考虑升档
        const int UpSustain = 4;                // 连续 4 秒优秀才升一级

        readonly float[] _hist = new float[30];
        int _idx, _cnt, _goodRuns;
        float _timer;
        float _baseShadow = 70f;

        void Awake()
        {
            I = this;
            _baseShadow = QualitySettings.shadowDistance > 1f ? QualitySettings.shadowDistance : 70f;
            Apply();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (float.IsNaN(dt) || dt <= 0f) return;
            _hist[_idx] = dt; _idx = (_idx + 1) % _hist.Length; if (_cnt < _hist.Length) _cnt++;

            _timer += dt;
            if (_timer < EvalInterval) return;
            _timer = 0f;

            float sum = 0f; for (int i = 0; i < _cnt; i++) sum += _hist[i];
            float ms = (_cnt > 0 ? sum / _cnt : dt) * 1000f;

            if (ms >= DownLowMs) { Level = PerfLevel.Low; _goodRuns = 0; }
            else if (ms >= DownMidMs) { if (Level < PerfLevel.Mid) Level = PerfLevel.Mid; _goodRuns = 0; }
            else if (ms <= UpMs)
            {
                _goodRuns++;
                if (_goodRuns >= UpSustain && Level > PerfLevel.High)
                {
                    Level = (PerfLevel)((int)Level - 1);
                    _goodRuns = 0;
                }
            }
            else _goodRuns = 0;

            Apply();
        }

        void Apply()
        {
            QualitySettings.shadowDistance =
                Level == PerfLevel.High ? _baseShadow :
                Level == PerfLevel.Mid ? _baseShadow * 0.6f : _baseShadow * 0.3f;
        }

        /// <summary>调试/测试用：手动指定档位并立即应用。</summary>
        public static void ForceLevel(PerfLevel l)
        {
            Level = l;
            if (I != null)
            {
                if (QualitySettings.shadowDistance <= 1f) I._baseShadow = 70f;
                I.Apply();
            }
        }
    }
}
