using System;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Disaster
{
    /// <summary>
    /// 灾害领域状态（纯数据）。持有当前灾害、严重度、年份，
    /// 连续无雨年数（干旱触发），以及一个可复现随机流。
    /// </summary>
    public sealed class DisasterState
    {
        public int Active;            // DisasterType
        public float Severity;
        public int ActiveYear;
        public int DryYears;          // 连续无降水年数
        public bool Initialized;

        private ulong _rng;

        public DisasterState()
        {
            Active = (int)DisasterType.None;
            Severity = 0f;
            ActiveYear = 0;
            DryYears = 0;
            Initialized = false;
            _rng = 0xabcdef0123456789UL;
        }

        public void Seed(ulong seed)
        {
            _rng = seed == 0UL ? 0xabcdef0123456789UL : seed;
        }

        public float Next()
        {
            ulong x = _rng;
            x ^= x << 13;
            x ^= x >> 7;
            x ^= x << 17;
            _rng = x;
            return (x >> 40) / (float)(1 << 24);
        }

        /// <summary>返回 [min,max) 浮点。</summary>
        public float Range(float min, float max) => min + (max - min) * Next();

        public ulong GetRng() => _rng;
        public void SetRng(ulong v) => _rng = v;
    }
}
