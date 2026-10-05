// V9.5.4 高架桥地图错乱&闪烁 5 处根因修复（V9.5.3 注释级修复被判无效，本轮代码级）
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixelToCivilization.Systems
{
    // 桥段（普通桥/跨海大桥/高架）
    public class BridgeSegment
    {
        public int Id;
        public int Tier;            // 1木桥 2石桥 3钢铁/混凝土桥 4高架
        public int EraTech;         // 建设时技术等级
        public int Span;
        public int Gx1, Gz1, Gx2, Gz2;
        public bool SeaCross;       // 跨海大桥
        public int Age;
        public int MaxAge;
        public GameObject View;
        public bool Dying;
    }

    // 高架柱
    public class ViaductPier
    {
        public int Id;
        public int Gx, Gz;
        public int Tier;
        public int Age, MaxAge;
        public GameObject View;
        public bool Dying;
    }

    public class BridgeSystem : MonoBehaviour
    {
        public static BridgeSystem I;
        public List<BridgeSegment> Segments = new List<BridgeSegment>();
        public List<ViaductPier> Piers = new List<ViaductPier>();
        public int[] SpanByTier = { 28, 50, 75, 100 };
        public const int HardMaxSpan = 100;
        public const int GrandSpan = 50;
        public int MainCap = 5, SecCap = 3, IslandCap = 2;
        public int NormalQuotaPer10Y = 300, GrandQuotaPer50Y = 50;
        public int NormalLifeMin = 10, NormalLifeMax = 30;
        public int GrandLifeMin = 50, GrandLifeMax = 100;
        public const int ViaductDeckY = 8;
        public const int ViaductMaxSpan = 60;
        public int ViaductLifeMin = 100, ViaductLifeMax = 150;
        public const int RunStride = 8;

        static int _nextId = 1;

        void Awake() { I = this; }

        int Idx(int gx, int gz) { return gz * Core.GameConstants.MapSize + gx; }
        int G(int gx, int gz) { return Core.GameConstants.MapSize; }
        int LandOfCell(int gx, int gz)
        {
            var s = Core.GameState.S;
            if (s == null || s.TerrainMap == null) return 0;
            int i = Idx(gx, gz);
            if (i < 0 || i >= s.TerrainMap.Length) return 0;
            return s.TerrainMap[i] > 0 ? s.TerrainMap[i] : 0;
        }
        float HeightAt(int gx, int gz)
        {
            var s = Core.GameState.S;
            if (s == null || s.HeightMap == null) return 0;
            int i = Idx(gx, gz);
            if (i < 0 || i >= s.HeightMap.Length) return 0;
            return s.HeightMap[i];
        }

        // R1 修复：统一到格中心（W2CX 取整回格中心），柱落点与显示一致
        public Vector2 SnapCell(float wx, float wz)
        {
            int gx = Mathf.RoundToInt(wx / 4f);
            int gz = Mathf.RoundToInt(wz / 4f);
            return new Vector2(gx * 4f + 2f, gz * 4f + 2f);
        }

        // 时代技术等级（与 EraDatabase 一致）
        public int EraTech()
        {
            var s = Core.GameState.S;
            int era = Mathf.Clamp(s.Era, 0, 8);
            if (era <= 2) return 1;
            if (era <= 4) return 2;
            if (era <= 6) return 3;
            return 4;
        }

        // 技术可达跨距上限
        public int MaxSpanByEra()
        {
            int t = EraTech();
            return SpanByTier[Mathf.Clamp(t - 1, 0, 3)];
        }

        public bool CanBuildNow(out string why)
        {
            var s = Core.GameState.S;
            // 普通桥每10年1额度；跨海每50年1额度
            int normT = s.Year / 10;
            int grandT = s.Year / 50;
            int normUsed = Segments.Count(x => !x.SeaCross);
            int grandUsed = Segments.Count(x => x.SeaCross);
            if (normUsed >= NormalQuotaPer10Y && normT > 0) { why = "普通桥额度已满"; return false; }
            if (grandUsed >= GrandQuotaPer50Y && grandT > 0) { why = "跨海大桥额度已满"; return false; }
            why = null;
            return true;
        }

        // 高架柱建造（玩家手动点地立柱；R2 修复：地形校验）
        public bool PlayerBuildPier(Vector3 worldPos, out string msg)
        {
            var v = SnapCell(worldPos.x, worldPos.z);
            int gx = Mathf.RoundToInt(v.x / 4f);
            int gz = Mathf.RoundToInt(v.y / 4f);
            // R2: 立柱前地形校验——山/高原（>6.5）拒绝，柱顶才不穿出桥面
            if (HeightAt(gx, gz) > 6.5f)
            {
                // 无声重试：就近找平地
                for (int r = 1; r <= 6; r++)
                {
                    for (int dz = -r; dz <= r; dz++)
                    {
                        for (int dx = -r; dx <= r; dx++)
                        {
                            int ngx = gx + dx, ngz = gz + dz;
                            if (HeightAt(ngx, ngz) <= 6.5f && LandOfCell(ngx, ngz) <= 0)
                            {
                                gx = ngx; gz = ngz;
                                v = new Vector2(gx * 4f + 2f, gz * 4f + 2f);
                                goto found;
                            }
                        }
                    }
                }
                msg = "此处地形过高，无法立柱";
                return false;
            }
        found:
            var s = Core.GameState.S;
            if (s.Res["steel"] < 80 || s.Res["concrete"] < 40) { msg = "高架柱需钢80砼40"; return false; }
            s.Res["steel"] -= 80; s.Res["concrete"] -= 40;
            var p = new ViaductPier();
            p.Id = _nextId++; p.Gx = gx; p.Gz = gz; p.Tier = 4;
            p.MaxAge = UnityEngine.Random.Range(ViaductLifeMin, ViaductLifeMax + 1);
            p.View = BuildPierView(gx, gz);
            Piers.Add(p);
            // 连线：与相邻柱自动连片高架
            TryLinkViaduct(gx, gz);
            msg = "已立高架柱，自动与相邻柱连片";
            return true;
        }

        void TryLinkViaduct(int gx, int gz)
        {
            for (int i = Piers.Count - 1; i >= 0; i--)
            {
                var p = Piers[i];
                if (p.Id <= 0) continue;
                if (p.Gx == gx && p.Gz == gz) continue;
                int dx = p.Gx - gx, dz = p.Gz - gz;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist <= ViaductMaxSpan)
                {
                    if (BuildViaduct(gx, gz, p.Gx, p.Gz, out _)) return;
                }
            }
        }

        // R3 修复：BuildViaduct 返回 bool；中段高度校验，不合格整段回滚
        public bool BuildViaduct(int gx1, int gz1, int gx2, int gz2, out string msg)
        {
            int dx = gx2 - gx1, dz = gz2 - gz1;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist > ViaductMaxSpan) { msg = "高架跨距超限(60)"; return false; }
            var marked = new List<(int,int)>();
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
            for (int st = 1; st < steps; st++)
            {
                int gx = gx1 + Mathf.RoundToInt(dx * st / (float)steps);
                int gz = gz1 + Mathf.RoundToInt(dz * st / (float)steps);
                marked.Add((gx, gz));
                // 中段高度 ≥ 桥面(7) 即穿山/穿地，整段拒绝
                if (HeightAt(gx, gz) >= ViaductDeckY - 1) { Rollback(marked); msg = "高架中段地形过高，整段取消"; return false; }
            }
            var s = Core.GameState.S;
            if (s.Res["steel"] < 60 || s.Res["concrete"] < 30) { Rollback(marked); msg = "高架缺钢60/砼30"; return false; }
            s.Res["steel"] -= 60; s.Res["concrete"] -= 30;
            var seg = new BridgeSegment();
            seg.Id = _nextId++; seg.Tier = 4; seg.EraTech = EraTech();
            seg.Span = Mathf.RoundToInt(dist * 4f);
            seg.Gx1 = gx1; seg.Gz1 = gz1; seg.Gx2 = gx2; seg.Gz2 = gz2;
            seg.SeaCross = false;
            seg.MaxAge = UnityEngine.Random.Range(ViaductLifeMin, ViaductLifeMax + 1);
            seg.View = BuildViaductView(gx1, gz1, gx2, gz2);
            Segments.Add(seg);
            msg = "高架连片成功";
            return true;
        }

        void Rollback(List<(int,int)> marked)
        {
            // 标记已扣资源回滚（当前仅扣标记，未实际扣，保持幂等）
            marked.Clear();
        }

        // 玩家普通桥/跨海桥（V9.4.4 手动建桥→自动连最近陆地）
        public bool PlayerBuildBridge(Vector3 worldPos, out string msg)
        {
            var v = SnapCell(worldPos.x, worldPos.z);
            int gx = Mathf.RoundToInt(v.x / 4f);
            int gz = Mathf.RoundToInt(v.y / 4f);
            if (LandOfCell(gx, gz) > 0) { msg = "请点水面"; return false; }
            // 找最近对岸陆地
            int bestGx = -1, bestGz = -1; float best = float.MaxValue;
            for (int r = 1; r <= HardMaxSpan; r++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int ngx = gx + dx, ngz = gz + dz;
                        if (LandOfCell(ngx, ngz) > 0)
                        {
                            float d = dx * dx + dz * dz;
                            if (d < best) { best = d; bestGx = ngx; bestGz = ngz; }
                        }
                    }
                }
                if (bestGx >= 0 && r >= 5) break;
            }
            if (bestGx < 0) { msg = "找不到对岸陆地"; return false; }
            float span = Mathf.Sqrt(best);
            if (span > MaxSpanByEra()) { msg = "技术跨距不足(最大" + MaxSpanByEra() + ")"; return false; }
            bool sea = span >= GrandSpan;
            if (!CanBuildNow(out string why)) { msg = why; return false; }
            int tier = EraTech();
            if (sea && tier < 3) { msg = "跨海大桥需钢铁技术(era6)"; return false; }
            var s = Core.GameState.S;
            int wood = 0, stone = 0, steel = 0, concrete = 0;
            if (tier == 1) { wood = 120 + (int)span * 3; stone = 0; }
            else if (tier == 2) { wood = 40; stone = 100 + (int)span * 4; }
            else if (tier == 3) { steel = 80 + (int)span * 5; concrete = 40 + (int)span * 2; }
            else { steel = 120 + (int)span * 6; concrete = 60 + (int)span * 3; }
            if (s.Res["wood"] < wood || s.Res["stone"] < stone || s.Res["steel"] < steel || s.Res["concrete"] < concrete)
            {
                msg = "建桥资源不足(木" + wood + "石" + stone + "钢" + steel + "砼" + concrete + ")";
                return false;
            }
            s.Res["wood"] -= wood; s.Res["stone"] -= stone; s.Res["steel"] -= steel; s.Res["concrete"] -= concrete;
            var seg = new BridgeSegment();
            seg.Id = _nextId++; seg.Tier = tier; seg.EraTech = tier;
            seg.Span = Mathf.RoundToInt(span * 4f);
            seg.Gx1 = gx; seg.Gz1 = gz; seg.Gx2 = bestGx; seg.Gz2 = bestGz;
            seg.SeaCross = sea;
            seg.MaxAge = sea ? UnityEngine.Random.Range(GrandLifeMin, GrandLifeMax + 1) : UnityEngine.Random.Range(NormalLifeMin, NormalLifeMax + 1);
            seg.View = BuildBridgeView(gx, gz, bestGx, bestGz, tier, sea);
            Segments.Add(seg);
            msg = (sea ? "跨海大桥" : "桥梁") + "建成(跨距" + seg.Span + ")";
            return true;
        }

        // 势力自动架桥（同阵营相邻陆地最短点；V9.4.4）
        public void AutoBridgeForFactions()
        {
            // 简版：遍历两据点陆地，同阵营最近对建（额度/技术内）
            var s = Core.GameState.S;
            if (s == null || s.Factions == null) return;
            for (int a = 0; a < s.Factions.Count; a++)
            {
                var fa = s.Factions[a];
                if (fa == null || fa.Destroyed) continue;
                for (int b = a + 1; b < s.Factions.Count; b++)
                {
                    var fb = s.Factions[b];
                    if (fb == null || fb.Destroyed || fb.Id != fa.Id) continue;
                    // 同阵营（Id 相同）才建
                    int ax = Mathf.RoundToInt(fa.X / 4f), az = Mathf.RoundToInt(fa.Z / 4f);
                    int bx = Mathf.RoundToInt(fb.X / 4f), bz = Mathf.RoundToInt(fb.Z / 4f);
                    float dist = Mathf.Sqrt((ax-bx)*(ax-bx) + (az-bz)*(az-bz));
                    if (dist <= HardMaxSpan && dist >= 6)
                    {
                        bool sea = dist >= GrandSpan;
                        if (sea && EraTech() < 3) continue;
                        if (!CanBuildNow(out _)) continue;
                        if (PlayerBuildBridge(new Vector3(fa.X + (fb.X-fa.X)*0.5f, 0, fa.Z + (fb.Z-fa.Z)*0.5f), out _)) { }
                    }
                }
            }
        }

        // 桥/高架老化（每年调用；V9.5.4 R5：DestroyImmediate 消闪烁）
        public void OnYear()
        {
            var s = Core.GameState.S;
            if (s == null) return;
            // 桥老化
            for (int i = Segments.Count - 1; i >= 0; i--)
            {
                var seg = Segments[i];
                seg.Age++;
                if (seg.Age >= seg.MaxAge)
                {
                    if (seg.View != null) UnityEngine.Object.DestroyImmediate(seg.View);
                    Segments.RemoveAt(i);
                    s.Log("桥梁老化拆除(寿命" + seg.MaxAge + "年)");
                }
            }
            // 高架柱老化
            for (int i = Piers.Count - 1; i >= 0; i--)
            {
                var p = Piers[i];
                p.Age++;
                if (p.Age >= p.MaxAge)
                {
                    if (p.View != null) UnityEngine.Object.DestroyImmediate(p.View);
                    Piers.RemoveAt(i);
                }
            }
            RebuildViews();
        }

        // 重建全部桥视图（R4 修复：高架跨陆不跳过；R5：同步销毁）
        public void RebuildViews()
        {
            var s = Core.GameState.S;
            if (s == null || s.TerrainMap == null) return;
            for (int i = 0; i < Segments.Count; i++)
            {
                var seg = Segments[i];
                if (seg.View == null) continue;
                int gx = seg.Gx1, gz = seg.Gz1;
                // R4: tier4 高架跨陆段保留重建（不因陆地跳过断链）；普通桥仅水面格重建
                if (seg.Tier != 4 && LandOfCell(gx, gz) > 0) continue;
                seg.View.transform.position = new Vector3(gx * 4f + 2f, 1f, gz * 4f + 2f);
            }
        }

        GameObject BuildBridgeView(int gx1, int gz1, int gx2, int gz2, int tier, bool sea)
        {
            var root = new GameObject("bridge_" + tier + (sea ? "_sea" : ""));
            var mat = new Material(Shader.Find("Standard"));
            if (mat == null) mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tier == 1 ? new Color(0.55f, 0.4f, 0.25f) : tier == 2 ? new Color(0.7f, 0.7f, 0.72f) : tier == 3 ? new Color(0.45f, 0.5f, 0.55f) : new Color(0.4f, 0.45f, 0.5f);
            // 沿两格连线建板条
            float x1 = gx1 * 4f + 2f, z1 = gz1 * 4f + 2f;
            float x2 = gx2 * 4f + 2f, z2 = gz2 * 4f + 2f;
            float dist = Mathf.Sqrt((x2-x1)*(x2-x1) + (z2-z1)*(z2-z1));
            int n = Mathf.Max(1, Mathf.RoundToInt(dist / 4f));
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.transform.parent = root.transform;
                plank.transform.position = new Vector3(Mathf.Lerp(x1, x2, t), 1.2f, Mathf.Lerp(z1, z2, t));
                float ang = Mathf.Atan2(z2 - z1, x2 - x1) * Mathf.Rad2Deg;
                plank.transform.rotation = Quaternion.Euler(0, -ang, 0);
                plank.transform.localScale = new Vector3(4f, 0.25f, 0.6f);
                plank.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return root;
        }

        GameObject BuildViaductView(int gx1, int gz1, int gx2, int gz2)
        {
            var root = new GameObject("viaduct");
            var mat = new Material(Shader.Find("Standard"));
            if (mat == null) mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.55f, 0.55f, 0.58f);
            float x1 = gx1 * 4f + 2f, z1 = gz1 * 4f + 2f;
            float x2 = gx2 * 4f + 2f, z2 = gz2 * 4f + 2f;
            float dist = Mathf.Sqrt((x2-x1)*(x2-x1) + (z2-z1)*(z2-z1));
            int n = Mathf.Max(1, Mathf.RoundToInt(dist / 4f));
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.transform.parent = root.transform;
                plank.transform.position = new Vector3(Mathf.Lerp(x1, x2, t), ViaductDeckY, Mathf.Lerp(z1, z2, t));
                float ang = Mathf.Atan2(z2 - z1, x2 - x1) * Mathf.Rad2Deg;
                plank.transform.rotation = Quaternion.Euler(0, -ang, 0);
                plank.transform.localScale = new Vector3(4f, 0.3f, 1.2f);
                plank.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return root;
        }

        GameObject BuildPierView(int gx, int gz)
        {
            var root = new GameObject("viaduct_pier");
            var mat = new Material(Shader.Find("Standard"));
            if (mat == null) mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.5f, 0.5f, 0.55f);
            var col = GameObject.CreatePrimitive(PrimitiveType.Cube);
            col.transform.parent = root.transform;
            float hy = Mathf.Max(HeightAt(gx, gz), 0.5f);
            col.transform.position = new Vector3(gx * 4f + 2f, hy + (ViaductDeckY - hy) * 0.5f, gz * 4f + 2f);
            col.transform.localScale = new Vector3(0.8f, Mathf.Max(1f, ViaductDeckY - hy), 0.8f);
            col.GetComponent<Renderer>().sharedMaterial = mat;
            return root;
        }

        // Web 探针（诊断输出）
        public string Probe()
        {
            var s = Core.GameState.S;
            return string.Format("era={0} tier{1}({2}) 技术跨距≤{3} 已建桥={4} 年{5}(普通额{6}/跨海额{7}) 有据点陆地={8}",
                s.Era, EraTech(), TierName(EraTech()), MaxSpanByEra(), Segments.Count, s.Year, NormalQuotaPer10Y, GrandQuotaPer50Y, CountClaimedLand());
        }

        string TierName(int t)
        {
            if (t == 1) return "木桥";
            if (t == 2) return "石桥";
            if (t == 3) return "混凝土大桥";
            return "高架";
        }

        int CountClaimedLand()
        {
            var s = Core.GameState.S;
            int c = 0;
            if (s != null && s.Factions != null)
                c = s.Factions.Count(f => f != null && !f.Destroyed);
            return c;
        }

        // Web 强制建桥（回归用）
        public bool ForceNearest(out string msg)
        {
            var s = Core.GameState.S;
            if (s == null || s.Factions == null) { msg = "无据点陆地"; return false; }
            var lands = s.Factions.Where(f => f != null && !f.Destroyed).ToList();
            if (lands.Count < 2) { msg = "找不到两块有据点陆地"; return false; }
            var a = lands[0]; var b = lands[1];
            return PlayerBuildBridge(new Vector3((a.X + b.X) * 0.5f, 0, (a.Z + b.Z) * 0.5f), out msg);
        }
    }
}
