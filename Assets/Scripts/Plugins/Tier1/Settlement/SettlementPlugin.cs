using System;
using System.Collections.Generic;
using UnityEngine;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Settlement
{
    /// <summary>
    /// 聚落插件（Tier-1，v10.4.0 治本统一）。建立首都、维护聚落发展度/升级；
    /// 人口已统一到 population 插件，故本插件不保存/恢复人口（人口由 population 段
    /// 按 settlement×layer 独立序列化）。
    /// </summary>
    public sealed class SettlementPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.settlement";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(2)
            {
                new PluginDependency("pxc.economy"),
                new PluginDependency("pxc.population"),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(2)
            {
                new CapabilityClaim(Capability.Save),
                new CapabilityClaim(Capability.SpawnEntity),
            };

        private IPluginContext _ctx;
        private SettlementRegistry _registry;
        private SettlementService _service;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "聚落系统";
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
            _registry = _ctx.Services.Resolve<SettlementRegistry>();
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            _service = new SettlementService(_registry, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<ISettlementService>(_service);

            // 新游戏：建立首都（内部 BindPopulation + SeedCapital 写入统一人口）。
            int gameYear = 0;
            var clock = _ctx.Services.Resolve<IClock>();
            if (clock != null) gameYear = clock.GameYear;
            _service.FoundInitialCapitals(factions, gameYear);
            _initialized = true;
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<ISettlementService>();
            _initialized = false;
        }

        public void OnTick(float simDt)
        {
            // 聚落按年结算。
        }

        public void OnYear(int gameYear)
        {
            if (!_initialized) return;
            // population 已先结算人口；此处只做发展度与升级。
            _service.AdvanceYear(gameYear);
        }

        public void OnEra(int newEra, int oldEra)
        {
        }

        public void OnDynasty(int dynastyIndex, int gameYear)
        {
        }

        public ISaveSegment GetSaveSegment()
        {
            if (_registry == null) return null;

            // 收集存活聚落（按 slot 顺序，保证恢复时顺序 Found 对齐）。
            var slots = new List<int>();
            var factions = new List<int>();
            var kinds = new List<int>();
            var names = new List<string>();
            var positions = new List<Vector3>();
            var levels = new List<int>();
            var foundedYears = new List<int>();
            var prosperities = new List<float>();

            int cap = _registry.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_registry.IsAlive(slot, _registry.GetGeneration(slot))) continue;
                var r = _registry.GetSafe(slot);
                slots.Add(slot);
                factions.Add(r.Faction);
                kinds.Add(r.Kind);
                names.Add(r.Name ?? string.Empty);
                positions.Add(r.Position);
                levels.Add(r.Level);
                foundedYears.Add(r.FoundedYear);
                prosperities.Add(r.Prosperity);
            }

            return new SettlementSaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 2,
                Count = slots.Count,
                Slots = slots.ToArray(),
                Factions = factions.ToArray(),
                Kinds = kinds.ToArray(),
                Names = names.ToArray(),
                Positions = positions.ToArray(),
                Levels = levels.ToArray(),
                FoundedYears = foundedYears.ToArray(),
                Prosperities = prosperities.ToArray(),
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is SettlementSaveSegment s)) return;

            _registry = _ctx.Services.Resolve<SettlementRegistry>();
            _registry.Clear();

            // 顺序 Found：Clear 后 slot 从 0 顺序分配，与 population 段的 slot 布局对齐。
            // 人口不在这里恢复（由 population 段按 settlement×layer 恢复）。
            for (int i = 0; i < s.Count; i++)
            {
                int slot = _registry.Found(
                    s.Factions[i],
                    (SettlementKind)s.Kinds[i],
                    s.Names[i] ?? string.Empty,
                    s.Positions[i],
                    s.FoundedYears[i],
                    s.Prosperities[i]);
                if (s.Slots != null && i < s.Slots.Length && s.Slots[i] != slot)
                    _ctx.Log($"[聚落] 恢复槽位 {s.Slots[i]} → {slot}（顺序重排，人口已对齐）");
            }

            _service = new SettlementService(_registry, _ctx.Services, _ctx.Log);
            _service.BindPopulation();
            _ctx.Services.Register<ISettlementService>(_service);
            _initialized = true;
        }
    }
}
