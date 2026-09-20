// V9.1.2 地球模式国家目录：一块大陆（按地理文化分区）至多 3 个主权国家；每国 1..5 座城市（Lv1村落/Lv2城镇/Lv3城市）。
// 玩家固定为中国（长江下游家园）；同一引擎陆块（如欧亚 id=1）按经纬度区域框再细分为东亚/南亚/欧洲等“分区”，
// 朝鲜半岛按【岛】处理（IsIsland，物理仍连欧亚，但铁路走窄海峡跨海通道，不走陆地铁路）。
// 无人区（南极6、格陵兰7、北极冰原）不立国；月球为太空岛，须太空时代建成太空电梯才能到达，任何铁路不可达。
using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.Core
{
    /// <summary>城市定义（经纬度 + 等级 1村落/2城镇/3城市）。</summary>
    public class EarthCity
    {
        public string Name;
        public float Lon, Lat;
        public int Level;
        public bool Capital;
        public EarthCity(string name, float lon, float lat, int level, bool capital = false)
        { Name = name; Lon = lon; Lat = lat; Level = level; Capital = capital; }
    }

    /// <summary>
    /// 主权国家。LandId=引擎地形陆块 id（同陆块可有多国，靠区域框区分）；
    /// MinLon..MaxLat=地理文化分区包围盒（0 表示不约束，用于选址螺旋兜底不越区）；
    /// IsIsland=按岛处理（日本/英国为真岛，朝鲜半岛按岛）。
    /// </summary>
    public class EarthCountry
    {
        public string Id, Name;
        public int Faction;          // 1 东方 / 2 西方 / 3 东亚海洋 / 4 南方
        public int LandId;           // EarthMapData.Lands 的引擎陆块 id
        public int BasePop;
        public bool IsIsland;
        public float MinLon, MaxLon, MinLat, MaxLat; // 分区包围盒（度）；全 0=不约束
        public List<EarthCity> Cities = new();
        public EarthCountry(string id, string name, int faction, int landId, int basePop, EarthCity[] cities,
                            bool isIsland = false,
                            float minLon = 0f, float maxLon = 0f, float minLat = 0f, float maxLat = 0f)
        {
            Id = id; Name = name; Faction = faction; LandId = landId; BasePop = basePop;
            IsIsland = isIsland;
            MinLon = minLon; MaxLon = maxLon; MinLat = minLat; MaxLat = maxLat;
            Cities.AddRange(cities);
        }
        public EarthCity Capital => Cities.Count > 0 ? Cities[0] : null;
        public bool HasRegionBox => MaxLon != 0f || MaxLat != 0f || MinLon != 0f || MinLat != 0f;
    }

    /// <summary>城市运行时落点（确定性选址后缓存，供城际网络/视图使用；派生数据，不单独进存档）。</summary>
    public class EarthCityRT
    {
        public string CountryId = "";
        public string CountryName = "";
        public string Name = "";
        public int NationId;         // 对应 NationEntity.Id
        public int Faction;
        public int LandId;
        public int Level;
        public bool IsPlayer, IsCapital;
        public float X, Z;
    }

    /// <summary>声明式国际铁路连接：两国之间一条线路、一班列车往返；Sea=true 走窄海峡跨海桥面，false 走陆地铺轨。</summary>
    public readonly struct EarthRailLink
    {
        public readonly string A, B;
        public readonly bool Sea;
        public EarthRailLink(string a, string b, bool sea) { A = a; B = b; Sea = sea; }
    }

    public static class EarthNations
    {
        public const int PlayerFaction = 1;

        // 城市顺序第 1 座为首都。玩家中国首都=长江下游家园（运行时用家园坐标覆盖上海落点）。
        // 一洲（分区）≤3 国：东亚(中国)、南亚(印度)、朝鲜半岛岛(韩国)、日本岛(日本)；
        // 北美 3 国(加拿大/美国/墨西哥)、南美 2 国(巴西/阿根廷)、欧洲 2 国(法国/德国)、英格兰群岛(英国)；
        // 非洲、澳洲各保留 1 国（沿用既有内容，不超过一洲三国上限）。
        public static readonly EarthCountry[] All =
        {
            // —— 东亚分区（欧亚陆块 id=1）——
            new("cn","中国",1,1,80,new[]{
                new EarthCity("上海",121.5f,31.2f,3,true),
                new EarthCity("北京",116.4f,39.9f,3),
                new EarthCity("广州",113.3f,23.1f,2),
                new EarthCity("成都",104.1f,30.7f,2),
                new EarthCity("哈尔滨",126.6f,45.8f,2) },
                false, 97f,132f,18f,54f),
            // —— 南亚次大陆分区（欧亚陆块 id=1）——
            new("in","印度",1,1,64,new[]{
                new EarthCity("新德里",77.2f,28.6f,3,true),
                new EarthCity("孟买",72.8f,19.1f,2),
                new EarthCity("加尔各答",88.4f,22.6f,2) },
                false, 67f,91f,5f,32f),
            // —— 朝鲜半岛【按岛处理】（引擎仍在欧亚陆块 id=1）——
            new("kr","韩国",3,1,40,new[]{
                new EarthCity("首尔",127.0f,37.5f,2,true) },
                true, 124f,131.5f,33f,44f),
            // —— 日本岛（陆块 id=9）——
            new("jp","日本",3,9,48,new[]{
                new EarthCity("东京",139.7f,35.7f,3,true) },
                true),
            // —— 北美分区（陆块 id=3）：加拿大（北）——
            new("ca","加拿大",2,3,46,new[]{
                new EarthCity("渥太华",-75.7f,45.4f,2,true),
                new EarthCity("多伦多",-79.4f,43.7f,3),
                new EarthCity("温哥华",-123.1f,49.3f,2) },
                false, -141f,-52f,43f,70f),
            // —— 美国（中）——
            new("us","美国",2,3,62,new[]{
                new EarthCity("华盛顿",-77.0f,38.9f,3,true),
                new EarthCity("纽约",-74.0f,40.7f,3),
                new EarthCity("底特律",-83.0f,42.3f,2),
                new EarthCity("洛杉矶",-118.2f,34.0f,3),
                new EarthCity("芝加哥",-87.6f,41.9f,2) },
                false, -126f,-66f,24f,43f),
            // —— 墨西哥（南）——
            new("mx","墨西哥",4,3,38,new[]{
                new EarthCity("墨西哥城",-99.1f,19.4f,3,true),
                new EarthCity("瓜达拉哈拉",-103.3f,20.7f,1) },
                false, -118f,-86f,14f,33f),
            // —— 南美分区（陆块 id=4）：巴西（北/中）——
            new("br","巴西",4,4,52,new[]{
                new EarthCity("巴西利亚",-47.9f,-15.8f,2,true),
                new EarthCity("圣保罗",-46.6f,-23.5f,3),
                new EarthCity("里约热内卢",-43.2f,-22.9f,2) },
                false, -74f,-34f,-24.5f,6f),
            // —— 阿根廷（南）——
            new("ar","阿根廷",4,4,40,new[]{
                new EarthCity("布宜诺斯艾利斯",-58.4f,-34.6f,3,true),
                new EarthCity("科尔多瓦",-64.2f,-31.4f,1) },
                false, -76f,-53f,-56f,-24.5f),
            // —— 欧洲分区（欧亚陆块 id=1）：法国（西）——
            new("fr","法国",2,1,50,new[]{
                new EarthCity("巴黎",2.3f,48.8f,3,true),
                new EarthCity("马赛",5.4f,43.3f,2),
                new EarthCity("里昂",4.8f,45.8f,1) },
                false, -6f,6.5f,41f,51.5f),
            // —— 德国（东）——
            new("de","德国",2,1,52,new[]{
                new EarthCity("柏林",13.4f,52.5f,3,true),
                new EarthCity("慕尼黑",11.6f,48.1f,2),
                new EarthCity("法兰克福",8.7f,50.1f,2) },
                false, 6.5f,15.5f,47f,55.5f),
            // —— 英格兰群岛（陆块 id=8，岛）——
            new("gb","英国",2,8,38,new[]{
                new EarthCity("伦敦",-0.1f,51.5f,3,true),
                new EarthCity("曼彻斯特",-2.2f,53.5f,2) },
                true),
            // —— 非洲（陆块 id=2，保留一国）——
            new("af","非洲联合",4,2,46,new[]{
                new EarthCity("开罗",31.2f,30.0f,2,true),
                new EarthCity("拉各斯",3.4f,6.5f,1),
                new EarthCity("内罗毕",36.8f,-1.3f,2) }),
            // —— 澳大利亚（陆块 id=5，保留一国）——
            new("au","澳大利亚",2,5,30,new[]{
                new EarthCity("悉尼",151.2f,-33.9f,2,true),
                new EarthCity("堪培拉",149.1f,-35.3f,1) }),
        };

        /// <summary>
        /// 国际铁路连接（一线一列车）。陆地连接=邻国陆地铺轨（取两国有可行性的最近城市对，跨距受限）；
        /// 跨海连接=岛国/异陆块在窄海峡（跨距≤100、两端陆地、中段连续外海）上铺跨海桥面轨道。
        /// 非洲/澳洲/月球不设国际铁路（无相邻建模国家 / 太空岛铁路不可达）。
        /// </summary>
        public static readonly EarthRailLink[] RailLinks =
        {
            new("us","ca",false),   // 北美：美国↔加拿大（陆地）
            new("us","mx",false),   // 北美：美国↔墨西哥（陆地）
            new("fr","de",false),   // 欧洲：法国↔德国（陆地）
            new("cn","in",false),   // 东亚↔南亚：中国↔印度（陆地）
            new("br","ar",false),   // 南美：巴西↔阿根廷（陆地）
            new("gb","fr",true),    // 英国↔法国：多佛尔海峡跨海
            new("jp","kr",true),    // 日本↔韩国：对马海峡跨海
            new("kr","cn",true),    // 韩国↔中国：黄海跨海（朝鲜半岛按岛，不走大陆桥）
        };

        public static EarthCountry ById(string id)
        {
            foreach (var c in All) if (c.Id == id) return c;
            return null;
        }

        public static string FactionHex(int f)
        {
            switch (f)
            {
                case 1: return "d9402f"; // 朱红
                case 2: return "2f6fb0"; // 深蓝
                case 3: return "e8a020"; // 琥珀
                default: return "3a9d4d"; // 翠绿
            }
        }

        public static string FactionName(int f)
        {
            switch (f)
            {
                case 1: return "东方协作阵营";
                case 2: return "西方阵营";
                case 3: return "东亚海洋阵营";
                default: return "南方阵营";
            }
        }

        public static string FactionMembers(int f)
        {
            switch (f)
            {
                case 1: return "中国 · 印度";
                case 2: return "美国 · 加拿大 · 英国 · 法国 · 德国 · 澳大利亚";
                case 3: return "日本 · 韩国";
                default: return "墨西哥 · 巴西 · 阿根廷 · 非洲";
            }
        }

        public static Color FactionColor(int f) => NationEntity.HexToColor(FactionHex(f));
    }
}
