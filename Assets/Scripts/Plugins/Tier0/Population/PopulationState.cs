using System;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Population
{
    /// <summary>
    /// 人口权威状态（v10.4.0 治本统一）。
    /// 人口唯一存储为 <b>聚落 × 分层（settlement × layer）</b>的连续 SoA：
    /// 数组布局 [settlementSlot * LayerCount + layer]，与 SettlementRegistry 的 slot 对齐。
    /// 势力总人口 = Σ该势力所有聚落各层；聚落人口 = Σ该聚落各层。
    /// 人口不再有"势力账本"与"聚落账本"两套，彻底消除背离。
    /// </summary>
    public sealed class PopulationState
    {
        /// <summary>分层数量（与 PopulationLayer 枚举一致）。</summary>
        public const int LayerCount = 6;

        private int[] _layers;
        private int _slotCapacity;

        public PopulationState(int initialSlotCapacity = 16)
        {
            _slotCapacity = Math.Max(16, initialSlotCapacity);
            _layers = new int[_slotCapacity * LayerCount];
        }

        /// <summary>当前可容纳的聚落槽位数。</summary>
        public int SlotCapacity => _slotCapacity;

        /// <summary>连续人口分层数组（只读视图，布局 [slot*LayerCount + layer]）。</summary>
        public int[] RawArray => _layers;

        /// <summary>
        /// 确保能容纳至少 slotCount 个聚落槽位。容量不足时倍增扩容。
        /// 由聚落 Found / 读档恢复时调用（与 SettlementRegistry 对齐）。
        /// </summary>
        public void EnsureCapacity(int slotCount)
        {
            if (slotCount <= _slotCapacity) return;
            int newCap = _slotCapacity;
            while (newCap < slotCount) newCap *= 2;
            Array.Resize(ref _layers, newCap * LayerCount);
            _slotCapacity = newCap;
        }

        /// <summary>
        /// 清零某聚落槽位的全部分层（聚落被 Kill / 废弃时调用）。
        /// </summary>
        public void ClearSlot(int slot)
        {
            if (slot < 0 || slot >= _slotCapacity) return;
            int baseIdx = slot * LayerCount;
            for (int i = 0; i < LayerCount; i++) _layers[baseIdx + i] = 0;
        }

        /// <summary>
        /// 读档恢复：用存档连续数组与槽位容量重建状态。
        /// 数组长度必须与 slotCapacity × LayerCount 一致（不一致时按可信长度对齐）。
        /// </summary>
        public void Restore(int[] raw, int slotCapacity)
        {
            if (raw == null)
            {
                _slotCapacity = Math.Max(16, slotCapacity);
                _layers = new int[_slotCapacity * LayerCount];
                return;
            }

            // 以数组长度反推槽位容量（取数组声明与 slotCapacity 中可信值）。
            int inferred = raw.Length / LayerCount;
            int cap = Math.Max(inferred, Math.Max(16, slotCapacity));
            EnsureCapacity(cap);
            Array.Clear(_layers, 0, _layers.Length);
            Array.Copy(raw, 0, _layers, 0, raw.Length);
        }

        private static int Index(int slot, PopulationLayer layer) =>
            slot * LayerCount + (int)layer;

        /// <summary>取某聚落某分层人口。</summary>
        public int Get(int slot, PopulationLayer layer)
        {
            int i = Index(slot, layer);
            return (i >= 0 && i < _layers.Length) ? _layers[i] : 0;
        }

        /// <summary>设置某聚落某分层人口（非负，钳制）。</summary>
        public void Set(int slot, PopulationLayer layer, int value)
        {
            EnsureCapacity(slot + 1);
            _layers[Index(slot, layer)] = Math.Max(0, value);
        }

        /// <summary>增减某聚落某分层人口（amount 可为负；结果不低于 0）。</summary>
        public void Add(int slot, PopulationLayer layer, int amount)
        {
            EnsureCapacity(slot + 1);
            int i = Index(slot, layer);
            int v = _layers[i] + amount;
            _layers[i] = Math.Max(0, v);
        }

        /// <summary>尝试从某聚落某分层移除 amount；不足返回 false 且不改状态。</summary>
        public bool TryRemove(int slot, PopulationLayer layer, int amount)
        {
            if (amount < 0) return false;
            int i = Index(slot, layer);
            if (i < 0 || i >= _layers.Length || _layers[i] < amount) return false;
            _layers[i] -= amount;
            return true;
        }

        /// <summary>某聚落总人口（Σ各层）。</summary>
        public int SlotTotal(int slot)
        {
            if (slot < 0 || slot >= _slotCapacity) return 0;
            int baseIdx = slot * LayerCount;
            int sum = 0;
            for (int i = 0; i < LayerCount; i++) sum += _layers[baseIdx + i];
            return sum;
        }

        /// <summary>
        /// 按指定分层顺序从某聚落移除最多 amount 人（灾害/饥荒优先扣弱势层）。
        /// 返回实际移除数量。每层依次尽量扣满。
        /// </summary>
        public int RemoveInOrder(int slot, int amount, PopulationLayer[] order)
        {
            if (amount <= 0 || order == null) return 0;
            int remaining = amount;
            for (int i = 0; i < order.Length && remaining > 0; i++)
            {
                int idx = Index(slot, order[i]);
                if (idx < 0 || idx >= _layers.Length) continue;
                int have = _layers[idx];
                int take = have < remaining ? have : remaining;
                _layers[idx] -= take;
                remaining -= take;
            }
            return amount - remaining;
        }
    }
}
