using System;
using System.Collections.Generic;
using PxC.Core.Contracts;

namespace PxC.Host
{
    /// <summary>
    /// 编译期插件注册点（无反射枚举）。
    /// 插件程序集在 [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] 中调用 Register；
    /// 宿主在 AfterSceneLoad 收集，按类型去重（兼容关闭 Domain Reload 的进 PlayMode）。
    /// </summary>
    public static class PluginRegistry
    {
        private static readonly Dictionary<Type, IPlugin> _plugins =
            new Dictionary<Type, IPlugin>(32);

        public static void Register(IPlugin plugin)
        {
            if (plugin == null) return;
            _plugins[plugin.GetType()] = plugin;
        }

        public static List<IPlugin> Collect()
        {
            var list = new List<IPlugin>(_plugins.Count);
            foreach (var kv in _plugins) list.Add(kv.Value);
            return list;
        }

        /// <summary>清空（仅测试/重置时调用）。</summary>
        public static void Clear() => _plugins.Clear();
    }
}
