using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Weather
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把天气插件放入 PluginRegistry。</summary>
    public static class WeatherPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new WeatherPlugin());
        }
    }
}
