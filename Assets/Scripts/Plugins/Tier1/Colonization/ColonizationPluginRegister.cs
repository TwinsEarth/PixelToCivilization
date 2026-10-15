using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Colonization
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把殖民插件放入 PluginRegistry。</summary>
    public static class ColonizationPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new ColonizationPlugin());
        }
    }
}
