using PxC.Bootstrap;
using PxC.Core.Time;
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
            // 1. 时钟推进（年结在本步内同步广播 OnYear）
            _ctx.Clock.Advance(realDt, y => _ctx.Runtime.TickYear(y));

            // 2. 模拟（测试中全量 tick，绕过算力分层与帧预算）
            var sims = _ctx.Runtime.SimPlugins;
            for (int i = 0; i < sims.Count; i++)
                _ctx.Runtime.TickSim(sims[i], realDt);

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
                // 大 realDt 一步可跨多年；HardCapTicks 兜底，故循环直到目标年份
                int guard = 0;
                while (_ctx.Clock.Year < start + years && guard < 100000)
                {
                    Step(1000f);
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
