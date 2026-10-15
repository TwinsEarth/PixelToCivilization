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

namespace PxC.Plugins.GroundForce
{
    /// <summary>
    /// 地面部队插件（Tier-0）。持有骑兵/阵兵/战车/坦克/装甲车/导弹车的
    /// 生成、10 维参数、自主 AI（寻敌/组队/布阵/攻击/追击/回撤）与存档。
    /// 铁律：陆地单位永不进入水域（生成与每步移动均陆地校验）。
    /// </summary>
    public sealed class GroundForcePlugin : IPlugin, ISimPlugin
    {
        public const string PluginIdValue = "pxc.groundforce";

        public string Id => PluginIdValue;
        public string DisplayName => "地面部队";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;
        public SimulationLayer Layer => SimulationLayer.P1Tactical;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        private static readonly PluginDependency[] _deps =
        {
            new PluginDependency("pxc.economy", 1),
            new PluginDependency("pxc.population", 1),
            new PluginDependency("pxc.tech", 1),
        };

        private static readonly CapabilityClaim[] _caps =
        {
            new CapabilityClaim(Capability.SpawnEntity),
        };

        private IPluginContext _ctx;
        private UnitRegistry _units;
        private FactionRegistry _factions;
        private GroundForceState _state;
        private GroundForceService _service;
        private GroundForceAi _ai;
        private IGeographyQuery _geo;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            _units = _ctx.Services.Resolve<UnitRegistry>();
            _factions = _ctx.Services.Resolve<FactionRegistry>();

            // 地形查询（可选）：缺失时用宽松占位（全部陆地，未验证）
            _geo = _ctx.Services.Resolve<IGeographyQuery>();
            if (_geo == null) _geo = new PermissiveGeography();

            // 战斗结算（可选）：combat 插件依赖本插件、拓扑上在之后初始化，故 AI 用延迟解析，不在此强 Resolve。
            _state = new GroundForceState(_units.Capacity);
            _service = new GroundForceService(_units, _state, _geo, _ctx.Services, _ctx.Log);

            // 聚落（首府坐标，用于战略行军目标）；在装配/启动期已注册，缺失则 null。
            var settlements = _ctx.Services.Resolve<SettlementRegistry>();

            _ai = new GroundForceAi(
                _units, _state, _geo,
                () => _ctx.Services.TryResolve<ICombatService>(out var c) ? c : null,
                settlements,
                () => _ctx.Services.TryResolve<IMilitaryService>(out var m) ? m : null,
                _ctx.Log);

            // 暴露给 UI / 集结令 / 征兵
            _ctx.Services.Register<GroundForceService>(_service);

            // 新游戏：为每个势力在出生点编组初始步兵；读档则等待 Restore
            var saves = _ctx.Services.Resolve<ISaveService>();
            bool hasSave = saves != null && saves.HasSave(0);
            if (!hasSave) SpawnInitialForces();
        }

        private void SpawnInitialForces()
        {
            for (int f = 0; f < _factions.Count; f++)
            {
                var info = _factions.Get(f);
                if (info == null || !info.Alive) continue;
                var home = new Vector3(info.HomeX, 0f, info.HomeZ);
                int squadId = f * 10 + 1;
                // 3 名阵兵编为一组，在出生点轻微错开
                for (int k = 0; k < 3; k++)
                {
                    var pos = home + new Vector3((k - 1) * 1.6f, 0f, 0.6f);
                    _service.Spawn(info.Id, GroundForceModel.Formation, pos, squadId);
                }
            }
            _ctx.Log("[地面] 各势力初始步兵已在出生点编组");
        }

        public void OnTick(float simDt)
        {
            _ai.Tick(simDt);
        }

        public void OnYear(int gameYear) { }
        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }

        // ------------------------------------------------------------
        // 存档
        // ------------------------------------------------------------
        public ISaveSegment GetSaveSegment()
        {
            var list = new List<GroundForceUnitDto>(32);
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsGround(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (!r.Alive) continue;
                list.Add(new GroundForceUnitDto
                {
                    Faction = r.Faction,
                    Model = r.ModelId,
                    X = r.Position.x,
                    Y = r.Position.y,
                    Z = r.Position.z,
                    Hp = r.Hp,
                    Level = r.Level,
                    Xp = _state.GetXp(slot),
                    Squad = _state.GetSquad(slot),
                    AiState = (int)r.State,
                });
            }

            return new GroundForceSaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                Count = list.Count,
                Units = list.ToArray(),
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is GroundForceSaveSegment data) || data.Units == null) return;

            // 清掉当前地面单位（OnInitialize 可能已生成初始部队）
            ClearGroundUnits();

            for (int i = 0; i < data.Units.Length; i++)
            {
                var u = data.Units[i];
                var model = (GroundForceModel)u.Model;
                var pos = new Vector3(u.X, u.Y, u.Z);
                var id = _service.Spawn(u.Faction, model, pos, u.Squad);
                if (!id.IsValid) continue;
                ref var r = ref _units.GetRef(id);
                r.Hp = Mathf.Clamp(u.Hp, 1f, r.MaxHp);
                r.Level = u.Level < 1 ? 1 : u.Level;
                _state.SetXp(id.Index, u.Xp);
                r.State = (UnitAiState)u.AiState;
            }
            _ctx.Log($"[地面] 已恢复 {data.Count} 个地面单位");
        }

        private void ClearGroundUnits()
        {
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsGround(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (r.Alive)
                {
                    var id = _units.GetEntityIdAt(slot);
                    if (id.IsValid) _units.Kill(id);
                }
                _state.OnKill(slot);
            }
        }

        public void OnShutdown()
        {
            ClearGroundUnits();
        }
    }
}
