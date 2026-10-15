using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Policy
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把政策插件放入 PluginRegistry。</summary>
    public static class PolicyPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new PolicyPlugin());
        }
    }
}
