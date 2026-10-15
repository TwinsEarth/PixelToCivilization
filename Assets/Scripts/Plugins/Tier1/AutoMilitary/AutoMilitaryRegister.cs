using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.AutoMilitary
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把自动军事建设插件放入 PluginRegistry。</summary>
    public static class AutoMilitaryRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new AutoMilitaryPlugin());
        }
    }
}
