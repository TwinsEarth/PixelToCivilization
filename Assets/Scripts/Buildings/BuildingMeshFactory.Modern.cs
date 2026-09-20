// V9.0.1 现代风（SimCity BuildIt 参考图）程序化建模：BuildingMeshFactory 的 partial。
// 覆盖：城市公共设施 7 类（消防站/医院/警察局/学校/公园/超市/小区）、摩天楼、现代工厂、
// 现代兵营、机场、发电站、现代水塔、数据中心/AI实验室/高铁站；LOD2 出主体识别色，LOD3 加窗格与细节。
using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.Buildings
{
    public partial class BuildingMeshFactory
    {
        // —— SimCity 明亮材质（按 颜色+金属+光滑 缓存，避免每栋建筑泄漏材质）——
        readonly Dictionary<string, Material> _mcCache = new Dictionary<string, Material>();
        Material MC(Color c, float metal = 0f, float smooth = 0.12f)
        {
            string key = $"{Mathf.RoundToInt(c.r * 255)}_{Mathf.RoundToInt(c.g * 255)}_{Mathf.RoundToInt(c.b * 255)}_{metal:F2}_{smooth:F2}";
            if (!_mcCache.TryGetValue(key, out var m))
            {
                int seed = 70000 + (Mathf.RoundToInt(c.r * 997 + c.g * 991 + c.b * 983) & 0x7fff);
                m = ShaderHelper.Pbr(c, metal, smooth, seed, 0f, false);
                _mcCache[key] = m;
            }
            return m;
        }
        // 调色板
        Material MConcrete => MC(new Color(0.78f, 0.78f, 0.76f), 0f, 0.08f);
        Material MConcreteDark => MC(new Color(0.52f, 0.53f, 0.55f), 0f, 0.08f);
        Material MAsphalt => MC(new Color(0.10f, 0.10f, 0.11f), 0f, 0.10f);
        Material MSidewalk => MC(new Color(0.84f, 0.82f, 0.76f), 0f, 0.05f);
        Material MCream => MC(new Color(0.94f, 0.89f, 0.76f), 0f, 0.06f);
        Material MWhite => MC(new Color(0.95f, 0.95f, 0.93f), 0f, 0.10f);
        Material MOrangeRoof => MC(new Color(0.78f, 0.32f, 0.17f), 0f, 0.08f);
        Material MRed => MC(new Color(0.82f, 0.16f, 0.14f), 0f, 0.08f);
        Material MRedDark => MC(new Color(0.60f, 0.12f, 0.11f), 0f, 0.08f);
        Material MBlue => MC(new Color(0.15f, 0.37f, 0.70f), 0f, 0.12f);
        Material MBlueLight => MC(new Color(0.30f, 0.60f, 0.85f), 0f, 0.20f);
        Material MYellow => MC(new Color(0.93f, 0.78f, 0.18f), 0f, 0.08f);
        Material MGreen => MC(new Color(0.28f, 0.60f, 0.24f), 0f, 0.06f);
        Material MGrass => MC(new Color(0.42f, 0.70f, 0.30f), 0f, 0.03f);
        Material MGlassBlue => MC(new Color(0.28f, 0.55f, 0.78f), 0.10f, 0.85f);
        Material MGlassCyan => MC(new Color(0.42f, 0.78f, 0.85f), 0.10f, 0.85f);
        Material MGlassDark => MC(new Color(0.12f, 0.26f, 0.40f), 0.10f, 0.80f);
        Material MWater => MC(new Color(0.14f, 0.40f, 0.66f), 0f, 0.90f);
        Material MSteel => MC(new Color(0.55f, 0.60f, 0.66f), 0.7f, 0.40f);
        Material MBrown => MC(new Color(0.46f, 0.33f, 0.22f), 0f, 0.06f);
        Material MPastel(int v)
        {
            var cols = new[] {
                new Color(0.62f,0.83f,0.62f), // 绿
                new Color(0.95f,0.86f,0.45f), // 黄
                new Color(0.96f,0.66f,0.40f), // 橙
                new Color(0.92f,0.55f,0.52f), // 红
                new Color(0.55f,0.72f,0.92f), // 蓝
                new Color(0.86f,0.70f,0.90f), // 紫
            };
            return MC(cols[((v % cols.Length) + cols.Length) % cols.Length], 0f, 0.08f);
        }
        int HashVar(float x, float z, int salt = 0) => Mathf.RoundToInt(x * 7.13f + z * 13.7f + salt * 3.7f);

        // V9.0.8 夜景：暖黄亮窗自发光材质（白天由 ShaderHelper 夜景表压暗、夜间提亮）
        Material MWinLit => ShaderHelper.Emissive(new Color(0.08f, 0.07f, 0.05f), new Color(1.0f, 0.80f, 0.48f));

        /// <summary>标准四棱锥（旋转立方体）作为塔松/尖顶。</summary>
        GameObject Pyramid(string name, Vector3 pos, Vector3 scale, Material mat, Transform p)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name; g.transform.SetParent(p, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            g.transform.localRotation = Quaternion.Euler(0, 45, 0);
            DestroyCol(g); g.GetComponent<Renderer>().material = mat;
            return g;
        }

        // 城市地块台基（水泥/草地），让现代建筑读成"街区"
        void Lot(Transform t, float w, float d, Material top)
        {
            Box("LotCurb", new Vector3(0, -0.18f, 0), new Vector3(w + 0.7f, 0.34f, d + 0.7f), MConcrete, t);
            Box("LotTop", new Vector3(0, 0.0f, 0), new Vector3(w + 0.5f, 0.06f, d + 0.5f), top, t);
        }

        // 规整窗格（仅 LOD3），front/back + 两侧
        void WinGrid(Transform t, float w, float d, float wallH, int floors, int cols, Material glass)
        {
            if (!CurHigh) return;
            floors = Mathf.Clamp(floors, 1, 8); cols = Mathf.Clamp(cols, 2, 6);
            float fh = wallH / (floors + 0.6f);
            for (int f = 0; f < floors; f++)
            {
                float y = fh * (f + 0.9f);
                float hh = fh * 0.52f;
                for (int c = 0; c < cols; c++)
                {
                    float cx = -w * 0.5f + (c + 0.5f) * (w / cols);
                    float ww = (w / cols) * 0.55f;
                    Box("Win", new Vector3(cx, y, d * 0.5f + 0.015f), new Vector3(ww, hh, 0.05f), glass, t);
                    Box("Win", new Vector3(cx, y, -d * 0.5f - 0.015f), new Vector3(ww, hh, 0.05f), glass, t);
                    // V9.0.8 夜景亮窗：确定性约 1/3 窗格暖黄发光，昼夜由 ShaderHelper.ApplyNight 统一调度
                    if (((f * 31 + c * 17 + floors) % 3) == 0)
                    {
                        Box("WinLit", new Vector3(cx, y, d * 0.5f + 0.047f), new Vector3(ww * 0.82f, hh * 0.82f, 0.03f), MWinLit, t);
                        Box("WinLit", new Vector3(cx, y, -d * 0.5f - 0.047f), new Vector3(ww * 0.82f, hh * 0.82f, 0.03f), MWinLit, t);
                    }
                }
                if (d > 1.6f)
                {
                    int sc = Mathf.Clamp(Mathf.FloorToInt(d / 0.9f), 1, 4);
                    for (int c = 0; c < sc; c++)
                    {
                        float cz = -d * 0.5f + (c + 0.5f) * (d / sc);
                        Box("WinS", new Vector3(w * 0.5f + 0.015f, y, cz), new Vector3(0.05f, hh, (d / sc) * 0.5f), glass, t);
                        Box("WinS", new Vector3(-w * 0.5f - 0.015f, y, cz), new Vector3(0.05f, hh, (d / sc) * 0.5f), glass, t);
                    }
                }
            }
        }

        // 小红十字（医院标识）
        void RedCross(Transform t, Vector3 pos, float s, bool faceZ)
        {
            if (faceZ)
            {
                Box("CrossV", pos, new Vector3(s * 0.34f, s, 0.06f), MRed, t);
                Box("CrossH", pos + new Vector3(0, 0, 0.005f), new Vector3(s, s * 0.34f, 0.07f), MRed, t);
            }
            else
            {
                Box("CrossV", pos, new Vector3(0.06f, s * 0.34f, s), MRed, t);
                Box("CrossH", pos + new Vector3(0.005f, 0, 0), new Vector3(0.07f, s, s * 0.34f), MRed, t);
            }
        }

        /// <summary>V9 现代路由：返回 true 表示由本文件接管（不再走通用/古代特型）。</summary>
        private bool BuildModern(GameObject host, BuildingEntity b, BuildingStyle s, int era, float w, float d, float wallH)
        {
            Transform t = host.transform;
            bool hi = CurHigh;
            int variant = HashVar(b.X, b.Z);
            switch (b.Type)
            {
                case "well": if (era < 5) return false; BuildWaterTower(t); break;
                case "apartment": BuildApartment(t, variant, hi); break;
                case "supermarket": BuildSupermarket(t, hi); break;
                case "modern_school": BuildSchool(t, hi); break;
                case "hospital": BuildHospital(t, hi); break;
                case "fire_station": BuildFireStation(t, hi); break;
                case "police_station": BuildPolice(t, hi); break;
                case "park": BuildPark(t, variant, hi); break;
                case "skyscraper": BuildSkyscraper(t, variant, hi, wallH, IsLandmarkSkyscraper(b)); break;
                case "factory_pre": case "factory_modern": BuildFactoryModern(t, b.Type, hi); break;
                case "new_army": BuildModernBarracks(t, hi); break;
                case "airport": BuildAirport(t, hi); break;
                case "power_plant": BuildPowerPlant(t, hi); break;
                case "data_center": BuildDataCenter(t, hi); break;
                case "ai_lab": BuildAiLab(t, hi); break;
                case "high_speed_rail": BuildHsStation(t, hi); break;
                case "subway": BuildSubway(t, hi); break;
                default: return false;
            }
            // V9.0.8 非地标摩天楼（第 4 座起，整体 55% 高）不再叠加等级饰边，避免饰边悬空
            if (hi && b.Level >= 2 && !(b.Type == "skyscraper" && !IsLandmarkSkyscraper(b)))
                AddLevelTrim(t, b.Level, w, d, ModernTrimHeight(b.Type));
            return true;
        }
        float ModernTrimHeight(string id)
        {
            if (id == "skyscraper") return 16f;
            if (id == "airport") return 4f;
            return 3.2f;
        }

        // ============ 小区：彩色 pastel 多层公寓，平屋顶 + 空调盒 + 蓝色玻璃单元门 ============
        void BuildApartment(Transform t, int v, bool hi)
        {
            float w = 3.0f, d = 2.6f, h = 5.2f;
            Lot(t, w + 0.4f, d + 0.4f, MSidewalk);
            var body = MPastel(v);
            Box("Block", new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), body, t);
            // 底层门厅（更深色）+ 蓝玻璃门
            Box("GroundFloor", new Vector3(0, 0.55f, d * 0.5f + 0.02f), new Vector3(w, 1.1f, 0.08f), MConcreteDark, t);
            Box("Entrance", new Vector3(0, 0.6f, d * 0.5f + 0.06f), new Vector3(0.9f, 1.1f, 0.1f), MGlassBlue, t);
            // 平屋顶女儿墙 + 空调外机盒
            Box("Parapet", new Vector3(0, h + 0.12f, 0), new Vector3(w + 0.12f, 0.24f, d + 0.12f), MConcreteDark, t);
            WinGrid(t, w, d, h, 5, 4, MGlassCyan);
            if (hi)
            {
                Box("AC1", new Vector3(-0.9f, h + 0.32f, 0.6f), new Vector3(0.5f, 0.34f, 0.5f), MWhite, t);
                Box("AC2", new Vector3(0.9f, h + 0.32f, -0.5f), new Vector3(0.5f, 0.34f, 0.5f), MWhite, t);
                Box("RoofRoom", new Vector3(0.7f, h + 0.5f, 0.0f), new Vector3(1.0f, 0.8f, 1.0f), body, t);
            }
        }

        // ============ 超市：单层白色大卖场 + 蓝檐 + 红色招牌带 + 玻璃门面 + 停车场 ============
        void BuildSupermarket(Transform t, bool hi)
        {
            float w = 3.6f, d = 2.6f, h = 1.7f;
            Lot(t, 4.6f, 4.4f, MAsphalt);                 // 停车场地块
            Box("Hall", new Vector3(0, h * 0.5f, -0.2f), new Vector3(w, h, d), MWhite, t);
            Box("Parapet", new Vector3(0, h + 0.1f, -0.2f), new Vector3(w + 0.1f, 0.3f, d + 0.1f), MBlue, t);
            Box("SignBand", new Vector3(0, h * 0.78f, d * 0.5f - 0.2f + 0.03f), new Vector3(w, 0.5f, 0.08f), MRed, t);
            Box("GlassFront", new Vector3(0, 0.7f, d * 0.5f - 0.16f), new Vector3(w * 0.8f, 1.2f, 0.08f), MGlassDark, t);
            Box("Canopy", new Vector3(0, 1.5f, d * 0.5f + 0.15f), new Vector3(w * 0.7f, 0.12f, 0.7f), MBlueLight, t);
            if (hi)
            {   // 停车场小车
                TinyCar(t, new Vector3(-1.5f, 0.12f, 1.5f), MRed);
                TinyCar(t, new Vector3(1.4f, 0.12f, 1.6f), MYellow);
                Box("CartCorral", new Vector3(1.5f, 0.3f, 0.6f), new Vector3(0.7f, 0.5f, 0.5f), Metal, t);
            }
        }
        void TinyCar(Transform t, Vector3 p, Material col)
        {
            Box("Car", p, new Vector3(0.55f, 0.22f, 0.3f), col, t);
            Box("CarCabin", p + new Vector3(0, 0.16f, 0), new Vector3(0.3f, 0.2f, 0.26f), MGlassDark, t);
        }

        // ============ 现代学校：黄色 2-3 层教学楼 + 操场 + 旗杆 ============
        void BuildSchool(Transform t, bool hi)
        {
            float w = 3.2f, d = 2.2f, h = 3.0f;
            Lot(t, 4.2f, 4.0f, MGrass);
            Box("School", new Vector3(0, h * 0.5f, -0.2f), new Vector3(w, h, d), MYellow, t);
            Box("SchoolBase", new Vector3(0, 0.4f, -0.2f), new Vector3(w, 0.8f, d + 0.04f), MWhite, t);
            Box("FlatRoof", new Vector3(0, h + 0.08f, -0.2f), new Vector3(w + 0.1f, 0.16f, d + 0.1f), MOrangeRoof, t);
            WinGrid(t, w, d, h, 3, 5, MGlassBlue);
            // 操场（红跑道+绿场）
            Box("Field", new Vector3(0, 0.04f, 1.55f), new Vector3(3.6f, 0.05f, 1.1f), MGreen, t);
            Box("Track", new Vector3(0, 0.05f, 1.55f), new Vector3(3.7f, 0.04f, 0.12f), MRed, t);
            // 旗杆
            Cyl("Flagpole", new Vector3(-1.7f, 1.1f, 1.5f), new Vector3(0.04f, 2.2f, 0.04f), MSteel, t);
            Box("Flag", new Vector3(-1.5f, 1.9f, 1.5f), new Vector3(0.5f, 0.3f, 0.04f), MRed, t);
        }

        // ============ 医院：白色 4 层主楼 + 红十字（立面+屋顶停机坪）+ 蓝玻璃入口 ============
        void BuildHospital(Transform t, bool hi)
        {
            float w = 3.0f, d = 2.4f, h = 4.4f;
            Lot(t, w + 0.6f, d + 0.6f, MSidewalk);
            Box("Hospital", new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), MWhite, t);
            // 中央竖向蓝玻璃井
            Box("GlassCore", new Vector3(0, h * 0.55f, d * 0.5f + 0.02f), new Vector3(0.8f, h * 0.9f, 0.06f), MGlassBlue, t);
            WinGrid(t, w, d, h, 4, 4, MGlassCyan);
            Box("Roof", new Vector3(0, h + 0.08f, 0), new Vector3(w + 0.1f, 0.16f, d + 0.1f), MConcrete, t);
            // 入口雨棚 + 门
            Box("Canopy", new Vector3(0, 1.3f, d * 0.5f + 0.3f), new Vector3(1.6f, 0.12f, 0.7f), MBlue, t);
            Box("Door", new Vector3(0, 0.7f, d * 0.5f + 0.04f), new Vector3(1.0f, 1.3f, 0.08f), MGlassDark, t);
            // 立面红十字
            RedCross(t, new Vector3(w * 0.5f + 0.05f, h * 0.72f, 0), 0.8f, false);
            if (hi)
            {   // 屋顶直升机坪 + 红十字
                Cyl("Helipad", new Vector3(0.6f, h + 0.2f, -0.4f), new Vector3(0.7f, 0.05f, 0.7f), MConcreteDark, t);
                RedCross(t, new Vector3(0.6f, h + 0.26f, -0.4f), 0.6f, true);
            }
        }

        // ============ 消防站：红色双车库（黑色卷帘门）+ 训练塔 + 黄条 ============
        void BuildFireStation(Transform t, bool hi)
        {
            float w = 3.6f, d = 2.4f, h = 2.0f;
            Lot(t, w + 0.6f, d + 0.6f, MSidewalk);
            Box("Station", new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), MRed, t);
            Box("Stripe", new Vector3(0, h - 0.25f, d * 0.5f + 0.03f), new Vector3(w, 0.22f, 0.06f), MYellow, t);
            // 两个车库卷帘门
            foreach (var bx in new[] { -0.85f, 0.85f })
            {
                Box("Bay", new Vector3(bx, 0.75f, d * 0.5f + 0.03f), new Vector3(1.4f, 1.5f, 0.08f), MConcreteDark, t);
                Box("BayFrame", new Vector3(bx, 1.55f, d * 0.5f + 0.05f), new Vector3(1.5f, 0.12f, 0.1f), MWhite, t);
            }
            // 训练/瞭望塔
            Box("Tower", new Vector3(w * 0.5f - 0.3f, h + 0.9f, -d * 0.5f + 0.3f), new Vector3(0.9f, 1.8f, 0.9f), MRedDark, t);
            Box("TowerTop", new Vector3(w * 0.5f - 0.3f, h + 1.85f, -d * 0.5f + 0.3f), new Vector3(1.0f, 0.18f, 1.0f), MWhite, t);
            if (hi) WinGrid(t, w, d, h, 1, 4, MGlassDark);
        }

        // ============ 警察局：白楼蓝条 + 车库 + 金色警徽 + 旗杆 ============
        void BuildPolice(Transform t, bool hi)
        {
            float w = 3.2f, d = 2.3f, h = 2.6f;
            Lot(t, w + 0.6f, d + 0.6f, MSidewalk);
            Box("Station", new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), MWhite, t);
            Box("BlueStripe", new Vector3(0, h * 0.62f, d * 0.5f + 0.02f), new Vector3(w, 0.5f, 0.06f), MBlue, t);
            Box("BlueStripeB", new Vector3(0, h * 0.62f, -d * 0.5f - 0.02f), new Vector3(w, 0.5f, 0.06f), MBlue, t);
            Box("Garage", new Vector3(-0.8f, 0.7f, d * 0.5f + 0.03f), new Vector3(1.2f, 1.4f, 0.08f), MConcreteDark, t);
            Box("Door", new Vector3(0.85f, 0.6f, d * 0.5f + 0.04f), new Vector3(0.8f, 1.2f, 0.08f), MGlassDark, t);
            // 金色警徽（小圆+星，用金圆柱面）
            Cyl("Badge", new Vector3(0.85f, h * 0.62f, d * 0.5f + 0.07f), new Vector3(0.28f, 0.06f, 0.28f), Gold, t).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Box("Roof", new Vector3(0, h + 0.08f, 0), new Vector3(w + 0.1f, 0.16f, d + 0.1f), MBlue, t);
            Cyl("Flagpole", new Vector3(-1.5f, 1.1f, -0.9f), new Vector3(0.04f, 2.2f, 0.04f), MSteel, t);
            Box("Flag", new Vector3(-1.3f, 1.9f, -0.9f), new Vector3(0.5f, 0.3f, 0.04f), MBlue, t);
            if (hi) WinGrid(t, w, d, h, 2, 4, MGlassBlue);
        }

        // ============ 公园：草地 + 池塘 + 喷泉 + 十字小径 + 灌木/塔松/长椅 ============
        void BuildPark(Transform t, int v, bool hi)
        {
            float w = 4.2f, d = 4.2f;
            Box("Grass", new Vector3(0, -0.05f, 0), new Vector3(w, 0.18f, d), MGrass, t);
            // 十字小径
            Box("PathV", new Vector3(0, 0.05f, 0), new Vector3(0.5f, 0.05f, d), MSidewalk, t);
            Box("PathH", new Vector3(0, 0.05f, 0), new Vector3(w, 0.05f, 0.5f), MSidewalk, t);
            // 池塘（一角）
            Sph("Pond", new Vector3(-1.25f, 0.06f, -1.25f), new Vector3(1.0f, 0.12f, 0.8f), MWater, t);
            // 中央喷泉
            Cyl("FountainBase", new Vector3(0, 0.12f, 0), new Vector3(0.55f, 0.18f, 0.55f), MConcrete, t);
            Cyl("FountainWater", new Vector3(0, 0.24f, 0), new Vector3(0.45f, 0.06f, 0.45f), MWater, t);
            Cyl("FountainJet", new Vector3(0, 0.45f, 0), new Vector3(0.06f, 0.4f, 0.06f), MWater, t);
            // 灌木 + 塔松
            Sph("Shrub1", new Vector3(1.3f, 0.25f, -1.2f), new Vector3(0.5f, 0.4f, 0.5f), MGreen, t);
            Sph("Shrub2", new Vector3(1.4f, 0.22f, 1.2f), new Vector3(0.42f, 0.34f, 0.42f), MGreen, t);
            Conifer(t, new Vector3(-1.3f, 0, 1.25f), 1.0f);
            if (hi)
            {
                Bench(t, new Vector3(0.9f, 0, 0.5f));
                Bench(t, new Vector3(-0.9f, 0, -0.5f));
                Lamp(t, new Vector3(1.7f, 0, 1.7f));
            }
        }
        void Conifer(Transform t, Vector3 p, float s)
        {
            Cyl("Trunk", p + new Vector3(0, 0.25f * s, 0), new Vector3(0.08f * s, 0.5f * s, 0.08f * s), MBrown, t);
            var c1 = Pyramid("Cone1", p + new Vector3(0, 0.75f * s, 0), new Vector3(0.9f * s, 0.9f * s, 0.9f * s), MGreen, t);
            var c2 = Pyramid("Cone2", p + new Vector3(0, 1.2f * s, 0), new Vector3(0.66f * s, 0.7f * s, 0.66f * s), MGreen, t);
            var c3 = Pyramid("Cone3", p + new Vector3(0, 1.6f * s, 0), new Vector3(0.42f * s, 0.55f * s, 0.42f * s), MGreen, t);
        }
        void Bench(Transform t, Vector3 p)
        {
            Box("BenchSeat", p + new Vector3(0, 0.28f, 0), new Vector3(0.7f, 0.08f, 0.25f), MBrown, t);
            Box("BenchBack", p + new Vector3(0, 0.45f, -0.1f), new Vector3(0.7f, 0.3f, 0.06f), MBrown, t);
        }
        void Lamp(Transform t, Vector3 p)
        {
            Cyl("LampPole", p + new Vector3(0, 0.9f, 0), new Vector3(0.04f, 1.8f, 0.04f), MSteel, t);
            Sph("LampBulb", p + new Vector3(0, 1.85f, 0), Vector3.one * 0.16f, ShaderHelper.Emissive(new Color(0.3f, 0.3f, 0.2f), new Color(1f, 0.9f, 0.6f)), t);
        }

        // ============ 摩天楼：帝国大厦阶梯收分 / 青色玻璃双塔 / 蓝灰玻璃板楼 ============
        // V9.0.8 地标限座：全地图按坐标确定性排序，仅前 3 座摩天楼为全高地标；第 4 座起为 55% 高的普通高层，形成城市天际线层次
        bool IsLandmarkSkyscraper(BuildingEntity self)
        {
            var gm = PixelToCivilization.Core.GameManager.Instance;
            if (gm == null || gm.State == null) return true;
            var pos = new List<Vector2>();
            foreach (var k in gm.State.Buildings)
                if (k != null && k.Type == "skyscraper" && k.MapId == "home")
                    pos.Add(new Vector2(k.X, k.Z));
            pos.Sort((a, b) => !Mathf.Approximately(a.x, b.x) ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            int ord = pos.FindIndex(p => Mathf.Abs(p.x - self.X) < 0.01f && Mathf.Abs(p.y - self.Z) < 0.01f);
            return ord < 0 || ord < 3;
        }
        void BuildSkyscraper(Transform t, int v, bool hi, float wallH, bool landmark)
        {
            int kind = ((v % 3) + 3) % 3;
            Lot(t, 3.2f, 3.2f, MConcreteDark);
            // 非地标楼体整体压到 55% 高（枢轴在地，仍贴地），台基保持原尺寸
            var cluster = new GameObject("SkyCluster");
            cluster.transform.SetParent(t, false);
            if (!landmark) cluster.transform.localScale = new Vector3(1f, 0.55f, 1f);
            Transform c = cluster.transform;
            if (kind == 0) BuildEmpire(c, hi);
            else if (kind == 1) BuildTwinGlass(c, hi);
            else BuildGlassSlab(c, hi);
        }
        // 帝国大厦：艺术装饰灰塔，逐级收分 + 竖向窗带 + 冠顶尖塔，棕砖基座
        void BuildEmpire(Transform t, bool hi)
        {
            var gray = MC(new Color(0.62f, 0.60f, 0.56f), 0f, 0.12f);
            Box("Base", new Vector3(0, 1.2f, 0), new Vector3(3.0f, 2.4f, 3.0f), MC(new Color(0.42f, 0.33f, 0.27f), 0f, 0.08f), t);
            Box("Tier1", new Vector3(0, 3.4f, 0), new Vector3(2.5f, 2.0f, 2.5f), gray, t);
            Box("Tier2", new Vector3(0, 5.2f, 0), new Vector3(2.0f, 1.8f, 2.0f), gray, t);
            Box("Tier3", new Vector3(0, 6.9f, 0), new Vector3(1.5f, 1.6f, 1.5f), gray, t);
            Box("Tier4", new Vector3(0, 8.4f, 0), new Vector3(1.0f, 1.4f, 1.0f), gray, t);
            Cyl("Spire", new Vector3(0, 10.2f, 0), new Vector3(0.08f, 2.4f, 0.08f), MSteel, t);
            if (hi)
            {   // 竖向发光窗带
                var win = ShaderHelper.Emissive(new Color(0.05f, 0.07f, 0.1f), new Color(0.5f, 0.75f, 1f));
                for (int i = -2; i <= 2; i++)
                    Box("VBand", new Vector3(i * 0.42f, 3.4f, 1.26f), new Vector3(0.12f, 3.6f, 0.04f), win, t);
                for (int i = -1; i <= 1; i++)
                    Box("VBand", new Vector3(i * 0.5f, 6.6f, 0.76f), new Vector3(0.12f, 2.6f, 0.04f), win, t);
            }
        }
        // 青色玻璃双塔 + 金/绿裙房 + 屋顶花园
        void BuildTwinGlass(Transform t, bool hi)
        {
            Box("Podium", new Vector3(0, 0.9f, 0), new Vector3(3.2f, 1.8f, 2.8f), MC(new Color(0.30f, 0.45f, 0.32f), 0f, 0.12f), t);
            foreach (var sx in new[] { -0.72f, 0.72f })
            {
                Box("Tower", new Vector3(sx, 5.2f, 0), new Vector3(1.2f, 8.6f, 1.6f), MGlassCyan, t);
                Box("TowerTrim", new Vector3(sx, 9.6f, 0), new Vector3(1.26f, 0.2f, 1.66f), Gold, t);
                if (hi) Box("RoofGarden", new Vector3(sx, 9.8f, 0), new Vector3(1.0f, 0.16f, 1.4f), MGreen, t);
            }
            Box("Bridge", new Vector3(0, 7.6f, 0), new Vector3(0.5f, 0.8f, 1.4f), MGlassBlue, t);
            if (hi) WinGrid(t, 3.0f, 2.6f, 1.8f, 2, 4, MGlassDark);
        }
        // 蓝灰反光玻璃板楼 + 屋顶 HVAC
        void BuildGlassSlab(Transform t, bool hi)
        {
            var glass = MC(new Color(0.30f, 0.44f, 0.58f), 0.15f, 0.9f);
            Box("Slab", new Vector3(0, 5.6f, 0), new Vector3(2.4f, 11.2f, 2.0f), glass, t);
            Box("Crown", new Vector3(0, 11.4f, 0), new Vector3(2.5f, 0.4f, 2.1f), MConcreteDark, t);
            if (hi)
            {
                var win = ShaderHelper.Emissive(new Color(0.04f, 0.08f, 0.12f), new Color(0.45f, 0.7f, 1f));
                for (int f = 0; f < 11; f++)
                {
                    float y = 0.7f + f;
                    Box("Band", new Vector3(0, y, 1.02f), new Vector3(2.2f, 0.18f, 0.04f), win, t);
                    Box("Band", new Vector3(0, y, -1.02f), new Vector3(2.2f, 0.18f, 0.04f), win, t);
                }
                Box("HVAC", new Vector3(0.5f, 11.9f, 0.3f), new Vector3(0.8f, 0.6f, 0.8f), MSteel, t);
            }
        }

        // ============ 现代工厂：白蓝厂房 + 蓝球形储罐 + 红顶白烟囱 + 圆筒筒仓 + 三脚水塔 ============
        void BuildFactoryModern(Transform t, string id, bool hi)
        {
            Lot(t, 4.2f, 3.8f, MConcrete);
            Box("Hall", new Vector3(-0.3f, 1.1f, -0.2f), new Vector3(2.8f, 2.2f, 2.2f), MWhite, t);
            Box("HallRoof", new Vector3(-0.3f, 2.3f, -0.2f), new Vector3(2.9f, 0.18f, 2.3f), MBlue, t);
            // 采光锯齿条
            if (hi) for (int i = -1; i <= 1; i++)
                    Box("Skylight", new Vector3(-0.3f + i * 0.8f, 2.42f, -0.2f), new Vector3(0.5f, 0.06f, 2.0f), MGlassCyan, t);
            Box("Door", new Vector3(-0.3f, 0.8f, 0.95f), new Vector3(1.6f, 1.6f, 0.08f), MConcreteDark, t);
            // 蓝色球形储罐
            Sph("Tank1", new Vector3(1.5f, 0.85f, 0.9f), new Vector3(0.7f, 0.7f, 0.7f), MBlueLight, t);
            Cyl("TankLeg", new Vector3(1.5f, 0.3f, 0.9f), new Vector3(0.12f, 0.6f, 0.12f), MSteel, t);
            // 红顶白烟囱
            Cyl("Stack", new Vector3(1.4f, 2.4f, -1.1f), new Vector3(0.28f, 4.0f, 0.28f), MWhite, t);
            Cyl("StackCap", new Vector3(1.4f, 4.5f, -1.1f), new Vector3(0.32f, 0.25f, 0.32f), MRed, t);
            // 筒仓
            Cyl("Silo", new Vector3(-1.7f, 1.2f, 1.0f), new Vector3(0.55f, 2.4f, 0.55f), MConcrete, t);
            Sph("SiloCap", new Vector3(-1.7f, 2.5f, 1.0f), new Vector3(0.57f, 0.3f, 0.57f), MConcreteDark, t);
        }

        // ============ 现代兵营：灰色营区 + 阅兵场 + 岗亭 + 旗杆（无城堡角楼）============
        void BuildModernBarracks(Transform t, bool hi)
        {
            Lot(t, 4.0f, 4.2f, MConcrete);
            Box("Barracks", new Vector3(0, 0.9f, -0.7f), new Vector3(3.0f, 1.8f, 1.8f), MConcreteDark, t);
            Box("Parade", new Vector3(0, 0.04f, 1.1f), new Vector3(3.4f, 0.05f, 2.0f), MC(new Color(0.60f, 0.62f, 0.55f), 0f, 0.04f), t);
            // 岗亭 + 道杆
            Box("Guard", new Vector3(-1.7f, 0.55f, 1.9f), new Vector3(0.6f, 1.1f, 0.6f), MWhite, t);
            Box("GuardRoof", new Vector3(-1.7f, 1.15f, 1.9f), new Vector3(0.7f, 0.12f, 0.7f), MGreen, t);
            Box("Barrier", new Vector3(-0.9f, 0.7f, 1.9f), new Vector3(1.4f, 0.08f, 0.08f), MWhite, t);
            Box("BarrierRed", new Vector3(-0.9f, 0.72f, 1.9f), new Vector3(0.2f, 0.1f, 0.1f), MRed, t);
            // 旗杆 + 军旗
            Cyl("Flagpole", new Vector3(0, 1.6f, 1.1f), new Vector3(0.05f, 3.2f, 0.05f), MSteel, t);
            Box("Flag", new Vector3(0.35f, 2.8f, 1.1f), new Vector3(0.7f, 0.42f, 0.04f), MRed, t);
            WinGrid(t, 3.0f, 1.8f, 1.8f, 2, 5, MGlassDark);
        }

        // ============ 机场：长跑道（标线）+ 玻璃航站楼 + 管制塔 + 停机坪静态飞机 ============
        void BuildAirport(Transform t, bool hi)
        {
            // 跑道沿 Z 长条
            Box("Runway", new Vector3(0, 0.04f, 2.0f), new Vector3(1.8f, 0.06f, 11.0f), MAsphalt, t);
            for (int i = -4; i <= 4; i++)
                Box("RwyMark", new Vector3(0, 0.09f, 2.0f + i * 1.1f), new Vector3(0.12f, 0.03f, 0.6f), MC(new Color(0.95f, 0.95f, 0.92f), 0f, 0.2f), t);
            // 停机坪
            Box("Apron", new Vector3(-2.2f, 0.03f, -1.5f), new Vector3(3.0f, 0.05f, 3.4f), MConcreteDark, t);
            // 航站楼（玻璃）
            Box("Terminal", new Vector3(-2.4f, 0.9f, -2.6f), new Vector3(2.6f, 1.8f, 1.6f), MGlassBlue, t);
            Box("TerminalRoof", new Vector3(-2.4f, 1.9f, -2.6f), new Vector3(2.8f, 0.18f, 1.8f), MWhite, t);
            // 管制塔
            Cyl("Tower", new Vector3(-3.4f, 1.6f, -1.6f), new Vector3(0.22f, 3.2f, 0.22f), MConcrete, t);
            Cyl("Cab", new Vector3(-3.4f, 3.4f, -1.6f), new Vector3(0.5f, 0.4f, 0.5f), MGlassDark, t);
            // 静态飞机
            StaticPlane(t, new Vector3(-2.2f, 0.25f, -1.2f), 0.7f, MWhite, MBlue);
        }
        void StaticPlane(Transform t, Vector3 p, float s, Material body, Material accent)
        {
            Cyl("Fuselage", p, new Vector3(0.22f * s, 1.6f * s, 0.22f * s), body, t).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Box("Wing", p + new Vector3(0, 0, 0.1f * s), new Vector3(2.0f * s, 0.05f * s, 0.35f * s), accent, t);
            Box("TailWing", p + new Vector3(0, 0.12f * s, -0.7f * s), new Vector3(0.8f * s, 0.05f * s, 0.2f * s), accent, t);
            Box("TailFin", p + new Vector3(0, 0.35f * s, -0.7f * s), new Vector3(0.06f * s, 0.5f * s, 0.3f * s), accent, t);
            Sph("Nose", p + new Vector3(0, 0, 0.85f * s), new Vector3(0.22f * s, 0.22f * s, 0.22f * s), body, t);
        }

        // ============ 发电站：灰厂房 + 两座双曲线近似冷却塔 + 红白烟囱 ============
        void BuildPowerPlant(Transform t, bool hi)
        {
            Lot(t, 4.2f, 3.6f, MConcrete);
            Box("PlantHall", new Vector3(-0.4f, 0.9f, -0.3f), new Vector3(2.6f, 1.8f, 2.0f), MConcreteDark, t);
            foreach (var cx in new[] { 0.9f, 1.9f })
            {
                Cyl("CoolTower", new Vector3(cx, 1.5f, 0.9f), new Vector3(0.55f, 3.0f, 0.55f), MWhite, t);
                Cyl("CoolMouth", new Vector3(cx, 3.05f, 0.9f), new Vector3(0.48f, 0.06f, 0.48f), MConcreteDark, t);
                Cyl("CoolBase", new Vector3(cx, 0.1f, 0.9f), new Vector3(0.6f, 0.2f, 0.6f), MConcrete, t);
            }
            Cyl("Chimney", new Vector3(-1.6f, 2.2f, -1.2f), new Vector3(0.22f, 4.4f, 0.22f), MWhite, t);
            Cyl("ChimneyRed", new Vector3(-1.6f, 4.0f, -1.2f), new Vector3(0.24f, 0.3f, 0.24f), MRed, t);
            if (hi) Box("TurbineHall", new Vector3(-0.4f, 1.95f, -0.3f), new Vector3(2.7f, 0.16f, 2.1f), MBlue, t);
        }

        // ============ 现代水塔（工业时代水井）：三脚支架 + 大水箱 + 锥顶 ============
        void BuildWaterTower(Transform t)
        {
            // 三脚
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                var leg = Box("Leg", new Vector3(Mathf.Sin(a) * 0.45f, 1.1f, Mathf.Cos(a) * 0.45f),
                              new Vector3(0.12f, 2.4f, 0.12f), MSteel, t);
                leg.transform.localRotation = Quaternion.Euler(Mathf.Cos(a) * 12f, 0, -Mathf.Sin(a) * 12f);
            }
            Cyl("Tank", new Vector3(0, 2.6f, 0), new Vector3(0.85f, 0.9f, 0.85f), MBlueLight, t);
            Cyl("TankRoof", new Vector3(0, 3.2f, 0), new Vector3(0.9f, 0.18f, 0.9f), MConcreteDark, t);
            Cyl("Pipe", new Vector3(0, 1.2f, 0), new Vector3(0.1f, 2.4f, 0.1f), MSteel, t);
            Box("Base", new Vector3(0, 0.08f, 0), new Vector3(1.4f, 0.16f, 1.4f), MConcrete, t);
        }

        // ============ 数据中心：白服务器大厅 + 蓝色通风格栅 + 冷机 ============
        void BuildDataCenter(Transform t, bool hi)
        {
            float w = 3.2f, d = 2.4f, h = 2.2f;
            Lot(t, w + 0.6f, d + 0.6f, MConcrete);
            Box("DC", new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), MWhite, t);
            Box("DCRoof", new Vector3(0, h + 0.08f, 0), new Vector3(w + 0.1f, 0.16f, d + 0.1f), MBlue, t);
            if (hi)
                for (int r = 0; r < 3; r++)
                    Box("Vent", new Vector3(0, 0.6f + r * 0.5f, d * 0.5f + 0.03f), new Vector3(w * 0.85f, 0.16f, 0.06f), MGlassDark, t);
            Cyl("Chiller1", new Vector3(1.1f, 0.5f, d * 0.5f + 0.4f), new Vector3(0.3f, 1.0f, 0.3f), MSteel, t);
            Cyl("Chiller2", new Vector3(-1.1f, 0.5f, d * 0.5f + 0.4f), new Vector3(0.3f, 1.0f, 0.3f), MSteel, t);
        }
        // ============ AI 实验室：白色立方体 + 青色玻璃角 + 雷达白球 ============
        void BuildAiLab(Transform t, bool hi)
        {
            float w = 2.8f, d = 2.8f, h = 3.0f;
            Lot(t, w + 0.6f, d + 0.6f, MConcrete);
            Box("Lab", new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), MWhite, t);
            Box("GlassCorner", new Vector3(w * 0.5f - 0.02f, h * 0.6f, 0), new Vector3(0.08f, h * 0.8f, d), MGlassCyan, t);
            Box("Band", new Vector3(0, h * 0.55f, d * 0.5f + 0.02f), new Vector3(w, 0.5f, 0.06f), MGlassCyan, t);
            Cyl("Mast", new Vector3(0, h + 0.7f, 0), new Vector3(0.06f, 1.2f, 0.06f), MSteel, t);
            Sph("Radome", new Vector3(0, h + 1.5f, 0), Vector3.one * 0.45f, MWhite, t);
        }
        // ============ 高铁站：白色大跨平屋顶 + 玻璃站房 + 站台 ============
        void BuildHsStation(Transform t, bool hi)
        {
            Lot(t, 4.4f, 3.4f, MConcrete);
            Box("Hall", new Vector3(0, 0.9f, -0.2f), new Vector3(3.4f, 1.8f, 2.0f), MGlassBlue, t);
            Box("CanopyRoof", new Vector3(0, 2.0f, -0.2f), new Vector3(4.0f, 0.18f, 2.6f), MWhite, t);
            Box("Platform", new Vector3(0, 0.12f, 1.3f), new Vector3(4.0f, 0.16f, 1.0f), MConcreteDark, t);
            // 简化一列高铁（白车身蓝线）
            Box("Train", new Vector3(0, 0.5f, 1.35f), new Vector3(3.6f, 0.5f, 0.5f), MWhite, t);
            Box("TrainStripe", new Vector3(0, 0.55f, 1.62f), new Vector3(3.6f, 0.12f, 0.04f), MBlue, t);
            Sph("TrainNose", new Vector3(1.85f, 0.5f, 1.35f), new Vector3(0.26f, 0.26f, 0.4f), MWhite, t);
        }

        // ============ V9.0.8 地铁站：人行道台基 + 两座玻璃出入口雨棚（自动扶梯口）+ 蓝色发光 M 标识塔 + 通风井 ============
        void BuildSubway(Transform t, bool hi)
        {
            float w = 3.2f, d = 2.6f;
            Lot(t, w, d, MSidewalk);
            // 两座扶梯口（深色开口 + 玻璃斜雨棚），左右对称
            foreach (var sx in new[] { -0.85f, 0.85f })
            {
                Box("Mouth", new Vector3(sx, 0.08f, 0.25f), new Vector3(0.95f, 0.12f, 1.35f), MConcreteDark, t);
                Box("StairDark", new Vector3(sx, 0.12f, 0.55f), new Vector3(0.7f, 0.08f, 0.8f), MAsphalt, t);
                var canopy = Box("Canopy", new Vector3(sx, 0.85f, 0.25f), new Vector3(1.05f, 0.1f, 1.5f), MGlassCyan, t);
                canopy.transform.localRotation = Quaternion.Euler(-16f, 0f, 0f);
                Box("CanopyEdge", new Vector3(sx, 0.55f, -0.45f), new Vector3(1.08f, 0.12f, 0.08f), MBlue, t);
                // 雨棚支柱
                Box("PostL", new Vector3(sx - 0.45f, 0.45f, -0.4f), new Vector3(0.07f, 0.9f, 0.07f), MSteel, t);
                Box("PostR", new Vector3(sx + 0.45f, 0.45f, -0.4f), new Vector3(0.07f, 0.9f, 0.07f), MSteel, t);
            }
            // 中央蓝色 M 标识塔（夜间发光，进 ShaderHelper 夜景表）
            Box("MPylon", new Vector3(0, 1.05f, -d * 0.5f + 0.25f), new Vector3(0.56f, 1.9f, 0.3f), MBlue, t);
            Box("MPanel", new Vector3(0, 1.35f, -d * 0.5f + 0.42f), new Vector3(0.4f, 0.85f, 0.06f),
                ShaderHelper.Emissive(new Color(0.04f, 0.18f, 0.55f), new Color(0.55f, 0.82f, 1f)), t);
            // 白色 M 字形（两根竖柱 + V 形双斜柱，立方体近似）
            Box("ML", new Vector3(-0.13f, 1.32f, -d * 0.5f + 0.46f), new Vector3(0.07f, 0.5f, 0.04f), MWhite, t);
            Box("MR", new Vector3(0.13f, 1.32f, -d * 0.5f + 0.46f), new Vector3(0.07f, 0.5f, 0.04f), MWhite, t);
            var m1 = Box("MS1", new Vector3(-0.065f, 1.5f, -d * 0.5f + 0.46f), new Vector3(0.07f, 0.34f, 0.04f), MWhite, t);
            m1.transform.localRotation = Quaternion.Euler(0, 0, 28f);
            var m2 = Box("MS2", new Vector3(0.065f, 1.5f, -d * 0.5f + 0.46f), new Vector3(0.07f, 0.34f, 0.04f), MWhite, t);
            m2.transform.localRotation = Quaternion.Euler(0, 0, -28f);
            // 后侧两座通风井（混凝土井 + 深色格栅）
            foreach (var sx in new[] { -1.25f, 1.25f })
            {
                Box("VentShaft", new Vector3(sx, 0.4f, -0.75f), new Vector3(0.55f, 0.8f, 0.55f), MConcrete, t);
                Box("VentGrille", new Vector3(sx, 0.45f, -0.46f), new Vector3(0.4f, 0.5f, 0.05f), MConcreteDark, t);
            }
            // LOD3：地面盲道砖引导条
            if (hi)
                Box("Tactile", new Vector3(0, 0.045f, 0.55f), new Vector3(0.35f, 0.03f, 1.7f), MYellow, t);
        }
    }
}
