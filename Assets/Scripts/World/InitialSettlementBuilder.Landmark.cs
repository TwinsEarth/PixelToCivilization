// V9.1.1 四大阵营地标（纯视觉、无功能、无碰撞、不进 S.Buildings）：
// F1 天坛祈年殿（红柱蓝瓦金顶）/ F2 帝国大厦式艺术装饰灰塔 / F3 东京塔（红白格构）/ F4 阶梯金字塔。
// 开局由聚落流程创建；读档后 RebuildEarthLandmarks 按国家阵营确定性重建。
using UnityEngine;
using PixelToCivilization.Core;

namespace PixelToCivilization.World
{
    public static partial class InitialSettlementBuilder
    {
        /// <summary>在 (x,z) 附近（固定环序列回退）放置一座阵营地标；落水会挪到最近陆地。</summary>
        public static GameObject BuildLandmark(GameManager gm, WorldGenerator terrain, float x, float z, int faction)
        {
            if (terrain == null || faction < 1 || faction > 4) return null;
            // 固定顺序找非水落点（确定性，保证重建一致）
            if (terrain.IsWater(x, z))
            {
                bool found = false;
                for (float r = 4f; r <= 24f && !found; r += 4f)
                    for (int i = 0; i < 16; i++)
                    {
                        float a = i * Mathf.PI * 2f / 16f;
                        float px = x + Mathf.Cos(a) * r, pz = z + Mathf.Sin(a) * r;
                        if (!terrain.IsWater(px, pz) && terrain.HeightAt(px, pz) <= 3.4f)
                        { x = px; z = pz; found = true; break; }
                    }
                if (!found) return null;
            }
            var root = new GameObject($"EarthLandmark_F{faction}_{Mathf.RoundToInt(x)}_{Mathf.RoundToInt(z)}");
            root.transform.SetParent(gm.transform, false);
            root.transform.position = new Vector3(x, terrain.HeightAt(x, z), z);
            Transform t = root.transform;
            switch (faction)
            {
                case 1: BuildTempleOfHeaven(t); break;
                case 2: BuildEmpireSpire(t); break;
                case 3: BuildTokyoTower(t); break;
                default: BuildStepPyramid(t); break;
            }
            return root;
        }

        /// <summary>读档后按存档国家重建全部阵营地标（先清旧根，避免重复）。</summary>
        public static void RebuildEarthLandmarks(GameManager gm, WorldGenerator terrain)
        {
            if (gm == null || terrain == null || !terrain.EarthMode) return;
            var old = new System.Collections.Generic.List<GameObject>();
            foreach (Transform c in gm.transform)
                if (c != null && c.name.StartsWith("EarthLandmark_")) old.Add(c.gameObject);
            foreach (var go in old) Object.DestroyImmediate(go);   // 同帧重建，必须立即移除避免地标叠加
            var S = gm.State;
            if (S?.Nations == null) return;
            int built = 0;
            foreach (var n in S.Nations)
                if (n != null && n.Alive && n.Faction >= 1)
                { BuildLandmark(gm, terrain, n.Cx + 6f, n.Cz - 6f, n.Faction); built++; }
            Debug.Log($"[V911] RebuildEarthLandmarks removed={old.Count} built={built}");
        }

        // ---------- F1 天坛·祈年殿：三层白石圆台 + 红柱层 + 三重蓝瓦攒尖 + 金宝顶 ----------
        static void BuildTempleOfHeaven(Transform t)
        {
            var marble = ShaderHelper.Mat(new Color(0.92f, 0.90f, 0.84f));
            var red = ShaderHelper.Mat(new Color(0.62f, 0.13f, 0.12f));
            var blue = ShaderHelper.Mat(new Color(0.10f, 0.27f, 0.52f));
            var gold = ShaderHelper.Emissive(new Color(0.83f, 0.66f, 0.22f), new Color(0.35f, 0.26f, 0.08f));
            LP(PrimitiveType.Cylinder, "Terrace1", new Vector3(0, 0.15f, 0), new Vector3(5.2f, 0.3f, 5.2f), marble, t);
            LP(PrimitiveType.Cylinder, "Terrace2", new Vector3(0, 0.45f, 0), new Vector3(4.2f, 0.3f, 4.2f), marble, t);
            LP(PrimitiveType.Cylinder, "Terrace3", new Vector3(0, 0.75f, 0), new Vector3(3.2f, 0.3f, 3.2f), marble, t);
            LP(PrimitiveType.Cylinder, "RedHall", new Vector3(0, 1.8f, 0), new Vector3(2.4f, 1.8f, 2.4f), red, t);
            HipRoof("Roof1", new Vector3(0, 3.05f, 0), new Vector3(4.2f, 0.75f, 4.2f), blue, t);
            HipRoof("Roof2", new Vector3(0, 3.78f, 0), new Vector3(3.1f, 0.62f, 3.1f), blue, t);
            HipRoof("Roof3", new Vector3(0, 4.42f, 0), new Vector3(2.0f, 0.55f, 2.0f), blue, t);
            LP(PrimitiveType.Sphere, "GoldBead", new Vector3(0, 4.92f, 0), Vector3.one * 0.4f, gold, t);
            LP(PrimitiveType.Cylinder, "GoldSpire", new Vector3(0, 5.35f, 0), new Vector3(0.07f, 0.7f, 0.07f), gold, t);
        }

