namespace PxC.Core.Services
{
    /// <summary>时钟服务（插件访问游戏时间的唯一入口；速度控制需 TimeControl 能力）。</summary>
    public interface IClock
    {
        int GameYear { get; }
        float YearProgress { get; }
        float Speed { get; }
        bool Paused { get; }
        bool Running { get; }

        void SetSpeed(float speed);
        void SetPaused(bool paused);
    }
}
