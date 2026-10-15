using System;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.World;

namespace PxC.Host
{
    /// <summary>
    /// 插件受限上下文实现。宿主为每个插件创建一个，只暴露事件/服务/世界/日志/能力校验，
    /// 不暴露内核对象。
    /// </summary>
    public sealed class PluginContext : IPluginContext
    {
        private readonly CapabilitySystem _capability;
        private readonly Action<string> _logger;

        public string PluginId { get; }
        public IEventBus Events { get; }
        public IServiceLocator Services { get; }
        public ChunkedWorld World { get; }

        public PluginContext(
            string pluginId,
            IEventBus events,
            IServiceLocator services,
            ChunkedWorld world,
            CapabilitySystem capability,
            Action<string> logger)
        {
            PluginId = pluginId;
            Events = events;
            Services = services;
            World = world;
            _capability = capability;
            _logger = logger;
        }

        public void Log(string message) => _logger?.Invoke($"[{PluginId}] {message}");

        public bool HasCapability(Capability capability, string scope = null) =>
            _capability.HasCapability(PluginId, capability, scope);

        public bool CheckCapability(Capability capability, string scope = null) =>
            _capability.CheckCapability(PluginId, capability, scope);
    }
}
