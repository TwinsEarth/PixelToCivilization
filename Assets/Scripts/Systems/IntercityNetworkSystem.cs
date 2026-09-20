// V9.1.2 城际交通网络（地球模式规则重写；经典模式保留原行政区 MST）。
//  地球模式（一洲/分区至多三国）：
//   · 铁路【只连接不同国家】（EarthNations.RailLinks 声明式）：陆地邻国在最近可行城市对间陆地铺轨（跨距≤380）；
//     岛国/异陆块在窄海峡（跨距≤100、两端陆地、中段连续外海）铺跨海桥面轨道；窄海峡在粗粒度地图上呈地峡时走陆地轨道；
//     陆地铁路允许短桥跨越窄海湾/河口（单段≤12、累计≤24）；朝鲜半岛按岛，对华走黄海跨海不走陆地。
//     一条线路仅 1 班列车往返；同一国家内部 0 铁路；月球为太空岛，任何铁路不可达。载客=下限城市等级(20/30/50)×机车系数，上限100。
//   · 内陆只修【4 车道沥青马路】：同一【国家】内城城相连(MST)、每城连最近海岸港口、近岸城市铺环岛/沿海公路；公路不跨国。
//   · 运河仅主大陆（由 CanalSystem 控制）；车辆只在道路上（ModernTrafficSystem 约束）。
// 纯派生视图：挂 "Intercity" 根，随清局销毁，按地形+城市+年代确定性重建，不进存档。
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.World;
using PixelToCivilization.Actors;

namespace PixelToCivilization.Systems
{
    public class IntercityNetworkSystem : GameSystemBase
    {
        Transform _root;
        WorldGenerator _terrain;
        Material _asphalt, _laneWhite, _laneYellow, _railMetal, _sleeper, _ballast, _deck, _pier;
        Material _trainRed, _trainBlue, _trainGreen, _trainAmber, _carWhite, _carRed, _carBlue, _carYellow;
        float _tick, _rebuild;
        int _tickN, _early, _moverN, _rebuildN, _roads, _rails, _cars, _trains;
        string _sig = "";
        // V9.1.1 已铺装公路路段（中线 a→b），供车辆“只在路上行驶 / 自动吸附最近道路”查询
        readonly List<(Vector3 a, Vector3 b)> _roadSegs = new();

        const float MAX_SEA_SPAN = 100f;   // 跨海铁路最大跨距（世界单位，≈7.5°）
        const float MAX_LAND_RAIL = 380f;  // 陆地国际铁路最大城市对跨距（世界单位，≈28°）
        const float COAST_SEARCH = 90f;    // 城市找海岸/港口的最大半径

