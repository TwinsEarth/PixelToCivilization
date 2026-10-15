using PxC.Gameplay.Tech;

namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 科技服务（tech 插件实现）。持有每势力的科技树状态（已研究集合 + 在研进度），
    /// 研究消耗研究点，解锁时代、单位与建筑。
    /// </summary>
    public interface ITechService
    {
        /// <summary>某科技是否已研究完成。</summary>
        bool IsResearched(int faction, string techId);

        /// <summary>当前所处时代。</summary>
        Era GetEra(int faction);

        /// <summary>某科技当前研究进度 [0,1]（未开始=0，完成=1）。</summary>
        float GetProgress(int faction, string techId);

        /// <summary>是否已解锁某时代（用于单位/建筑分代闸门）。</summary>
        bool HasEra(int faction, Era era);
    }
}
