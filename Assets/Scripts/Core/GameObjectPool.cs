using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.Core
{
    /// <summary>
    /// V7.0.3 通用 GameObject 对象池：按 key 分桶 Rent/Return，复用短生命周期视图
    /// （投射物、漂浮文字、粒子特效等），降低 Instantiate/Destroy 造成的 GC 尖峰。
    /// 池物体在 Return 时挂到隐藏根节点并 SetActive(false)；世界重置时调用 ClearAll 全部释放。
    /// </summary>
    public static class GameObjectPool
    {
        sealed class Bucket
        {
            public readonly Stack<GameObject> Free = new Stack<GameObject>();
            public readonly HashSet<GameObject> Rented = new HashSet<GameObject>();
            public int Cap = 64;
        }

        static readonly Dictionary<string, Bucket> _buckets = new Dictionary<string, Bucket>();
        static Transform _root;

        static Transform Root
        {
            get
            {
                if (_root == null)
                {
                    var go = new GameObject("ObjectPool");
                    _root = go.transform;
                }
                return _root;
            }
        }

        /// <summary>租用一个物体：优先取空闲实例，否则用 create 新建。返回的物体已激活并挂到 parent。</summary>
        public static GameObject Rent(string key, Transform parent, System.Func<GameObject> create)
        {
            if (string.IsNullOrEmpty(key)) key = "default";
            if (!_buckets.TryGetValue(key, out var bk)) { bk = new Bucket(); _buckets[key] = bk; }

            GameObject go = null;
            while (bk.Free.Count > 0)
            {
                go = bk.Free.Pop();
                if (go != null) break;
            }
            if (go == null) go = create != null ? create() : new GameObject(key);

            bk.Rented.RemoveWhere(o => o == null);
            bk.Rented.Add(go);
            go.transform.SetParent(parent != null ? parent : Root, true);
            if (!go.activeSelf) go.SetActive(true);
            return go;
        }

        /// <summary>归还物体：停用并缓存；超过桶容量或已销毁则直接释放。</summary>
        public static void Return(string key, GameObject go)
        {
            if (go == null) return;
            if (string.IsNullOrEmpty(key)) key = "default";
            if (!_buckets.TryGetValue(key, out var bk)) { Object.Destroy(go); return; }

            bk.Rented.Remove(go);
            if (bk.Free.Count >= bk.Cap) { Object.Destroy(go); return; }
            go.SetActive(false);
            go.transform.SetParent(Root, false);
            bk.Free.Push(go);
        }

        /// <summary>设置某桶最大缓存数量。</summary>
        public static void SetCapacity(string key, int cap)
        {
            if (!_buckets.TryGetValue(key, out var bk)) { bk = new Bucket(); _buckets[key] = bk; }
            bk.Cap = Mathf.Max(0, cap);
            while (bk.Free.Count > bk.Cap)
            {
                var go = bk.Free.Pop();
                if (go != null) Object.Destroy(go);
            }
        }

        /// <summary>世界重置/读档时清空全部池（含已租用物体，调用方应同时释放自己的引用）。</summary>
        public static void ClearAll()
        {
            foreach (var kv in _buckets)
            {
                var bk = kv.Value;
                while (bk.Free.Count > 0)
                {
                    var go = bk.Free.Pop();
                    if (go != null) Object.Destroy(go);
                }
                foreach (var go in bk.Rented) if (go != null) Object.Destroy(go);
                bk.Rented.Clear();
            }
            _buckets.Clear();
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }
        }

        public static string Stats()
        {
            int free = 0, rented = 0;
            foreach (var kv in _buckets) { free += kv.Value.Free.Count; kv.Value.Rented.RemoveWhere(o => o == null); rented += kv.Value.Rented.Count; }
            return $"[Pool] buckets={_buckets.Count} free={free} rented={rented}";
        }

        // ===== V9.6.4 内存架构扩展：预生长 / 分桶报告 / 桶计数（供 MemoryBudgetManager 采样与冒烟测试）=====

        /// <summary>预生长：一次创建 n 个实例进 Free 栈（热身，避免战斗开场首波 Instantiate 尖峰）。create 必须返回新实例。</summary>
        public static void Warmup(string key, Transform parent, System.Func<GameObject> create, int n)
        {
            if (n <= 0) return;
            if (!_buckets.TryGetValue(key, out var bk)) { bk = new Bucket(); _buckets[key] = bk; }
            for (int i = 0; i < n && bk.Free.Count < bk.Cap; i++)
            {
                var go = create != null ? create() : new GameObject(key);
                go.SetActive(false);
                go.transform.SetParent(parent != null ? parent : Root, false);
                bk.Free.Push(go);
            }
        }

        /// <summary>分桶统计（调试/测试用；Rented 会先清理已销毁引用）。</summary>
        public struct PoolStat
        {
            public string Key; public int Free; public int Rented; public int Cap;
        }

        public static List<PoolStat> StatsDetailed()
        {
            var list = new List<PoolStat>(_buckets.Count);
            foreach (var kv in _buckets)
            {
                var bk = kv.Value;
                bk.Rented.RemoveWhere(o => o == null);
                list.Add(new PoolStat { Key = kv.Key, Free = bk.Free.Count, Rented = bk.Rented.Count, Cap = bk.Cap });
            }
            return list;
        }

        /// <summary>桶数量（MemoryBudgetManager 报告用）。</summary>
        public static int BucketCount() => _buckets.Count;

        /// <summary>一行文本报告：如 "wfx_explosion:r0/f12/c64,wfx_hit:r0/f8/c64,..."（超长截断 160 字符）。</summary>
        public static string ReportText()
        {
            var list = StatsDetailed();
            if (list.Count == 0) return "empty";
            var sb = new System.Text.StringBuilder(160);
            int shown = 0;
            foreach (var s in list)
            {
                if (sb.Length >= 140) break;
                if (shown > 0) sb.Append(',');
                sb.Append(s.Key).Append(":r").Append(s.Rented).Append("/f").Append(s.Free).Append("/c").Append(s.Cap);
                shown++;
            }
            if (list.Count > shown) sb.Append(",...");
            return sb.ToString();
        }
    }
}
