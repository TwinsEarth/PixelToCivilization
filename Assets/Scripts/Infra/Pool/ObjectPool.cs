using System;
using System.Collections.Generic;
using PxC.Core.Services;
using UnityEngine;

namespace PxC.Infra.Pool
{
    /// <summary>标记池对象，记录其来源 prefab（归还时据此分组）。</summary>
    public sealed class PooledObject : MonoBehaviour
    {
        [NonSerialized] public GameObject Source;
    }

    /// <summary>
    /// 统一对象池：GameObject 按 prefab 分组（Stack 复用 + 活跃计数），
    /// 数据对象按类型分组。Rent/Return 配对，Prewarm 预热，Clear 释放。
    /// </summary>
    public sealed class ObjectPool : IObjectPool
    {
        private sealed class GoPool
        {
            public readonly Stack<GameObject> Inactive = new Stack<GameObject>();
            public int Active;
        }

        private readonly Dictionary<GameObject, GoPool> _goPools;
        private readonly Dictionary<Type, Stack<object>> _dataPools;
        private readonly Transform _root;

        public ObjectPool(Transform root)
        {
            _root = root;
            _goPools = new Dictionary<GameObject, GoPool>(64);
            _dataPools = new Dictionary<Type, Stack<object>>(64);
        }

        public GameObject Rent(GameObject prefab, Transform parent = null)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (!_goPools.TryGetValue(prefab, out var pool))
            {
                pool = new GoPool();
                _goPools[prefab] = pool;
            }

            GameObject inst;
            if (pool.Inactive.Count > 0)
            {
                inst = pool.Inactive.Pop();
            }
            else
            {
                inst = UnityEngine.Object.Instantiate(prefab);
                var marker = inst.GetComponent<PooledObject>();
                if (marker == null) marker = inst.AddComponent<PooledObject>();
                marker.Source = prefab;
            }

            inst.transform.SetParent(parent ?? _root, false);
            inst.SetActive(true);
            pool.Active++;
            return inst;
        }

        public void Return(GameObject instance)
        {
            if (instance == null) return;
            var marker = instance.GetComponent<PooledObject>();
            if (marker == null || marker.Source == null)
            {
                UnityEngine.Object.Destroy(instance);
                return;
            }
            if (!_goPools.TryGetValue(marker.Source, out var pool))
            {
                UnityEngine.Object.Destroy(instance);
                return;
            }
            instance.SetActive(false);
            instance.transform.SetParent(_root, false);
            pool.Inactive.Push(instance);
            pool.Active--;
        }

        public void Prewarm(GameObject prefab, int count, Transform parent = null)
        {
            if (!_goPools.TryGetValue(prefab, out var pool))
            {
                pool = new GoPool();
                _goPools[prefab] = pool;
            }
            // 幂等：补齐到 count（Inactive + Active），而非 Rent/Return 反复复用同一实例
            int need = count - (pool.Inactive.Count + pool.Active);
            for (int i = 0; i < need; i++)
            {
                var inst = UnityEngine.Object.Instantiate(prefab);
                var marker = inst.GetComponent<PooledObject>();
                if (marker == null) marker = inst.AddComponent<PooledObject>();
                marker.Source = prefab;
                inst.SetActive(false);
                inst.transform.SetParent(parent ?? _root, false);
                pool.Inactive.Push(inst);
            }
        }

        public T RentData<T>(Func<T> factory = null) where T : class, new()
        {
            Type t = typeof(T);
            if (_dataPools.TryGetValue(t, out var stack) && stack.Count > 0)
                return (T)stack.Pop();
            return factory != null ? factory() : new T();
        }

        public void ReturnData<T>(T instance) where T : class
        {
            if (instance == null) return;
            Type t = typeof(T);
            if (!_dataPools.TryGetValue(t, out var stack))
            {
                stack = new Stack<object>();
                _dataPools[t] = stack;
            }
            stack.Push(instance);
        }

        public int ActiveCount(GameObject prefab) =>
            _goPools.TryGetValue(prefab, out var p) ? p.Active : 0;

        public int InactiveCount(GameObject prefab) =>
            _goPools.TryGetValue(prefab, out var p) ? p.Inactive.Count : 0;

        public void Clear(GameObject prefab)
        {
            if (!_goPools.TryGetValue(prefab, out var p)) return;
            while (p.Inactive.Count > 0)
                UnityEngine.Object.Destroy(p.Inactive.Pop());
        }

        public void ClearAll()
        {
            foreach (var kv in _goPools)
            {
                while (kv.Value.Inactive.Count > 0)
                    UnityEngine.Object.Destroy(kv.Value.Inactive.Pop());
            }
            _goPools.Clear();
            _dataPools.Clear();
        }
    }
}