        // ---------- F2 帝国大厦式：棕砖基座 + 四级灰色艺术装饰收分 + 钢尖顶 ----------
        static void BuildEmpireSpire(Transform t)
        {
            var brown = ShaderHelper.Mat(new Color(0.42f, 0.33f, 0.27f));
            var gray = ShaderHelper.Mat(new Color(0.62f, 0.60f, 0.56f));
            var steel = ShaderHelper.Mat(new Color(0.55f, 0.60f, 0.66f));
            LP(PrimitiveType.Cube, "Base", new Vector3(0, 0.6f, 0), new Vector3(3.0f, 1.2f, 3.0f), brown, t);
            LP(PrimitiveType.Cube, "Tier1", new Vector3(0, 2.2f, 0), new Vector3(2.5f, 2.0f, 2.5f), gray, t);
            LP(PrimitiveType.Cube, "Tier2", new Vector3(0, 4.1f, 0), new Vector3(2.0f, 1.8f, 2.0f), gray, t);
            LP(PrimitiveType.Cube, "Tier3", new Vector3(0, 5.8f, 0), new Vector3(1.5f, 1.6f, 1.5f), gray, t);
            LP(PrimitiveType.Cube, "Tier4", new Vector3(0, 7.3f, 0), new Vector3(1.0f, 1.4f, 1.0f), gray, t);
            LP(PrimitiveType.Cylinder, "Spire", new Vector3(0, 9.4f, 0), new Vector3(0.1f, 2.8f, 0.1f), steel, t);
        }

        // ---------- F3 东京塔：三段红白格构塔身 + 三道白环 + 展望台 + 天线 ----------
        static void BuildTokyoTower(Transform t)
        {
            var red = ShaderHelper.Mat(new Color(0.78f, 0.12f, 0.12f));
            var white = ShaderHelper.Mat(new Color(0.95f, 0.94f, 0.90f));
            var steel = ShaderHelper.Mat(new Color(0.55f, 0.58f, 0.62f));
            LP(PrimitiveType.Cylinder, "S1", new Vector3(0, 1.3f, 0), new Vector3(2.0f, 2.6f, 2.0f), red, t);
            LP(PrimitiveType.Cylinder, "W1", new Vector3(0, 2.6f, 0), new Vector3(2.06f, 0.3f, 2.06f), white, t);
            LP(PrimitiveType.Cylinder, "S2", new Vector3(0, 3.9f, 0), new Vector3(1.4f, 2.6f, 1.4f), red, t);
            LP(PrimitiveType.Cylinder, "W2", new Vector3(0, 5.2f, 0), new Vector3(1.46f, 0.3f, 1.46f), white, t);
            LP(PrimitiveType.Cylinder, "S3", new Vector3(0, 6.3f, 0), new Vector3(0.9f, 2.2f, 0.9f), red, t);
            LP(PrimitiveType.Cube, "Deck", new Vector3(0, 6.7f, 0), new Vector3(1.5f, 0.5f, 1.5f), white, t);
            LP(PrimitiveType.Cylinder, "W3", new Vector3(0, 7.5f, 0), new Vector3(0.96f, 0.26f, 0.96f), white, t);
            LP(PrimitiveType.Cylinder, "S4", new Vector3(0, 8.6f, 0), new Vector3(0.42f, 2.0f, 0.42f), red, t);
            LP(PrimitiveType.Cylinder, "Antenna", new Vector3(0, 10.4f, 0), new Vector3(0.08f, 1.8f, 0.08f), steel, t);
        }

        // ---------- F4 阶梯金字塔：五级土黄石阶 + 顶部绿色小庙 ----------
        static void BuildStepPyramid(Transform t)
        {
            var sand = ShaderHelper.Mat(new Color(0.72f, 0.60f, 0.42f));
            var sandDark = ShaderHelper.Mat(new Color(0.62f, 0.50f, 0.34f));
            var green = ShaderHelper.Mat(new Color(0.28f, 0.55f, 0.25f));
            float w = 5.0f;
            for (int i = 0; i < 5; i++)
            {
                float ww = w - i * 0.8f;
                LP(PrimitiveType.Cube, "Step" + i, new Vector3(0, 0.35f + i * 0.7f, 0), new Vector3(ww, 0.7f, ww),
                    i % 2 == 0 ? sand : sandDark, t);
            }
            LP(PrimitiveType.Cube, "Temple", new Vector3(0, 4.0f, 0), new Vector3(1.4f, 1.0f, 1.4f), green, t);
            HipRoof("TempleRoof", new Vector3(0, 4.65f, 0), new Vector3(1.8f, 0.35f, 1.8f), sandDark, t);
        }

        // 四坡尖顶（旋转 45° 扁立方体近似攒尖/庑殿顶）
        static void HipRoof(string name, Vector3 pos, Vector3 scale, Material m, Transform p)
        {
            var g = LP(PrimitiveType.Cube, name, pos, scale, m, p);
            g.transform.localRotation = Quaternion.Euler(0, 45, 0);
        }

        static GameObject LP(PrimitiveType pt, string name, Vector3 pos, Vector3 scale, Material m, Transform p)
        {
            var g = GameObject.CreatePrimitive(pt);
            Object.Destroy(g.GetComponent<Collider>());
            g.name = name;
            g.transform.SetParent(p, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }
    }
}
