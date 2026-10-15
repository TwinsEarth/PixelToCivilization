using System;
using UnityEngine;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;

namespace PxC.Plugins.GroundForce
{
    /// <summary>
    /// 地面单位自主 AI：在 <see cref="Tick"/> 中驱动状态机
    /// 待命→寻敌→追击→攻击→（追击/回撤）。所有移动仅在 xz 平面、
    /// 每步经 <see cref="IGeographyQuery"/> 陆地校验，下一位置是水则停在原地——
    /// 从机制上保证陆地单位永不被拉进湖里。能点选、能随战斗经验升级。
    /// </summary>
    public sealed class GroundForceAi
    {
        /// <summary>到达目标点的距离阈值（世界单位）。</summary>
        private const float ArriveEps = 0.8f;

        private readonly UnitRegistry _units;
        private readonly GroundForceState _state;
        private readonly IGeographyQuery _geo;
        private readonly Func<ICombatService> _combatProvider;
        private readonly Action<string> _log;

        // 复用的敌人搜索缓冲（避免每帧分配）
        private readonly int[] _hits = new int[32];

        public GroundForceAi(
            UnitRegistry units,
            GroundForceState state,
            IGeographyQuery geo,
            Func<ICombatService> combatProvider,
            Action<string> log)
        {
            _units = units;
            _state = state;
            _geo = geo;
            _combatProvider = combatProvider;
            _log = log;
        }

        /// <summary>
        /// 推进所有地面单位一帧。返回本帧发起攻击的单位数（供统计/测试）。
        /// </summary>
        public int Tick(float dt)
        {
            int attacks = 0;
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsGround(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (!r.Alive)
                {
                    _state.OnKill(slot);
                    continue;
                }

                float cd = _state.GetCooldown(slot);
                if (cd > 0f) _state.SetCooldown(slot, cd - dt);

                switch (r.State)
                {
                    case UnitAiState.Idle:
                        TickIdle(slot, ref r);
                        break;
                    case UnitAiState.Moving:
                        TickMoving(ref r, dt);
                        break;
                    case UnitAiState.Pursuing:
                        TickPursuing(slot, ref r, dt);
                        break;
                    case UnitAiState.Attacking:
                        if (TickAttacking(slot, ref r, dt)) attacks++;
                        break;
                    case UnitAiState.Returning:
                        TickReturning(ref r, dt);
                        break;
                }
            }
            return attacks;
        }

        // ------------------------------------------------------------
        // 状态处理
        // ------------------------------------------------------------
        private void TickIdle(int slot, ref UnitRecord r)
        {
            int target = FindNearestEnemy(slot, r, r.Position);
            if (target >= 0)
            {
                r.TargetSlot = target;
                r.State = UnitAiState.Pursuing;
            }
            // 否则保持驻守待命
        }

        private void TickMoving(ref UnitRecord r, float dt)
        {
            bool arrived = MoveTowards(ref r, r.MoveTarget, dt);
            if (IsClose(r.Position, r.MoveTarget))
            {
                r.HasMoveTarget = false;
                r.State = UnitAiState.Idle;
            }
            else if (!arrived && FrontIsWater(r, r.MoveTarget))
            {
                // 集结路径被水阻挡：停在陆地，不硬闯（不掉湖）
                r.HasMoveTarget = false;
                r.State = UnitAiState.Idle;
            }
        }

        private void TickPursuing(int slot, ref UnitRecord r, float dt)
        {
            int ts = r.TargetSlot;
            if (!TargetAlive(ts))
            {
                r.TargetSlot = -1;
                r.State = UnitAiState.Idle;
                return;
            }
            ref var target = ref _units.GetSlotRef(ts);
            var p = GroundForceProfile.Get((GroundForceModel)r.ModelId);
            float dist = FlatDistance(r.Position, target.Position);

            if (dist <= p.AttackRange)
            {
                r.State = UnitAiState.Attacking;
                return;
            }

            bool reached = MoveTowards(ref r, target.Position, dt);
            if (!reached && FrontIsWater(r, target.Position))
            {
                // 敌人在水对岸：陆地单位不涉水，放弃该目标（退回待命）
                r.TargetSlot = -1;
                r.State = UnitAiState.Idle;
            }
        }

