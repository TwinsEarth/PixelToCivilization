using System;

namespace PxC.Core.Services
{
    /// <summary>内存压力等级。</summary>
    public enum MemoryPressure
    {
        Normal = 0,
        Warning = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// 内存监控：三级水位、生成闸门、活动计数探针。
    /// 内存水位：High=1.1GB、Critical=1.5GB；逼近 90% 暂停生成。
    /// 快照能力依赖 Memory Profiler 包（未安装时用托管/Profiler 计数并标注未验证）。
    /// </summary>
    public interface IMemoryMonitor
    {
        MemoryPressure Pressure { get; }
        long ManagedBytes { get; }
        float UsedMB { get; }
        float HighMB { get; }
        float CriticalMB { get; }

        /// <summary>是否允许继续生成（High/Critical 或逼近上限时返回 false）。</summary>
        bool CanSpawn(int estimatedCostMB = 0);

        /// <summary>注册一个活动对象计数探针（如对象池/实体存储）。</summary>
        void RegisterLiveCount(string label, Func<int> provider);

        /// <summary>每帧/关键节点更新探针。</summary>
        void Tick();

        /// <summary>请求捕获内存快照（Memory Profiler 未安装时记录为未验证）。</summary>
        void CaptureSnapshot(string label);
    }
}
