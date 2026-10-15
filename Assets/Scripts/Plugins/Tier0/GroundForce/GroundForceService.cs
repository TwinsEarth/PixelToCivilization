using System;
using UnityEngine;
using PxC.Core.Services;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;

namespace PxC.Plugins.GroundForce
{
    /// <summary>
    /// 地面部队服务：生成 / 集结 / 征兵 / 升级参数的唯一入口。
    /// 铁律：① 一切地面单位 <see cref="EntityDomain.Land"/>，出生点即锚点，
    /// 生成前做陆地校验，绝不在水域生成；② 集结点必须为陆地，否则安全失败；
    /// ③ 全局上限 120（SoA 常量 Ground120）。
    /// </summary>
    public sealed class GroundForceService
    {
        /// <summary>地面单位全局上限（SoA 常量 Ground=120）。</summary>
        public const int HardCap = 120;

        private readonly UnitRegistry _units;
        private readonly GroundForceState _state;
        private readonly IGeographyQuery _geo;
        private readonly IServiceLocator _services;
        private readonly Action<string> _log;

        public GroundForceService(
            UnitRegistry units,
            GroundForceState state,
            IGeographyQuery geo,
            IServiceLocator services,
            Action<string> log)
        {
            _units = units;
            _state = state;
            _geo = geo;
            _services = services;
            _log = log;
        }

        /// <summary>当前存活地面单位数（全量扫描）。</summary>
        public int Count
        {
            get
            {
                int n = 0;
                int cap = _units.Capacity;
                for (int i = 0; i < cap; i++)
                {
                    ref var r = ref _units.GetSlotRef(i);
                    if (r.Alive && r.Domain == EntityDomain.Land) n++;
                }
                return n;
            }
        }

        /// <summary>某势力存活地面单位数。</summary>
        public int CountByFaction(int faction) =>
            _units.CountByFaction(faction, EntityDomain.Land);

        /// <summary>某势力全部地面单位当前攻击力之和（火力评分口径）。</summary>
        public float TotalFirepower(int faction)
        {
            float sum = 0f;
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsGround(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (r.Alive && r.Faction == faction) sum += CurrentAttack(slot);
            }
            return sum;
        }

        /// <summary>
        /// 在 <paramref name="position"/> 生成一个地面单位。
        /// 若该点为水域则拒绝（返回 Invalid），从源头杜绝"出生被拉进湖里"。
        /// </summary>
        public EntityId Spawn(int faction, GroundForceModel model, Vector3 position, int squadId = 0)
        {
            if (Count >= HardCap)
            {
                _log?.Invoke($"[地面] 已达上限 {HardCap}，{model} 取消");
                return EntityId.Invalid;
            }

            // 陆地校验：出生点必须为陆地
            if (_geo.IsWater(position.x, position.z))
            {
                _log?.Invoke($"[地面] 出生点 ({position.x},{position.z}) 为水域，{model} 拒绝生成");
                return EntityId.Invalid;
            }

            var p = GroundForceProfile.Get(model);
            position.y = _geo.TerrainHeight(position.x, position.z);

            var id = _units.Spawn(
                faction, EntityDomain.Land, (int)model,
                position, p.Durability, p.Speed);
            _state.BindSpawn(id.Index, squadId);

            // 防区锚点 = 出生点（AI 回撤据此，绝不被改写为水域）
            ref var r = ref _units.GetRef(id);
            r.HomePosition = position;

            return id;
        }

