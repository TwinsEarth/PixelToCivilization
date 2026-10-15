using System;

namespace PxC.Core.Services
{
    /// <summary>
    /// 服务定位器：插件通过它查询宿主提供的服务（不使用静态单例）。
    /// 服务以接口注册，可被测试替身替换。
    /// </summary>
    public interface IServiceLocator
    {
        void Register<T>(T service) where T : class;
        void Unregister<T>() where T : class;
        T Resolve<T>() where T : class;
        bool TryResolve<T>(out T service) where T : class;
        bool IsRegistered<T>() where T : class;
    }
}
