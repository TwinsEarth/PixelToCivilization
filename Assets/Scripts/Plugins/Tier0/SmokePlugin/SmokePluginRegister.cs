using PxC.Host;
using UnityEngine;

namespace PxC.Plugin.Smoke
{
    /// <summary>
    /// 编译期自动注册：在 BeforeSceneLoad 阶段把插件放入 PluginRegistry。
    /// 宿主在 AfterSceneLoad 收集，无需反射枚举程序集。
    /// </summary>
    public static class SmokePluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new SmokePlugin());
        }
    }
}
