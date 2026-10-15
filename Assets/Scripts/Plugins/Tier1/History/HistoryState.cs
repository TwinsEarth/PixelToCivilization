using System;
using PxC.Gameplay.Services;

namespace PxC.Plugins.History
{
    /// <summary>
    /// 历史环形缓冲（纯数据）：固定容量，满了覆盖最旧；
    /// 按时间倒序访问（GetRecent(0)=最新）。
    /// </summary>
    public sealed class HistoryState
    {
        private readonly HistoryEntry[] _entries;
        private readonly int _capacity;
        private int _head;
        private int _count;

        public HistoryState(int capacity = 256)
        {
            _capacity = capacity < 16 ? 16 : capacity;
            _entries = new HistoryEntry[_capacity];
            _head = 0;
            _count = 0;
        }

        public int Count => _count;
        public int Capacity => _capacity;

        public void Add(HistoryEntry entry)
        {
            _entries[_head] = entry;
            _head++;
            if (_head >= _capacity) _head = 0;
            if (_count < _capacity) _count++;
        }

        /// <summary>倒序取第 i 条（i=0 最新）。</summary>
        public HistoryEntry GetRecent(int i)
        {
            if (i < 0 || i >= _count) return default;
            int newestIndex = (_head - 1 + _capacity) % _capacity;
            int idx = (newestIndex - i + _capacity) % _capacity;
            return _entries[idx];
        }

        /// <summary>读档：直接以传入数组重建（数组按时间正序，长度=count）。</summary>
        public void Restore(HistoryEntry[] entries)
        {
            _head = 0;
            _count = 0;
            if (entries == null) return;
            for (int i = 0; i < entries.Length && _count < _capacity; i++)
                Add(entries[i]);
        }

        /// <summary>存档：按时间正序导出全部条目。</summary>
        public HistoryEntry[] ToArray()
        {
            var result = new HistoryEntry[_count];
            for (int i = 0; i < _count; i++)
            {
                // 最旧 → 最新
                int oldestIndex = _count < _capacity
                    ? i
                    : _head;
                int idx = (oldestIndex + i) % _capacity;
                result[i] = _entries[idx];
            }
            return result;
        }
    }
}