        /// <summary>当前是否存在可行驶铺装公路。</summary>
        public bool HasRoads => _roadSegs.Count > 0;
        /// <summary>离 from 最近的公路中线点（距离≤maxR）。供现代交通系统把车辆吸附到道路上。</summary>
        public bool NearestRoadPoint(Vector3 from, float maxR, out Vector3 pt)
        {
            pt = default; bool found = false; float bd = maxR * maxR;
            foreach (var seg in _roadSegs)
            {
                var q = ClosestPointOnSegment(from, seg.a, seg.b);
                float d = (q - from).sqrMagnitude;
                if (d < bd) { bd = d; pt = q; found = true; }
            }
            return found;
        }
        static Vector3 ClosestPointOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            float t = Vector3.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude);
            return a + ab * Mathf.Clamp01(t);
        }
        void RecordRoad(Vector3 a, Vector3 b) { _roadSegs.Add((a, b)); }
        /// <summary>随机取一段公路中线点（供现代车辆选目的地，保证目标落在道路上）。</summary>
        public bool RandomRoadPoint(out Vector3 pt)
        {
            pt = default;
            if (_roadSegs.Count == 0) return false;
            var s = _roadSegs[Random.Range(0, _roadSegs.Count)];
            pt = Vector3.Lerp(s.a, s.b, Random.value); pt.y = 0f;
            return true;
        }

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _terrain = Object.FindObjectOfType<WorldGenerator>();
            _asphalt   = ShaderHelper.Pbr(new Color(0.13f,0.13f,0.14f), 0f, 0.55f, 1401, 0.4f);
            _laneWhite = ShaderHelper.Pbr(new Color(0.92f,0.92f,0.90f), 0f, 0.2f, 1402, 0.3f);
            _laneYellow= ShaderHelper.Pbr(new Color(0.95f,0.80f,0.15f), 0f, 0.2f, 1403, 0.3f);
            _railMetal = ShaderHelper.Pbr(new Color(0.55f,0.57f,0.60f), 0.85f, 0.5f, 1404, 0.25f);
            _sleeper   = ShaderHelper.Pbr(new Color(0.36f,0.27f,0.18f), 0f, 0.3f, 1405, 0.6f);
            _ballast   = ShaderHelper.Pbr(new Color(0.30f,0.28f,0.26f), 0f, 0.2f, 1406, 0.7f);
            _deck      = ShaderHelper.Pbr(new Color(0.55f,0.56f,0.58f), 0.3f, 0.35f, 1407, 0.4f);
            _pier      = ShaderHelper.Pbr(new Color(0.48f,0.49f,0.51f), 0.2f, 0.3f, 1408, 0.5f);
            _trainRed   = ShaderHelper.Pbr(new Color(0.72f,0.13f,0.12f), 0.2f, 0.4f, 1409, 0.4f);
            _trainBlue  = ShaderHelper.Pbr(new Color(0.12f,0.36f,0.66f), 0.2f, 0.4f, 1410, 0.4f);
            _trainGreen = ShaderHelper.Pbr(new Color(0.16f,0.45f,0.24f), 0.2f, 0.4f, 1411, 0.4f);
            _trainAmber = ShaderHelper.Pbr(new Color(0.85f,0.55f,0.10f), 0.2f, 0.4f, 1412, 0.4f);
            _carWhite  = ShaderHelper.Pbr(new Color(0.85f,0.86f,0.88f), 0.2f, 0.4f, 1413, 0.4f);
            _carRed    = ShaderHelper.Pbr(new Color(0.70f,0.12f,0.12f), 0.2f, 0.4f, 1414, 0.4f);
            _carBlue   = ShaderHelper.Pbr(new Color(0.14f,0.32f,0.62f), 0.2f, 0.4f, 1415, 0.4f);
            _carYellow = ShaderHelper.Pbr(new Color(0.85f,0.70f,0.15f), 0.2f, 0.4f, 1416, 0.4f);
        }

        Transform Root { get { if (_root == null) _root = EntityViewFactory.EnsureRoot("Intercity", GM.transform); return _root; } }

        public override void Tick(float dt)
        {
            _tickN++;
            if (S == null || _terrain == null) { _early++; return; }
            _tick += dt;
            if (_tick >= 6f) { _tick = 0f; Rebuild(); }
            TickMovers(dt);
        }

        /// <summary>调试/时代跃迁后立即重建。</summary>
        public void RefreshNow() { _sig = ""; Rebuild(); }
        public override void OnEra(int n, int o) { base.OnEra(n, o); RefreshNow(); }
        public string Diagnose()
        {
            var sb = new StringBuilder();
            sb.Append("[Intercity] tick=").Append(_tickN).Append(" early=").Append(_early)
              .Append(" rebuild=").Append(_rebuildN).Append(" roads=").Append(_roads)
              .Append(" rails=").Append(_rails).Append(" cars=").Append(_cars)
              .Append(" trains=").Append(_trains).Append(" movers=").Append(_moverN)
              .Append(" earth=").Append(_terrain != null && _terrain.EarthMode);
            return sb.ToString();
        }

        // ============ 总入口：地球 / 经典 分流 ============
        void Rebuild()
        {
            _rebuildN++;
            if (_terrain != null && _terrain.EarthMode) { RebuildEarth(); return; }
            RebuildClassic();
        }

        // ---------------- 经典模式：行政区 MST（原样保留） ----------------
        void RebuildClassic()
        {
            var d = GM.Districts;
            if (d == null) return;
            d.RefreshNow();
            var nodes = d.Districts;
            int tier = TrainSystem.TierForYear(S.Year);
            bool roadDue = S.Era >= 5;
            string sig = "C|" + nodes.Count + "|" + tier + "|" + (roadDue ? 1 : 0);
            if (sig == _sig && Root.childCount > 0) return;
            _sig = sig;
            for (int i = Root.childCount - 1; i >= 0; i--) Object.Destroy(Root.GetChild(i).gameObject);
            _roads = _rails = _cars = _trains = 0; _roadSegs.Clear();
            if (nodes.Count < 2) return;
            var pts = new List<Vector3>();
            foreach (var dist in nodes) pts.Add(new Vector3(dist.Cx, 0f, dist.Cz));
            bool railDue = tier > 0;
            var railEdges = railDue ? BuildNetwork(pts, 8, tier >= 4 ? 9f : 2.6f, true) : new List<(int a, int b)>();
            var roadEdges = roadDue ? BuildNetwork(pts, 10, 8f, false) : new List<(int a, int b)>();
            foreach (var e in railEdges)
            {
                Vector3 a = pts[e.a] + new Vector3(0, 0, 2.1f);
                Vector3 b = pts[e.b] + new Vector3(0, 0, 2.1f);
                if (Feasible(a, b, tier >= 4 ? 9f : 2.6f)) { BuildRailView(a, b, tier); _rails++; }
            }
            foreach (var e in roadEdges)
            {
                Vector3 a = pts[e.a] - new Vector3(0, 0, 2.1f);
                Vector3 b = pts[e.b] - new Vector3(0, 0, 2.1f);
                if (Feasible(a, b, 8f)) { BuildRoadView(a, b); _roads++; AddCars(a, b); }
            }
        }

        // ---------------- 地球模式：一陆块一国 + 跨海铁路 + 4车道马路 ----------------
        void RebuildEarth()
        {
            var cities = GM.Nation != null ? GM.Nation.EarthCities : null;
            int tier = TrainSystem.TierForYear(S.Year);
            bool railDue = tier > 0;
            int ad = S.Year - 3000;
            bool roadDue = ad >= 1900;          // 近代起才有铺装城际马路
            string sig = "E|" + (cities == null ? 0 : cities.Count) + "|" + tier + "|" + (roadDue ? 1 : 0);
            if (sig == _sig && Root.childCount > 0) return;
            _sig = sig;
            for (int i = Root.childCount - 1; i >= 0; i--) Object.Destroy(Root.GetChild(i).gameObject);
            _roads = _rails = _cars = _trains = 0; _roadSegs.Clear();
            if (cities == null || cities.Count == 0) return;

            // —— 公路：同一【国家】内 城城 MST + 每城连最近港口海岸；近岸铺环岛（公路不跨国）——
            if (roadDue) BuildEarthRoads(cities);

            // —— 铁路：只连接【不同国家】（声明式）：邻国陆地铺轨 / 岛国窄海峡跨海，一线一列车；国内 0 铁路 ——
            if (railDue) BuildEarthRail(cities);

            Debug.Log(Diagnose());
        }

        void BuildEarthRoads(List<EarthCityRT> cities)
        {
            // V9.1.2 按【国家】分组（同一陆块可有多个国家，公路不跨国）
            var byCountry = new Dictionary<string, List<EarthCityRT>>();
            foreach (var c in cities)
            {
                if (!byCountry.TryGetValue(c.CountryId, out var l)) { l = new List<EarthCityRT>(); byCountry[c.CountryId] = l; }
                l.Add(c);
            }
            foreach (var kv in byCountry)
            {
                var group = kv.Value;
                var pts = new List<Vector3>();
                foreach (var c in group) pts.Add(new Vector3(c.X, 0f, c.Z));

                // 城城 MST（陆地可行，宽松高差带）
                if (group.Count >= 2)
                {
                    var edges = BuildNetwork(pts, 6, 999f, false);
                    foreach (var e in edges)
                        if (Feasible(pts[e.a], pts[e.b], 999f)) { BuildRoad4View(pts[e.a], pts[e.b]); _roads++; AddCars(pts[e.a], pts[e.b]); }
                }

                // 每城连最近海岸港口；近岸城市顺带铺一段沿海/环岛弧
                foreach (var c in group)
                {
                    if (EarthNationPlacement.NearestCoast(_terrain, c.LandId, c.X, c.Z, COAST_SEARCH, out float px, out float pz))
                    {
                        var port = new Vector3(px, 0f, pz);
                        if (Feasible(new Vector3(c.X, 0, c.Z), port, 999f))
                        { var cityPtRoad = new Vector3(c.X, 0f, c.Z); BuildRoad4View(cityPtRoad, port); _roads++; AddCars(cityPtRoad, port); }
                        BuildCoastalLoop(c.LandId, px, pz, c.IsPlayer ? 46f : 30f);
                    }
                }
            }
        }

        /// <summary>围绕一个海岸港口，在近岸陆地上取点串成闭合沿海/环岛公路（短弧，避免跨洲长线）。</summary>
        void BuildCoastalLoop(int landId, float cx, float cz, float radius)
        {
            var ring = new List<Vector3>();
            const int SEG = 14;
            for (int i = 0; i < SEG; i++)
            {
                float a = (float)i / SEG * Mathf.PI * 2f;
                float x = cx + Mathf.Cos(a) * radius, z = cz + Mathf.Sin(a) * radius * 0.7f;
                // 取该方向上最靠海的陆地格（向海推到岸边）
                Vector3? coast = null;
                for (float rr = radius; rr >= 8f; rr -= 4f)
                {
                    float qx = cx + Mathf.Cos(a) * rr, qz = cz + Mathf.Sin(a) * rr * 0.7f;
                    if (_terrain.ContinentAt(qx, qz) == landId && !_terrain.IsWater(qx, qz)
                        && EarthNationPlacement.HasSeaNeighbor(_terrain, qx, qz)) { coast = new Vector3(qx, 0, qz); break; }
                }
                if (coast.HasValue) ring.Add(coast.Value);
            }
            for (int i = 0; i < ring.Count; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                if (Vector3.Distance(a, b) > 34f) continue;            // 缺口过大不硬连
                if (Feasible(a, b, 999f)) { BuildRoad4View(a, b); _roads++; if (i % 2 == 0) AddCars(a, b); }
            }
        }

        /// <summary>V9.1.2 国际铁路：按 EarthNations.RailLinks 在两国之间各铺一条线（陆地/跨海），一线一列车。</summary>
        void BuildEarthRail(List<EarthCityRT> cities)
        {
            int tier = TrainSystem.TierForYear(S.Year);
            var byCountry = new Dictionary<string, List<EarthCityRT>>();
            foreach (var c in cities)
            {
                if (!byCountry.TryGetValue(c.CountryId, out var l)) { l = new List<EarthCityRT>(); byCountry[c.CountryId] = l; }
                l.Add(c);
            }

            foreach (var link in EarthNations.RailLinks)
            {
                if (!byCountry.TryGetValue(link.A, out var ga) || !byCountry.TryGetValue(link.B, out var gb)) continue;
                int cap = RailCapacity(Mathf.Min(MinCityLevel(ga), MinCityLevel(gb)), tier);
                Color col = FactionColorOf(ga[0].Faction);
                bool ok = link.Sea ? TrySeaRail(link, ga, gb, tier, cap, col)
                                   : TryLandRail(link, ga, gb, tier, cap, col);
                if (!ok)
                    Debug.Log($"[Intercity] 国际铁路 {link.A}↔{link.B}({(link.Sea ? "跨海" : "陆地")}) 暂无可铺通道");
            }
        }

        /// <summary>陆地国际铁路：在两国城市对中选跨距≤MAX_LAND_RAIL、全程陆地可行的最近一对铺轨。</summary>
        bool TryLandRail(EarthRailLink link, List<EarthCityRT> ga, List<EarthCityRT> gb, int tier, int cap, Color col)
        {
            EarthCityRT ba = null, bb = null; float best = MAX_LAND_RAIL;
            foreach (var a in ga)
                foreach (var b in gb)
                {
                    var pa = new Vector3(a.X, 0f, a.Z); var pb = new Vector3(b.X, 0f, b.Z);
                    float d = Vector3.Distance(pa, pb);
                    if (d >= best) continue;
                    if (!LandRailOK(pa, pb)) continue;   // 全程在疆域内；可跨江河（架铁路桥），绝不过外海
                    best = d; ba = a; bb = b;
                }
            if (ba == null) return false;
            var va = new Vector3(ba.X, 0f, ba.Z); var vb = new Vector3(bb.X, 0f, bb.Z);
            BuildRailView(va, vb, tier, col, $"LandRail_{link.A}_{link.B}_t{tier}_cap{cap}");
            _rails++;
            Debug.Log($"[Intercity] 陆地铁路 {link.A}↔{link.B}（{ba.Name}↔{bb.Name}）跨距{best:0.0} 机车t{tier} 载客{cap}");
            return true;
        }

        /// <summary>跨海国际铁路：两国各收集海岸点，找最短窄海峡铺桥面轨道；同陆块（朝鲜半岛按岛）走同陆块海峡判定。</summary>
        bool TrySeaRail(EarthRailLink link, List<EarthCityRT> ga, List<EarthCityRT> gb, int tier, int cap, Color col)
        {
            int landA = ga[0].LandId, landB = gb[0].LandId;
            var ca = new List<Vector2>(); var cb = new List<Vector2>();
            foreach (var c in ga) CollectCoast(landA, c.X, c.Z, COAST_SEARCH, 22, ca);
            foreach (var c in gb) CollectCoast(landB, c.X, c.Z, COAST_SEARCH, 22, cb);

            Vector2 seaA, seaB;
            if (landA != landB)
            {
                // 异陆块：以对方岸点为锚补齐本方正对海峡岸点（解决城市远离海峡，如多佛尔）
                var ca2 = new List<Vector2>(ca); var cb2 = new List<Vector2>(cb);
                AugmentAcross(landA, cb2, ca2); AugmentAcross(landB, ca2, cb2);
                if (!FindSeaCrossing(landA, landB, ca2, cb2, out seaA, out seaB, out Vector2 lA, out Vector2 lB, out bool isthmus)) return false;
                if (isthmus)
                {
                    // 粗粒度地图上两岸以地峡相接（如引擎版多佛尔陆桥）：走陆地轨道
                    var vla = new Vector3(lA.x, 0f, lA.y);
                    var vlb = new Vector3(lB.x, 0f, lB.y);
                    BuildRailView(vla, vlb, tier, col, $"LandRail_{link.A}_{link.B}_t{tier}_cap{cap}");
                    _rails++;
                    Debug.Log($"[Intercity] 跨海铁路 {link.A}↔{link.B} 经地峡陆桥 跨距{Vector3.Distance(vla, vlb):0.0} 机车t{tier} 载客{cap}");
                    return true;
                }
            }
            else
            {
                // 同陆块跨海（朝鲜半岛按岛、对华黄海通道）：两岸点同属一个陆块，仅要求中段连续外海
                var ca2 = new List<Vector2>(ca); var cb2 = new List<Vector2>(cb);
                AugmentAcross(landA, cb2, ca2); AugmentAcross(landB, ca2, cb2);
                if (!FindSameLandSeaCrossing(ca2, cb2, out seaA, out seaB)) return false;
            }

            Vector3 pa = new Vector3(seaA.x, 0f, seaA.y), pb = new Vector3(seaB.x, 0f, seaB.y);
            BuildSeaRailView(pa, pb, tier);
            _rails++;
            var tr = CreateTrain(Root, (pa + pb) * 0.5f, tier,
                                 Mathf.Abs(pb.x - pa.x) >= Mathf.Abs(pb.z - pa.z), EarthTrainSpeed(tier), col);
            tr.name = $"SeaRail_{link.A}_{link.B}_t{tier}_cap{cap}";
            var mv = tr.GetComponent<CorridorMover>();
            if (mv != null) { mv.A = pa + Vector3.up * 0.5f; mv.B = pb + Vector3.up * 0.5f; mv.L = Vector3.Distance(pa, pb); mv.Speed = EarthTrainSpeed(tier); }
            _trains++;
            Debug.Log($"[Intercity] 跨海铁路 {link.A}↔{link.B} 跨距{Vector3.Distance(pa,pb):0.0} 机车t{tier} 载客{cap}");
            return true;
        }

        /// <summary>同一引擎陆块内的跨海通道（朝鲜半岛按岛）：两岸点同陆块，向对方推到水线，中段须连续外海。</summary>
        bool FindSameLandSeaCrossing(List<Vector2> ca, List<Vector2> cb, out Vector2 seaA, out Vector2 seaB)
        {
            seaA = seaB = default; float best = float.MaxValue;
            foreach (var a in ca)
                foreach (var b in cb)
                {
                    float d = Vector2.Distance(a, b);
                    if (d < 6f || d > MAX_SEA_SPAN || d >= best) continue;
                    if (!PushToWaterSameLand(a, b, out Vector2 sA)) continue;
                    if (!PushToWaterSameLand(b, a, out Vector2 sB)) continue;
                    float span = Vector2.Distance(sA, sB);
                    if (span < 2f || span > MAX_SEA_SPAN) continue;
                    if (!OpenSeaStraight(sA, sB)) continue;
                    best = d; seaA = sA; seaB = sB;
                }
            return best < float.MaxValue;
        }

        bool PushToWaterSameLand(Vector2 from, Vector2 toward, out Vector2 water)
        {
            Vector2 dir = (toward - from).normalized;
            Vector2 cur = from;
            water = default;
            for (int s = 0; s < 40; s++)
            {
                cur += dir * 2f;
                bool waterHere = _terrain.IsWater(cur.x, cur.y) && _terrain.BiomeAt(cur.x, cur.y) != BiomeKind.FreshWater;
                if (waterHere) { water = cur; return true; }
            }
            return false;
        }

        bool OpenSeaStraight(Vector2 a, Vector2 b)
        {
            float len = Vector2.Distance(a, b);
            int n = Mathf.CeilToInt(len / 2f);
            for (int i = 1; i < n; i++)
            {
                var p = Vector2.Lerp(a, b, (float)i / n);
                if (!_terrain.IsWater(p.x, p.y)) return false;
                if (_terrain.BiomeAt(p.x, p.y) == BiomeKind.FreshWater) return false;
            }
            return true;
        }

        static int MinCityLevel(List<EarthCityRT> cities)
        {
            int m = 3;
            foreach (var c in cities) if (c.Level < m) m = c.Level;
            return m;
        }

        static Color FactionColorOf(int faction) => NationEntity.HexToColor(
            faction == 1 ? "d9402f" : faction == 2 ? "2f6fb0" : faction == 3 ? "e8a020" : "3a9d4d");

        /// <summary>载客=城市等级基础(20/30/50)×机车系数，四舍五入，封顶100。</summary>
        public static int RailCapacity(int lowCityLevel, int tier)
        {
            int @base = lowCityLevel <= 1 ? 20 : lowCityLevel == 2 ? 30 : 50;
            float mult = tier == 1 ? 1.0f : tier == 2 ? 1.1f : tier == 3 ? 1.25f : tier == 4 ? 1.6f : 2.0f;
            return Mathf.Min(100, Mathf.RoundToInt(@base * mult));
        }
        static float EarthTrainSpeed(int tier) => tier == 1 ? 7f : tier == 2 ? 12f : tier == 3 ? 18f : tier == 4 ? 36f : 46f;

        void CollectCoast(int landId, float cx, float cz, float maxR, int cap, List<Vector2> sink)
        {
            float step = 5f;
            for (float r = 10f; r <= maxR && sink.Count < cap; r += step)
            {
                int n = Mathf.Max(12, Mathf.RoundToInt(2f * Mathf.PI * r / 5f));
                for (int i = 0; i < n && sink.Count < cap; i++)
                {
                    float a = (float)i / n * Mathf.PI * 2f;
                    float x = cx + Mathf.Cos(a) * r, z = cz + Mathf.Sin(a) * r;
                    if (_terrain.ContinentAt(x, z) != landId || _terrain.IsWater(x, z)) continue;
                    if (!EarthNationPlacement.HasSeaNeighbor(_terrain, x, z)) continue;
                    var v = new Vector2(x, z);
                    bool dup = false;
                    foreach (var q in sink) if (Vector2.Distance(q, v) < 8f) { dup = true; break; }
                    if (!dup) sink.Add(v);
                }
            }
        }

        /// <summary>
        /// 以对方陆块的岸点 anchors 为圆心，在最大跨海跨距内环扫，为本方陆块 landId 补齐
        /// “正对海峡”的岸点（解决城市远离海峡时无候选点的问题，如多佛尔）。每锚点少量增补、去重。
        /// </summary>
        void AugmentAcross(int landId, List<Vector2> anchors, List<Vector2> sink, int addCap = 12)
        {
            int added = 0;
            foreach (var q in anchors)
            {
                if (added >= addCap) break;
                for (float r = 8f; r <= MAX_SEA_SPAN + 6f && added < addCap; r += 6f)
                {
                    int n = Mathf.Max(12, Mathf.RoundToInt(2f * Mathf.PI * r / 4.5f));
                    for (int i = 0; i < n && added < addCap; i++)
                    {
                        float a = (float)i / n * Mathf.PI * 2f;
                        float x = q.x + Mathf.Cos(a) * r, z = q.y + Mathf.Sin(a) * r;
                        if (_terrain.ContinentAt(x, z) != landId || _terrain.IsWater(x, z)) continue;
                        if (!EarthNationPlacement.HasSeaNeighbor(_terrain, x, z)) continue;
                        var v = new Vector2(x, z);
                        bool dup = false;
                        foreach (var e in sink) if (Vector2.Distance(e, v) < 8f) { dup = true; break; }
                        if (!dup) { sink.Add(v); added++; }
                    }
                }
            }
        }

        /// <summary>
        /// 在两块陆地的海岸点间找最短跨海通道：跨距 6..100。
        /// 粗粒度地球图可能把窄海峡（如多佛尔）画成地峡：中段陆地像素只允许属于两岸陆块，
        /// 此时 isthmus=true，由调用方改铺陆地轨道；否则输出两岸水线点 seaA/seaB 铺海面桥轨。
        /// </summary>
        bool FindSeaCrossing(int landA, int landB, List<Vector2> ca, List<Vector2> cb,
                             out Vector2 seaA, out Vector2 seaB, out Vector2 landPtA, out Vector2 landPtB,
                             out bool isthmus)
        {
            seaA = seaB = landPtA = landPtB = default; isthmus = false;
            float best = float.MaxValue;
            foreach (var a in ca)
                foreach (var b in cb)
                {
                    float d = Vector2.Distance(a, b);
                    if (d < 6f || d > MAX_SEA_SPAN || d >= best) continue;
                    bool wa = WalkEdge(a, b, landA, landB, out Vector2 eA, out bool touchA);
                    bool wb = WalkEdge(b, a, landB, landA, out Vector2 eB, out bool touchB);
                    if (!wa || !wb) continue;
                    if (!CorridorClear(eA, eB, landA, landB)) continue;
                    float span = Vector2.Distance(eA, eB);
                    if (span > MAX_SEA_SPAN) continue;
                    bool landBridge = touchA || touchB || span < 2f;
                    // 地峡直通优先；同类取最短
                    float score = (landBridge ? span - 100000f : span);
                    if (score >= best) continue;
                    best = score; seaA = eA; seaB = eB; landPtA = a; landPtB = b; isthmus = landBridge;
                }
            return best < float.MaxValue;
        }

        /// <summary>从本方岸点向对方走：遇到外海返回水线点；直接踏上对方陆地返回接触点（地峡）；淡水/第三陆块失败。</summary>
        bool WalkEdge(Vector2 from, Vector2 toward, int ownLand, int otherLand,
                      out Vector2 edge, out bool touchedOther)
        {
            Vector2 dir = (toward - from).normalized;
            Vector2 cur = from;
            edge = default; touchedOther = false;
            for (int s = 0; s < 40; s++)
            {
                cur += dir * 2f;
                bool water = _terrain.IsWater(cur.x, cur.y);
                int cid = _terrain.ContinentAt(cur.x, cur.y);
                if (water)
                {
                    if (_terrain.BiomeAt(cur.x, cur.y) == BiomeKind.FreshWater) return false; // 海峡不能是湖/河
                    edge = cur; return true;
                }
                if (cid == otherLand) { edge = cur; touchedOther = true; return true; }
                if (cid != ownLand) return false; // 第三陆块挡路
            }
            return false;
        }

        /// <summary>两水线/接触点之间：只允许外海或两岸自身陆块（地峡），不允许淡水湖、第三陆块。</summary>
        bool CorridorClear(Vector2 a, Vector2 b, int landA, int landB)
        {
            float len = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 2f));
            for (int i = 1; i < n; i++)
            {
                var p = Vector2.Lerp(a, b, (float)i / n);
                if (_terrain.BiomeAt(p.x, p.y) == BiomeKind.FreshWater) return false;
                if (!_terrain.IsWater(p.x, p.y))
                {
                    int cid = _terrain.ContinentAt(p.x, p.y);
                    if (cid != landA && cid != landB) return false; // 中段只能是两岸自身（地峡）
                }
            }
            return true;
        }

        // ============ 网络（Kruskal 最小生成树 + 近邻闭环），经典沿用 ============
        class Edge { public int A, B; public float W; }
        List<(int a, int b)> BuildNetwork(List<Vector3> pts, int maxLinks, float band, bool rail)
        {
            var edges = new List<Edge>();
            for (int i = 0; i < pts.Count; i++)
                for (int j = i + 1; j < pts.Count; j++)
                {
                    float d = Vector3.Distance(pts[i], pts[j]);
                    if (!Feasible(pts[i], pts[j], band)) continue;
                    edges.Add(new Edge { A = i, B = j, W = d });
                }
            edges.Sort((p, q) => p.W.CompareTo(q.W));
            var parent = new int[pts.Count];
            for (int i = 0; i < pts.Count; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            bool Union(int a, int b) { int ra = Find(a), rb = Find(b); if (ra == rb) return false; parent[ra] = rb; return true; }

            var res = new List<(int, int)>();
            foreach (var e in edges) { if (Union(e.A, e.B)) { res.Add((e.A, e.B)); if (res.Count >= Mathf.Max(1, pts.Count - 1)) break; } }
            // 近邻闭环（增强连通，封顶 maxLinks）
            foreach (var e in edges) { if (res.Count >= maxLinks) break; if (!res.Contains((e.A, e.B)) && !res.Contains((e.B, e.A))) res.Add((e.A, e.B)); }
            return res;
        }

        bool PointOK(Vector3 p)
        {
            if (_terrain == null) return true;
            if (_terrain.IsWater(p.x, p.z)) return IsOnBridge(p.x, p.z);
            return _terrain.InsideFrontier(p.x, p.z);
        }
        bool Feasible(Vector3 a, Vector3 b, float band)
        {
            float d = Vector3.Distance(a, b);
            int n = Mathf.Max(2, Mathf.CeilToInt(d / 4f));
            float ha = _terrain.HeightAt(a.x, a.z), hb = _terrain.HeightAt(b.x, b.z);
            for (int i = 0; i <= n; i++)
            {
                var p = Vector3.Lerp(a, b, (float)i / n);
                if (!PointOK(p)) return false;
                float h = _terrain.HeightAt(p.x, p.z);
                if (Mathf.Abs(h - Mathf.Lerp(ha, hb, (float)i / n)) > band) return false;
            }
            return true;
        }
        bool IsOnBridge(float x, float z)
        {
            var br = GM.Bridge;
            if (br == null) return false;
            return br.IsBridgeAt(x, z);
        }

        /// <summary>
        /// 陆地国际铁路可行性：采样点必须在开图疆域内；淡水江河/湖湾可架桥；
        /// 外海仅允许短桥跨越窄海湾/河口（单个连续外海段≤12、全线累计≤24，如加利福尼亚湾顶、
        /// 恒河三角洲、墨西哥湾沿岸切削），更长开阔海面否决；忽略高差带（山地可凿隧道）。
        /// </summary>
        bool LandRailOK(Vector3 a, Vector3 b)
        {
            const float MAX_SEA_BRIDGE = 12f;   // 单段跨海铁路桥上限（世界单位）
            const float MAX_SEA_TOTAL = 24f;    // 全线跨海桥累计上限
            float d = Vector3.Distance(a, b);
            int n = Mathf.Max(2, Mathf.CeilToInt(d / 4f));
            float step = d / n;
            float seaRun = 0f, seaTotal = 0f;
            for (int i = 0; i <= n; i++)
            {
                var p = Vector3.Lerp(a, b, (float)i / n);
                if (!_terrain.InsideFrontier(p.x, p.z)) return false;
                bool openSea = _terrain.IsWater(p.x, p.z) && _terrain.BiomeAt(p.x, p.z) != BiomeKind.FreshWater;
                if (openSea)
                {
                    seaRun += step; seaTotal += step;
                    if (seaRun > MAX_SEA_BRIDGE || seaTotal > MAX_SEA_TOTAL) return false;
                }
                else seaRun = 0f;
            }
            return true;
        }

        // ============ 视图：4 车道马路 ============
        void BuildRoad4View(Vector3 a, Vector3 b)
        {
            RecordRoad(a, b);
            var go = new GameObject("Road4");
            go.transform.SetParent(Root, false);
            float len = Vector3.Distance(a, b);
            var mid = (a + b) * 0.5f;
            float y = _terrain.HeightAt(mid.x, mid.z) + 0.18f;
            var rot = Quaternion.LookRotation(b - a);
            const float W = 7.6f; // 4 车道总宽
            Strip(go, _asphalt, mid, y, rot, len, W, 0.16f);
            Strip(go, _laneWhite, mid, y + 0.02f, rot, len, 0.14f, 0.02f, 0f, W * 0.5f - 0.35f);   // 两侧边线
            Strip(go, _laneWhite, mid, y + 0.02f, rot, len, 0.14f, 0.02f, 0f, -(W * 0.5f - 0.35f));
            Strip(go, _laneYellow, mid, y + 0.025f, rot, len, 0.10f, 0.02f, 0f, 0.22f);            // 中央双黄线
            Strip(go, _laneYellow, mid, y + 0.025f, rot, len, 0.10f, 0.02f, 0f, -0.22f);
            // 同向车道间白虚线（±1.9）
            for (float s = 3f; s < len * 0.5f; s += 6f)
                foreach (var off in new[] { 1.9f, -1.9f })
                {
                    var p = mid + rot * new Vector3(off, 0, s);
                    Strip(go, _laneWhite, p, y + 0.02f, rot, 2.4f, 0.10f, 0.02f);
                }
        }

        // ============ 视图：经典双车道（保留） ============
        void BuildRoadView(Vector3 a, Vector3 b)
        {
            RecordRoad(a, b);
            var go = new GameObject("Road");
            go.transform.SetParent(Root, false);
            float len = Vector3.Distance(a, b);
            var mid = (a + b) * 0.5f;
            float y = _terrain.HeightAt(mid.x, mid.z) + 0.18f;
            var rot = Quaternion.LookRotation(b - a);
            Strip(go, _asphalt, mid, y, rot, len, 3.1f, 0.14f);
            for (float s = -len * 0.5f + 3f; s < len * 0.5f; s += 6f)
            {
                var p = mid + rot * new Vector3(0, 0, s);
                Strip(go, _laneWhite, p, y + 0.02f, rot, 2.6f, 0.12f, 0.02f);
            }
        }

        // ============ 视图：经典陆地铁路（保留；默认红色列车） ============
        void BuildRailView(Vector3 a, Vector3 b, int tier)
            => BuildRailView(a, b, tier, new Color(0.72f, 0.13f, 0.12f), null);

        /// <summary>陆地铁路视图（V9.1.2 国际线可指定列车阵营色与名称；自带一班往返列车）。</summary>
        void BuildRailView(Vector3 a, Vector3 b, int tier, Color trainCol, string trainName)
        {
            var go = new GameObject("Rail");
            go.transform.SetParent(Root, false);
            float len = Vector3.Distance(a, b);
            var mid = (a + b) * 0.5f;
            float y = _terrain.HeightAt(mid.x, mid.z) + (tier >= 4 ? 1.3f : 0.22f);
            var rot = Quaternion.LookRotation(b - a);
            Strip(go, _ballast, mid, y - 0.08f, rot, len, 2.6f, 0.10f);
            for (float s = -len * 0.5f; s <= len * 0.5f; s += 3f)
            {
                var p = mid + rot * new Vector3(0, 0, s);
                Strip(go, _sleeper, p, y, Quaternion.Euler(0, 90f, 0), 2.2f, 0.18f, 0.08f);
            }
            Strip(go, _railMetal, mid, y + 0.06f, rot, len, 0.10f, 0.08f, 0f, 0.75f);
            Strip(go, _railMetal, mid, y + 0.06f, rot, len, 0.10f, 0.08f, 0f, -0.75f);
            if (tier >= 3)
                for (float s = -len * 0.5f; s < len * 0.5f; s += 8f)
                {
                    var p = mid + rot * new Vector3(0, 0, s);
                    Pole(go, p, y, 4.4f);
                }
            if (tier >= 4)
                for (float s = -len * 0.5f; s <= len * 0.5f; s += 10f)
                {
                    var p = mid + rot * new Vector3(0, 0, s);
                    Pier(go, p, y, 1.3f);
                }
            var tr = CreateTrain(go.transform, mid + Vector3.up * 0.6f, tier,
                                 Mathf.Abs(b.x - a.x) >= Mathf.Abs(b.z - a.z), EarthTrainSpeed(tier), trainCol);
            if (!string.IsNullOrEmpty(trainName)) tr.name = trainName;
            var mv = tr.GetComponent<CorridorMover>();
            if (mv != null) { mv.A = a + Vector3.up * (tier >= 4 ? 1.6f : 0.5f); mv.B = b + Vector3.up * (tier >= 4 ? 1.6f : 0.5f); mv.L = len; }
            _trains++;
        }

        // ============ 视图：跨海铁路桥面（轨道只在海面上方） ============
        void BuildSeaRailView(Vector3 a, Vector3 b, int tier)
        {
            var go = new GameObject("SeaRail");
            go.transform.SetParent(Root, false);
            float len = Vector3.Distance(a, b);
            var mid = (a + b) * 0.5f;
            float waterY = -0.2f;
            float deckY = waterY + 0.62f;
            var rot = Quaternion.LookRotation(b - a);
            // 桥面 + 道砟
            Strip(go, _deck, mid, deckY, rot, len, 3.4f, 0.22f);
            Strip(go, _ballast, mid, deckY + 0.14f, rot, len, 2.4f, 0.08f);
            // 枕木 + 双轨
            for (float s = -len * 0.5f; s <= len * 0.5f; s += 3f)
            {
                var p = mid + rot * new Vector3(0, 0, s);
                Strip(go, _sleeper, p, deckY + 0.20f, Quaternion.Euler(0, 90f, 0), 2.2f, 0.16f, 0.07f);
            }
            Strip(go, _railMetal, mid, deckY + 0.28f, rot, len, 0.10f, 0.08f, 0f, 0.75f);
            Strip(go, _railMetal, mid, deckY + 0.28f, rot, len, 0.10f, 0.08f, 0f, -0.75f);
            // 桥墩（落到水下）
            for (float s = -len * 0.5f; s <= len * 0.5f; s += 10f)
            {
                var p = mid + rot * new Vector3(0, 0, s);
                Pier(go, p, deckY, deckY - (waterY - 2.2f));
            }
            if (tier >= 3)
                for (float s = -len * 0.5f; s < len * 0.5f; s += 9f)
                    Pole(go, mid + rot * new Vector3(0, 0, s), deckY + 0.2f, 4.0f);
        }

        // ============ 列车 / 汽车 ============
        Transform CreateTrain(Transform parent, Vector3 at, int tier, bool axisX, float speed, Color col)
        {
            var tr = EntityViewFactory.Spawn("Train", parent, PrimitiveType.Cube, col, 1f);
            tr.transform.localScale = new Vector3(axisX ? 7.5f : 1.7f, 1.5f, axisX ? 1.7f : 7.5f);
            tr.transform.position = at;
            int cars = tier >= 4 ? 5 : tier == 3 ? 4 : 3;
            for (int i = 1; i < cars; i++)
            {
                var c = EntityViewFactory.Spawn("car", tr.transform, PrimitiveType.Cube, Color.Lerp(col, Color.white, 0.25f), 1f);
                float off = i * 2.4f;
                c.transform.localScale = new Vector3(axisX ? 2.2f : 1.5f, 1.3f, axisX ? 1.5f : 2.2f);
                c.transform.localPosition = axisX ? new Vector3(off, 0, 0) : new Vector3(0, 0, off);
            }
            var mv = tr.AddComponent<CorridorMover>();
            mv.A = at; mv.B = at; mv.L = 10f; mv.Speed = speed; mv.AxisX = axisX;
            return tr.transform;
        }

        void AddCars(Vector3 a, Vector3 b)
        {
            float len = Vector3.Distance(a, b);
            int want = Mathf.Clamp(Mathf.RoundToInt(len / 60f), 1, 2);
            var cols = new[] { new Color(0.85f,0.86f,0.88f), new Color(0.70f,0.12f,0.12f),
                               new Color(0.14f,0.32f,0.62f), new Color(0.85f,0.70f,0.15f) };
            for (int i = 0; i < want; i++)
            {
                float t = (i + 1f) / (want + 1f);
                var p = Vector3.Lerp(a, b, t);
                var rot = Quaternion.LookRotation(b - a);
                bool truck = (i == 0 && want == 2);
                var car = EntityViewFactory.Spawn(truck ? "Truck" : "Car", Root, PrimitiveType.Cube,
                                                  cols[Random.Range(0, cols.Length)], truck ? 1.1f : 0.8f);
                float cy = _terrain.HeightAt(p.x, p.z) + 0.5f;
                car.transform.position = new Vector3(p.x, cy, p.z);
                car.transform.rotation = rot;
                car.transform.localScale = truck ? new Vector3(1.3f, 1.1f, 3.0f) : new Vector3(1.2f, 0.9f, 2.1f);
                var mv = car.AddComponent<CorridorMover>();
                mv.L = len; mv.Speed = truck ? 5.5f : 8f;
                mv.A = new Vector3(a.x, cy, a.z); mv.B = new Vector3(b.x, cy, b.z);
                _cars++;
            }
        }

        void TickMovers(float dt)
        {
            var movers = Root.GetComponentsInChildren<CorridorMover>();
            _moverN = movers.Length;
            foreach (var mv in movers) mv.Tick(dt, _terrain);
        }

        // ============ 基础件 ============
        void Strip(GameObject parent, Material m, Vector3 pos, float y, Quaternion rot, float len, float width, float thick,
                   float yawExtra = 0f, float lateral = 0f)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(box.GetComponent<Collider>());
            box.name = "strip";
            box.transform.SetParent(parent.transform, false);
            var lr = rot * Quaternion.Euler(0, yawExtra, 0);
            box.transform.position = pos + lr * new Vector3(lateral, y, 0);
            box.transform.rotation = lr;
            box.transform.localScale = new Vector3(width, thick, Mathf.Max(0.5f, len));
            if (m != null) box.GetComponent<MeshRenderer>().sharedMaterial = m;
        }
        void Pole(GameObject parent, Vector3 p, float y, float h)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(pole.GetComponent<Collider>());
            pole.name = "pole"; pole.transform.SetParent(parent.transform, false);
            pole.transform.position = p + Vector3.up * (y + h * 0.5f);
            pole.transform.localScale = new Vector3(0.12f, h, 0.12f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = _railMetal;
            var wire = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(wire.GetComponent<Collider>());
            wire.name = "wire"; wire.transform.SetParent(parent.transform, false);
            wire.transform.position = p + Vector3.up * (y + h);
            wire.transform.localScale = new Vector3(2.4f, 0.05f, 0.05f);
            wire.GetComponent<MeshRenderer>().sharedMaterial = _railMetal;
        }
        void Pier(GameObject parent, Vector3 p, float topY, float h)
        {
            var pier = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(pier.GetComponent<Collider>());
            pier.name = "pier"; pier.transform.SetParent(parent.transform, false);
            pier.transform.position = new Vector3(p.x, topY - h * 0.5f, p.z);
            pier.transform.localScale = new Vector3(0.5f, h * 0.5f, 0.5f);
            pier.GetComponent<MeshRenderer>().sharedMaterial = _pier;
        }
    }
}
