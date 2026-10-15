using System;

namespace PxC.Core.Events
{
    /// <summary>
    /// 强类型事件总线。订阅/反订阅必须在插件 OnInitialize/OnShutdown 中配对。
    /// 高频事件建议 PublishDeferred，由 GameLoop 在帧末统一派发，避免遍历中重入。
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler) where T : struct;
        void Unsubscribe<T>(Action<T> handler) where T : struct;

        /// <summary>立即派发（实现需保证遍历安全）。</summary>
        void Publish<T>(T evt) where T : struct;

        /// <summary>缓冲到帧末统一派发（零 GC、避免重入）。</summary>
        void PublishDeferred<T>(T evt) where T : struct;

        /// <summary>派发全部缓冲事件（GameLoop 帧末调用）。</summary>
        void DispatchPending();

        /// <summary>当前是否存在某类型的订阅者（用于避免无谓的发布计算）。</summary>
        bool HasSubscribers<T>() where T : struct;
    }
}
