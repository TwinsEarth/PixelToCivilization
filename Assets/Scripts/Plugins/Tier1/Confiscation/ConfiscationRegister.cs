using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Confiscation
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把灭国没收插件放入 PluginRegistry。</summary>
    public static class ConfiscationRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new ConfiscationPlugin());
        }
    }
}
