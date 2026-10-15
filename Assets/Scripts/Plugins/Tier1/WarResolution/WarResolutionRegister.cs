using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.WarResolution
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把战争结果插件放入 PluginRegistry。</summary>
    public static class WarResolutionRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new WarResolutionPlugin());
        }
    }
}
