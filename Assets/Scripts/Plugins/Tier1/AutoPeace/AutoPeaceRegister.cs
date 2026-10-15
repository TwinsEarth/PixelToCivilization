using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.AutoPeace
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把自动和约插件放入 PluginRegistry。</summary>
    public static class AutoPeaceRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new AutoPeacePlugin());
        }
    }
}
