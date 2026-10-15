using PxC.Host;
using UnityEngine;

namespace PxC.Plugins.Settlement
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把聚落插件放入 PluginRegistry。</summary>
    public static class SettlementPluginRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new SettlementPlugin());
        }
    }
}
