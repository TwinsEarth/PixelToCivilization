using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.History
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把历史插件放入 PluginRegistry。</summary>
    public static class HistoryPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new HistoryPlugin());
        }
    }
}
