using System.Collections.Generic;
using PxC.Core.Capabilities;

namespace PxC.Core.Contracts
{
    /// <summary>对其他插件的依赖声明。</summary>
    public sealed class PluginDependency
    {
        public string Id;
        public int MinVersion;
        public bool Optional;

        public PluginDependency(string id = null, int minVersion = 1, bool optional = false)
        {
            Id = id;
            MinVersion = minVersion;
            Optional = optional;
        }
    }

    /// <summary>能力申请项；Scope 用于限定网络域名/资源路径等。</summary>
    public sealed class CapabilityClaim
    {
        public Capability Capability;
        public string Scope;

        public CapabilityClaim(Capability capability = 0, string scope = null)
        {
            Capability = capability;
            Scope = scope;
        }
    }

    /// <summary>插件声明的只读元数据集合（便于宿主收集，避免每插件 new 一堆临时对象）。</summary>
    public static class PluginDeclarations
    {
        public static IReadOnlyList<PluginDependency> Deps(params PluginDependency[] deps) => deps;

        public static IReadOnlyList<CapabilityClaim> Caps(params CapabilityClaim[] caps) => caps;
    }
}
