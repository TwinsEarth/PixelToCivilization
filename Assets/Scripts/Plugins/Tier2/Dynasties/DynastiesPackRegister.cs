using PxC.Host;
using UnityEngine;

namespace PxC.Content.Dynasties
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把朝代内容包放入 PluginRegistry。</summary>
    public static class DynastiesPackRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new DynastiesPack());
        }
    }
}
