using System;
using PxC.Gameplay.Resources;

namespace PxC.Plugins.Economy
{
    /// <summary>
    /// 经济资源 SoA：所有势力、所有资源存入一段连续 float 数组
    /// [faction * ResourceCount + type]，最大化缓存命中。
    /// 资源数据为纯数据，无视图引用；存档直接序列化 _resources。
    /// </summary>
    public sealed class EconomyState
    {
        private float[] _resources;
        private int _factionCount;

        /// <summary>绑定势力数量并分配连续内存（新游戏调用）。</summary>
        public void Bind(int factionCount)
        {
            _factionCount = factionCount;
            _resources = new float[factionCount * ResourceTypes.Count];
        }

        /// <summary>势力数量。</summary>
        public int FactionCount => _factionCount;

        /// <summary>底层连续数组（供存档/监控读取，勿在外部改长度）。</summary>
        public float[] RawArray => _resources;

        /// <summary>从存档的连续数组恢复。</summary>
        public void Restore(float[] raw, int factionCount)
        {
            _factionCount = factionCount;
            _resources = raw ?? new float[factionCount * ResourceTypes.Count];
        }

        private static int Index(int faction, ResourceType type) =>
            faction * ResourceTypes.Count + (int)type;

        /// <summary>查询余额。</summary>
        public float Get(int faction, ResourceType type) =>
            _resources[Index(faction, type)];

        /// <summary>直接设置（钳制非负，资源不可为负）。</summary>
        public void Set(int faction, ResourceType type, float value)
        {
            if (value < 0f) value = 0f;
            _resources[Index(faction, type)] = value;
        }

        /// <summary>增减（产出/消耗结算；Add 可负，但最终余额钳制非负）。</summary>
        public void Add(int faction, ResourceType type, float amount)
        {
            int i = Index(faction, type);
            float v = _resources[i] + amount;
            _resources[i] = v < 0f ? 0f : v;
        }

        /// <summary>仅在余额足够时扣减（不改动余额），返回是否足够。</summary>
        public bool CanAfford(int faction, ResourceType type, float amount) =>
            _resources[Index(faction, type)] >= amount;

        /// <summary>清零某势力全部资源（灭国）。</summary>
        public void ClearFaction(int faction)
        {
            int baseIdx = faction * ResourceTypes.Count;
            Array.Clear(_resources, baseIdx, ResourceTypes.Count);
        }
    }
}
