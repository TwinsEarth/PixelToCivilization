using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.Core
{
    /// <summary>
    /// V9.7.0 Addressables 适配层（不引入依赖铁律下的就绪架构）。
    /// 统一资产加载/释放语义：Load 登记引用计数，Release 必须镜像配对，禁止只 Destroy 不 Release。
    ///  - 已安装 com.unity.addressables（#if UNITY_ADDRESSABLES）：走 Addressables.LoadAssetAsync / Release(handle)，
    ///    引用计数由 Addressables 内部管理，本层额外登记手柄便于诊断。
    ///  - 未安装（当前仓库现状）：降级 Resources.Load + 本层引用计数语义（LoadCount 增减），
    ///    保持"加载/释放必须配对"的统一契约，未来启用 Addressables 时业务代码零改动。
    /// 诊断：LiveCount / Stats() / WebAddressablesProbe 可验证当前未释放残留。
    /// </summary>
    public static class AddressablesManager
    {
        sealed class Entry { public int LoadCount; public string Kind; public object Handle; }

        static readonly Dictionary<string, Entry> _registry = new Dictionary<string, Entry>();
        static readonly object _lock = new object();

        /// <summary>当前未释放的加载项数（探针/泄漏检测用）。</summary>
        public static int LiveCount
        {
            get { lock (_lock) return _registry.Count; }
        }

        /// <summary>加载资产：返回实例。调用方用完后必须调用 Release(key) 配对释放。</summary>
        public static T Load<T>(string key) where T : Object
        {
            lock (_lock)
            {
                if (!_registry.TryGetValue(key, out var e)) { e = new Entry(); _registry[key] = e; }
                e.LoadCount++;
            }
#if UNITY_ADDRESSABLES
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(key);
            handle.WaitForCompletion();
            if (!handle.IsValid() || handle.Result == null)
            {
                Debug.LogError("[AddrMgr] Addressables 加载失败: " + key);
                return null;
            }
            lock (_lock) { if (_registry.TryGetValue(key, out var e2)) e2.Handle = handle; }
            return handle.Result;
#else
            var res = Resources.Load<T>(key);
            if (res == null)
                Debug.LogError("[AddrMgr] Resources.Load 失败: " + key);
            return res;
#endif
        }

        /// <summary>释放资产：必须与 Load 镜像配对。LoadCount 归零才真正释放/卸载。</summary>
        public static void Release(string key)
        {
            Entry e = null;
            lock (_lock) { if (_registry.TryGetValue(key, out e)) { e.LoadCount--; if (e.LoadCount <= 0) _registry.Remove(key); } }
            if (e == null)
            {
                Debug.LogWarning("[AddrMgr] Release 未匹配的 Load: " + key + "（忽略；可能是重复释放）");
                return;
            }
#if UNITY_ADDRESSABLES
            if (e.Handle is UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle h && h.IsValid())
                UnityEngine.AddressableAssets.Addressables.Release(h);
#else
            if (e.LoadCount <= 0)
                Resources.UnloadUnusedAssets();
#endif
        }

        /// <summary>统计文本（探针/调试）：当前在册的 key 与引用次数。</summary>
        public static string Stats()
        {
            var sb = new System.Text.StringBuilder();
            lock (_lock)
            {
                sb.Append("live=").Append(_registry.Count);
                int n = 0;
                foreach (var kv in _registry)
                {
                    if (n++ >= 8) break;
                    sb.Append(' ').Append(kv.Key).Append('x').Append(kv.Value.LoadCount);
                }
            }
            return sb.ToString();
        }
    }
}
