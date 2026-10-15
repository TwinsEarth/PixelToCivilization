using System;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Population
{
    /// <summary>人口分层数（=PopulationLayer 枚举项数）。</summary>
    public static class PopulationLayers
    {
        public const int Count = 6;
    }

    /// <summary>
    /// 人口 SoA：所有势力、各分层存入一段连续 int 数组
    /// [faction * LayerCount + layer]，纯数据、零视图引用。
    /// </summary>
    public sealed class PopulationState
    {
        private int[] _layers;
        private int _factionCount;

        public void Bind(int factionCount)
        {
            _factionCount = factionCount;
            _layers = new int[factionCount * PopulationLayers.Count];
        }

        public int FactionCount => _factionCount;
        public int[] RawArray => _layers;

        public void Restore(int[] raw, int factionCount)
        {
            _factionCount = factionCount;
            _layers = raw ?? new int[factionCount * PopulationLayers.Count];
        }

        private static int Index(int faction, PopulationLayer layer) =>
            faction * PopulationLayers.Count + (int)layer;

        public int Get(int faction, PopulationLayer layer) =>
            _layers[Index(faction, layer)];

        /// <summary>直接设置（钳制非负）。</summary>
        public void Set(int faction, PopulationLayer layer, int value)
        {
            if (value < 0) value = 0;
            _layers[Index(faction, layer)] = value;
        }

        /// <summary>增加（钳制非负）。</summary>
        public void Add(int faction, PopulationLayer layer, int amount)
        {
            int i = Index(faction, layer);
            int v = _layers[i] + amount;
            _layers[i] = v < 0 ? 0 : v;
        }

        /// <summary>尝试从某层扣减（不足返回 false，不改数据）。</summary>
        public bool TryRemove(int faction, PopulationLayer layer, int amount)
        {
            if (_layers[Index(faction, layer)] < amount) return false;
            _layers[Index(faction, layer)] -= amount;
            return true;
        }

        /// <summary>总人口（各层求和）。</summary>
        public int Total(int faction)
        {
            int baseIdx = faction * PopulationLayers.Count;
            int sum = 0;
            for (int i = 0; i < PopulationLayers.Count; i++)
                sum += _layers[baseIdx + i];
            return sum;
        }

        /// <summary>从指定层开始按顺序扣减（饥荒时按 [Elder→Children→Soldier→Artisan→Scholar→Laborer]）。</summary>
        public int RemoveInOrder(int faction, int amount, PopulationLayer[] order)
        {
            int removed = 0;
            for (int i = 0; i < order.Length && removed < amount; i++)
            {
                int idx = Index(faction, order[i]);
                int available = _layers[idx];
                int take = Math.Min(available, amount - removed);
                _layers[idx] = available - take;
                removed += take;
            }
            return removed;
        }
    }
}
