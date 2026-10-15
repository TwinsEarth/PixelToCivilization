using System;
using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Disaster
{
    /// <summary>
    /// 灾害插件（Tier-1）。订阅 WeatherChangedEvent 检测洪水/干旱/地震/瘟疫/台风/寒潮，
    /// 施加人口/建筑/粮食影响，注册 IDisasterService。
    /// 与天气为松耦合（事件驱动），无天气插件时不触发灾害。
    /// </summary>
    public sealed class DisasterPlugin : IPlugin
    {
        public const string PluginIdValue = "pxc.disaster";

        private const ulong NewGameSeed = 0x517cc1b727220a95UL;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(0);

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.Save),
            };

        private IPluginContext _ctx;
        private DisasterState _state;
        private DisasterService _service;
        private Action<WeatherChangedEvent> _onWeather;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "灾害系统";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            _state = new DisasterState();
            _state.Seed(NewGameSeed);
            _service = new DisasterService(_state, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<IDisasterService>(_service);

            // 订阅当年天气确认（同步回调，时序与 OnYear 顺序无关）。
            _onWeather = _service.OnWeatherChanged;
            _ctx.Events.Subscribe(_onWeather);

            _initialized = true;
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            if (_onWeather != null) _ctx.Events.Unsubscribe(_onWeather);
            _onWeather = null;
            _ctx.Services.Unregister<IDisasterService>();
            _initialized = false;
        }

        public ISaveSegment GetSaveSegment()
        {
            if (_state == null) return null;
            return new DisasterSaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                Active = _state.Active,
                Severity = _state.Severity,
                ActiveYear = _state.ActiveYear,
                DryYears = _state.DryYears,
                RngState = _state.GetRng(),
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is DisasterSaveSegment s)) return;
            _state = new DisasterState();
            _state.Active = s.Active;
            _state.Severity = s.Severity;
            _state.ActiveYear = s.ActiveYear;
            _state.DryYears = s.DryYears;
            _state.SetRng(s.RngState);
            _state.Initialized = true;
            _service = new DisasterService(_state, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<IDisasterService>(_service);

            _onWeather = _service.OnWeatherChanged;
            _ctx.Events.Subscribe(_onWeather);
            _initialized = true;
        }
    }
}
