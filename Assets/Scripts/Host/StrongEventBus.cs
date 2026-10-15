using System;
using System.Collections.Generic;
using System.Reflection;
using PxC.Core.Events;

namespace PxC.Host
{
    /// <summary>
    /// 强类型事件总线。委托不可变，订阅/反订阅只产生新委托，Publish 取快照天然遍历安全。
    /// 延迟事件按类型存 List&lt;T&gt;（首次创建后复用，帧末 Clear），避免装箱与 GC。
    /// </summary>
    public sealed class StrongEventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers;
        private readonly Dictionary<Type, object> _deferred;
        private readonly List<Type> _deferredTypes;
        private readonly Dictionary<Type, MethodInfo> _dispatchCache;

        public StrongEventBus(int capacity = 64)
        {
            _handlers = new Dictionary<Type, Delegate>(capacity);
            _deferred = new Dictionary<Type, object>(capacity);
            _deferredTypes = new List<Type>(capacity);
            _dispatchCache = new Dictionary<Type, MethodInfo>(capacity);
        }

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type t = typeof(T);
            _handlers.TryGetValue(t, out var d);
            _handlers[t] = d == null ? handler : Delegate.Combine(d, handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type t = typeof(T);
            if (!_handlers.TryGetValue(t, out var d)) return;
            var rest = Delegate.Remove(d, handler);
            if (rest == null) _handlers.Remove(t);
            else _handlers[t] = rest;
        }

        public void Publish<T>(T evt) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out var d) && d is Action<T> action)
                action.Invoke(evt);
        }

        public void PublishDeferred<T>(T evt) where T : struct
        {
            Type t = typeof(T);
            if (!_deferred.TryGetValue(t, out var box) || !(box is List<T> list))
            {
                list = new List<T>(8);
                _deferred[t] = list;
                if (!_deferredTypes.Contains(t)) _deferredTypes.Add(t);
            }
            list.Add(evt);
        }

        public void DispatchPending()
        {
            for (int i = 0; i < _deferredTypes.Count; i++)
            {
                Type t = _deferredTypes[i];
                if (!_deferred.TryGetValue(t, out var box)) continue;
                _handlers.TryGetValue(t, out var d);
                InvokeDispatch(t, box, d);
            }
        }

        public bool HasSubscribers<T>() where T : struct =>
            _handlers.TryGetValue(typeof(T), out var d) && d != null;

        /// <summary>清空所有订阅与缓冲（世界重置，仅宿主调用）。</summary>
        public void Clear()
        {
            _handlers.Clear();
            _deferred.Clear();
            _deferredTypes.Clear();
        }

        private void InvokeDispatch(Type t, object box, Delegate d)
        {
            if (!_dispatchCache.TryGetValue(t, out var method))
            {
                method = typeof(StrongEventBus).GetMethod(
                    nameof(DispatchTyped),
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                method = method.MakeGenericMethod(t);
                _dispatchCache[t] = method;
            }
            method.Invoke(null, new[] { box, d });
        }

        private static void DispatchTyped<T>(List<T> list, Delegate d) where T : struct
        {
            if (d is Action<T> action)
            {
                for (int i = 0; i < list.Count; i++)
                    action(list[i]);
            }
            list.Clear();
        }
    }
}
