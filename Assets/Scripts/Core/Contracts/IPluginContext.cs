using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.World;

namespace PxC.Core.Contracts
{
    /// <summary>
    /// 插件受限上下文：宿主向插件暴露的唯一 API 面。
    /// 插件不直接访问宿主内核，只通过本接口发布事件、查询服务、访问世界，并经能力校验。
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>当前插件 id。</summary>
        string PluginId { get; }

        /// <summary>强类型事件总线（订阅/反订阅需配对）。</summary>
        IEventBus Events { get; }

        /// <summary>服务定位器。</summary>
        IServiceLocator Services { get; }

        /// <summary>数据块世界（按段访问）。</summary>
        ChunkedWorld World { get; }

        /// <summary>日志（关键路径）。</summary>
        void Log(string message);

        /// <summary>当前插件是否已被授予某能力（scope 可空）。</summary>
        bool HasCapability(Capability capability, string scope = null);

        /// <summary>
        /// 校验能力：未授权则安全失败（返回 false，由调用方处理，不崩溃）。
        /// 与 HasCapability 区别：本方法会同时记录一次越权尝试。
        /// </summary>
        bool CheckCapability(Capability capability, string scope = null);
    }
}
