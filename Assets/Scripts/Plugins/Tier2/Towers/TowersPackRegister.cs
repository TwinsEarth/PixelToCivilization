using PxC.Host;
using UnityEngine;

namespace PxC.Content.Towers
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把塔防视觉内容包放入 PluginRegistry。</summary>
    public static class TowersPackRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new TowersPack());
        }
    }
}
