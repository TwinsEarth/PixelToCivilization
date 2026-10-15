using System;
using UnityEngine;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Naval
{
    /// <summary>
    /// 船艇自主 AI：巡航→寻敌→追击→攻击→回撤。每步经水域校验，
    /// 下一位置不是水（陆地）则停在原地——船永不上岸。
    /// 民用船（木筏/渔船）不攻击，只做巡航/避战。
    /// </summary>
    public sealed class NavalAi
    {
        private const float ArriveEps = 1.2f;
        private readonly UnitRegistry _units;
        private readonly NavalState _state;
        private readonly IGeographyQuery _geo;
        private readonly ICombatService _combat;
        private readonly Action<string> _log;
        private readonly int[] _hits = new int[32];

        public NavalAi(UnitRegistry units, NavalState state, IGeographyQuery geo,
            ICombatService combat, Action<string> log)
        {
            _units = units;
            _state = state;
            _geo = geo;
            _combat = combat;
            _log = log;
        }

        public int Tick(float dt)
        {
            int attacks = 0;
            int cap = _units.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_state.IsNaval(slot)) continue;
                ref var r = ref _units.GetSlotRef(slot);
                if (!r.Alive) { _state.OnKill(slot); continue; }
                float cd = _state.GetCooldown(slot);
                if (cd > 0f) _state.SetCooldown(slot, cd - dt);

                var p = NavalProfile.Get((NavalModel)r.ModelId);
                if (p.Civilian)
                {
                    // 民用船：不寻敌，仅处理集结移动
                    if (r.State == UnitAiState.Moving) TickMoving(ref r, dt);
                    continue;
                }

                switch (r.State)
                {
                    case UnitAiState.Idle:
                        int target = FindNearestEnemy(r, r.Position);
                        if (target >= 0) { r.TargetSlot = target; r.State = UnitAiState.Pursuing; }
                        break;
                    case UnitAiState.Moving:
                        TickMoving(ref r, dt);
                        break;
                    case UnitAiState.Pursuing:
                        TickPursuing(ref r, dt);
                        break;
                    case UnitAiState.Attacking:
                        if (TickAttacking(ref r, slot)) attacks++;
                        break;
                    case UnitAiState.Returning:
                        MoveTowards(ref r, r.HomePosition, dt);
                        if (IsClose(r.Position, r.HomePosition)) r.State = UnitAiState.Idle;
                        break;
                }
            }
            return attacks;
        }

        private void TickMoving(ref UnitRecord r, float dt)
        {
            MoveTowards(ref r, r.MoveTarget, dt);
            if (IsClose(r.Position, r.MoveTarget))
            {
                r.HasMoveTarget = false;
                r.State = UnitAiState.Idle;
            }
            else if (FrontIsLand(r, r.MoveTarget))
            {
                r.HasMoveTarget = false;
                r.State = UnitAiState.Idle;
            }
        }

        private void TickPursuing(ref UnitRecord r, float dt)
        {
            int ts = r.TargetSlot;
            if (ts < 0 || !_units.GetSlotRef(ts).Alive)
            {
                r.TargetSlot = -1; r.State = UnitAiState.Idle; return;
            }
            ref var target = ref _units.GetSlotRef(ts);
            var p = NavalProfile.Get((NavalModel)r.ModelId);
            if (FlatDistance(r.Position, target.Position) <= p.AttackRange)
            {
                r.State = UnitAiState.Attacking;
                return;
            }
            bool moved = MoveTowards(ref r, target.Position, dt);
            if (!moved && FrontIsLand(r, target.Position))
            {
                r.TargetSlot = -1;
                r.State = UnitAiState.Idle;
            }
        }

        private bool TickAttacking(ref UnitRecord r, int slot)
        {
            int ts = r.TargetSlot;
            if (ts < 0 || !_units.GetSlotRef(ts).Alive)
            {
                r.TargetSlot = -1; r.State = UnitAiState.Idle; return false;
            }
            ref var target = ref _units.GetSlotRef(ts);
            var p = NavalProfile.Get((NavalModel)r.ModelId);
            if (FlatDistance(r.Position, target.Position) > p.AttackRange * 1.25f)
            {
                r.State = UnitAiState.Pursuing;
                return false;
            }
            if (_state.GetCooldown(slot) <= 0f)
            {
                if (_combat != null)
                {
                    var src = _units.GetEntityIdAt(slot);
                    var tgt = _units.GetEntityIdAt(ts);
                    if (src.IsValid && tgt.IsValid) { _combat.ResolveAttack(src, tgt); }
                }
                else _log?.Invoke($"[海军] 攻击 #{ts}（combat 未接线，未验证）");
                _state.SetCooldown(slot, 1f / Mathf.Max(0.01f, p.FireRate));
                return true;
            }
            return false;
        }

        private int FindNearestEnemy(UnitRecord self, Vector3 center)
        {
            var p = NavalProfile.Get((NavalModel)self.ModelId);
            int n = _units.FindEnemiesInRadius(center, p.PerceptionRadius, self.Faction, _hits, _hits.Length);
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                int s = _hits[i];
                ref var e = ref _units.GetSlotRef(s);
                if (e.Domain != EntityDomain.Naval) continue; // 只追水面目标
                float d = FlatDistance(center, e.Position);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

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
            // 关键：下一位置必须是水，否则停（不上岸）
            if (!_geo.IsWater(nx, nz)) return false;
            r.Position.x = nx;
            r.Position.z = nz;
            r.Position.y = _geo.TerrainHeight(nx, nz);
            return dist <= step + ArriveEps;
        }

        private bool FrontIsLand(UnitRecord r, Vector3 target)
        {
            float dx = target.x - r.Position.x;
            float dz = target.z - r.Position.z;
            float dist = (float)Math.Sqrt(dx * dx + dz * dz);
            if (dist < 1e-5f) return false;
            return !_geo.IsWater(r.Position.x + dx / dist, r.Position.z + dz / dist);
        }

        private static bool IsClose(Vector3 a, Vector3 b) => FlatDistance(a, b) <= ArriveEps;

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
