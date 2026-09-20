using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.World
{
    /// <summary>
    /// V9.1.0 真实地球地形数据（等距圆柱投影 equirectangular）：
    /// ·经度 lon∈[-180,180] → 世界 X∈[-2400,2400]（每格 4 世界单位，gx=600+lon*3.3333）
    /// ·纬度 lat∈[-90,90]  → 世界 Z∈[-1200,1200] 中央带（每格 2.4，gz=600+lat*5.5556，北纬+z），两极各留 240 海洋/冰缘 padding
    /// 大陆轮廓为低精度手绘多边形（识别度优先，非测绘精度）；Id 即 ContinentMap 归属，供 V9.1.1「每洲一国」直接使用。
    /// Kind=0 大陆（可立国），Kind=1 岛屿（山地带，不可立国）。
    /// </summary>
    public class EarthLand
    {
        public int Id; public string Name; public float[] Lon; public float[] Lat; public int Kind; public bool Polar;
        public EarthLand(int id,string name,int kind,bool polar,float[] lon,float[] lat){ Id=id; Name=name; Kind=kind; Polar=polar; Lon=lon; Lat=lat; }
    }
    /// <summary>山脉折线：沿线每隔固定步长盖高斯山峰，H=峰高，R=峰底半径（世界单位）</summary>
    public class EarthRange
    {
        public float[] Lon; public float[] Lat; public float H, R, Step;
        public EarthRange(float h,float r,float step,float[] lon,float[] lat){ H=h; R=r; Step=step; Lon=lon; Lat=lat; }
    }
    /// <summary>圆形地表特征（沙漠/雨林/高原/湖泊）：经纬度中心 + 半径(度) + 强度（湖泊为下沉深度，其余为抬升/标记半径）</summary>
    public class EarthCircle
    {
        public float Lon,Lat,R,Amount; public bool EllipseZ;
        public EarthCircle(float lon,float lat,float r,float amount,bool ellipseZ=false){ Lon=lon; Lat=lat; R=r; Amount=amount; EllipseZ=ellipseZ; }
    }
    /// <summary>河流折线（lon,lat 交错），从内陆高地入海口；V9.1.0 直接沿折线压出淡水河道</summary>
    public class EarthRiver { public float[] Lon; public float[] Lat; public EarthRiver(float[] lon,float[] lat){ Lon=lon; Lat=lat; } }

    public static class EarthMapData
    {
        // 投影常量（与 WorldGenerator 的 HalfX=2400 / 赤道带半宽 1200 对齐）
        public const float EquatHalfZ = 1200f;
        public static float LonToX(float lon) => lon/180f*2400f;
        public static float LatToZ(float lat) => lat/90f*EquatHalfZ;   // 北纬 -> +z（与 WorldGenerator C2WZ 同向）
        public static float XToLon(float wx) => wx/2400f*180f;
        public static float ZToLat(float wz) => wz/EquatHalfZ*90f;

        public static string ContinentName(int id)
        {
            foreach (var L in Lands) if (L.Id==id) return L.Name;
            return "海洋";
        }

        // ================= 大陆与主要岛屿（Id 固定，勿随意改序，存档与国家系统依赖） =================
        public static readonly EarthLand[] Lands =
        {
            // 1 欧亚大陆（含欧洲/亚洲/阿拉伯/印度/中南半岛；非洲以苏伊士地峡处海域分离，隔海独立发展）
            new(1,"欧亚大陆",0,false,new[]{-9.2f,-9.5f,-1.5f,1.5f,4f,8.5f,11f,7f,5f,12f,21f,30f,44f,62f,80f,105f,135f,160f,172f,166f,156f,143f,134f,129.5f,127f,122f,120.5f,116f,110f,108f,106f,103.5f,100.5f,96f,91f,86f,80f,77.5f,73f,69f,66f,60f,55f,50f,44f,40f,36f,33f,30f,26f,22f,17f,12f,7f,2f,-3f,-9.2f},
                new[]{38.5f,43.5f,43.5f,46f,48f,49f,54f,58f,62f,65f,68f,70f,72f,74f,76f,73f,70f,66f,60f,55f,51f,45f,42f,35.5f,34f,31f,25f,21f,18f,14f,9f,2f,6f,15f,21f,22f,13f,8f,15f,22f,25f,25f,20f,13f,12.5f,16f,22f,30f,33f,35f,38f,40f,40f,45f,44f,42f,37f,38.5f}),
            // 2 非洲
            new(2,"非洲",0,false,new[]{-17f,-12f,-6f,0f,9f,20f,30f,33.5f,36f,40f,43f,51f,46f,40f,35f,32f,25f,18f,14f,12f,9f,5f,-5f,-12f,-16f,-17f},
                new[]{21f,27f,35f,36f,34f,32f,31.5f,28f,23f,18f,12f,11f,5f,-2f,-12f,-22f,-30f,-34.5f,-32f,-24f,-17f,-5f,5f,9f,14f,21f}),
            // 3 北美洲（含中美地峡；格陵兰另立）
            new(3,"北美洲",0,false,new[]{-168f,-162f,-150f,-138f,-125f,-110f,-95f,-82f,-70f,-56f,-58f,-65f,-70f,-76f,-80f,-82f,-89f,-97f,-105f,-112f,-117f,-124f,-130f,-137f,-148f,-158f,-168f},
                new[]{65.5f,60f,59f,58f,50f,52f,58f,62f,64f,60f,52f,46f,42f,37f,30f,26f,21f,18f,20f,27f,32f,40f,48f,55f,58f,57f,65.5f}),
            // 4 南美洲
            new(4,"南美洲",0,false,new[]{-78f,-72f,-63f,-55f,-44f,-38f,-39f,-48f,-58f,-65f,-70f,-73f,-72f,-75f,-80f,-78f},
                new[]{9f,11.5f,10f,5f,-3f,-13f,-22f,-28f,-36f,-45f,-53f,-50f,-40f,-26f,-8f,9f}),
            // 5 澳大利亚
            new(5,"澳大利亚",0,false,new[]{114f,115f,121f,129f,137f,142f,146f,150f,153f,148f,145f,140f,133f,126f,114f},
                new[]{-22f,-32f,-34f,-35f,-37f,-38.5f,-38f,-37f,-28f,-23f,-17f,-11f,-12f,-16f,-22f}),
            // 6 南极洲（极地，不可立国）
            new(6,"南极洲",1,true,new[]{-180f,-120f,-60f,0f,60f,120f,180f,180f,-180f},
                new[]{-70f,-72f,-74f,-70f,-68f,-67f,-70f,-90f,-90f}),
            // 7 格陵兰（极地大岛）
            new(7,"格陵兰",1,true,new[]{-46f,-50f,-56f,-58f,-52f,-40f,-28f,-22f,-28f,-38f,-46f},
                new[]{60f,64f,69f,75f,80f,83f,80f,74f,68f,62f,60f}),
            // 8 不列颠群岛
            new(8,"不列颠群岛",1,false,new[]{-10.5f,-6f,-2f,0.5f,1.5f,0f,-5f,-10.5f},
                new[]{51.5f,50f,51f,53f,57f,58.5f,57f,51.5f}),
            // 9 日本列岛
            new(9,"日本列岛",1,false,new[]{130f,133f,137f,140f,142f,145f,144f,140f,136f,132f,130f},
                new[]{31f,33f,35f,37f,41f,44f,45f,42f,37f,34f,31f}),
            // 10 马达加斯加
            new(10,"马达加斯加",1,false,new[]{43.5f,48f,50.5f,49f,45f,43.5f},
                new[]{-16f,-15f,-20f,-25.5f,-24f,-16f}),
            // 11 新西兰
            new(11,"新西兰",1,false,new[]{166f,171f,174.5f,178f,175f,170f,166f},
                new[]{-46f,-41f,-37f,-39f,-42f,-47f,-46f}),
            // 12 新几内亚
            new(12,"新几内亚",1,false,new[]{131f,138f,145f,150f,148f,141f,134f,131f},
                new[]{-2f,-1.5f,-3f,-7f,-10f,-10f,-8f,-2f}),
            // 13 婆罗洲
            new(13,"婆罗洲",1,false,new[]{109f,116f,119f,117f,111f,109f},
                new[]{1f,2f,-2f,-5f,-4f,1f}),
            // 14 苏门答腊
            new(14,"苏门答腊",1,false,new[]{95f,101f,106f,105f,99f,95f},
                new[]{5f,3f,-4f,-7f,-3f,5f}),
            // 15 爪哇
            new(15,"爪哇",1,false,new[]{105f,112f,115f,113f,107f,105f},
                new[]{-6f,-6.5f,-7.5f,-8.8f,-8.2f,-6f}),
            // 16 冰岛
            new(16,"冰岛",1,false,new[]{-24f,-15f,-13f,-16f,-22f,-24f},
                new[]{64f,63.5f,65f,67f,67f,64f}),
            // 17 古巴
            new(17,"古巴",1,false,new[]{-85f,-78f,-74f,-76f,-83f,-85f},
                new[]{22f,22.5f,20.5f,19.5f,19.8f,22f}),
            // 18 菲律宾
            new(18,"菲律宾",1,false,new[]{120f,124f,123f,120.5f,119f,120f},
                new[]{18f,15f,9f,7f,12f,18f}),
            // 19 台湾
            new(19,"台湾",1,false,new[]{120.3f,122f,121.5f,120.3f},
                new[]{25.2f,24.5f,22f,22.8f}),
            // 20 斯里兰卡
            new(20,"斯里兰卡",1,false,new[]{80f,82f,81.8f,80f},
                new[]{9.5f,8f,5.8f,6.8f}),
            // 21 爱尔兰
            new(21,"爱尔兰",1,false,new[]{-10.5f,-6f,-6f,-10f,-10.5f},
                new[]{52f,52f,55.5f,55f,52f}),
        };

        // ================= 山脉（折线高斯峰带） =================
        public static readonly EarthRange[] Ranges =
        {
            new(8.5f,7f,1.6f,new[]{-72f,-70f,-70f,-69f,-68f,-71f,-72f},new[]{10f,-2f,-15f,-28f,-38f,-47f,-54f}),          // 安第斯
            new(6.5f,6.5f,1.8f,new[]{-146f,-132f,-122f,-112f,-106f,-100f},new[]{60f,55f,50f,42f,33f,24f}),                 // 落基
            new(10.5f,7.5f,1.2f,new[]{70f,78f,86f,92f,98f},new[]{34f,33f,31f,29f,27f}),                                  // 喜马拉雅
            new(6f,5f,1.2f,new[]{5f,11f,16f,20f},new[]{46f,46.5f,47f,45f}),                                              // 阿尔卑斯
            new(5f,4.5f,1.6f,new[]{59f,60f,61f,62f},new[]{67f,60f,54f,48f}),                                             // 乌拉尔
            new(5.5f,5f,1.6f,new[]{148f,150f,149f,146f},new[]{-18f,-26f,-33f,-38f}),                                     // 大分水岭
            new(4.5f,4.5f,1.6f,new[]{-80f,-77f,-74f},new[]{42f,38f,34f}),                                                // 阿巴拉契亚
            new(6f,5f,1.4f,new[]{30f,34f,38f},new[]{-29f,-26f,-15f}),                                                    // 德拉肯斯堡
        };

        // ================= 高原（宽缓抬升） =================
        public static readonly EarthCircle[] Plateaus =
        {
            new(88f,33f,13f,2.0f,true),    // 青藏高原
            new(100f,42f,11f,0.9f,false), // 蒙古高原
            new(-110f,42f,9f,0.8f,false), // 北美西部高原盆地
            new(68f,48f,8f,0.7f,false),   // 中亚草原台地
            new(-67f,-15f,7f,0.8f,false), // 玻利维亚高原
        };

        // ================= 沙漠（经纬度圆/椭圆覆盖，标记 Biome.Desert） =================
        public static readonly EarthCircle[] Deserts =
        {
            new(10f,22f,21f,0),           // 撒哈拉
            new(47f,23f,9f,0),            // 阿拉伯
            new(22f,-23f,7f,0),           // 卡拉哈里
            new(100f,42f,9f,0),           // 戈壁
            new(82f,39f,5f,0),            // 塔克拉玛干
            new(-115f,35f,5f,0),          // 莫哈韦
            new(-70f,-22f,2.5f,0),        // 阿塔卡马
            new(128f,-27f,8f,0),          // 澳大利亚大维多利亚
            new(72f,27f,4f,0),            // 塔尔
            new(-68f,-47f,4.5f,0),        // 巴塔哥尼亚
            new(30f,27f,6f,0),            // 利比亚/东部撒哈拉补强
        };

        // ================= 雨林（深绿着色 + 植被加密依据） =================
        public static readonly EarthCircle[] Jungles =
        {
            new(-62f,-5f,16f,0),          // 亚马孙
            new(22f,0f,9f,0),             // 刚果
            new(104f,3f,11f,0),           // 东南亚/印尼
            new(-72f,-8f,5f,0),           // 中美洲地峡
        };

        // ================= 湖泊（高斯凹陷，度半径） =================
        public static readonly EarthCircle[] Lakes =
        {
            new(-88f,48f,3.2f,2.6f),      // 苏必利尔
            new(-83f,44f,2.2f,2.4f),      // 休伦
            new(-87f,43f,1.6f,2.2f),      // 密歇根
            new(50f,42f,5f,2.8f,true),    // 里海（椭圆）
            new(108f,54f,2.2f,2.6f,true), // 贝加尔湖（椭圆）
            new(33f,-1.5f,2f,2.2f),       // 维多利亚湖
            new(-120f,66f,2.6f,2.4f),     // 大熊湖
            new(-114f,62f,2.2f,2.2f),     // 大奴湖
            new(60f,57f,2.5f,2.2f,true),  // 咸海-乌拉尔河源湿地
            new(30f,31f,2.4f,2.2f),       // 尼罗河三角洲湖盆
        };

        // ================= 主要河流（源头→入海口折线） =================
        public static readonly EarthRiver[] Rivers =
        {
            new(new[]{32f,31f,30.5f,31f,31.5f},new[]{-2f,10f,20f,27f,31.5f}),             // 尼罗河
            new(new[]{-74f,-66f,-58f,-52f,-48f},new[]{-8f,-6f,-4f,-1f,0f}),               // 亚马孙
            new(new[]{91f,98f,105f,112f,118f,121.5f},new[]{33f,30f,30f,30.5f,31f,31.3f}), // 长江（玩家家园附近）
            new(new[]{96f,103f,110f,116f,119f},new[]{35f,37f,37.5f,37f,38f}),             // 黄河
            new(new[]{-92f,-91f,-90f,-90f,-89f},new[]{46f,40f,34f,30f,29.2f}),            // 密西西比
            new(new[]{78f,83f,88f,90f},new[]{30f,26f,23f,22f}),                           // 恒河
            new(new[]{34f,42f,47f,48f,46f},new[]{57f,54f,50f,46f,41f}),                   // 伏尔加河
            new(new[]{25f,22f,18f,14f,12f},new[]{-12f,-6f,-2f,2f,-6f}),                   // 刚果河
            new(new[]{96f,101f,104f,106f},new[]{33f,22f,14f,10f}),                        // 湄公河
            new(new[]{8f,15f,22f,27f,29f},new[]{48f,47f,45f,44f,45f}),                    // 多瑙河
            new(new[]{-10f,-2f,4f,7f,6f},new[]{10f,14f,13f,9f,4f}),                       // 尼日尔河
            new(new[]{148f,144f,140f,139f},new[]{-36f,-34.5f,-35f,-36f}),                 // 墨累河
            new(new[]{73f,70f,67f},new[]{34f,28f,24f}),                                   // 印度河
            new(new[]{-58f,-62f,-66f},new[]{-15f,-25f,-35f}),                              // 拉普拉塔-巴拉那
        };
    }
}
