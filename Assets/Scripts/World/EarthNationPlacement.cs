// V9.1.1 地球模式城市选址：固定经纬度 → 世界坐标，螺旋外退回退到期望陆块上最近的合格陆地。
// 全程确定性（无随机），保证开局落位与读档地标重建位置完全一致。
using UnityEngine;

namespace PixelToCivilization.World
{
    public static class EarthNationPlacement
    {
        const float MaxRadius = 160f;   // 世界单位最大回退半径
        const float Step = 4f;          // 螺旋步长

        /// <summary>polarLand=true 表示该陆块为极地无人区（南极6/格陵兰7），永不立国。</summary>
        static bool IsPolar(int cid) => cid == 6 || cid == 7;

        static bool InRegion(float x, float z, float minLon, float maxLon, float minLat, float maxLat)
        {
            if (maxLon == 0f && maxLat == 0f && minLon == 0f && minLat == 0f) return true; // 不约束
            float lon = EarthMapData.XToLon(x), lat = EarthMapData.ZToLat(z);
            return lon >= minLon && lon <= maxLon && lat >= minLat && lat <= maxLat;
        }

        static bool Accept(WorldGenerator t, float x, float z, int wantLand, int pass,
                           float minLon = 0f, float maxLon = 0f, float minLat = 0f, float maxLat = 0f)
        {
            int cid = t.ContinentAt(x, z);
            if (cid <= 0 || IsPolar(cid)) return false;
            if (t.IsWater(x, z)) return false;
            float h = t.HeightAt(x, z);
            if (h > 3.4f) return false;             // 不上高山/高原
            // V9.1.2 同陆块多国家：pass1/2 必须落在本国地理分区包围盒内，pass3 放宽分区但仍限本陆块（不跨洲）。
            if (pass <= 2 && !InRegion(x, z, minLon, maxLon, minLat, maxLat)) return false;
            if (pass == 3) return cid == wantLand;  // 兜底：本陆块任意非极地平地（不再跨洲）
            if (cid != wantLand) return false;
            if (pass == 1 && t.BiomeAt(x, z) == BiomeKind.Desert) return false;
            return true;
        }

        /// <summary>解析城市落点（无分区约束，兼容旧调用）。</summary>
        public static bool Resolve(WorldGenerator t, float lon, float lat, int wantLand,
                                   out float x, out float z, out int landUsed)
            => Resolve(t, lon, lat, wantLand, out x, out z, out landUsed, 0f, 0f, 0f, 0f);

        /// <summary>
        /// 解析城市落点。pass1=期望陆块+本国分区、非沙漠平地；pass2=同区放宽沙漠；pass3=本陆块放宽分区（不跨洲）。
        /// 分区包围盒为经纬度 [minLon,maxLon]×[minLat,maxLat]，全 0 表示不约束。
        /// </summary>
        public static bool Resolve(WorldGenerator t, float lon, float lat, int wantLand,
                                   out float x, out float z, out int landUsed,
                                   float minLon, float maxLon, float minLat, float maxLat)
        {
            x = 0f; z = 0f; landUsed = 0;
            if (t == null) return false;
            float tx = EarthMapData.LonToX(lon);
            float tz = EarthMapData.LatToZ(lat);
            for (int pass = 1; pass <= 3; pass++)
            {
                // r=0 先试目标点本身
                if (Accept(t, tx, tz, wantLand, pass, minLon, maxLon, minLat, maxLat))
                { x = tx; z = tz; landUsed = t.ContinentAt(tx, tz); return true; }
                for (float r = Step; r <= MaxRadius; r += Step)
                {
                    int n = Mathf.Max(8, Mathf.RoundToInt(2f * Mathf.PI * r / 5f));
                    float phase = (lon * 0.7f + lat * 1.3f) * Mathf.Deg2Rad;
                    for (int i = 0; i < n; i++)
                    {
                        float a = phase + (float)i / n * Mathf.PI * 2f;
                        float px = tx + Mathf.Cos(a) * r;
                        float pz = tz + Mathf.Sin(a) * r;
                        if (Accept(t, px, pz, wantLand, pass, minLon, maxLon, minLat, maxLat))
                        { x = px; z = pz; landUsed = t.ContinentAt(px, pz); return true; }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 找某陆块上、离城市 (cx,cz) 最近的海岸登陆点（陆地且 8 邻域内有外海）。
        /// 用于跨海铁路/沿海公路的端点。找不到返回 false。
        /// </summary>
        public static bool NearestCoast(WorldGenerator t, int landId, float cx, float cz,
                                        float maxR, out float ox, out float oz)
        {
            ox = cx; oz = cz;
            if (t == null) return false;
            bool found = false; float best = float.MaxValue;
            for (float r = 0f; r <= maxR; r += Step)
            {
                int n = Mathf.Max(10, Mathf.RoundToInt(2f * Mathf.PI * r / 4f));
                for (int i = 0; i < n; i++)
                {
                    float a = (float)i / n * Mathf.PI * 2f;
                    float x = cx + Mathf.Cos(a) * r, z = cz + Mathf.Sin(a) * r;
                    if (t.ContinentAt(x, z) != landId || t.IsWater(x, z)) continue;
                    if (!HasSeaNeighbor(t, x, z)) continue;
                    float d = (x - cx) * (x - cx) + (z - cz) * (z - cz);
                    if (d < best) { best = d; ox = x; oz = z; found = true; }
                }
                // 找到一圈最近海岸即可（r 从小到大，首圈命中即返回，控制开销）
                if (found) return true;
            }
            return found;
        }

        /// <summary>该陆地格 8 邻域是否有外海（深/浅水，非淡水湖）。</summary>
        public static bool HasSeaNeighbor(WorldGenerator t, float x, float z)
        {
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    float nx = x + dx * 4f, nz = z + dz * 2.4f;
                    if (!t.IsWater(nx, nz)) continue;
                    if (t.BiomeAt(nx, nz) == BiomeKind.FreshWater) continue; // 不算湖泊
                    return true;
                }
            return false;
        }
    }
}
