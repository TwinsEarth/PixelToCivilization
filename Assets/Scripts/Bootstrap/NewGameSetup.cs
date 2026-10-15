using PxC.Gameplay.Factions;

namespace PxC.Bootstrap
{
    /// <summary>
    /// 新游戏初始世界（开局势力布局）。仅负责"开局有哪些势力"这一最小初始状态，
    /// 各插件随后在 OnInitialize 中按势力数量创建自己的初始数据（人口/资源/科技…）。
    /// 读档路径不经过本类（后续版本由存档恢复势力）。
    /// </summary>
    public static class NewGameSetup
    {
        /// <summary>开局势力数（玩家 + 四方部族，受 SoA 上限 8 约束）。</summary>
        public const int InitialFactionCount = 5;

        /// <summary>填充开局势力：华夏（玩家）+ 东夷/西戎/南蛮/北狄（AI）。</summary>
        public static void PopulateNewWorld(FactionRegistry factions)
        {
            // 玩家主文明：华夏部落，出生于世界中心
            factions.Add("华夏部落", isPlayer: true, homeX: 0f, homeZ: 0f, colorIndex: 0);

            // 周边 AI 部族（四裔），分布于玩家四周
            factions.Add("东夷部落", isPlayer: false, homeX: 60f, homeZ: 10f, colorIndex: 1);
            factions.Add("西戎部落", isPlayer: false, homeX: -60f, homeZ: -5f, colorIndex: 2);
            factions.Add("南蛮部落", isPlayer: false, homeX: 10f, homeZ: 60f, colorIndex: 3);
            factions.Add("北狄部落", isPlayer: false, homeX: -10f, homeZ: -60f, colorIndex: 4);
        }
    }
}
