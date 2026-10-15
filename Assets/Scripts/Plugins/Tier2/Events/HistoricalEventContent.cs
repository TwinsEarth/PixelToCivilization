using PxC.Gameplay.Services;

namespace PxC.Content.Events
{
    /// <summary>
    /// 一条重大历史事件（EventsPack 内容数据）。
    /// </summary>
    public struct HistoricalEventInfo
    {
        public string Name;
        public int AdYear;
        public int Kind;       // HistoryKind
        public string Description;
    }

    /// <summary>
    /// 重大历史事件静态表（全球，含中国史与世界史）。
    /// 公历：负数=公元前。
    /// </summary>
    public static class HistoricalEventContent
    {
        public static readonly HistoricalEventInfo[] Sequence =
        {
            E("商鞅变法", -356, HistoryKind.Policy, "废井田、奖军功，奠定秦国强盛之基"),
            E("秦始皇统一六国", -221, HistoryKind.Dynasty, "书同文、车同轨，建立中央集权"),
            E("张骞通西域", -138, HistoryKind.Other, "开辟丝绸之路，连接欧亚"),
            E("蔡伦改进造纸术", 105, HistoryKind.Tech, "纸张普及，推动文明传播"),
            E("赤壁之战", 208, HistoryKind.War, "奠定三国鼎立格局"),
            E("隋朝统一", 589, HistoryKind.Dynasty, "结束南北分裂，开大运河"),
            E("贞观之治", 627, HistoryKind.Policy, "大唐盛世，开放包容"),
            E("安史之乱", 755, HistoryKind.War, "唐朝由盛转衰"),
            E("毕昇发明活字印刷", 1041, HistoryKind.Tech, "活字印刷，知识传播加速"),
            E("成吉思汗统一蒙古", 1206, HistoryKind.War, "蒙古崛起，横跨欧亚"),
            E("郑和下西洋", 1405, HistoryKind.Other, "七下西洋，远达非洲"),
            E("哥伦布到达美洲", 1492, HistoryKind.Other, "大航海时代，全球连为一体"),
            E("瓦特改良蒸汽机", 1775, HistoryKind.Tech, "工业革命开端"),
            E("第一条商用铁路", 1825, HistoryKind.Tech, "蒸汽机车改变运输"),
            E("鸦片战争", 1840, HistoryKind.War, "中国进入近代"),
            E("第一次世界大战", 1914, HistoryKind.War, "全球规模战争"),
            E("第二次世界大战", 1939, HistoryKind.War, "人类史上最大战争"),
            E("原子弹试爆", 1945, HistoryKind.Tech, "核时代来临"),
            E("人类登月", 1969, HistoryKind.Other, "首次踏足地外天体"),
            E("互联网普及", 1983, HistoryKind.Tech, "信息时代，全球互联"),
            E("大模型突破", 2022, HistoryKind.Tech, "人工智能进入通用化新阶段"),
        };

        private static HistoricalEventInfo E(string name, int ad, HistoryKind kind, string desc)
        {
            return new HistoricalEventInfo
            {
                Name = name,
                AdYear = ad,
                Kind = (int)kind,
                Description = desc,
            };
        }
    }
}
