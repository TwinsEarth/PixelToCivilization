using PxC.Plugins.Building;

namespace PxC.Content.Towers
{
    /// <summary>塔的主体材质类别。</summary>
    public enum TowerMaterialKind
    {
        Wood = 0,
        Earth = 1,
        Stone = 2,
        Metal = 3,
    }

    /// <summary>
    /// 一型塔的视觉内容数据（多层收分塔的每层外观），数据驱动视图。
    /// 形制（层数/收分）来自 Tier-0 的 TowerProfile；本内容包提供颜色、
    /// 檐角、箭孔、顶饰等"视觉层"，确保渲染出的是塔而非方形楼。
    /// </summary>
    public sealed class TowerVisualProfile
    {
        /// <summary>建筑类型（BuildingType 索引）。</summary>
        public int TypeId;

        /// <summary>主体材质。</summary>
        public TowerMaterialKind Material;

        /// <summary>底层主体颜色（十六进制，如 "#8a5a2b"）。</summary>
        public string BaseColorHex;

        /// <summary>檐角数（木塔 4/6/8，出檐用）。</summary>
        public int EaveCorners;

        /// <summary>每层箭孔/窗洞数。</summary>
        public int EmbrasuresPerLevel;

        /// <summary>顶饰描述（"尖顶"/"炮管"/"旗杆"/"火盆"）。</summary>
        public string TopFinial;

        /// <summary>发射光/焰颜色（十六进制，可为 null）。</summary>
        public string EffectColorHex;
    }

    /// <summary>塔防五型视觉内容静态表。</summary>
    public static class TowerVisualContent
    {
        public static readonly TowerVisualProfile[] Profiles =
        {
            new TowerVisualProfile
            {
                TypeId = (int)BuildingType.ArrowTower,
                Material = TowerMaterialKind.Wood,
                BaseColorHex = "#8a5a2b",
                EaveCorners = 8,
                EmbrasuresPerLevel = 4,
                TopFinial = "旗杆",
                EffectColorHex = "#d9c28a",
            },
            new TowerVisualProfile
            {
                TypeId = (int)BuildingType.FireTower,
                Material = TowerMaterialKind.Earth,
                BaseColorHex = "#9c7a4a",
                EaveCorners = 0,
                EmbrasuresPerLevel = 2,
                TopFinial = "火盆",
                EffectColorHex = "#ff5a1f",
            },
            new TowerVisualProfile
            {
                TypeId = (int)BuildingType.CannonTower,
                Material = TowerMaterialKind.Stone,
                BaseColorHex = "#7d7d82",
                EaveCorners = 4,
                EmbrasuresPerLevel = 2,
                TopFinial = "旋转炮管",
                EffectColorHex = "#ffd24a",
            },
            new TowerVisualProfile
            {
                TypeId = (int)BuildingType.Beacon,
                Material = TowerMaterialKind.Wood,
                BaseColorHex = "#6f4a28",
                EaveCorners = 0,
                EmbrasuresPerLevel = 0,
                TopFinial = "火盆",
                EffectColorHex = "#ff7a1f",
            },
            new TowerVisualProfile
            {
                TypeId = (int)BuildingType.Bunker,
                Material = TowerMaterialKind.Metal,
                BaseColorHex = "#5d6659",
                EaveCorners = 0,
                EmbrasuresPerLevel = 4,
                TopFinial = "机枪座",
                EffectColorHex = "#ffe27a",
            },
        };

        /// <summary>按建筑类型取视觉内容（无匹配 null）。</summary>
        public static TowerVisualProfile ForType(int typeId)
        {
            for (int i = 0; i < Profiles.Length; i++)
            {
                if (Profiles[i].TypeId == typeId) return Profiles[i];
            }
            return null;
        }
    }
}
