using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Disaster
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把灾害插件放入 PluginRegistry。</summary>
    public static class DisasterPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new DisasterPlugin());
        }
    }
}
