using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Population
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把 population 插件放入 PluginRegistry。</summary>
    public static class PopulationPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new PopulationPlugin());
        }
    }
}
