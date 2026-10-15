using System;
using System.Collections.Generic;
using PxC.Core.World;
using UnityEngine;

namespace PxC.Gameplay.Entities
{
    /// <summary>单位的运行时 AI 状态（驱动移动/寻敌/攻击/回撤）。</summary>
    public enum UnitAiState
    {
        /// <summary>驻守/待命。</summary>
        Idle = 0,

        /// <summary>向目标点移动（集结/巡逻/回防）。</summary>
        Moving = 1,

        /// <summary>追击敌方单位。</summary>
        Pursuing = 2,

        /// <summary>在射程内攻击。</summary>
        Attacking = 3,

        /// <summary>攻击后返回出生点/防区。</summary>
        Returning = 4,
    }

    /// <summary>
    /// 可移动单位的统一记录（SoA 视角下的一条 AoS 记录，内部为连续数组）。
    /// 位置/HP/速度/AI 状态为公共数据，领域特定参数（型号/军衔/10 维）由
    /// naval / groundforce 插件按 UnitId 另行持有。
    /// </summary>
    public struct UnitRecord
    {
        /// <summary>是否存活。</summary>
        public bool Alive;

        /// <summary>所属势力 id。</summary>
        public int Faction;

        /// <summary>领域：Land / Naval / Air。</summary>
        public EntityDomain Domain;

        /// <summary>型号 id（领域内由 naval / groundforce 解释，如 0=骑兵/0=小船）。</summary>
        public int ModelId;

        /// <summary>当前世界坐标。</summary>
        public Vector3 Position;

        /// <summary>移动目标点。</summary>
        public Vector3 MoveTarget;

        /// <summary>是否有移动目标点。</summary>
        public bool HasMoveTarget;

        /// <summary>当前生命。</summary>
        public float Hp;

        /// <summary>最大生命。</summary>
        public float MaxHp;

        /// <summary>移动速度（世界单位/秒，已按型号参数赋值）。</summary>
        public float Speed;

        /// <summary>AI 状态。</summary>
        public UnitAiState State;

        /// <summary>当前攻击目标单位的 slot（-1 无）。</summary>
        public int TargetSlot;

        /// <summary>等级（随战斗经验成长）。</summary>
        public int Level;

        /// <summary>出生点/防区中心（回撤与驻守锚点）。</summary>
        public Vector3 HomePosition;
    }

    /// <summary>
    /// 可移动单位权威登记表（运行时服务）。统一 slot 分配 + generation 失效检测，
    /// 死亡槽位回收复用（不做 swap-back，保证单位 id 引用稳定）。
    /// 地面/水上/空中一切会移动、会战斗的单位都登记于此。
    /// </summary>
    public sealed class UnitRegistry
    {
        private UnitRecord[] _records;
        private int[] _generations;
        private readonly Stack<int> _freeSlots = new Stack<int>();
        private int _highest;           // 已用到的最高槽位 +1
        private int _count;             // 存活数

        public UnitRegistry(int initialCapacity = 128)
        {
            int cap = Math.Max(16, initialCapacity);
            _records = new UnitRecord[cap];
            _generations = new int[cap];
        }

        /// <summary>存活单位数。</summary>
        public int Count => _count;

        /// <summary>已分配的槽位上界（含空闲），供遍历。</summary>
        public int Capacity => _highest;

        /// <summary>生成一个单位并返回其 EntityId。</summary>
        public EntityId Spawn(int faction, EntityDomain domain, int modelId, Vector3 position, float hp, float speed)
        {
            int slot;
            if (_freeSlots.Count > 0)
            {
                slot = _freeSlots.Pop();
            }
            else
            {
                slot = _highest;
                if (slot >= _records.Length) Grow();
                _highest++;
                _generations[slot] = 1;   // 首次使用即 generation=1，保证 IsValid
            }

            _records[slot] = new UnitRecord
            {
                Alive = true,
                Faction = faction,
                Domain = domain,
                ModelId = modelId,
                Position = position,
                MoveTarget = position,
                HasMoveTarget = false,
                Hp = hp,
                MaxHp = hp,
                Speed = speed,
                State = UnitAiState.Idle,
                TargetSlot = -1,
                Level = 1,
                HomePosition = position,
            };
            _count++;
            return new EntityId(slot, _generations[slot], domain);
        }

