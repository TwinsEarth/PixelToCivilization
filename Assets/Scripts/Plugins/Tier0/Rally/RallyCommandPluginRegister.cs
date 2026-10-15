using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Rally
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把集结令接线插件放入 PluginRegistry。</summary>
    public static class RallyCommandPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new RallyCommandPlugin());
        }
    }
}
