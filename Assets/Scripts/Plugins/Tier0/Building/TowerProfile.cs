using System;

namespace PxC.Plugins.Building
{
    /// <summary>
    /// 多层收分塔形制（根治"塔被建成方形高楼"）。
    /// 规则：层数 3–4；每层为独立一层，向上逐层收分（ShrinkRatio≈0.82）；
    /// 层间出檐；低矮敦实（总高不超过底层直径的 2 倍）。禁止做成单体方楼。
    /// </summary>
    public sealed class TowerProfile
    {
        /// <summary>层数（必须 3 或 4）。</summary>
        public int Levels;

        /// <summary>底层外接半径（米）。</summary>
        public float BaseRadius;

        /// <summary>每层相对下一层的底边比（收分，0.78–0.85）。</summary>
        public float ShrinkRatio;

        /// <summary>每一层的高度（米，低矮）。</summary>
        public float LevelHeight;

        /// <summary>是否层间出檐（木结构为 true）。</summary>
        public bool HasEaves;

        /// <summary>顶部是否为尖顶（火塔土垒为 true）。</summary>
        public bool PointedTop;

        /// <summary>第 level 层（0=底层）的外接半径。</summary>
        public float RadiusAt(int level)
        {
            float r = BaseRadius;
            for (int i = 0; i < level; i++) r *= ShrinkRatio;
            return r;
        }

        /// <summary>第 level 层底部的 Y（从地面起）。</summary>
        public float HeightAt(int level) => level * LevelHeight;

        /// <summary>塔身总高（不含尖顶）。</summary>
        public float TotalBodyHeight => Levels * LevelHeight;

        /// <summary>是否满足多层收分硬约束（用于编辑器/测试断言）。</summary>
        public bool IsValidMultiLevelTower()
        {
            if (Levels < 3 || Levels > 4) return false;
            if (ShrinkRatio < 0.78f || ShrinkRatio > 0.85f) return false;
            // 相邻层底边比必须在 0.78–0.85（每层比下一层小）
            for (int i = 1; i < Levels; i++)
            {
                float ratio = RadiusAt(i) / RadiusAt(i - 1);
                if (ratio < 0.78f || ratio > 0.85f) return false;
            }
            // 低矮敦实：总高不超过底层直径的 2 倍
            if (TotalBodyHeight > BaseRadius * 2f * 2f) return false;
            return true;
        }
    }

    /// <summary>各塔防类型的形制工厂（数据驱动，视图层据此生成多层模型）。</summary>
    public static class TowerProfiles
    {
        /// <summary>按建筑类型取多层塔形制（非塔返回 null）。</summary>
        public static TowerProfile For(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.ArrowTower:
                    // 木制多层塔：4 层、出檐、射弩箭
                    return new TowerProfile
                    {
                        Levels = 4,
                        BaseRadius = 2.2f,
                        ShrinkRatio = 0.80f,
                        LevelHeight = 1.6f,
                        HasEaves = true,
                        PointedTop = false,
                    };

                case BuildingType.FireTower:
                    // 土垒尖塔：3 层、收分明显、顶部喷火
                    return new TowerProfile
                    {
                        Levels = 3,
                        BaseRadius = 2.5f,
                        ShrinkRatio = 0.78f,
                        LevelHeight = 1.8f,
                        HasEaves = false,
                        PointedTop = true,
                    };

                case BuildingType.CannonTower:
                    // 方形矮墩：3 层、收分小、顶部旋转炮塔
                    return new TowerProfile
                    {
                        Levels = 3,
                        BaseRadius = 2.8f,
                        ShrinkRatio = 0.85f,
                        LevelHeight = 1.4f,
                        HasEaves = false,
                        PointedTop = false,
                    };

                case BuildingType.Bunker:
                    // 圆形堡垒：单层矮墩 + 四面机枪（不强制多层）
                    return new TowerProfile
                    {
                        Levels = 3,
                        BaseRadius = 3.2f,
                        ShrinkRatio = 0.82f,
                        LevelHeight = 1.2f,
                        HasEaves = false,
                        PointedTop = false,
                    };

                case BuildingType.Beacon:
                    // 烽火台：木架锅（非塔，视图特殊）
                    return new TowerProfile
                    {
                        Levels = 3,
                        BaseRadius = 1.8f,
                        ShrinkRatio = 0.82f,
                        LevelHeight = 1.3f,
                        HasEaves = false,
                        PointedTop = false,
                    };

                default:
                    return null;
            }
        }
    }
}
