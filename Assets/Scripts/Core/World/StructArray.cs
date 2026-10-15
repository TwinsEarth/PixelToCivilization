using System;

namespace PxC.Core.World
{
    /// <summary>
    /// 连续内存列（SoA 基础构件）。存储 struct，使用 SwapBack 删除保持数组紧凑，
    /// 最大化缓存命中率；容量自动倍增。高频系统应通过 Ref 访问，避免拷贝。
    /// </summary>
    public struct StructArray<T> where T : struct
    {
        private T[] _items;
        private int _count;

        public StructArray(int capacity)
        {
            _items = new T[capacity <= 0 ? 8 : capacity];
            _count = 0;
        }

        public int Count => _count;
        public int Capacity => _items == null ? 0 : _items.Length;
        public bool IsCreated => _items != null;

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        /// <summary>返回元素引用（批量遍历时避免结构体拷贝）。</summary>
        public ref T Ref(int index) => ref _items[index];

        /// <summary>追加元素，返回其索引。</summary>
        public int Add(T value)
        {
            if (_items == null) _items = new T[8];
            if (_count == _items.Length) Resize(_items.Length * 2);
            _items[_count] = value;
            return _count++;
        }

        /// <summary>用末尾元素覆盖该位置并缩短长度（不保持顺序）。</summary>
        public void RemoveSwapBack(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            int last = _count - 1;
            _items[index] = _items[last];
            _items[last] = default;
            _count--;
        }

        public void EnsureCapacity(int required)
        {
            if (_items == null) { _items = new T[required]; return; }
            if (_items.Length < required) Resize(required);
        }

        private void Resize(int newCapacity)
        {
            var grown = new T[newCapacity];
            Array.Copy(_items, grown, _count);
            _items = grown;
        }

        public void Clear()
        {
            if (_items != null) Array.Clear(_items, 0, _count);
            _count = 0;
        }

        /// <summary>遍历每一行（只读处理）。</summary>
        public void ForEach(Action<T> action)
        {
            for (int i = 0; i < _count; i++) action(_items[i]);
        }
    }
}
