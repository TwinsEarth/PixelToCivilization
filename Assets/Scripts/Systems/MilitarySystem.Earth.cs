// V9.1.1 地球模式军事势力：三支阵营色远征军在玩家周边环带登陆（替代随机东夷/西戎五方）。
// 不做全球行军（全球尺度路径与性能不成立）；远征军沿用既有塔防/讨伐/相克/据点守军规则。
using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    public partial class MilitarySystem
    {
        // 西方列强远征军（F2 深蓝）/ 日本远征军（F3 琥珀）/ 南方联军（F4 翠绿）
        static readonly (string id, string name, long color)[] EarthExpeditionDefs =
        {
            ("west_exp","西方列强远征军",0x2f6fb0),
            ("jp_exp","日本远征军",0xe8a020),
            ("south_exp","南方联军",0x3a9d4d),
        };

        /// <summary>新开局清理上一局割据/远征军据点（数据由 State.Reset 清，系统持有的据点视图与列表在此清）。</summary>
        public void ResetForNewGame()
        {
            foreach (var f in Factions) if (f.BaseView) Object.Destroy(f.BaseView);
            Factions.Clear();
            FactionsInited = false;
        }

        void InitEarthFactions()
        {
            int home = _terrain != null ? _terrain.HomeContinent : 1;
            float hx = S.VillageX.Count > 0 ? S.VillageX[0] : 0f;
            float hz = S.VillageZ.Count > 0 ? S.VillageZ[0] : 0f;
            var used = new List<Vector2>();
            int placed = 0;
            foreach (var d in EarthExpeditionDefs)
                if (TryPlaceEarthFaction(d.id, d.name, d.color, home, hx, hz, used)) placed++;
            FactionsInited = true;
            GM.AddEvent("bad", $"⚔️ 列强远征军在沿海登陆（{placed} 支），整军备战！");
            Debug.Log($"[MilitaryEarth] 远征军据点放置 {placed}/3");
        }

        bool TryPlaceEarthFaction(string id, string name, long color, int cid, float hx, float hz, List<Vector2> used)
        {
            for (int att = 0; att < 140; att++)
            {
                float ang = Random.value * Mathf.PI * 2f;
                float dist = 60f + Random.value * 100f;   // 环带 60~160
                float x = hx + Mathf.Cos(ang) * dist;
                float z = hz + Mathf.Sin(ang) * dist;
                if (_terrain == null) continue;
                if (_terrain.ContinentAt(x, z) != cid) continue;   // 只在玩家所在陆块（欧亚）
                if (_terrain.IsWater(x, z) || _terrain.IsBeach(x, z)) continue;
                if (_terrain.HeightAt(x, z) > 3.4f) continue;      // 不上高原山地
                bool close = false;
                foreach (var p in used)
                    if (Vector2.Distance(p, new Vector2(x, z)) < 46f) { close = true; break; }
                if (close) continue;
                var f = new Faction
                {
                    Id = id, Name = name, ColorHex = color,
                    X = x, Z = z, Population = 20 + Random.value * 30, HomeContinent = cid,
                };
                Factions.Add(f);
                used.Add(new Vector2(x, z));
                f.BaseView = EntityViewFactory.Spawn("Base_" + name, _root, PrimitiveType.Cylinder,
                    EntityViewFactory.Hex(color), 2.2f);
                EntityViewFactory.Place(f.BaseView, _terrain, x, z, 2f);
                OverheadBillboard.Attach(f.BaseView, EntityViewFactory.Hex(color), false, 2.0f, 3.4f)?.SetBarVisible(false);
                for (int k = 0; k < 5; k++) f.Army.Add(MakeUnit(x, z, color));
                return true;
            }
            return false;
        }
    }
}
