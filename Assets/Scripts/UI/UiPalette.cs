using UnityEngine;

namespace PxC.UI
{
    /// <summary>
    /// v10.8.0 UI 表现层调色板：华夏文明主题（深底 + 金/米文字 + 五势力色）。
    /// 势力色按 FactionInfo.ColorIndex 索引（0=玩家华夏，1-4=四方部族）。
    /// </summary>
    public static class UiPalette
    {
        // ---- 主题 ----
        public static readonly Color Background = Hex("#14141c");
        public static readonly Color Panel = Hex("#1e1e2b");
        public static readonly Color PanelDark = Hex("#171722");
        public static readonly Color Border = Hex("#3a3a4d");
        public static readonly Color Accent = Hex("#e8c887");   // 米金
        public static readonly Color AccentBright = Hex("#ffd98a");
        public static readonly Color TextMain = Hex("#e6e0d0");
        public static readonly Color TextDim = Hex("#9a968a");
        public static readonly Color ButtonBg = Hex("#2c2c3e");
        public static readonly Color ButtonBgHover = Hex("#3c3c54");
        public static readonly Color Danger = Hex("#d06050");
        public static readonly Color Good = Hex("#7fd08a");
        public static readonly Color MapLand = Hex("#23301f");   // 地图陆地底
        public static readonly Color MapGrid = Hex("#1b2618");   // 网格线
        public static readonly Color MapSea = Hex("#182430");    // 海（世界边缘）

        /// <summary>势力色板（5 色，与开局五势力对应）。</summary>
        public static readonly Color[] FactionColors =
        {
            Hex("#c0392b"), // 华夏（红）
            Hex("#2e86c1"), // 东夷（蓝）
            Hex("#28b463"), // 西戎（绿）
            Hex("#8e44ad"), // 南蛮（紫）
            Hex("#d68910"), // 北狄（橙）
        };

        /// <summary>取势力色（越界回退灰色）。</summary>
        public static Color Faction(int colorIndex)
        {
            if (colorIndex >= 0 && colorIndex < FactionColors.Length) return FactionColors[colorIndex];
            return Hex("#888888");
        }

        /// <summary>将未存活势力压暗。</summary>
        public static Color Dead => Hex("#55555f");

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
    }
}
