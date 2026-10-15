using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Conquest
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把领土占领插件放入 PluginRegistry。</summary>
    public static class ConquestRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new ConquestPlugin());
        }
    }
}
