namespace PxC.Core.Contracts
{
    /// <summary>插件分层。Tier-0 为必选核心，Tier-3 为社区第三方。</summary>
    public enum Tier
    {
        Core0 = 0,
        System1 = 1,
        Content2 = 2,
        Community3 = 3
    }

    /// <summary>算力分层：决定 Tick 的调度频率与节流策略。</summary>
    public enum SimulationLayer
    {
        /// <summary>核心（时间/经济/人口等），永不节流。</summary>
        P0Core = 0,
        /// <summary>战术层，默认 1Hz，Low 档 2s。</summary>
        P1Tactical = 1,
        /// <summary>环境层，按 AmbientHz（4/2/1Hz），高负载跳过。</summary>
        P2Ambient = 2
    }
}
