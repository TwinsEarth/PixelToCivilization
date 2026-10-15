using System;
using System.Collections.Generic;
using PxC.Core.Services;

namespace PxC.Host
{
    /// <summary>服务定位器实现：以接口注册/查询，替代静态单例，支持测试替身。</summary>
    public sealed class ServiceRegistry : IServiceLocator
    {
        private readonly Dictionary<Type, object> _services;

        public ServiceRegistry(int capacity = 32)
        {
            _services = new Dictionary<Type, object>(capacity);
        }

        public void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
        }

        public void Unregister<T>() where T : class => _services.Remove(typeof(T));

        public T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s)) return (T)s;
            throw new InvalidOperationException($"服务未注册: {typeof(T).FullName}");
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s))
            {
                service = (T)s;
                return true;
            }
            service = null;
            return false;
        }

        public bool IsRegistered<T>() where T : class => _services.ContainsKey(typeof(T));

        public void Clear() => _services.Clear();
    }
}