        /// <summary>
        /// 集结令：命令地面单位移动到旗帜点。
        /// radius &lt;= 0 表示全体；旗帜点若为水域则安全失败（不移动任何单位）。
        /// 仅改命令，不瞬时搬运（单位沿陆地行进，根治"被拉到某坐标"）。
        /// </summary>
        public int Rally(int faction, Vector3 flagPoint, float radius, bool includeAllFactions)
        {
            if (_geo.IsWater(flagPoint.x, flagPoint.z))
            {
                _log?.Invoke("[集结] 旗帜点为水域，集结取消（安全失败）");
                return 0;
            }
            flagPoint.y = _geo.TerrainHeight(flagPoint.x, flagPoint.z);

            float r2 = radius > 0f ? radius * radius : -1f;
            int ordered = 0;
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsGround(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (!r.Alive) continue;
                if (!includeAllFactions && r.Faction != faction) continue;

                if (r2 >= 0f)
                {
                    float dx = r.Position.x - flagPoint.x;
                    float dz = r.Position.z - flagPoint.z;
                    if (dx * dx + dz * dz > r2) continue;
                }

                // 直接改命令（与 UnitRegistry.OrderMove 同字段，避免构造 EntityId）
                r.MoveTarget = flagPoint;
                r.HasMoveTarget = true;
                r.TargetSlot = -1;
                r.State = UnitAiState.Moving;
                ordered++;
            }
            _log?.Invoke($"[集结] 旗帜 ({flagPoint.x},{flagPoint.z})，{ordered} 个地面单位受命集结");
            return ordered;
        }

        /// <summary>取得某 slot 的型号。</summary>
        public GroundForceModel ModelOf(int slot) =>
            (GroundForceModel)_units.GetSlotRef(slot).ModelId;

        /// <summary>按等级现算攻击力（不额外存储，避免冗余）。</summary>
        public float CurrentAttack(int slot)
        {
            ref var r = ref _units.GetSlotRef(slot);
            var p = GroundForceProfile.Get((GroundForceModel)r.ModelId);
            return p.Attack * (1f + p.LevelGrowth * (r.Level - 1));
        }

        /// <summary>按等级现算防御力。</summary>
        public float CurrentDefense(int slot)
        {
            ref var r = ref _units.GetSlotRef(slot);
            var p = GroundForceProfile.Get((GroundForceModel)r.ModelId);
            return p.Defense * (1f + p.LevelGrowth * 0.5f * (r.Level - 1));
        }

        /// <summary>升级后把耐久上限/当前值同步到 UnitRegistry。</summary>
        public void ApplyLevelToUnit(int slot)
        {
            ref var r = ref _units.GetSlotRef(slot);
            var p = GroundForceProfile.Get((GroundForceModel)r.ModelId);
            float factor = 1f + p.LevelGrowth * (r.Level - 1);
            r.MaxHp = p.Durability * factor;
            r.Hp = r.MaxHp;
        }

        /// <summary>
        /// 增加经验并自动升级（升级阈值 = level*100）。返回本次提升的级数。
        /// </summary>
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

        /// <summary>某型号的征兵/制造成本（按分代给资源）。</summary>
        public static ResourceCost[] RecruitCost(GroundForceModel model)
        {
            switch (model)
            {
                case GroundForceModel.Cavalry:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Food, 25f),
                        new ResourceCost(ResourceType.Bronze, 8f),
                    };
                case GroundForceModel.Formation:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Food, 30f),
                        new ResourceCost(ResourceType.Iron, 10f),
                    };
                case GroundForceModel.Chariot:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Wood, 40f),
                        new ResourceCost(ResourceType.Bronze, 15f),
                    };
                case GroundForceModel.Tank:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Steel, 120f),
                        new ResourceCost(ResourceType.Oil, 60f),
                        new ResourceCost(ResourceType.Capital, 80f),
                    };
                case GroundForceModel.Armor:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Steel, 70f),
                        new ResourceCost(ResourceType.Oil, 40f),
                        new ResourceCost(ResourceType.Capital, 50f),
                    };
                case GroundForceModel.MissileVehicle:
                    return new[]
                    {
                        new ResourceCost(ResourceType.Steel, 90f),
                        new ResourceCost(ResourceType.Data, 30f),
                        new ResourceCost(ResourceType.Capital, 120f),
                    };
                default:
                    return Array.Empty<ResourceCost>();
            }
        }
    }
}
