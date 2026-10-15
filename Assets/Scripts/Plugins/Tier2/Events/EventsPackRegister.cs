using PxC.Host;
using UnityEngine;

namespace PxC.Content.Events
{
    /// <summary>编译期自动注册：BeforeSceneLoad 把历史事件包放入 PluginRegistry。</summary>
    public static class EventsPackRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PluginRegistry.Register(new EventsPack());
        }
    }
}
