using System;
using UnityEngine;

namespace PxC.Core.Time
{
    /// <summary>
    /// 游戏时钟：纯数据/逻辑，由 GameLoop 驱动（不是 MonoBehaviour）。
    /// 1 倍速下 1 游戏年 = 60 真实秒；Day 满 365 为 1 年。
    /// 高倍速/掉帧下采用自适应工作量预算：单帧年结超过预算即停，剩余 Day 结转下帧。
    /// </summary>
    public sealed class GameClock
    {
        public const int YearDays = 365;
        public const float DaySeconds = 365f / 60f;   // 1x 下 1 年 ≈ 60 秒
        public const int StartGameYear = 4700;        // 公元 1700
        public const int HardCapTicks = 120;          // 单帧年结硬上限兜底

        public int Year { get; private set; }
        public float Day { get; private set; }
        public float Speed { get; set; } = 1f;
        public bool Running { get; set; } = true;
        public bool Paused { get; set; }

        public int LastFrameTicks { get; private set; }
        public float AvgYearTickMs { get; private set; }

        public float SimFrameBudgetSec { get; set; } =
#if UNITY_WEBGL && !UNITY_EDITOR
            0.006f;
#else
            0.010f;
#endif

        /// <summary>可替换的真实时间提供者（测试用），默认 Time.realtimeSinceStartup。</summary>
        public Func<float> RealtimeProvider { get; set; } = () => UnityEngine.Time.realtimeSinceStartup;

        public float EffectiveSpeed => !Running || Paused ? 0f : Speed;

        public float YearProgress => Day / YearDays;

        /// <summary>
        /// 推进时间；每跨一个游戏年调用一次 onYear(新游戏年)。
        /// 按自适应工作量预算在帧预算内停止。
        /// </summary>
        public void Advance(float realDt, Action<int> onYear)
        {
            LastFrameTicks = 0;
            if (!Running || Paused) return;

            Day += realDt * EffectiveSpeed * DaySeconds;
            float frameStart = RealtimeProvider();
            int did = 0;
            while (Day >= YearDays && did < HardCapTicks)
            {
                Day -= YearDays;
                Year++;
                onYear?.Invoke(Year);
                did++;
                if (RealtimeProvider() - frameStart >= SimFrameBudgetSec) break;
            }
            if (did > 0)
            {
                float oneMs = (RealtimeProvider() - frameStart) * 1000f / did;
                AvgYearTickMs = AvgYearTickMs <= 0f ? oneMs : AvgYearTickMs * 0.85f + oneMs * 0.15f;
                LastFrameTicks = did;
            }
        }

        /// <summary>开局静默对齐到公元 1700（游戏年 4700），不触发任何年结事件。</summary>
        public void SnapToStart()
        {
            Year = StartGameYear;
            Day = 0f;
        }

        /// <summary>直接设置（读档用）。</summary>
        public void Restore(int year, float day)
        {
            Year = year;
            Day = day;
        }
    }
}
