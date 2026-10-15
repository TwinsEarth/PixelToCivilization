using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.GroundForce
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把地面部队插件放入 PluginRegistry。</summary>
    public static class GroundForcePluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new GroundForcePlugin());
        }
    }
}
