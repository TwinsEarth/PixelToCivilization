using System;
using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;

namespace PxC.Plugins.History
{
    /// <summary>
    /// 历史插件（Tier-1，对应"小时"）。订阅灾害/饥荒/政策/文化/时代/朝代/战争事件，
    /// 环形记录，注册 IHistoryService。
    /// </summary>
    public sealed class HistoryPlugin : IPlugin
    {
        public const string PluginIdValue = "pxc.history";

        private const int BufferCapacity = 256;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(0);

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.Save),
            };

        private IPluginContext _ctx;
        private HistoryState _state;
        private HistoryService _service;
        private bool _initialized;

        // 事件委托（保存引用以便反订阅）
        private Action<DisasterOccurredEvent> _onDisaster;
        private Action<FamineEvent> _onFamine;
        private Action<PolicyChangedEvent> _onPolicy;
        private Action<CultureLevelUpEvent> _onCulture;
        private Action<EraChangedEvent> _onEra;
        private Action<DynastyChangedEvent> _onDynasty;
        private Action<WarDeclaredEvent> _onWar;
        private Action<VictoryEvent> _onVictory;

        public string Id => PluginIdValue;
        public string DisplayName => "历史记录";
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
            _state = new HistoryState(BufferCapacity);
            _service = new HistoryService(_state, _ctx.Log);
            _ctx.Services.Register<IHistoryService>(_service);

            var bus = _ctx.Events;
            _onDisaster = OnDisaster;
            _onFamine = OnFamine;
            _onPolicy = OnPolicy;
            _onCulture = OnCulture;
            _onEra = OnEra;
            _onDynasty = OnDynasty;
            _onWar = OnWar;
            _onVictory = OnVictory;
            bus.Subscribe(_onDisaster);
            bus.Subscribe(_onFamine);
            bus.Subscribe(_onPolicy);
            bus.Subscribe(_onCulture);
            bus.Subscribe(_onEra);
            bus.Subscribe(_onDynasty);
            bus.Subscribe(_onWar);
            bus.Subscribe(_onVictory);

            _initialized = true;
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            var bus = _ctx.Events;
            bus.Unsubscribe(_onDisaster);
            bus.Unsubscribe(_onFamine);
            bus.Unsubscribe(_onPolicy);
            bus.Unsubscribe(_onCulture);
            bus.Unsubscribe(_onEra);
            bus.Unsubscribe(_onDynasty);
            bus.Unsubscribe(_onWar);
            bus.Unsubscribe(_onVictory);
            _ctx.Services.Unregister<IHistoryService>();
            _initialized = false;
        }

        // ------------------------------------------------------------
        // 事件 → 历史
        // ------------------------------------------------------------
        private void OnDisaster(DisasterOccurredEvent e)
        {
            if (e.Type == (int)DisasterType.None) return;
            _service.Record(HistoryKind.Disaster, e.GameYear, e.Faction,
                e.Deaths, e.BuildingsDestroyed, ((DisasterType)e.Type).ToString());
        }

        private void OnFamine(FamineEvent e)
        {
            _service.Record(HistoryKind.Famine, e.GameYear, e.Faction,
                (int)Math.Ceiling(e.Shortfall), 0, "饥荒");
        }

        private void OnPolicy(PolicyChangedEvent e)
        {
            _service.Record(HistoryKind.Policy, e.GameYear, e.Faction,
                e.NewPolicy, 0, ((PolicyType)e.NewPolicy).ToString());
        }

        private void OnCulture(CultureLevelUpEvent e)
        {
            _service.Record(HistoryKind.Culture, e.GameYear, e.Faction,
                e.NewLevel, 0, "文化繁荣");
        }

        private void OnEra(EraChangedEvent e)
        {
            // 时代为全局事件，无势力字段。
            _service.Record(HistoryKind.Tech, e.GameYear, -1,
                e.NewEra, 0, ((Era)e.NewEra).ToString());
        }

        private void OnDynasty(DynastyChangedEvent e)
        {
            _service.Record(HistoryKind.Dynasty, e.GameYear, -1,
                e.DynastyIndex, 0, "朝代更替");
        }

        private void OnWar(WarDeclaredEvent e)
        {
            int year = ResolveYear();
            _service.Record(HistoryKind.War, year, -1,
                e.AttackerFaction, e.DefenderFaction, "战争");
        }

        private void OnVictory(VictoryEvent e)
        {
            _service.Record(HistoryKind.Victory, e.GameYear, e.Faction,
                e.Type, 0, e.VictoryName);
        }

        public ISaveSegment GetSaveSegment()
        {
            if (_state == null) return null;
            return new HistorySaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                Entries = _state.ToArray(),
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is HistorySaveSegment s)) return;
            _state = new HistoryState(BufferCapacity);
            _state.Restore(s.Entries);
            _service = new HistoryService(_state, _ctx.Log);
            _ctx.Services.Register<IHistoryService>(_service);

            var bus = _ctx.Events;
            _onDisaster = OnDisaster;
            _onFamine = OnFamine;
            _onPolicy = OnPolicy;
            _onCulture = OnCulture;
            _onEra = OnEra;
            _onDynasty = OnDynasty;
            _onWar = OnWar;
            _onVictory = OnVictory;
            bus.Subscribe(_onDisaster);
            bus.Subscribe(_onFamine);
            bus.Subscribe(_onPolicy);
            bus.Subscribe(_onCulture);
            bus.Subscribe(_onEra);
            bus.Subscribe(_onDynasty);
            bus.Subscribe(_onWar);
            bus.Subscribe(_onVictory);
            _initialized = true;
        }

        private int ResolveYear()
        {
            if (_ctx.Services.TryResolve<IClock>(out var clock))
                return clock.GameYear;
            return 0;
        }
    }
}
