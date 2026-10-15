using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Economy
{
    /// <summary>
    /// 编译期自动注册：BeforeSceneLoad 把 economy 插件放入 PluginRegistry，
    /// 宿主在 AfterSceneLoad 收集，无需反射枚举程序集。
    /// </summary>
    public static class EconomyPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new EconomyPlugin());
        }
    }
}
