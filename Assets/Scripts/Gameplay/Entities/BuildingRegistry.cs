using System;
using System.Collections.Generic;
using UnityEngine;

namespace PxC.Gameplay.Entities
{
    /// <summary>
    /// 静态建筑记录（建筑/塔防/据点/设施）。位置固定、可被攻击、可升级。
    /// 形制与类型属性由 building 插件的类型目录按 TypeId 解释。
    /// </summary>
    public struct BuildingRecord
    {
        /// <summary>是否存在（未拆除）。</summary>
        public bool Alive;

        /// <summary>所属势力 id。</summary>
        public int Faction;

        /// <summary>建筑类型索引（building 类型目录）。</summary>
        public int TypeId;

        /// <summary>世界坐标。</summary>
        public Vector3 Position;

        /// <summary>当前生命。</summary>
        public float Hp;

        /// <summary>最大生命。</summary>
        public float MaxHp;

        /// <summary>等级（升级提升产出/耐久）。</summary>
        public int Level;

        /// <summary>是否为塔防（箭塔/火塔/炮塔/碉堡/烽火台），承载多层收分形制。</summary>
        public bool IsTower;
    }

    /// <summary>
    /// 建筑权威登记表（运行时服务）。slot 分配 + generation 失效，
    /// 死亡槽位回收复用（不 swap-back）。一切静态建筑/塔/设施登记于此。
    /// </summary>
    public sealed class BuildingRegistry
    {
        private BuildingRecord[] _records;
        private int[] _generations;
        private readonly Stack<int> _freeSlots = new Stack<int>();
        private int _highest;
        private int _count;

        public BuildingRegistry(int initialCapacity = 256)
        {
            int cap = Math.Max(16, initialCapacity);
            _records = new BuildingRecord[cap];
            _generations = new int[cap];
        }

        /// <summary>建筑总数。</summary>
        public int Count => _count;

        /// <summary>槽位上界。</summary>
        public int Capacity => _highest;

        /// <summary>建造一个建筑，返回其 slot。</summary>
        public int Spawn(int faction, int typeId, Vector3 position, float hp, bool isTower)
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
                _generations[slot] = 1;
            }

            _records[slot] = new BuildingRecord
            {
                Alive = true,
                Faction = faction,
                TypeId = typeId,
                Position = position,
                Hp = hp,
                MaxHp = hp,
                Level = 1,
                IsTower = isTower,
            };
            _count++;
            return slot;
        }

        /// <summary>slot 是否仍为有效建筑。</summary>
        public bool IsAlive(int slot, int generation)
        {
            if (slot < 0 || slot >= _highest) return false;
            return _generations[slot] == generation && _records[slot].Alive;
        }

        /// <summary>取记录引用（遍历/修改用，调用方检查 Alive）。</summary>
        public ref BuildingRecord GetSlotRef(int slot) => ref _records[slot];

        /// <summary>取记录只读副本。</summary>
        public BuildingRecord GetSafe(int slot) =>
            (slot >= 0 && slot < _highest) ? _records[slot] : default;

        /// <summary>取某 slot 的 generation。</summary>
        public int GetGeneration(int slot) =>
            (slot >= 0 && slot < _highest) ? _generations[slot] : 0;

        /// <summary>拆除/摧毁建筑，slot 回收。</summary>
        public void Kill(int slot)
        {
            if (slot < 0 || slot >= _highest || !_records[slot].Alive) return;
            _records[slot].Alive = false;
            _generations[slot]++;
            _freeSlots.Push(slot);
            _count--;
        }

        /// <summary>统计某势力某类型建筑数量。</summary>
        public int CountByType(int faction, int typeId)
        {
            int n = 0;
            for (int i = 0; i < _highest; i++)
            {
                ref var r = ref _records[i];
                if (r.Alive && r.Faction == faction && r.TypeId == typeId) n++;
            }
            return n;
        }

        /// <summary>统计某势力建筑总数。</summary>
        public int TotalCount(int faction)
        {
            int n = 0;
            for (int i = 0; i < _highest; i++)
            {
                ref var r = ref _records[i];
                if (r.Alive && r.Faction == faction) n++;
            }
            return n;
        }

        /// <summary>清空。</summary>
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
