using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.AutoDiplomacy
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把自动外交插件放入 PluginRegistry。</summary>
    public static class AutoDiplomacyRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new AutoDiplomacyPlugin());
        }
    }
}
