using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Culture
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把文化插件放入 PluginRegistry。</summary>
    public static class CulturePluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new CulturePlugin());
        }
    }
}
