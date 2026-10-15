using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Vassalization
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把战争附庸与纳贡插件放入 PluginRegistry。</summary>
    public static class VassalizationRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new VassalizationPlugin());
        }
    }
}
