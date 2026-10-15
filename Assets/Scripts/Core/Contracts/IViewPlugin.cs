namespace PxC.Core.Contracts
{
    /// <summary>
    /// 表现插件：视图创建/刷新/回收。只读数据世界、订阅事件，不持有模拟状态。
    /// 视图对象必须经对象池/资源句柄创建，OnViewRelease 中镜像 Return/Release。
    /// </summary>
    public interface IViewPlugin : IPlugin
    {
        void OnViewReady();
        void OnViewTick(float realDt);
        void OnViewRelease();
    }
}
