using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Weather
{
    /// <summary>
    /// 天气插件（Tier-1，对应"小天"）。年主导天气：晴/云/雨/雪/风/雾/暴风雨，
    /// 提供 IWeatherService 给 Economy（农业系数）、Disaster（灾害触发）、（远期）军事。
    /// 时序：天气提前一年生成，保证被依赖的 Economy 在 OnYear 取到当年系数。
    /// </summary>
    public sealed class WeatherPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.weather";

        private const ulong NewGameSeed = 0x9e3779b97f4a7c15UL;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(0);

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.Save),
            };

        private IPluginContext _ctx;
        private WeatherState _state;
        private WeatherService _service;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "天气系统";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;
        public SimulationLayer Layer => SimulationLayer.P2Ambient;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            int startYear = ResolveStartYear();
            _state = new WeatherState();
            _state.Seed(NewGameSeed);
            _service = new WeatherService(_state, _ctx.Services, _ctx.Log);
            _service.Initialize(startYear);
            _ctx.Services.Register<IWeatherService>(_service);
            _initialized = true;
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<IWeatherService>();
            _initialized = false;
        }

        public void OnTick(float simDt)
        {
            // 天气按年推进；短期可视化（雨雪粒子）由视图层按需播放。
        }

        public void OnYear(int gameYear)
        {
            if (!_initialized) return;

            // 保险：若当前年与游戏年错位（正常不会），先补齐到当前年。
            int guard = 0;
            while (_service.CurrentYear < gameYear && guard < 8)
            {
                _service.AdvanceTo(_service.CurrentYear + 1);
                guard++;
            }

            // 发布"当年天气确认"：Disaster 在同步回调中检测（此时 current 尚未 advance）。
            _ctx.Events.Publish(new WeatherChangedEvent
            {
                NewWeather = _state.Condition,
                Intensity = _state.Intensity,
                GameYear = gameYear,
            });

            // 提前为下一年备好（Economy 下一年 OnYear 取到的就是下一年系数）。
            _service.AdvanceTo(gameYear + 1);
            _ctx.Log($"[天气] {gameYear + 1} 年天气：{_service.Describe()}");
        }

        public void OnEra(int newEra, int oldEra)
        {
        }

        public void OnDynasty(int dynastyIndex, int gameYear)
        {
        }

        public ISaveSegment GetSaveSegment()
        {
            if (_state == null) return null;
            return new WeatherSaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                Condition = _state.Condition,
                Intensity = _state.Intensity,
                CurrentYear = _state.CurrentYear,
                RngState = _state.GetRng(),
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is WeatherSaveSegment s)) return;
            _state = new WeatherState();
            _state.Condition = s.Condition;
            _state.Intensity = s.Intensity;
            _state.CurrentYear = s.CurrentYear;
            _state.SetRng(s.RngState);
            _state.Initialized = true;
            _service = new WeatherService(_state, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<IWeatherService>(_service);
            _initialized = true;
        }

        private int ResolveStartYear()
        {
            if (_ctx.Services.TryResolve<IClock>(out var clock))
                return clock.GameYear;
            return 0;
        }
    }
}
