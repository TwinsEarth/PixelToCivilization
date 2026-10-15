using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.CoercedPeace
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把压制性和约插件放入 PluginRegistry。</summary>
    public static class CoercedPeaceRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new CoercedPeacePlugin());
        }
    }
}
