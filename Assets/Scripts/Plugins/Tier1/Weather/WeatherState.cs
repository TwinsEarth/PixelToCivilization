using System;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Weather
{
    /// <summary>
    /// 天气领域状态（纯数据，无 Unity 依赖）。
    /// 持有当前（本年）主导天气、强度、对应年份，以及一个可复现的随机状态。
    /// 天气按年推进：每次 Step 用伪随机 + 纬度/气候概率选择下一年天气。
    /// </summary>
    public sealed class WeatherState
    {
        /// <summary>当前天气（对应 <see cref="CurrentYear"/>）。</summary>
        public int Condition;

        /// <summary>当前强度 [0,1]。</summary>
        public float Intensity;

        /// <summary>当前天气对应的游戏年份。</summary>
        public int CurrentYear;

        /// <summary>已初始化（OnInitialize 已生成初始年天气）。</summary>
        public bool Initialized;

        // 可复现随机（线性同余，避免每次 new System.Random 导致序列不可复现）
        private ulong _rng;

        public WeatherState()
        {
            Condition = (int)WeatherCondition.Clear;
            Intensity = 0f;
            CurrentYear = 0;
            Initialized = false;
            _rng = 0x123456789abcdef0UL;
        }

        /// <summary>用指定种子初始化随机流（新游戏用固定种子，读档可复现）。</summary>
        public void Seed(ulong seed)
        {
            _rng = seed == 0UL ? 0x123456789abcdef0UL : seed;
        }

        /// <summary>返回 [0,1) 伪随机数。</summary>
        public float Next()
        {
            // xorshift64
            ulong x = _rng;
            x ^= x << 13;
            x ^= x >> 7;
            x ^= x << 17;
            _rng = x;
            // 取高 24 位映射到 [0,1)
            return (x >> 40) / (float)(1 << 24);
        }

        /// <summary>读取当前随机流状态（存档）。</summary>
        public ulong GetRng() => _rng;

        /// <summary>恢复随机流状态（读档）。</summary>
        public void SetRng(ulong value) => _rng = value;

        /// <summary>返回 [min,max) 浮点。</summary>
        public float Range(float min, float max) => min + (max - min) * Next();
    }
}
