using PxC.Gameplay.Tech;

namespace PxC.Plugins.GroundForce
{
    /// <summary>
    /// 地面作战单位型号（按历史分代）。
    /// 1949 前：骑兵 / 阵兵（3 人一组）/ 战车；
    /// 1949 后：坦克 / 装甲车 / 导弹车。
    /// 全部为 <see cref="PxC.Core.World.EntityDomain.Land"/>，
    /// 永不进入水域、永不被船舶牵引。
    /// </summary>
    public enum GroundForceModel
    {
        // ---- 1949 前（冷兵器 / 畜力 / 早期机械）----
        /// <summary>骑兵：机动高、冲击强，骑乘作战。</summary>
        Cavalry = 0,

        /// <summary>阵兵：3 人一组列阵，近战防御，组阵推进。</summary>
        Formation = 1,

        /// <summary>战车：畜力战车，冲击与载员，先秦主力。</summary>
        Chariot = 2,

        // ---- 1949 后（机械化 / 信息化）----
        /// <summary>坦克：重甲火炮，地面突击主力。</summary>
        Tank = 3,

        /// <summary>装甲车：轻甲快速，步兵投送与侦察。</summary>
        Armor = 4,

        /// <summary>导弹车：远程精确打击，锁定炮塔/装甲集群。</summary>
        MissileVehicle = 5,
    }

    /// <summary>地面单位型号数量常量（=6）。</summary>
    public static class GroundForceModels
    {
        public const int Count = 6;
    }

    /// <summary>型号的历史分代元数据（解锁时代 / 是否机械化 / 组阵规模）。</summary>
    public static class GroundForceGeneration
    {
        /// <summary>该型号解锁所需时代（Era 闸门）。</summary>
        public static Era RequiredEra(GroundForceModel model)
        {
            switch (model)
            {
                case GroundForceModel.Cavalry:
                case GroundForceModel.Chariot:
                    return Era.BronzeAge;
                case GroundForceModel.Formation:
                    return Era.Primitive;   // 基础步兵，最早即可编组
                case GroundForceModel.Tank:
                case GroundForceModel.Armor:
                    return Era.Modern;
                case GroundForceModel.MissileVehicle:
                    return Era.Information;
                default:
                    return Era.Primitive;
            }
        }

        /// <summary>是否为 1949 后机械化单位（现代/信息时代）。</summary>
        public static bool IsMechanized(GroundForceModel model) =>
            model == GroundForceModel.Tank ||
            model == GroundForceModel.Armor ||
            model == GroundForceModel.MissileVehicle;

        /// <summary>基础组阵规模（同型号多少个单位编为一组，阵兵=3）。</summary>
        public static int SquadSize(GroundForceModel model) =>
            model == GroundForceModel.Formation ? 3 : 1;
    }
}
