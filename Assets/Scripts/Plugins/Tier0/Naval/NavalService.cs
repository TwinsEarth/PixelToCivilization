using System;
using UnityEngine;
using PxC.Core.Services;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;

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

        /// <summary>某势力某船型的存活数量（经济/军事统计口径）。</summary>
        public int CountByModel(int faction, NavalModel model)
        {
            int n = 0;
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsNaval(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (r.Alive && r.Faction == faction && r.ModelId == (int)model)
                    n++;
            }
            return n;
        }

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

        /// <summary>
        /// 付费建造船艇：在势力首府附近找水域，先上限/水域/经济校验，
        /// 按 <see cref="BuildCost"/> 扣费，成功才在水域生成。
        /// 无水域（内陆势力）/ 资源不足 / 达上限均拒绝并返回 Invalid（扣费前完成全部校验）。
        /// 这是制造船艇的唯一入口（新游戏初始木筏除外）。
        /// </summary>
        public EntityId TryBuildUnit(
            int faction, NavalModel model, float homeX, float homeZ,
            float searchRadius = 40f)
        {
            if (Count >= HardCap)
            {
                _log?.Invoke($"[海军] 已达上限 {HardCap}，{model} 取消");
                return EntityId.Invalid;
            }
            if (!FindWaterNear(homeX, homeZ, searchRadius, out var water))
            {
                _log?.Invoke($"[海军] 势力 {faction} 附近无水域，{model} 跳过（内陆势力不造船）");
                return EntityId.Invalid;
            }
            if (_services.TryResolve<IEconomyService>(out var economy))
            {
                ResourceCost[] costs = BuildCost(model);
                if (!economy.TrySpendBundle(faction, costs))
                {
                    _log?.Invoke($"[海军] 资源不足，{model} 取消");
                    return EntityId.Invalid;
                }
            }
            return Spawn(faction, model, water);
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

        /// <summary>
        /// 单船蒸汽铁甲换代：扣费（<see cref="UpgradeCost"/>）后直接改型号，
        /// 保留等级 / 经验 / 位置，速度与耐久 / 攻防按新型号 Profile 重置。
        /// 资源不足、型号非法或非船艇返回 false。
        /// </summary>
        public bool TryUpgradeModel(int slot, NavalModel newModel)
        {
            if (!_state.IsNaval(slot)) return false;
            ref var r = ref _units.GetSlotRef(slot);
            if (!r.Alive) return false;

            var oldModel = (NavalModel)r.ModelId;
            if (newModel == oldModel) return false;

            ResourceCost[] costs = UpgradeCost(oldModel, newModel);
            if (costs.Length == 0) return false;

            if (_services.TryResolve<IEconomyService>(out var economy))
            {
                if (!economy.TrySpendBundle(r.Faction, costs))
                {
                    _log?.Invoke($"[海军换代] 资源不足，{oldModel}→{newModel} 取消");
                    return false;
                }
            }

            r.ModelId = (int)newModel;
            var p = NavalProfile.Get(newModel);
            r.Speed = p.Speed;
            ApplyLevelToUnit(slot);
            _log?.Invoke($"[海军换代] {oldModel}→{newModel}（slot {slot}，等级 {r.Level}）");
            return true;
        }

        /// <summary>
        /// 某势力的蒸汽铁甲换代步骤：遍历该势力所有船艇，把进入工业后过时的
        /// 战船 / 帆船换代为蒸汽轮船；资源不足跳过（来年再试）。返回成功换代数量。
        /// </summary>
        public int UpgradeFactionStep(int faction, Era era)
        {
            int done = 0;
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsNaval(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (!r.Alive || r.Faction != faction) continue;

                var oldModel = (NavalModel)r.ModelId;
                if (NavalGeneration.TryGetSteamUpgrade(oldModel, era, out var target))
                {
                    if (TryUpgradeModel(slot, target)) done++;
                }
            }
            return done;
        }

        /// <summary>在势力出生点附近寻找一片水域（供初始船只锚泊）。</summary>
        public bool FindWaterNear(float homeX, float homeZ, float searchRadius, out Vector3 water)
        {
            // 在半径内逐格扫描，返回最近的水域（旧实现只扫 8 方向稀疏环，
            // ring 从 8 起步，半径内任意格的水会被漏判）。
            int radius = Mathf.CeilToInt(searchRadius);
            int hx = Mathf.RoundToInt(homeX);
            int hz = Mathf.RoundToInt(homeZ);
            int bestX = 0;
            int bestZ = 0;
            float bestDist = float.MaxValue;
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist > searchRadius || dist >= bestDist) continue;
                    int x = hx + dx;
                    int z = hz + dz;
                    if (_geo.IsWater(x, z))
                    {
                        bestDist = dist;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }
            if (bestDist < float.MaxValue)
            {
                water = new Vector3(bestX, _geo.TerrainHeight(bestX, bestZ), bestZ);
                return true;
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

        /// <summary>
        /// 蒸汽铁甲换代成本：在老船体 / 船员基础上改造，比新建便宜（船体可复用）。
        /// 战船 / 帆船 → 蒸汽轮船：Steel90 + Coal40（新建蒸汽轮船为 Steel120+Coal60）。
        /// 未定义的映射返回空数组（调用方据此拒绝换代）。
        /// </summary>
        public static ResourceCost[] UpgradeCost(NavalModel oldModel, NavalModel newModel)
        {
            if (newModel == NavalModel.Steamship &&
                (oldModel == NavalModel.Warship ||
                 oldModel == NavalModel.SailingShip))
            {
                return new[]
                {
                    new ResourceCost(ResourceType.Steel, 90f),
                    new ResourceCost(ResourceType.Coal, 40f),
                };
            }

            return Array.Empty<ResourceCost>();
        }
    }
}
