// V8.0.1 乐高积木像素风：塑料色调色板 + 共享凸点(stud)几何工具。
using UnityEngine;

namespace PixelToCivilization.World
{
    /// <summary>乐高积木像素风工具：塑料提色 + 顶面圆形凸点。
    /// 凸点为纯装饰、无 Collider、不参与点击判定；只应挂在各模型 LV3 近景节点下，随 LOD 远距隐藏。</summary>
    public static class LegoKit
    {
        public const bool On = false;   // V9.0.1 现代风：停用乐高凸点（V8.0.1 专属）

        // —— 经典乐高饱和塑料色板（供各系统取用）——
        public static readonly Color Red    = new Color(0.78f, 0.12f, 0.10f);
        public static readonly Color Blue   = new Color(0.07f, 0.38f, 0.85f);
        public static readonly Color Yellow = new Color(0.98f, 0.80f, 0.10f);
        public static readonly Color Green  = new Color(0.13f, 0.62f, 0.25f);
        public static readonly Color White  = new Color(0.95f, 0.95f, 0.93f);
        public static readonly Color Black  = new Color(0.08f, 0.08f, 0.09f);
        public static readonly Color Brown  = new Color(0.45f, 0.28f, 0.13f);
        public static readonly Color Tan    = new Color(0.83f, 0.70f, 0.45f);
        public static readonly Color Gray   = new Color(0.55f, 0.57f, 0.60f);
        public static readonly Color Orange = new Color(0.92f, 0.50f, 0.10f);

        /// <summary>把任意基色提亮、提饱和为塑料色（保留 alpha 与色相）。</summary>
        public static Color Tint(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * 1.16f + 0.03f);
            v = Mathf.Clamp01(v * 1.03f + 0.02f);
            var r = Color.HSVToRGB(h, s, v);
            r.a = c.a;
            return r;
        }

        static Mesh _stud;
        static Mesh StudMesh()
        {
            if (_stud != null) return _stud;
            // 借用内置圆柱网格（引擎内置资源，销毁临时物体后网格仍可复用）
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _stud = g.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(g);
            return _stud;
        }

        /// <summary>在 parent 下加一个无碰撞凸点。内置圆柱高 2、半径 0.5，按 radius/height 缩放。</summary>
        public static GameObject Stud(Transform parent, Vector3 localPos, float radius, float height, Material mat, string name = "Stud")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = StudMesh();
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            return go;
        }

        /// <summary>在水平大板顶面布稀疏凸点网格。
        /// centerXZ=板面中心(local x,z)，topY=板面顶 localY，w/d=板尺寸，spacing=凸点间距，maxTotal=数量上限。
        /// 板面过小返回 0；超上限自动放大间距/截断，保证不爆量。</summary>
        public static int StudGrid(Transform parent, Vector2 centerXZ, float topY, float w, float d, Material mat,
                                  float spacing = 0.55f, int maxTotal = 36, float radius = 0.16f, float height = 0.12f)
        {
            if (!On || mat == null || parent == null) return 0;
            int nx = Mathf.FloorToInt((w + 0.05f) / spacing);
            int nz = Mathf.FloorToInt((d + 0.05f) / spacing);
            if (nx < 1 || nz < 1) return 0;
            if (nx * nz > maxTotal)
            {
                spacing *= 1.6f;
                nx = Mathf.FloorToInt((w + 0.05f) / spacing);
                nz = Mathf.FloorToInt((d + 0.05f) / spacing);
                if (nx < 1 || nz < 1) return 0;
                if (nx * nz > maxTotal) { nx = Mathf.Min(nx, 6); nz = Mathf.Min(nz, 6); }
            }
            float ox = centerXZ.x - (nx - 1) * spacing * 0.5f;
            float oz = centerXZ.y - (nz - 1) * spacing * 0.5f;
            for (int ix = 0; ix < nx; ix++)
                for (int iz = 0; iz < nz; iz++)
                    Stud(parent, new Vector3(ox + ix * spacing, topY + height * 0.5f, oz + iz * spacing), radius, height, mat);
            return nx * nz;
        }

        /// <summary>沿一条直线布单排凸点（檐线/脊线用）。</summary>
        public static int StudRow(Transform parent, Vector3 start, Vector3 step, int count, float radius, float height, Material mat)
        {
            if (!On || count <= 0 || mat == null || parent == null) return 0;
            for (int i = 0; i < count; i++) Stud(parent, start + step * i, radius, height, mat);
            return count;
        }
    }
}
