using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Building
{
    /// <summary>
    /// 建筑核心插件（Tier-0）。注册 IBuildingService，负责建造/升级/拆除，
    /// 并在 OnYear 按建筑类型目录统一结算建筑产出（农田/伐木场/采石场/市集/工坊）。
    /// 塔防的多层收分形制由 TowerProfile 数据驱动（视图层据此生成，未验证）。
    /// </summary>
    public sealed class BuildingPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.building";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(1)
            {
                new PluginDependency("pxc.economy"),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.SpawnEntity),
            };

        private IPluginContext _ctx;
        private BuildingRegistry _registry;
        private BuildingService _service;
        private bool _initialized;

        public string Id => PluginIdValue;
        public string DisplayName => "建筑系统";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;
        public SimulationLayer Layer => SimulationLayer.P0Core;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context)
        {
            _ctx = context;
        }

        public void OnInitialize()
        {
            _registry = _ctx.Services.Resolve<BuildingRegistry>();
            _service = new BuildingService(_ctx.Services, _registry, _ctx.Log);
            _ctx.Services.Register<IBuildingService>(_service);
            _initialized = true;
        }

        public void OnShutdown()
        {
            if (!_initialized) return;
            _ctx.Services.Unregister<IBuildingService>();
            _initialized = false;
        }

        public void OnTick(float simDt)
        {
        }

        public void OnYear(int gameYear)
        {
            if (!_initialized) return;
            if (!_ctx.Services.TryResolve<IEconomyService>(out var economy)) return;

            // 科技为可选服务（用于判定建筑副产出时代是否解锁）；缺失时副产出不产出（保守）。
            _ctx.Services.TryResolve<ITechService>(out var tech);

            // 遍历全部存活建筑，按类型产出（等级提升产出）
            int cap = _registry.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                var rec = _registry.GetSafe(slot);
                if (!rec.Alive) continue;

                var entry = BuildingCatalog.Get((BuildingType)rec.TypeId);
                float levelScale = 1f + 0.25f * (rec.Level - 1);

                if (entry.Produces != PxC.Gameplay.Resources.ResourceType.None
                    && entry.ProducePerYear > 0f)
                {
                    economy.Add(rec.Faction, entry.Produces,
                        entry.ProducePerYear * levelScale);
                }

                // 按时代解锁的副产出（青铜/铁/煤/油/钢/电/数据/资本）
                if (entry.EraOutputs != null && tech != null)
                {
                    for (int i = 0; i < entry.EraOutputs.Length; i++)
                    {
                        var eo = entry.EraOutputs[i];
                        if (tech.HasEra(rec.Faction, eo.MinEra))
                            economy.Add(rec.Faction, eo.Resource, eo.Amount * levelScale);
                    }
                }
            }
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

            var live = new List<BuildingRecordDto>();
            int cap = _registry.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                var rec = _registry.GetSafe(slot);
                if (!rec.Alive) continue;
                live.Add(new BuildingRecordDto
                {
                    Faction = rec.Faction,
                    TypeId = rec.TypeId,
                    X = rec.Position.x,
                    Y = rec.Position.y,
                    Z = rec.Position.z,
                    Hp = rec.Hp,
                    MaxHp = rec.MaxHp,
                    Level = rec.Level,
                    IsTower = rec.IsTower,
                });
            }

            return new BuildingSaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                Count = live.Count,
                Buildings = live.ToArray(),
            };
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is BuildingSaveSegment s)) return;
            _registry = _ctx.Services.Resolve<BuildingRegistry>();
            _registry.Clear();

            if (s.Buildings != null)
            {
                for (int i = 0; i < s.Buildings.Length; i++)
                {
                    var b = s.Buildings[i];
                    var pos = new UnityEngine.Vector3(b.X, b.Y, b.Z);
                    int slot = _registry.Spawn(b.Faction, b.TypeId, pos, b.MaxHp, b.IsTower);
                    ref var r = ref _registry.GetSlotRef(slot);
                    r.Level = b.Level;
                    r.Hp = b.Hp;
                }
            }

            _service = new BuildingService(_ctx.Services, _registry, _ctx.Log);
            _ctx.Services.Register<IBuildingService>(_service);
            _initialized = true;
        }
    }
}