        private bool TickAttacking(int slot, ref UnitRecord r, float dt)
        {
            int ts = r.TargetSlot;
            if (!TargetAlive(ts))
            {
                r.TargetSlot = -1;
                r.State = UnitAiState.Idle;
                return false;
            }
            ref var target = ref _units.GetSlotRef(ts);
            var p = GroundForceProfile.Get((GroundForceModel)r.ModelId);
            float dist = FlatDistance(r.Position, target.Position);

            if (dist > p.AttackRange * 1.25f)
            {
                r.State = UnitAiState.Pursuing;
                return false;
            }

            if (_state.GetCooldown(slot) <= 0f)
            {
                Fire(slot, ts);
                _state.SetCooldown(slot, 1f / Mathf.Max(0.01f, p.FireRate));
                return true;
            }
            return false;
        }

        private void TickReturning(ref UnitRecord r, float dt)
        {
            MoveTowards(ref r, r.HomePosition, dt);
            if (IsClose(r.Position, r.HomePosition))
            {
                r.State = UnitAiState.Idle;
            }
        }

        // ------------------------------------------------------------
        // 攻击 / 寻敌
        // ------------------------------------------------------------
        private void Fire(int sourceSlot, int targetSlot)
        {
            // combat 依赖 groundforce/naval，拓扑上在本插件之后初始化，故延迟解析（此时应已就绪）。
            ICombatService combat = _combatProvider != null ? _combatProvider() : null;
            if (combat != null)
            {
                var source = _units.GetEntityIdAt(sourceSlot);
                var target = _units.GetEntityIdAt(targetSlot);
                if (source.IsValid && target.IsValid)
                {
                    combat.ResolveAttack(source, target);
                    return;
                }
            }
            // combat 未接线时（占位）：仅记录，不结算（未验证）
            var p = GroundForceProfile.Get(ModelOf(sourceSlot));
            _log?.Invoke($"[地面] 攻击 #{targetSlot}（combat 未接线，伤害未结算；未验证） dmg~{p.Attack}");
        }

        private int FindNearestEnemy(int selfSlot, UnitRecord self, Vector3 center)
        {
            var p = GroundForceProfile.Get((GroundForceModel)self.ModelId);
            int n = _units.FindEnemiesInRadius(
                center, p.PerceptionRadius, self.Faction, _hits, _hits.Length);
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                int s = _hits[i];
                ref var e = ref _units.GetSlotRef(s);
                // 只追击同域（陆地）目标，不会把水上目标当陆地目标追进湖
                if (e.Domain != EntityDomain.Land) continue;
                float d = FlatDistance(center, e.Position);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        private GroundForceModel ModelOf(int slot) =>
            (GroundForceModel)_units.GetSlotRef(slot).ModelId;

        // ------------------------------------------------------------
        // 移动（陆地校验，核心）
        // ------------------------------------------------------------
        private bool MoveTowards(ref UnitRecord r, Vector3 target, float dt)
        {
            float dx = target.x - r.Position.x;
            float dz = target.z - r.Position.z;
            float dist = (float)Math.Sqrt(dx * dx + dz * dz);
            if (dist < 1e-5f) return true;

            float step = Mathf.Min(r.Speed * dt, dist);
            float inv = 1f / dist;
            float nx = r.Position.x + dx * inv * step;
            float nz = r.Position.z + dz * inv * step;

            // 关键：下一位置是水域则停在原地，绝不进入
            if (_geo.IsWater(nx, nz))
                return false;

            r.Position.x = nx;
            r.Position.z = nz;
            r.Position.y = _geo.TerrainHeight(nx, nz);
            return dist <= step + ArriveEps;
        }

        private bool TargetAlive(int slot)
        {
            if (slot < 0 || slot >= _units.Capacity) return false;
            return _units.GetSlotRef(slot).Alive;
        }

        private bool FrontIsWater(UnitRecord r, Vector3 target)
        {
            float dx = target.x - r.Position.x;
            float dz = target.z - r.Position.z;
            float dist = (float)Math.Sqrt(dx * dx + dz * dz);
            if (dist < 1e-5f) return false;
            float nx = r.Position.x + dx / dist;
            float nz = r.Position.z + dz / dist;
            return _geo.IsWater(nx, nz);
        }

        private static bool IsClose(Vector3 a, Vector3 b) =>
            FlatDistance(a, b) <= ArriveEps;

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
