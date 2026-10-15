using PxC.Gameplay.Tech;

namespace PxC.Plugins.Naval
{
    /// <summary>
    /// 船艇型号（按时代）。全部为 <see cref="PxC.Core.World.EntityDomain.Naval"/>，
    /// 只在水域内移动，永不上岸。
    /// 1949 前：木筏/渔船/战船/帆船；1949 后：轮船/潜艇/航母。
    /// </summary>
    public enum NavalModel
    {
        /// <summary>木筏/小船：最早水上运载。</summary>
        Raft = 0,

        /// <summary>渔船：渔业产出，无武装。</summary>
        FishingBoat = 1,

        /// <summary>战船：木质桨帆战舰，接舷/冲撞。</summary>
        Warship = 2,

        /// <summary>帆船：远洋风帆，大航海时代。</summary>
        SailingShip = 3,

        /// <summary>轮船：蒸汽/内燃机，工业后。</summary>
        Steamship = 4,

        /// <summary>潜艇：水下突袭，现代。</summary>
        Submarine = 5,

        /// <summary>航空母舰：舰载机平台，现代。</summary>
        AircraftCarrier = 6,
    }

    /// <summary>船艇型号数量（=7）。</summary>
    public static class NavalModels
    {
        public const int Count = 7;
    }

    /// <summary>船艇型号的时代分代元数据。</summary>
    public static class NavalGeneration
    {
        public static Era RequiredEra(NavalModel model)
        {
            switch (model)
            {
                case NavalModel.Raft:
                    return Era.Primitive;
                case NavalModel.FishingBoat:
                    return Era.BronzeAge;
                case NavalModel.Warship:
                    return Era.IronAge;
                case NavalModel.SailingShip:
                    return Era.AgeOfSail;
                case NavalModel.Steamship:
                    return Era.Industrial;
                case NavalModel.Submarine:
                case NavalModel.AircraftCarrier:
                    return Era.Modern;
                default:
                    return Era.Primitive;
            }
        }

        /// <summary>
        /// 蒸汽铁甲换代目标：进入工业时代（<paramref name="currentEra"/> ≥ <see cref="Era.Industrial"/>）后，
        /// 木质桨帆 / 远洋风帆战舰统一换装为蒸汽铁甲舰（<see cref="NavalModel.Steamship"/>）。
        /// 蒸汽机统一水面舰艇，近战战船与远洋帆船角色在工业化后合并。
        /// 民用船（木筏 / 渔船）角色永恒不换代；蒸汽轮船、潜艇、航母不换代。
        /// 返回 true 表示需要换代，<paramref name="upgrade"/> 为目标型号。
        /// </summary>
        public static bool TryGetSteamUpgrade(
            NavalModel current, Era currentEra, out NavalModel upgrade)
        {
            upgrade = current;
            if (currentEra < Era.Industrial) return false;
            switch (current)
            {
                case NavalModel.Warship:
                case NavalModel.SailingShip:
                    upgrade = NavalModel.Steamship;
                    return true;
                default:
                    // 民用船（Raft/FishingBoat）保留；蒸汽轮船 / 潜艇 / 航母不换代。
                    return false;
            }
        }
    }
}
