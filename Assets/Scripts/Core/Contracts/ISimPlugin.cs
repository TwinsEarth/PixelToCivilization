namespace PxC.Core.Contracts
{
    /// <summary>
    /// 模拟插件：驱动游戏逻辑。可由实现 IPlugin 的同一类同时实现。
    /// Tick 频率由 GameLoop 结合算力分层（Layer）与帧预算控制。
    /// </summary>
    public interface ISimPlugin : IPlugin
    {
        SimulationLayer Layer { get; }

        /// <summary>每帧模拟（simDt 已乘游戏速度，单位为游戏年对应的真实时间）。</summary>
        void OnTick(float simDt);

        /// <summary>每跨一个游戏年（订阅式年度结算）。</summary>
        void OnYear(int gameYear);

        /// <summary>时代切换。</summary>
        void OnEra(int newEra, int oldEra);

        /// <summary>朝代更迭。</summary>
        void OnDynasty(int dynastyIndex, int gameYear);
    }
}
