using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Settlement
{
    /// <summary>
    /// 聚落插件（Tier-1）。新游戏为每个势力建立首都；年度驱动聚落人口增长、
    /// 发展度累积与自动升级；注册 ISettlementService。
    /// 依赖 economy/population，保证发展在年度经济、人口结算之后。
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
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.Save),
            };

        private IPluginContext _ctx;
        private SettlementService _service;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "聚落系统";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;
        public SimulationLayer Layer => SimulationLayer.P1Tactical;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            var reg = _ctx.Services.Resolve<SettlementRegistry>();
            _service = new SettlementService(reg, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<ISettlementService>(_service);

            // 新游戏：为每个势力建立首都（读档路径随后由 RestoreSaveSegment 覆盖）
            if (_ctx.Services.TryResolve<FactionRegistry>(out var factions))
            {
                int year = ResolveGameYear();
                _service.FoundInitialCapitals(factions, year);
            }
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
            // 聚落发展按年结算；建造/视图反馈由对应系统处理。
        }

        public void OnYear(int gameYear)
        {
            if (!_initialized) return;
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
            var reg = _ctx.Services.Resolve<SettlementRegistry>();
            var live = new List<int>();
            int cap = reg.Capacity;
            for (int i = 0; i < cap; i++)
                if (reg.IsAlive(i, reg.GetGeneration(i))) live.Add(i);

            int n = live.Count;
            var seg = new SettlementSaveSegment
            {
                PluginId = PluginIdValue,
                Count = n,
                Slots = new int[n],
                Factions = new int[n],
                Kinds = new int[n],
                Names = new string[n],
                Positions = new UnityEngine.Vector3[n],
                Levels = new int[n],
                Populations = new int[n],
                FoundedYears = new int[n],
                Prosperities = new float[n],
            };

            for (int i = 0; i < n; i++)
            {
                ref var r = ref reg.GetSlotRef(live[i]);
                seg.Slots[i] = live[i];
                seg.Factions[i] = r.Faction;
                seg.Kinds[i] = r.Kind;
                seg.Names[i] = r.Name;
                seg.Positions[i] = r.Position;
                seg.Levels[i] = r.Level;
                seg.Populations[i] = r.Population;
                seg.FoundedYears[i] = r.FoundedYear;
                seg.Prosperities[i] = r.Prosperity;
            }
            return seg;
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is SettlementSaveSegment s)) return;
            var reg = _ctx.Services.Resolve<SettlementRegistry>();
            reg.Clear();

            for (int i = 0; i < s.Count; i++)
            {
                int slot = reg.Found(
                    s.Factions[i],
                    (SettlementKind)s.Kinds[i],
                    s.Names[i],
                    s.Positions[i],
                    s.FoundedYears[i],
                    s.Populations[i],
                    s.Prosperities[i]);
                ref var r = ref reg.GetSlotRef(slot);
                r.Level = s.Levels[i];
            }

            _service = new SettlementService(reg, _ctx.Services, _ctx.Log);
            _ctx.Services.Register<ISettlementService>(_service);
            _initialized = true;
        }

        private int ResolveGameYear()
        {
            if (_ctx.Services.TryResolve<IClock>(out var clock))
                return clock.GameYear;
            return 0;
        }
    }
}
