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

        /// <summary>战略行军包围敌方首府时的环布半径（世界单位）。</summary>
        private const float EncircleRadius = 3.0f;

        /// <summary>黄金角（环布去中心，保证行军目标点不重叠）。</summary>
        private const float GoldenAngle = 2.399963f;

        /// <summary>
        /// 战略接敌半径：战争状态下，在此范围内主动拦截最近的敌方单位（而非直奔首府），
        /// 避免两军沿直线互相错过（"换家"）。应明显大于战术感知半径。
        /// </summary>
        private const float StrategicEngageRadius = 45f;

        /// <summary>
        /// 追击放弃时间（模拟秒）：追击持续超过该值仍无法进入攻击范围
        /// （目标持续逃跑/双方绕圈/水阻），放弃该目标并回 Idle 重新评估，
        /// 防止部队被一个追不上的目标永久牵走。
        /// </summary>
        private const float GiveUpSeconds = 20f;

        private readonly UnitRegistry _units;
        private readonly GroundForceState _state;
        private readonly IGeographyQuery _geo;
        private readonly Func<ICombatService> _combatProvider;
        private readonly SettlementRegistry _settlements;
        private readonly Func<IMilitaryService> _militaryProvider;
        private readonly Action<string> _log;

        // 复用的敌人搜索缓冲（避免每帧分配）
        private readonly int[] _hits = new int[32];

        public GroundForceAi(
            UnitRegistry units,
            GroundForceState state,
            IGeographyQuery geo,
            Func<ICombatService> combatProvider,
            SettlementRegistry settlements,
            Func<IMilitaryService> militaryProvider,
            Action<string> log)
        {
            _units = units;
            _state = state;
            _geo = geo;
            _combatProvider = combatProvider;
            _settlements = settlements;
            _militaryProvider = militaryProvider;
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
                        TickMoving(slot, ref r, dt);
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
        private void EnterPursuit(int slot, ref UnitRecord r, int target)
        {
            r.TargetSlot = target;
            _state.ResetPursuitTime(slot);
            r.State = UnitAiState.Pursuing;
        }

        private void TickIdle(int slot, ref UnitRecord r)
        {
            var p = GroundForceProfile.Get((GroundForceModel)r.ModelId);

            // 1. 优先处理感知范围内的敌人（战术反应）。
            int target = FindNearestEnemy(slot, r, r.Position, p.PerceptionRadius);
            if (target >= 0)
            {
                EnterPursuit(slot, ref r, target);
                return;
            }

            // 2. 战术感知无敌，但本势力处于战争状态：在战略接敌半径内拦截最近敌方单位，
            //    使其互相靠拢接敌（而不是沿直线向首府、互相错过）。
            int intercept = FindNearestEnemy(slot, r, r.Position, StrategicEngageRadius);
            if (intercept >= 0)
            {
                EnterPursuit(slot, ref r, intercept);
                return;
            }

            // 3. 附近无敌方单位：向敌方首府推进（战略纵深）。
            TryStrategicMarch(slot, ref r);
        }

        /// <summary>
        /// 战略行军：本势力处于战争状态时，选定一个敌对势力的首府，向其环布点行军。
        /// 走到敌方附近后感知到敌方单位即进入追击/攻击，使战争真正接敌。
        /// </summary>
        private void TryStrategicMarch(int slot, ref UnitRecord r)
        {
            if (_settlements == null) return;
            var military = _militaryProvider != null ? _militaryProvider() : null;
            if (military == null) return;

            // 遍历所有势力，找到第一个交战中、仍存活、且有首府的敌方势力。
            int enemyFaction = -1;
            int enemyCapitalSlot = -1;
            int factionCount = _settlements.Capacity; // 上界（足够大）；实际以 FindCapital 结果为准
            // 用 FactionRegistry 的数量更准确，但 AI 只持有 SettlementRegistry。
            // 改用 UnitRegistry/Faction 数量：遍历 0..MaxFactions-1（8）。
            for (int other = 0; other < 8; other++)
            {
                if (other == r.Faction) continue;
                if (!military.IsAtWar(r.Faction, other)) continue;
                int capSlot = _settlements.FindCapital(other);
                if (capSlot < 0) continue;
                ref var cap = ref _settlements.GetSlotRef(capSlot);
                if (!cap.Alive) continue;
                enemyFaction = other;
                enemyCapitalSlot = capSlot;
                break;
            }

            if (enemyFaction < 0) return;
            ref var enemyCapital = ref _settlements.GetSlotRef(enemyCapitalSlot);

            // 包围点：围绕敌方首府做黄金角环布（不同单位落点不同，避免重叠）。
            float angle = slot * GoldenAngle;
            var dest = new Vector3(
                enemyCapital.Position.x + Mathf.Cos(angle) * EncircleRadius,
                0f,
                enemyCapital.Position.z + Mathf.Sin(angle) * EncircleRadius);

            r.MoveTarget = dest;
            r.HasMoveTarget = true;
            r.State = UnitAiState.Moving;
        }

        private void TickMoving(int slot, ref UnitRecord r, float dt)
        {
            var p = GroundForceProfile.Get((GroundForceModel)r.ModelId);

            // 行军途中先做战术感知：近距离遭遇立即接战（不等走到首府）。
            int encounter = FindNearestEnemy(slot, r, r.Position, p.PerceptionRadius);
            if (encounter >= 0)
            {
                r.HasMoveTarget = false;
                EnterPursuit(slot, ref r, encounter);
                return;
            }

            // 战略接敌：在更大半径内拦截敌方单位，避免行军途中错过。
            int intercept = FindNearestEnemy(slot, r, r.Position, StrategicEngageRadius);
            if (intercept >= 0)
            {
                r.HasMoveTarget = false;
                EnterPursuit(slot, ref r, intercept);
                return;
            }

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

            // 绕圈治理：累计追击时间，超过 GiveUpSeconds 仍无法进入攻击范围
            // （目标持续逃跑/绕圈），放弃该目标，回 Idle 重新评估，不再被永久牵走。
            _state.AddPursuitTime(slot, dt);
            if (_state.GetPursuitTime(slot) >= GiveUpSeconds)
            {
                r.TargetSlot = -1;
                r.State = UnitAiState.Idle;
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

        private int FindNearestEnemy(int selfSlot, UnitRecord self, Vector3 center, float radius)
        {
            int n = _units.FindEnemiesInRadius(
                center, radius, self.Faction, _hits, _hits.Length);
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
