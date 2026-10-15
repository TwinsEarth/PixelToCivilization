using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Naval
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把海军插件放入 PluginRegistry。</summary>
    public static class NavalPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new NavalPlugin());
        }
    }
}
