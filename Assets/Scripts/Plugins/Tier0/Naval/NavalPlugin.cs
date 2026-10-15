using System.Collections.Generic;
using UnityEngine;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Naval
{
    /// <summary>
    /// 海军插件（Tier-0）。船艇的生成、10 维参数、海上自主 AI、集结与存档。
    /// 船艇只在水域内活动，永不上岸。
    /// </summary>
    public sealed class NavalPlugin : IPlugin, ISimPlugin
    {
        public const string PluginIdValue = "pxc.naval";

        public string Id => PluginIdValue;
        public string DisplayName => "海军";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;
        public SimulationLayer Layer => SimulationLayer.P1Tactical;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        private static readonly PluginDependency[] _deps =
        {
            new PluginDependency("pxc.economy", 1),
            new PluginDependency("pxc.tech", 1),
        };

        private static readonly CapabilityClaim[] _caps =
        {
            new CapabilityClaim(Capability.SpawnEntity),
        };

        private IPluginContext _ctx;
        private UnitRegistry _units;
        private FactionRegistry _factions;
        private NavalState _state;
        private NavalService _service;
        NavalAi _ai;
        private IGeographyQuery _geo;
        private ICombatService _combat;

        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize()
        {
            _units = _ctx.Services.Resolve<UnitRegistry>();
            _factions = _ctx.Services.Resolve<FactionRegistry>();
            _geo = _ctx.Services.Resolve<IGeographyQuery>();
            if (_geo == null) _geo = new PermissiveGeography();
            _combat = _ctx.Services.Resolve<ICombatService>();

            _state = new NavalState(_units.Capacity);
            _service = new NavalService(_units, _state, _geo, _ctx.Services, _ctx.Log);
            _ai = new NavalAi(_units, _state, _geo, _combat, _ctx.Log);
            _ctx.Services.Register<NavalService>(_service);

            var saves = _ctx.Services.Resolve<ISaveService>();
            bool hasSave = saves != null && saves.HasSave(0);
            if (!hasSave) SpawnInitialShips();
        }

        private void SpawnInitialShips()
        {
            int spawned = 0;
            for (int f = 0; f < _factions.Count; f++)
            {
                var info = _factions.Get(f);
                if (info == null || !info.Alive) continue;
                if (!_service.FindWaterNear(info.HomeX, info.HomeZ, 40f, out var water))
                {
                    // 地图无水域信息（占位全陆地）：不生成初始船，未验证
                    continue;
                }
                if (_service.Spawn(info.Id, NavalModel.Raft, water).IsValid) spawned++;
            }
            if (spawned == 0)
                _ctx.Log("[海军] 未在势力附近找到水域，初始船只跳过（需地图水域；未验证）");
        }

        public void OnTick(float simDt) => _ai.Tick(simDt);
        public void OnYear(int gameYear) { }
        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }

        public ISaveSegment GetSaveSegment()
        {
            var list = new List<NavalUnitDto>(32);
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsNaval(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (!r.Alive) continue;
                list.Add(new NavalUnitDto
                {
                    Faction = r.Faction, Model = r.ModelId,
                    X = r.Position.x, Y = r.Position.y, Z = r.Position.z,
                    Hp = r.Hp, Level = r.Level,
                    Xp = _state.GetXp(slot), AiState = (int)r.State,
                });
            }
            return new NavalSaveSegment
            {
                PluginId = PluginIdValue, SegmentVersion = 1,
                Count = list.Count, Units = list.ToArray(),
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is NavalSaveSegment data) || data.Units == null) return;
            ClearNaval();
            for (int i = 0; i < data.Units.Length; i++)
            {
                var u = data.Units[i];
                var pos = new Vector3(u.X, u.Y, u.Z);
                var id = _service.Spawn(u.Faction, (NavalModel)u.Model, pos);
                if (!id.IsValid) continue;
                ref var r = ref _units.GetRef(id);
                r.Hp = Mathf.Clamp(u.Hp, 1f, r.MaxHp);
                r.Level = u.Level < 1 ? 1 : u.Level;
                _state.SetXp(id.Index, u.Xp);
                r.State = (UnitAiState)u.AiState;
            }
        }

        private void ClearNaval()
        {
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsNaval(slot)) continue;
                var id = _units.GetEntityIdAt(slot);
                if (id.IsValid) _units.Kill(id);
                _state.OnKill(slot);
            }
        }

        public void OnShutdown() => ClearNaval();
    }
}
