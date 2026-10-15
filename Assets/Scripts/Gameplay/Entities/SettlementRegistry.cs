using System;
using System.Collections.Generic;
using UnityEngine;

namespace PxC.Gameplay.Entities
{
    /// <summary>
    /// 聚落类型（决定扩张的时代门控与选址）。
    /// 陆地聚落任何时代可建；沿海聚落中世纪后；岛屿聚落大航海后；星球聚落太空时代。
    /// </summary>
    public enum SettlementKind
    {
        /// <summary>首都（每势力最初的核心聚落，唯一）。</summary>
        Capital = 0,

        /// <summary>村落（陆地农耕聚落）。</summary>
        Village = 1,

        /// <summary>城镇（陆地大型聚落）。</summary>
        Town = 2,

        /// <summary>沿海聚落（陆地边缘，兼具港口）。</summary>
        Coastal = 3,

        /// <summary>岛屿聚落（跨海殖民）。</summary>
        Island = 4,

        /// <summary>前哨据点（边境/资源点）。</summary>
        Outpost = 5,

        /// <summary>星球殖民地（太空时代）。</summary>
        Planet = 6,
    }

    /// <summary>
    /// 聚落记录（城市/村落/据点/殖民地）。一个势力可拥有多个聚落，
    /// 聚落是领土扩张的锚点，其人口与等级决定势力发展度。
    /// </summary>
    public struct SettlementRecord
    {
        /// <summary>是否存在。</summary>
        public bool Alive;

        /// <summary>所属势力 id。</summary>
        public int Faction;

        /// <summary>聚落类型（SettlementKind）。</summary>
        public int Kind;

        /// <summary>聚落名（村/城/殖民地名）。</summary>
        public string Name;

        /// <summary>世界坐标（领土中心）。</summary>
        public Vector3 Position;

        /// <summary>等级（1-10，发展自动提升）。</summary>
        public int Level;

        /// <summary>聚落规模人口（移民基数 + 自身增长）。</summary>
        public int Population;

        /// <summary>建立的游戏年份。</summary>
        public int FoundedYear;

        /// <summary>发展度（年度累积，决定升级）。</summary>
        public float Prosperity;
    }

    /// <summary>
    /// 聚落权威登记表（运行时服务）。slot 分配 + generation 失效，
    /// 死亡槽位回收复用（不 swap-back）。一切聚落/城市/殖民地登记于此。
    /// </summary>
    public sealed class SettlementRegistry
    {
        private SettlementRecord[] _records;
        private int[] _generations;
        private readonly Stack<int> _freeSlots = new Stack<int>();
        private int _highest;
        private int _count;

        public SettlementRegistry(int initialCapacity = 128)
        {
            int cap = Math.Max(16, initialCapacity);
            _records = new SettlementRecord[cap];
            _generations = new int[cap];
        }

        /// <summary>聚落总数。</summary>
        public int Count => _count;

        /// <summary>槽位上界。</summary>
        public int Capacity => _highest;

        /// <summary>建立一个聚落，返回其 slot。</summary>
        public int Found(
            int faction,
            SettlementKind kind,
            string name,
            Vector3 position,
            int foundedYear,
            int initialPopulation,
            float initialProsperity = 0f)
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

            _records[slot] = new SettlementRecord
            {
                Alive = true,
                Faction = faction,
                Kind = (int)kind,
                Name = name,
                Position = position,
                Level = 1,
                Population = Math.Max(1, initialPopulation),
                FoundedYear = foundedYear,
                Prosperity = initialProsperity,
            };
            _count++;
            return slot;
        }

        /// <summary>slot 是否仍为有效聚落。</summary>
        public bool IsAlive(int slot, int generation)
        {
            if (slot < 0 || slot >= _highest) return false;
            return _generations[slot] == generation && _records[slot].Alive;
        }

        /// <summary>取记录引用（遍历/修改用，调用方检查 Alive）。</summary>
        public ref SettlementRecord GetSlotRef(int slot) => ref _records[slot];

        /// <summary>取记录只读副本。</summary>
        public SettlementRecord GetSafe(int slot) =>
            (slot >= 0 && slot < _highest) ? _records[slot] : default;

        /// <summary>取某 slot 的 generation。</summary>
        public int GetGeneration(int slot) =>
            (slot >= 0 && slot < _highest) ? _generations[slot] : 0;

        /// <summary>摧毁/废弃聚落，slot 回收。</summary>
        public void Kill(int slot)
        {
            if (slot < 0 || slot >= _highest || !_records[slot].Alive) return;
            _records[slot].Alive = false;
            _generations[slot]++;
            _freeSlots.Push(slot);
            _count--;
        }

        /// <summary>统计某势力聚落总数。</summary>
        public int CountByFaction(int faction)
        {
            int n = 0;
            for (int i = 0; i < _highest; i++)
            {
                ref var r = ref _records[i];
                if (r.Alive && r.Faction == faction) n++;
            }
            return n;
        }

        /// <summary>查找某势力的首都 slot（无则 -1）。</summary>
        public int FindCapital(int faction)
        {
            for (int i = 0; i < _highest; i++)
            {
                ref var r = ref _records[i];
                if (r.Alive && r.Faction == faction && r.Kind == (int)SettlementKind.Capital)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 收集以 center 为圆心、radius 内的聚落 slot，结果写入 results，返回数量。
        /// 用于领土查询（哪些聚落覆盖某点）。
        /// </summary>
        public int GetInRadius(Vector3 center, float radius, int[] results)
        {
            int n = 0;
            float r2 = radius * radius;
            for (int i = 0; i < _highest && n < results.Length; i++)
            {
                ref var r = ref _records[i];
                if (!r.Alive) continue;
                float dx = r.Position.x - center.x;
                float dz = r.Position.z - center.z;
                if (dx * dx + dz * dz <= r2)
                {
                    results[n] = i;
                    n++;
                }
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
