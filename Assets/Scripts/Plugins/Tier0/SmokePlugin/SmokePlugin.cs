using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Persistence;

namespace PxC.Plugin.Smoke
{
    /// <summary>
    /// 最小冒烟插件：同时实现 ISimPlugin（P0Core）与 IViewPlugin，
    /// 用于验证宿主发现→拓扑→初始化→Tick→年结→卸载的完整链路。
    /// </summary>
    public sealed class SmokePlugin : ISimPlugin, IViewPlugin
    {
        // ---- 身份 ----
        public string Id => "core.smoke";
        public string DisplayName => "冒烟插件";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;
        public SimulationLayer Layer => SimulationLayer.P0Core;

        // ---- 依赖 / 能力（无）----
        public IReadOnlyList<PluginDependency> Dependencies => null;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => null;

        private IPluginContext _ctx;
        private int _ticks;
        private int _years;

        // ---- 生命周期 ----
        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize() => _ctx?.Log("SmokePlugin 已初始化");

        public void OnShutdown() => _ctx?.Log("SmokePlugin 已卸载");

        // ---- 模拟 ----
        public void OnTick(float simDt) => _ticks++;

        public void OnYear(int gameYear)
        {
            _years++;
            if (_years % 100 == 0)
                _ctx?.Log($"SmokePlugin 已观测 {_years} 个游戏年");
        }

        public void OnEra(int newEra, int oldEra) =>
            _ctx?.Log($"SmokePlugin 时代切换 {oldEra} -> {newEra}");

        public void OnDynasty(int dynastyIndex, int gameYear)
        {
            // 冒烟插件不处理朝代，仅演示回调存在
        }

        // ---- 视图 ----
        public void OnViewReady() { }
        public void OnViewTick(float realDt) { }
        public void OnViewRelease() { }

        // ---- 存档（冒烟无数据）----
        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }

        public int ObservedTicks => _ticks;
        public int ObservedYears => _years;
    }
}
