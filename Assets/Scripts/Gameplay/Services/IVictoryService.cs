namespace PxC.Gameplay.Services
{
    /// <summary>胜利类型。</summary>
    public enum VictoryType
    {
        /// <summary>未达成。</summary>
        None = 0,

        /// <summary>征服：玩家消灭所有其他势力（仅剩自己存活）。</summary>
        Conquest = 1,

        /// <summary>科技：进入太空时代（Era.Space）。</summary>
        Technology = 2,

        /// <summary>文化：达到最高文化等级。</summary>
        Culture = 3,

        /// <summary>发展：人口与建筑达到繁荣阈值。</summary>
        Development = 4,
    }

    /// <summary>
    /// 胜利服务（victory 插件实现，Tier-1）。每年检测四种胜利条件，
    /// 达成后记录并发布 VictoryEvent，提供各路线进度。
    /// </summary>
    public interface IVictoryService
    {
        /// <summary>已达成的胜利类型（None 表示未达成）。</summary>
        VictoryType AchievedType { get; }

        /// <summary>是否已达成任意胜利。</summary>
        bool IsAchieved { get; }

        /// <summary>年度检测：若达成则返回胜利类型（None=未达成）。</summary>
        VictoryType Check(int gameYear);

        /// <summary>征服进度 [0,1]（玩家势力占存活势力比例）。</summary>
        float ConquestProgress();

        /// <summary>科技进度 [0,1]（当前时代/太空时代）。</summary>
        float TechnologyProgress();

        /// <summary>文化进度 [0,1]。</summary>
        float CultureProgress();

        /// <summary>发展进度 [0,1]（人口与建筑综合）。</summary>
        float DevelopmentProgress();
    }
}
