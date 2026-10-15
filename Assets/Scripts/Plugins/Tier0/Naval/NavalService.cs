using System;
using UnityEngine;
using PxC.Core.Services;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Naval
{
    /// <summary>
    /// 船艇服务：生成 / 集结 / 制造成本 / 升级参数的唯一入口。
    /// 铁律：船艇 <see cref="EntityDomain.Naval"/>，生成点与集结点必须为水域，
    /// 否则拒绝（船不上岸）；全局上限 200（Ship200）。
    /// </summary>
    public sealed class NavalService
    {
        /// <summary>船艇全局上限（Ship=200）。</summary>
        public const int HardCap = 200;

        private readonly UnitRegistry _units;
        private readonly NavalState _state;
        private readonly IGeographyQuery _geo;
        private readonly IServiceLocator _services;
        private readonly Action<string> _log;

        public NavalService(
            UnitRegistry units, NavalState state, IGeographyQuery geo,
            IServiceLocator services, Action<string> log)
        {
            _units = units;
            _state = state;
            _geo = geo;
            _services = services;
            _log = log;
        }

        public int Count
        {
            get
            {
                int n = 0;
                int cap = _units.Capacity;
                for (int i = 0; i < cap; i++)
                {
                    ref var r = ref _units.GetSlotRef(i);
                    if (r.Alive && r.Domain == EntityDomain.Naval) n++;
                }
                return n;
            }
        }

        public int CountByFaction(int faction) =>
            _units.CountByFaction(faction, EntityDomain.Naval);

        /// <summary>某势力全部船艇当前攻击力之和（火力评分口径）。</summary>
        public float TotalFirepower(int faction)
        {
            float sum = 0f;
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsNaval(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (r.Alive && r.Faction == faction) sum += CurrentAttack(slot);
            }
            return sum;
        }

        /// <summary>在水域 <paramref name="position"/> 生成船艇；陆地/无水点拒绝。</summary>
        public EntityId Spawn(int faction, NavalModel model, Vector3 position)
        {
            if (Count >= HardCap)
            {
                _log?.Invoke($"[海军] 已达上限 {HardCap}，{model} 取消");
                return EntityId.Invalid;
            }
            if (!_geo.IsWater(position.x, position.z))
            {
                _log?.Invoke($"[海军] 生成点 ({position.x},{position.z}) 非水域，{model} 拒绝");
                return EntityId.Invalid;
            }

            var p = NavalProfile.Get(model);
            position.y = _geo.TerrainHeight(position.x, position.z);
            var id = _units.Spawn(
                faction, EntityDomain.Naval, (int)model,
                position, p.Durability, p.Speed);
            _state.BindSpawn(id.Index);
            ref var r = ref _units.GetRef(id);
            r.HomePosition = position;
            return id;
        }

        /// <summary>海上集结：集结点必须为水域，否则安全失败。</summary>
        public int Rally(int faction, Vector3 point, float radius, bool includeAllFactions)
        {
            if (!_geo.IsWater(point.x, point.z))
            {
                _log?.Invoke("[海军集结] 集结点非水域，取消");
                return 0;
            }
            point.y = _geo.TerrainHeight(point.x, point.z);
            float r2 = radius > 0f ? radius * radius : -1f;
            int ordered = 0;
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsNaval(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (!r.Alive) continue;
                if (!includeAllFactions && r.Faction != faction) continue;
                if (r2 >= 0f)
                {
                    float dx = r.Position.x - point.x;
                    float dz = r.Position.z - point.z;
                    if (dx * dx + dz * dz > r2) continue;
                }
                r.MoveTarget = point;
                r.HasMoveTarget = true;
                r.TargetSlot = -1;
                r.State = UnitAiState.Moving;
                ordered++;
            }
            return ordered;
        }

        public float CurrentAttack(int slot)
        {
            ref var r = ref _units.GetSlotRef(slot);
            var p = NavalProfile.Get((NavalModel)r.ModelId);
            return p.Attack * (1f + p.LevelGrowth * (r.Level - 1));
        }

        public float CurrentDefense(int slot)
        {
            ref var r = ref _units.GetSlotRef(slot);
            var p = NavalProfile.Get((NavalModel)r.ModelId);
            return p.Defense * (1f + p.LevelGrowth * 0.5f * (r.Level - 1));
        }

        public void ApplyLevelToUnit(int slot)
        {
            ref var r = ref _units.GetSlotRef(slot);
            var p = NavalProfile.Get((NavalModel)r.ModelId);
            float factor = 1f + p.LevelGrowth * (r.Level - 1);
            r.MaxHp = p.Durability * factor;
            r.Hp = r.MaxHp;
        }

        /// <summary>增加经验并自动升级（阈值 level*100）。返回提升级数。</summary>
        public int AddExperience(int slot, int xp)
        {
            ref var r = ref _units.GetSlotRef(slot);
            if (!r.Alive || xp <= 0) return 0;
            _state.AddXp(slot, xp);
            int levels = 0;
            while (r.Level < 10)
            {
                int need = r.Level * 100;
                if (_state.GetXp(slot) < need) break;
                _state.SetXp(slot, _state.GetXp(slot) - need);
                r.Level++;
                ApplyLevelToUnit(slot);
                levels++;
            }
            return levels;
        }

        /// <summary>在势力出生点附近寻找一片水域（供初始船只锚泊）。</summary>
        public bool FindWaterNear(float homeX, float homeZ, float searchRadius, out Vector3 water)
        {
            for (float ring = 8f; ring <= searchRadius; ring += 4f)
            {
                for (int a = 0; a < 8; a++)
                {
                    float ang = a * (Mathf.PI * 2f / 8f);
                    float x = homeX + Mathf.Cos(ang) * ring;
                    float z = homeZ + Mathf.Sin(ang) * ring;
                    if (_geo.IsWater(x, z))
                    {
                        water = new Vector3(x, _geo.TerrainHeight(x, z), z);
                        return true;
                    }
                }
            }
            water = Vector3.zero;
            return false;
        }

        public static ResourceCost[] BuildCost(NavalModel model)
        {
            switch (model)
            {
                case NavalModel.Raft:
                    return new[] { new ResourceCost(ResourceType.Wood, 15f) };
                case NavalModel.FishingBoat:
                    return new[] { new ResourceCost(ResourceType.Wood, 25f) };
                case NavalModel.Warship:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Wood, 80f),
                        new ResourceCost(ResourceType.Iron, 20f),
                    };
                case NavalModel.SailingShip:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Wood, 100f),
                        new ResourceCost(ResourceType.Goods, 20f),
                    };
                case NavalModel.Steamship:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Steel, 120f),
                        new ResourceCost(ResourceType.Coal, 60f),
                    };
                case NavalModel.Submarine:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Steel, 100f),
                        new ResourceCost(ResourceType.Oil, 50f),
                        new ResourceCost(ResourceType.Capital, 60f),
                    };
                case NavalModel.AircraftCarrier:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Steel, 300f),
                        new ResourceCost(ResourceType.Oil, 150f),
                        new ResourceCost(ResourceType.Capital, 200f),
                    };
                default:
                    return Array.Empty<ResourceCost>();
            }
        }
    }
}
