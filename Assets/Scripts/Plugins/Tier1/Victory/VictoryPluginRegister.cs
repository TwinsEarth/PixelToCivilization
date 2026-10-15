using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Victory
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把胜利插件放入 PluginRegistry。</summary>
    public static class VictoryPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new VictoryPlugin());
        }
    }
}
