using System;
using UnityEngine;

namespace PixelToCivilization.Rendering
{
    /// <summary>算力优先级层（V9.9.0）：P0 核心模拟 / P1 战术层 / P2 生态装饰层。</summary>
    public enum SimLayer { P0Core = 0, P1Tactical = 1, P2Ambient = 2 }

    /// <summary>
    /// 算力优先级分层（V9.9.0）：
    /// · P0Core    核心模拟（时间推进/存档心跳/战斗结算）——永远执行，不做节流。
    /// · P1Tactical战术层（寻路/编队/索敌/塔防）——画质降档或帧耗紧张时按 1Hz 决策节流。
    /// · P2Ambient 生态/装饰层（鸟鱼寿命补员、天气粒子、LOD 刷新）——按 AmbientHz 低频执行，
    ///   高负载直接跳过（SkipCount 统计），是"按需加载/内存回收"在 CPU 侧的镜像。
    /// 设计目标：WebGL 单线程下，把每帧单位时间内的计算量按重要性分层收敛，
    /// 保证"世界无限大、设备负担恒定"——负载高时先牺牲 P2/P1，P0 永不被拖垮。
    /// </summary>
    public static class SimPriority
    {
        // ---------- 分层节流 ----------
        /// <summary>P2 生态层执行间隔（秒）：High 0.25s(4Hz) / Mid 0.5s(2Hz) / Low 1s(1Hz)。</summary>
        public static float AmbientHz =>
            FrameBudget.Level == PerfLevel.High ? 0.25f :
            FrameBudget.Level == PerfLevel.Mid ? 0.5f : 1f;

        /// <summary>P2 层低频闸门：到点返回 true 并更新节流时间戳；未到点返回 false（调用方应跳过本帧该块）。</summary>
        public static bool AmbientTick(ref float lastAt)
        {
            float now = Time.unscaledTime;
            if (now - lastAt < AmbientHz) return false;
            lastAt = now;
            return true;
        }

        /// <summary>P1 战术层闸门（1Hz 决策节流；Low 档再降为 2s 一次）。</summary>
        public static bool TacticalTick(ref float lastAt)
        {
            float interval = FrameBudget.Level == PerfLevel.Low ? 2f : 1f;
            float now = Time.unscaledTime;
            if (now - lastAt < interval) return false;
            lastAt = now;
            return true;
        }

        // ---------- 统计（性能神/探针用） ----------
        public static long AmbientSkipped;      // P2 被跳过帧次数（高负载牺牲）
        public static long LayeredP2Swallowed;  // P2 异常被吞次数
        public static long LayeredP1Recovered;  // P1 异常单次跳过并恢复次数
        public static long LayeredP0Fatal;      // P0 异常整局安全模式次数
    }
}

namespace PixelToCivilization.Core
{
    /// <summary>
    /// V9.9.0 分级分层崩溃设计（挂载于 CrashGuardSystem）：
    /// 把"整局安全模式"降级为三级分层——
    /// · P0Core：模拟核心异常 → 现有整局安全模式（自动存档 + 降级运行 + 恢复引导）。
    /// · P1Tactical：子系统单次 tick 异常 → 跳过本次、记录并计数；连续 5 次再升级整局安全模式。
    /// · P2Ambient：表现/生态层异常 → 吞掉并计数，绝不影响模拟。
    /// 目的：偶发的一处表现层崩溃不再拖垮整个文明模拟（"允许倒退不允许灭绝"的工程镜像）。
    /// </summary>
    public static class LayeredGuard
    {
        const int P1EscalateThreshold = 5;
        static int _p1Consecutive;
        static string _lastP1Name = "";

        /// <summary>按层执行受保护代码块。异常不抛出：P2 吞掉；P1 跳过本帧；P0 走整局安全模式。</summary>
        public static void Run(string name, Rendering.SimLayer layer, Action body)
        {
            try { body(); return; }
            catch (Exception e)
            {
                if (layer == Rendering.SimLayer.P2Ambient)
                {
                    Rendering.SimPriority.LayeredP2Swallowed++;
                    Debug.LogWarning("[LayeredGuard] P2 表现层异常已吞（" + name + "）: " + e.Message);
                    return;
                }
                if (layer == Rendering.SimLayer.P1Tactical)
                {
                    Rendering.SimPriority.LayeredP1Recovered++;
                    _p1Consecutive = (name == _lastP1Name) ? _p1Consecutive + 1 : 1;
                    _lastP1Name = name;
                    Debug.LogWarning("[LayeredGuard] P1 战术层异常已跳过本次（" + name + "）: " + e.Message);
                    if (_p1Consecutive >= P1EscalateThreshold)
                    {
                        Debug.LogError("[LayeredGuard] " + name + " 连续 " + P1EscalateThreshold + " 次异常，升级整局安全模式");
                        EscalateFatal(name, e);
                    }
                    return;
                }
                // P0 核心异常：走整局安全模式（CrashGuardSystem 已有自动存档/降级/恢复）
                Rendering.SimPriority.LayeredP0Fatal++;
                EscalateFatal(name, e);
            }
        }

        static void EscalateFatal(string name, Exception e)
        {
            CrashGuardSystem.LastCrashSummary = name + " : " + e.Message;
            Debug.LogError("[LayeredGuard] P0 核心异常触发安全模式（" + name + "）: " + e);
        }

        /// <summary>探针/性能神摘要。</summary>
        public static string Report()
        {
            return "[Layered] p0fatal=" + Rendering.SimPriority.LayeredP0Fatal +
                   " p1recover=" + Rendering.SimPriority.LayeredP1Recovered +
                   " p2swallow=" + Rendering.SimPriority.LayeredP2Swallowed +
                   " p2skip=" + Rendering.SimPriority.AmbientSkipped +
                   " ambientHz=" + Rendering.SimPriority.AmbientHz.ToString("F2") +
                   " frameMs=" + FrameBudgetMs.ToString("F1");
        }

        /// <summary>帧耗 EWMA（毫秒，性能神量化：掉帧 → P2 跳过增多）。</summary>
        static float _fbMs = -1f;
        public static float FrameBudgetMs { get { UpdateFrameMs(); return _fbMs; } }
        static float _fbLast = -999f;
        static void UpdateFrameMs()
        {
            float now = Time.unscaledTime;
            if (now - _fbLast < 0.5f) return;   // 0.5s 采样一次
            _fbLast = now;
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;
            float ms = dt * 1000f;
            _fbMs = _fbMs < 0f ? ms : _fbMs * 0.8f + ms * 0.2f;
        }
    }
}
