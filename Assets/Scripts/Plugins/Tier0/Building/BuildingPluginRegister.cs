using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Building
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把 building 插件放入 PluginRegistry。</summary>
    public static class BuildingPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new BuildingPlugin());
        }
    }
}
