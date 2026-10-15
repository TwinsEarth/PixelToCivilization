using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Tech
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把科技插件放入 PluginRegistry。</summary>
    public static class TechPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new TechPlugin());
        }
    }
}
