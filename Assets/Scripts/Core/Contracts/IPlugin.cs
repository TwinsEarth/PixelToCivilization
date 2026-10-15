using System.Collections.Generic;
using PxC.Core.Persistence;

namespace PxC.Core.Contracts
{
    /// <summary>
    /// 插件基础契约。所有业务模块（系统/内容/副本）均以本契约实现。
    /// 插件间禁止直接引用，只通过事件、服务、数据世界通信。
    /// </summary>
    public interface IPlugin
    {
        // ---- 身份 ----
        string Id { get; }
        string DisplayName { get; }
        int Version { get; }
        int MinHostVersion { get; }
        Tier Tier { get; }

        // ---- 依赖与能力 ----
        IReadOnlyList<PluginDependency> Dependencies { get; }
        IReadOnlyList<CapabilityClaim> RequiredCapabilities { get; }

        // ---- 生命周期 ----
        /// <summary>注册：拿到受限上下文（此时只保存引用，不做重逻辑）。</summary>
        void OnRegister(IPluginContext context);
        /// <summary>初始化：分配、订阅、读配置（与 OnShutdown 镜像）。</summary>
        void OnInitialize();
        /// <summary>卸载：反订阅、释放句柄、清空，Diff 归零。</summary>
        void OnShutdown();

        // ---- 存档段（可返回 null=无存档）----
        ISaveSegment GetSaveSegment();
        void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion);
    }
}
