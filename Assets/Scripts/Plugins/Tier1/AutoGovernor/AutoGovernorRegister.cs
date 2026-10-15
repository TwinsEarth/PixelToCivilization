using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.AutoGovernor
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把自动发展插件放入 PluginRegistry。</summary>
    public static class AutoGovernorRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new AutoGovernorPlugin());
        }
    }
}
