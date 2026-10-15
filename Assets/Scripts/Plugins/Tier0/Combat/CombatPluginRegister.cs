using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Combat
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把战斗插件放入 PluginRegistry。</summary>
    public static class CombatPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new CombatPlugin());
        }
    }
}
