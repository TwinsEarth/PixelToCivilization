using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.0.9 多城区/行政区系统：把全城建筑按空间邻近度贪心聚类为若干行政区，
    /// 每区有确定性的方位名与主导功能，视图为区中心的彩色横幅标识（纯派生数据，不进存档；
    /// 视图挂 "Districts" 根随清局销毁，聚类结果变化时重建）。城际公路/铁路/航线以本区表为节点。
    /// </summary>
    public class DistrictSystem : GameSystemBase
    {
        public class District
        {
            public int Id;
            public string Name = "";
            public string Specialty = "";
            public float Cx, Cz;
            public int Count;
            public readonly Dictionary<string, int> CatCount = new();
        }

        private const float MergeRadius = 42f;     // 建筑归入最近区的最大质心距离
        private const int MinBuildings = 6;        // 少于该数的区并入最近邻
        public const int MaxDistricts = 8;

        private readonly List<District> _districts = new();
        private Transform _root;
        private readonly List<GameObject> _marks = new();
        private float _rebuildCd;
        private string _sig = "";

        // 行政区横幅调色板（明亮、彼此可辨）
        private static readonly Color[] Palette =
        {
            new(0.86f,0.27f,0.20f), new(0.20f,0.45f,0.85f), new(0.95f,0.72f,0.15f),
            new(0.25f,0.65f,0.35f), new(0.55f,0.35f,0.80f), new(0.10f,0.65f,0.68f),
            new(0.90f,0.45f,0.15f), new(0.75f,0.25f,0.55f)
        };

        public IReadOnlyList<District> Districts => _districts;
        private Transform Root => _root = EntityViewFactory.EnsureRoot("Districts", GM.transform);

        public override void Tick(float dt)
        {
            if (S == null) return;
            // 清局/新开局会从外部销毁根下标识：检测到悬空引用则强制重算
            if (_marks.Count > 0 && _marks[0] == null) { _marks.Clear(); _sig = ""; }
            _rebuildCd -= dt;
            if (_rebuildCd <= 0f)
            {
                _rebuildCd = 5f;
                Recompute();
            }
        }

        /// <summary>立即重算（探针/跳年后用）。</summary>
        public void RefreshNow() => Recompute();

        private void Recompute()
        {
            _districts.Clear();
            var bs = S.Buildings;
            if (bs == null || bs.Count == 0) { RebuildMarks(); return; }

            // 全局中心（用于方位命名）
            float gx = 0, gz = 0; int gn = 0;
            foreach (var b in bs) { if (b == null) continue; gx += b.X; gz += b.Z; gn++; }
            if (gn == 0) { RebuildMarks(); return; }
            gx /= gn; gz /= gn;

            // 贪心聚类：按 (X,Z) 顺序遍历，并入最近且距离≤MergeRadius 的既有区，否则开新区
            var sums = new List<Vector3>();   // x=ΣX y=count z=ΣZ
            foreach (var b in bs)
            {
                if (b == null) continue;
                int best = -1; float bestD = MergeRadius * MergeRadius;
                for (int i = 0; i < _districts.Count; i++)
                {
                    var s = sums[i];
                    float cx = s.x / s.y, cz = s.z / s.y;
                    float dx = cx - b.X, dz = cz - b.Z, d2 = dx * dx + dz * dz;
                    if (d2 < bestD) { bestD = d2; best = i; }
                }
                if (best < 0)
                {
                    if (_districts.Count >= MaxDistricts)
                    {
                        // 超上限：并入全局最近区（忽略半径）
                        float bd = float.MaxValue; int bi = 0;
                        for (int i = 0; i < _districts.Count; i++)
                        {
                            var s = sums[i];
                            float cx = s.x / s.y, cz = s.z / s.y;
                            float d2 = (cx - b.X) * (cx - b.X) + (cz - b.Z) * (cz - b.Z);
                            if (d2 < bd) { bd = d2; bi = i; }
                        }
                        best = bi;
                    }
                    else
                    {
                        _districts.Add(new District { Id = _districts.Count });
                        sums.Add(new Vector3(0, 0, 0));
                        best = _districts.Count - 1;
                    }
                }
                var sum = sums[best];
                sums[best] = new Vector3(sum.x + b.X, sum.y + 1, sum.z + b.Z);
                var d = _districts[best];
                string cat = GM.Def(b.Type)?.Cat ?? "基础";
                d.CatCount.TryGetValue(cat, out int cn); d.CatCount[cat] = cn + 1;
            }

            // 小区并入最近邻
            bool merged = true;
            while (merged)
            {
                merged = false;
                int small = -1;
                for (int i = 0; i < _districts.Count; i++)
                    if (sums[i].y < MinBuildings) { small = i; break; }
                if (small >= 0 && _districts.Count > 1)
                {
                    float cx = sums[small].x / sums[small].y, cz = sums[small].z / sums[small].y;
                    float bd = float.MaxValue; int bi = -1;
                    for (int i = 0; i < _districts.Count; i++)
                    {
                        if (i == small) continue;
                        var s = sums[i];
                        float dx = s.x / s.y - cx, dz = s.z / s.y - cz, d2 = dx * dx + dz * dz;
                        if (d2 < bd) { bd = d2; bi = i; }
                    }
                    if (bi >= 0)
                    {
                        var st = sums[small];
                        var t = sums[bi];
                        sums[bi] = new Vector3(t.x + st.x, t.y + st.y, t.z + st.z);
                        foreach (var kv in _districts[small].CatCount)
                        {
                            _districts[bi].CatCount.TryGetValue(kv.Key, out int v);
                            _districts[bi].CatCount[kv.Key] = v + kv.Value;
                        }
                        _districts.RemoveAt(small); sums.RemoveAt(small);
                        merged = true;
                    }
                }
            }

            // 质心与命名（按质心相对全局中心的方位，确定性）
            var named = new List<(float x, float z, District d)>();
            for (int i = 0; i < _districts.Count; i++)
            {
                var s = sums[i];
                _districts[i].Cx = s.x / s.y; _districts[i].Cz = s.z / s.y; _districts[i].Count = (int)s.y;
                named.Add((_districts[i].Cx, _districts[i].Cz, _districts[i]));
            }
            // 方位名按角度去重分配
            var usedNames = new HashSet<string>();
            foreach (var n in named)
            {
                string dir = DirectionName(n.x - gx, n.z - gz);
                string spec = DominantSpecialty(n.d);
                string baseName = dir;
                string name = baseName + "区", k2 = name;
                int suf = 2;
                while (usedNames.Contains(name)) { name = baseName + "区" + suf; suf++; }
                usedNames.Add(name);
                n.d.Name = name; n.d.Specialty = spec;
            }
            // Id 按质心 X 排序稳定
            _districts.Sort((a, b) => a.Cx.CompareTo(b.Cx));
            for (int i = 0; i < _districts.Count; i++) _districts[i].Id = i;

            // 变化签名（数量+质心量化），避免每 5 秒重建视图
            var sb = new StringBuilder();
            sb.Append(_districts.Count).Append('|');
            foreach (var d in _districts) sb.Append(d.Name).Append(':').Append(Mathf.RoundToInt(d.Cx / 8f)).Append(',').Append(Mathf.RoundToInt(d.Cz / 8f)).Append(';');
            string sig = sb.ToString();
            if (sig != _sig) { _sig = sig; RebuildMarks(); }
        }

        private static string DirectionName(float dx, float dz)
        {
            if (Mathf.Abs(dx) < 14f && Mathf.Abs(dz) < 14f) return "中央";
            float adx = Mathf.Abs(dx), adz = Mathf.Abs(dz);
            string h = dx >= 0 ? "东" : "西";
            string v = dz >= 0 ? "南" : "北";
            if (adx > adz * 1.8f) return h;
            if (adz > adx * 1.8f) return v;
            return v + h;   // 复合方位，如 北东/南西
        }

        private static string DominantSpecialty(District d)
        {
            // 归并大类后取主导
            int Group(string c) => c switch
            {
                "居住" => 1,
                "食物" => 2,
                "工业" or "资源" or "能源" => 3,
                "经济" => 4,
                "军事" => 5,
                "文化" or "科技" or "太空" => 6,
                "海洋" or "交通" => 7,
                _ => 8
            };
            string Label(int g) => g switch
            {
                1 => "住宅", 2 => "农业", 3 => "工业", 4 => "商业",
                5 => "军事", 6 => "文教", 7 => "港务", _ => "综合"
            };
            var grp = new int[9];
            foreach (var kv in d.CatCount) grp[Group(kv.Key)] += kv.Value;
            int best = 8, bv = -1;
            for (int g = 1; g <= 8; g++) if (grp[g] > bv) { bv = grp[g]; best = g; }
            return Label(best);
        }

        private void RebuildMarks()
        {
            foreach (var m in _marks) if (m != null) Object.Destroy(m);
            _marks.Clear();
            var terrain = Object.FindObjectOfType<WorldGenerator>();
            for (int i = 0; i < _districts.Count; i++)
            {
                var d = _districts[i];
                var col = Palette[i % Palette.Length];
                float gy = terrain != null ? terrain.HeightAt(d.Cx, d.Cz) : 0f;
                var host = new GameObject("District_" + d.Name);
                host.transform.SetParent(Root, false);
                host.transform.position = new Vector3(d.Cx, gy, d.Cz);
                // 地面深色圆台
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(disc.GetComponent<Collider>());
                disc.name = "Disc"; disc.transform.SetParent(host.transform, false);
                disc.transform.localPosition = new Vector3(0, 0.06f, 0);
                disc.transform.localScale = new Vector3(3.2f, 0.08f, 3.2f);
                disc.GetComponent<Renderer>().material = ShaderHelper.Pbr(new Color(0.10f, 0.10f, 0.12f), 0.2f, 0.4f, 1330 + i, 0.4f);
                // 彩色横幅（无血条），放大、升高，成为行政区标识
                OverheadBillboard.Attach(host, col, false, 1.7f, 6.0f);
                _marks.Add(host);
            }
        }

        public string Diagnose()
        {
            var sb = new StringBuilder();
            sb.Append($"[DIST] districts={_districts.Count}");
            foreach (var d in _districts)
                sb.Append($" | {d.Name}·{d.Specialty}({d.Count})");
            return sb.ToString();
        }
    }
}
