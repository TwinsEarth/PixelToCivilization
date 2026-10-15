using System;
using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.Time;
using UnityEngine;

namespace PxC.Host
{
    /// <summary>
    /// 极薄主循环：只负责驱动顺序，不含业务规则。
    /// 顺序：帧预算开始 → 时钟推进(年结) → 算力分层 Tick 模拟 → 视图 Tick →
    ///       派发事件 → 每帧外部服务(内存/存档)。
    /// </summary>
    public sealed class GameLoop : MonoBehaviour
    {
        private GameClock _clock;
        private PluginRuntime _runtime;
        private IEventBus _events;
        private IFrameBudget _budget;
        private ISimScheduler _scheduler;
        private readonly List<Action<float>> _perFrame = new List<Action<float>>(8);
        private readonly List<Action> _onQuit = new List<Action>(4);
        private bool _configured;

        public void Configure(
            GameClock clock,
            PluginRuntime runtime,
            IEventBus events,
            IFrameBudget budget,
            ISimScheduler scheduler)
        {
            _clock = clock;
            _runtime = runtime;
            _events = events;
            _budget = budget;
            _scheduler = scheduler;
            _configured = true;
        }

        /// <summary>注册每帧需更新的外部逻辑（如内存监控、自动存档）。</summary>
        public void AddPerFrame(Action<float> action)
        {
            if (action != null && !_perFrame.Contains(action)) _perFrame.Add(action);
        }

        /// <summary>注册应用退出回调（如标记正常运行、最终存档）。</summary>
        public void AddQuitHandler(Action action)
        {
            if (action != null && !_onQuit.Contains(action)) _onQuit.Add(action);
        }

        private void OnApplicationQuit()
        {
            for (int i = 0; i < _onQuit.Count; i++)
                _onQuit[i]();
        }

        private void Update()
        {
            if (!_configured) return;
            float realDt = Time.deltaTime;

            _budget?.BeginFrame();

            // 1. 时钟推进（年结回调在本帧内同步广播 OnYear）
            _clock.Advance(realDt, OnYear);

            // 2. 模拟（算力分层 + 帧预算）
            TickSim(realDt);

            // 3. 视图（真实时间）
            _runtime.TickView(realDt);

            // 4. 统一派发延迟事件
            _events.DispatchPending();

            // 5. 外部每帧服务
            for (int i = 0; i < _perFrame.Count; i++)
                _perFrame[i](realDt);
        }

        private void OnYear(int gameYear) => _runtime.TickYear(gameYear);

        private void TickSim(float realDt)
        {
            var sims = _runtime.SimPlugins;
            for (int i = 0; i < sims.Count; i++)
            {
                var p = sims[i];
                float hz = _scheduler.EffectiveHz(p.Layer);
                if (!_scheduler.ShouldTick(p.Layer, realDt, hz)) continue;
                if (_budget != null && !_budget.HasBudget(0.1f)) break;

                float t0 = Time.realtimeSinceStartup;
                _runtime.TickSim(p, realDt);
                float used = (Time.realtimeSinceStartup - t0) * 1000f;
                _budget?.Consume(used);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // 切后台/暂停时立即 flush 事件（崩溃/存档系统可在此做检查点）
                _events?.DispatchPending();
            }
        }
    }
}
