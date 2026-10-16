using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.WarReparations
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把战争赔款插件放入 PluginRegistry。</summary>
    public static class WarReparationsRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new WarReparationsPlugin());
        }
    }
}
