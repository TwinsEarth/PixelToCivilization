using PxC.Bootstrap;
using PxC.Context;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Core.Time;
using PxC.Infra.Save;
using UnityEngine;

namespace PxC.Tests.PlayMode
{
    /// <summary>
    /// PlayMode 测试宿主：镜像 GameLoop 的驱动顺序，但不依赖 MonoBehaviour，
    /// 让冒烟测试可以手动、确定性地推进世界。不写业务规则，只做编排。
    /// </summary>
    public sealed class PlayModeTestHost
    {
        private readonly BootstrapTestContext _ctx;
        private float _savedFrameBudget;

        public PlayModeTestHost()
        {
            _ctx = GameBootstrap.TestContext;
            if (_ctx == null) return;

            // 测试环境隔离（关键）：PlayMode 测试共享一个播放会话，GameBootstrap.Boot
            // 只执行一次。测试推进年份时自动存档（OnYearAdvanced → MainSlot）会残留
            // persistentDataPath，导致后续测试启动走"恢复存档"路径（GameYear 非开局年）。
            // 每个测试构造时清除 MainSlot 并复位时钟，保证"新游戏"断言（4700）稳定。
            if (_ctx.Services.TryResolve<ISaveService>(out var saves))
                saves.Delete(SaveCoordinator.MainSlot);
            _ctx.Clock.Restore(GameClock.StartGameYear, 0f);
        }

        /// <summary>宿主是否已成功启动（安全模式/装配失败时为 false）。</summary>
        public bool Ready => _ctx != null;

        /// <summary>启动后的测试上下文。</summary>
        public BootstrapTestContext Context => _ctx;

        public int GameYear => _ctx.Clock.Year;

        public int PluginCount => _ctx.Runtime.PluginCount;

        /// <summary>解析服务（缺失抛异常，与生产口径一致）。</summary>
        public T Resolve<T>() where T : class => _ctx.Services.Resolve<T>();

        /// <summary>尝试解析服务。</summary>
        public bool TryResolve<T>(out T service) where T : class =>
            _ctx.Services.TryResolve(out service);

        /// <summary>
        /// 推进一帧（镜像 GameLoop.Update）：时钟年结 → 模拟 → 视图 → 派发事件。
        /// </summary>
        public void Step(float realDt)
        {
            // 1. 时钟推进（年结在本步内同步广播 OnYear），取本帧推进的模拟时间
            _ctx.Clock.Advance(realDt, y => _ctx.Runtime.TickYear(y), out float simDt);

            // 2. 模拟（测试中全量 tick，绕过算力分层与帧预算），用模拟时间
            var sims = _ctx.Runtime.SimPlugins;
            for (int i = 0; i < sims.Count; i++)
                _ctx.Runtime.TickSim(sims[i], simDt);

            // 3. 视图
            _ctx.Runtime.TickView(realDt);

            // 4. 统一派发延迟事件
            _ctx.Events.DispatchPending();
        }

        /// <summary>按真实帧间隔逐帧推进（用于移动安全/行为测试）。</summary>
        public void StepFrames(int frames, float frameDt = 0.016f)
        {
            for (int i = 0; i < frames; i++) Step(frameDt);
        }

        /// <summary>
        /// 快速推进 N 个游戏年（解除自适应预算限制，用于年度聚合/存档测试）。
        /// 返回实际推进的年数。
        /// </summary>
        public int AdvanceYears(int years)
        {
            int start = _ctx.Clock.Year;
            _savedFrameBudget = _ctx.Clock.SimFrameBudgetSec;
            _ctx.Clock.SimFrameBudgetSec = 100000f;

            try
            {
                // 1 游戏年 = 60 真实秒（GameClock.DaySeconds = 365/60）。
                // 每步推进略超 1 年（60.1s），年结恰好触发 1 次；循环 years 步精确到目标年份。
                // 浮点余量约 0.6 天/步，50 年累计约 30 天 < 365，不会额外跨年。
                float stepDt = GameClock.YearDays / GameClock.DaySeconds + 0.1f; // ≈ 60.1s
                int guard = 0;
                while (_ctx.Clock.Year < start + years && guard < 100000)
                {
                    Step(stepDt);
                    guard++;
                }
            }
            finally
            {
                _ctx.Clock.SimFrameBudgetSec = _savedFrameBudget;
            }

            return _ctx.Clock.Year - start;
        }
    }
}
