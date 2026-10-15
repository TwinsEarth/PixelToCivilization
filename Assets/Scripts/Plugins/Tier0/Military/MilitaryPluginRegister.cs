using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Military
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把军事总控插件放入 PluginRegistry。</summary>
    public static class MilitaryPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new MilitaryPlugin());
        }
    }
}