        /// <summary>校验 EntityId 是否仍存活有效。</summary>
        public bool IsAlive(EntityId id)
        {
            if (!id.IsValid) return false;
            int i = id.Index;
            if (i < 0 || i >= _highest) return false;
            return _generations[i] == id.Generation && _records[i].Alive;
        }

        /// <summary>取得单位记录的只读副本（无效时返回默认，Alive=false）。</summary>
        public UnitRecord GetSafe(EntityId id)
        {
            if (IsAlive(id)) return _records[id.Index];
            return default;
        }

        /// <summary>取得单位记录的引用用于修改（调用前必须确认 IsAlive）。</summary>
        public ref UnitRecord GetRef(EntityId id) => ref _records[id.Index];

        /// <summary>直接按 slot 取引用（遍历用，调用方自行检查 Alive）。</summary>
        public ref UnitRecord GetSlotRef(int slot) => ref _records[slot];

        /// <summary>按 slot 构造当前有效 EntityId（含正确 generation；无效 slot 返回 Invalid）。</summary>
        public EntityId GetEntityIdAt(int slot)
        {
            if (slot < 0 || slot >= _highest || !_records[slot].Alive)
                return EntityId.Invalid;
            return new EntityId(slot, _generations[slot], _records[slot].Domain);
        }

        /// <summary>击杀/移除单位，槽位回收，generation 自增使旧 id 失效。</summary>
        public void Kill(EntityId id)
        {
            if (!IsAlive(id)) return;
            int i = id.Index;
            _records[i].Alive = false;
            _generations[i]++;
            _freeSlots.Push(i);
            _count--;
        }

        /// <summary>命令单位移动到目标点（集结/回防/巡逻）。</summary>
        public void OrderMove(EntityId id, Vector3 destination)
        {
            if (!IsAlive(id)) return;
            ref var r = ref _records[id.Index];
            r.MoveTarget = destination;
            r.HasMoveTarget = true;
            r.TargetSlot = -1;
            r.State = UnitAiState.Moving;
        }

        /// <summary>
        /// 收集以 center 为圆心、radius 内、与 faction 敌对的存活单位 slot。
        /// 简单线性扫描（单位数受 SoA 上限约束，配合分块可进一步加速）。
        /// </summary>
        public int FindEnemiesInRadius(Vector3 center, float radius, int faction, int[] results, int maxResults)
        {
            float r2 = radius * radius;
            int n = 0;
            for (int i = 0; i < _highest && n < maxResults; i++)
            {
                ref var r = ref _records[i];
                if (!r.Alive || r.Faction == faction) continue;
                float dx = r.Position.x - center.x;
                float dz = r.Position.z - center.z;
                if (dx * dx + dz * dz <= r2)
                    results[n++] = i;
            }
            return n;
        }

        /// <summary>统计某势力、某领域的存活单位数。</summary>
        public int CountByFaction(int faction, EntityDomain domain)
        {
            int n = 0;
            for (int i = 0; i < _highest; i++)
            {
                ref var r = ref _records[i];
                if (r.Alive && r.Faction == faction && r.Domain == domain) n++;
            }
            return n;
        }

        /// <summary>清空所有单位（新游戏/卸载时调用）。</summary>
        public void Clear()
        {
            Array.Clear(_records, 0, _highest);
            for (int i = 0; i < _highest; i++) _generations[i]++;
            _freeSlots.Clear();
            _highest = 0;
            _count = 0;
        }

        private void Grow()
        {
            int newCap = _records.Length * 2;
            Array.Resize(ref _records, newCap);
            Array.Resize(ref _generations, newCap);
        }
    }
}
