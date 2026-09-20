using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.0.1 现代交通 + V9.0.3 道路分级与城市车流（SimCity BuildIt 风格）。
    /// 纯装饰车辆有上限、LOD 由外层控制、无 Collider、不进存档，视图挂 ModernTraffic 根随清局销毁；
    /// 道路本身是建筑（进存档），由组织神按年代/人口自动修建。
    /// ① 道路四级：驰道/大马路（既有沥青）→主干道 arterial(约1900)→高速公路 highway_modern(约1930)→互通立交 interchange(约1960)；
    /// ② 车辆：彩色轿车、货运卡车（随工厂数）、公交巴士（era≥5 按人口，上限12），只在陆地/桥面行驶绝不下水；
    /// ③ 车辆偏向沿路行驶，车速随道路等级提升；区域拥堵度=车辆/道路容量，>0.8 减速（通勤效率，供9.0.4/9.0.5读取）；
    /// ④ era≥6 机场最多 3 架客机盘旋。
    /// </summary>
    public class ModernTrafficSystem : GameSystemBase
    {
        private Transform _root;
        private WorldGenerator _terrain;
        private float _spawnCd, _roadCd, _congCd;
        private readonly List<Car> _cars = new();
        private readonly List<Plane> _planes = new();
        private readonly Dictionary<int,float> _cong = new();
        private float _avgCong, _maxCong;
        private Material _bodyDark, _tire, _metal, _glass, _busWhite, _busBlue;
        private Material _fireRed, _fireWhite, _policeWhite, _policeBlue, _ambWhite, _ambRed, _beaconRed, _beaconBlue;
        // V9.0.6 应急车辆（消防红/警车蓝白/救护白红）与航班起降计数（探针读取）
        private readonly List<Emergency> _em = new();
        private int _emDone;
        private const int MAX_EMERGENCY = 6;
        private const float Cell = 48f;
        private static readonly Color[] CarColors = {
            new(0.86f,0.27f,0.24f), new(0.25f,0.55f,0.92f), new(0.95f,0.80f,0.25f),
            new(0.30f,0.72f,0.40f), new(0.92f,0.92f,0.90f), new(0.45f,0.45f,0.50f),
            new(0.70f,0.45f,0.85f), new(0.95f,0.55f,0.20f) };

        private class Car
        {
            public GameObject View; public float X,Z,H,TX,TZ; public float Speed; public int Kind; // 0轿车 1卡车 2公交
            public float Wander;
            public bool Truck => Kind==1; public bool Bus => Kind==2;
        }
        private class Plane
        {
            public GameObject View; public float CX,CZ,R,Alt,Ang,AngV;
            public int Phase; public float PT, R0, Alt0;   // 0巡航 1降落 2地面 3起飞
            // V9.0.9 航线组网：≥2 机场时在机场对间直线往返；Route=false 保持单机盘旋
            public bool Route; public float HX,HZ,TX,TZ; public float CruiseAlt;
        }
        private class Emergency
        {
            public GameObject View; public int Kind;        // 3消防车 4警车 5救护车
            public float X,Z,H; public int Phase;           // 0赶赴 1处置 2返回
            public float T, Stuck, LastMove, Age; public int Wp;
            public readonly List<Vector2> Path = new();
            public GameObject BeaconR, BeaconB; public float Flash;
        }

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _terrain = Object.FindObjectOfType<WorldGenerator>();
            _bodyDark = ShaderHelper.Pbr(new Color(0.12f,0.12f,0.14f),0.2f,0.5f,951,0.5f);
            _tire     = ShaderHelper.Pbr(new Color(0.05f,0.05f,0.06f),0f,0.3f,952,0.4f);
            _metal    = ShaderHelper.Pbr(new Color(0.65f,0.68f,0.72f),0.7f,0.6f,953,0.4f);
            _glass    = ShaderHelper.Pbr(new Color(0.35f,0.6f,0.8f),0.1f,0.9f,954,0.3f);
            _busWhite = ShaderHelper.Pbr(new Color(0.93f,0.94f,0.95f),0.1f,0.5f,955,0.5f);
            _busBlue  = ShaderHelper.Pbr(new Color(0.16f,0.42f,0.78f),0.2f,0.6f,956,0.5f);
            _fireRed   = ShaderHelper.Pbr(new Color(0.80f,0.10f,0.08f),0.15f,0.55f,957,0.5f);
            _fireWhite = ShaderHelper.Pbr(new Color(0.93f,0.94f,0.92f),0.1f,0.5f,958,0.5f);
            _policeWhite = ShaderHelper.Pbr(new Color(0.93f,0.94f,0.96f),0.1f,0.5f,959,0.5f);
            _policeBlue  = ShaderHelper.Pbr(new Color(0.10f,0.30f,0.78f),0.2f,0.6f,961,0.5f);
            _ambWhite = ShaderHelper.Pbr(new Color(0.94f,0.95f,0.95f),0.1f,0.5f,962,0.5f);
            _ambRed   = ShaderHelper.Pbr(new Color(0.85f,0.08f,0.10f),0.15f,0.55f,963,0.5f);
            _beaconRed  = ShaderHelper.Pbr(new Color(1.0f,0.12f,0.10f),0.05f,0.15f,964,0.6f);
            _beaconBlue = ShaderHelper.Pbr(new Color(0.15f,0.45f,1.0f),0.05f,0.15f,965,0.6f);
        }

        private Transform Root => _root = EntityViewFactory.EnsureRoot("ModernTraffic", GM.transform);

        public override void Tick(float dt)
        {
            if (S == null) return;
            _spawnCd -= dt;
            if (_spawnCd <= 0f) { _spawnCd = 2f; ManageSpawn(); }
            _roadCd -= dt;
            if (_roadCd <= 0f) { _roadCd = 5f; ManageRoads(); }
            _congCd -= dt;
            if (_congCd <= 0f) { _congCd = 1f; RecomputeCongestion(); }
            TickCars(dt);
            TickEmergency(dt);
            TickPlanes(dt);
        }

        // ---------- 道路分级 ----------
        public static bool IsRoad(string t) =>
            t=="road"||t=="highway"||t=="arterial"||t=="highway_modern"||t=="interchange";
        private int CountRoad(string id){ int n=0; foreach(var b in S.Buildings) if(b.Type==id) n++; return n; }
        private int SubwayCount(){ int n=0; foreach(var b in S.Buildings) if(b!=null&&b.Type=="subway") n++; return n; }
        private int RoadCount()
        {
            int n = 0;
            foreach (var b in S.Buildings) if (IsRoad(b.Type)) n++;
            return n;
        }
        // 单段道路通行容量（用于拥堵度）
        private static float RoadCapacity(string t) => t switch
        {
            "interchange" => 20f,
            "highway_modern" => 12f,
            "arterial" => 6f,
            "highway" => 3f,
            "road" => 2f,
            _ => 0f
        };
        // 年代门控（公元 = 游戏年 - 3000）
        private static int RoadTierDue(string id, int year, int pop)
        {
            int ad = year - 3000;
            switch(id)
            {
                case "arterial":        if(ad < 1900) return 0; return Mathf.Min(16, 4 + pop/90);
                case "highway_modern":  if(ad < 1930) return 0; return Mathf.Min(10, 2 + pop/160);
                case "interchange":     if(ad < 1960) return 0; return pop>=600?2:(pop>=200?1:0);
                default: return 0;
            }
        }
        private void ManageRoads()
        {
            if (_terrain == null || S.Buildings.Count == 0) return;
            foreach (var id in new[]{"arterial","highway_modern","interchange"})
            {
                int have = CountRoad(id), want = RoadTierDue(id, S.Year, S.Pop);
                if (have < want) TryBuildRoad(id);   // 每次评估每级至多补 1 段，避免瞬时铺满地
            }
        }
        private bool TryBuildRoad(string id)
        {
            if (GM.Building == null) return false;
            if (!GM.Building.FindAutoPosition(id, out float x, out float z)) return false;
            return GM.Building.PlaceBuilding(id, x, z);
        }
        /// <summary>探针/跳年用：立即把当前年代应有的各等级道路补齐（有界）。</summary>
        public void RefreshNow()
        {
            if (S == null) return;
            foreach (var id in new[]{"arterial","highway_modern","interchange"})
            {
                int guard = 0;
                while (guard++ < 24 && CountRoad(id) < RoadTierDue(id, S.Year, S.Pop))
                    if (!TryBuildRoad(id)) break;
            }
        }

        // ---------- 车辆配额 ----------
        private int FreightCount()
        {
            int n=0;
            foreach(var b in S.Buildings)
                if(b.Type=="factory_modern"||b.Type=="factory_pre"||b.Type=="workshop"||b.Type=="iron_smelter"
                   ||b.Type=="lumbermill"||b.Type=="mine"||b.Type=="market"||b.Type=="supermarket"
                   ||b.Type=="brick_works"||b.Type=="porcelain_kiln") n++;
            return n;
        }
        private int WantTrucks() => S.Era<5?0:Mathf.Min(10, FreightCount());
        private int WantBuses()  => S.Era<5?0:Mathf.Min(12, Mathf.Max(0,(S.Pop-150)/80));
        private int WantCars()
        {
            if (S.Era < 5) return 0;
            int total = Mathf.Min(34, 4 + RoadCount()/2 + S.Pop/70);
            return Mathf.Max(0, total - WantTrucks() - WantBuses());
        }

        private void ManageSpawn()
        {
            if (_terrain == null || S.Buildings.Count == 0) return;
            _cars.RemoveAll(c => c.View == null);
            _planes.RemoveAll(p => p.View == null);
            for (int i = _em.Count - 1; i >= 0; i--)
                if (_em[i].View == null) _em.RemoveAt(i);

            int cars=0, trucks=0, buses=0;
            foreach (var c in _cars) { if(c.Kind==1) trucks++; else if(c.Kind==2) buses++; else cars++; }
            if (trucks < WantTrucks() && TrySpawnCar(1)) return;
            if (buses  < WantBuses()  && TrySpawnCar(2)) return;
            if (cars   < WantCars()   && TrySpawnCar(0)) return;

            if (S.Era >= 6)
            {
                var aps = Airports();
                // V9.0.9 航线组网：每座机场 2 架、上限 4；≥2 机场时机场对间往返，单机时原地盘旋
                int cap = Mathf.Min(4, Mathf.Max(1, aps.Count) * 2);
                if (_planes.Count < cap && Random.value < 0.5f)
                {
                    if (aps.Count >= 2)
                    {
                        var h = aps[Random.Range(0, aps.Count)];
                        BuildingEntity t; int guard = 0;
                        do { t = aps[Random.Range(0, aps.Count)]; } while (t == h && aps.Count > 1 && guard++ < 4);
                        SpawnPlane(h, t);
                    }
                    else if (aps.Count == 1) SpawnPlane(aps[0], null);
                }
            }
        }

        /// <summary>全部机场建筑（仅主世界）。</summary>
        private List<BuildingEntity> Airports()
        {
            var list = new List<BuildingEntity>();
            foreach (var b in S.Buildings)
                if (b != null && b.MapId == "home" && b.Type == "airport") list.Add(b);
            return list;
        }

        private bool TrySpawnCar(int kind)
        {
            // V9.1.1 车辆只在道路上：出生即吸附最近铺装公路（建筑道路或城际 4 车道马路），无路不生成
            for (int k = 0; k < 18; k++)
            {
                float bx, bz;
                if (GM.Intercity != null && GM.Intercity.HasRoads && GM.Intercity.RandomRoadPoint(out var rp))
                { bx = rp.x; bz = rp.z; }
                else if (S.Buildings.Count > 0)
                { var anchor = S.Buildings[Random.Range(0, S.Buildings.Count)]; bx = anchor.X; bz = anchor.Z; }
                else continue;
                if (!SnapRoad(bx, bz, 80f, out var pt)) continue;
                if (_terrain.IsWater(pt.x, pt.y) && !(GM.Bridge != null && GM.Bridge.IsBridgeAt(pt.x, pt.y))) continue;
                if (!_terrain.InsideFrontier(pt.x, pt.y)) continue;
                SpawnCar(kind, pt.x, pt.y);
                return true;
            }
            return false;
        }

        /// <summary>V9.1.1 统一道路吸附：在 maxR 内找最近的建筑道路点或城际公路中线点。</summary>
        private bool SnapRoad(float x, float z, float maxR, out Vector2 pt)
        {
            pt = default; bool found = false; float bd = maxR * maxR;
            foreach (var b in S.Buildings)
            {
                if (b == null || !IsRoad(b.Type)) continue;
                float dx = b.X - x, dz = b.Z - z, d2 = dx * dx + dz * dz;
                if (d2 < bd) { bd = d2; pt = new Vector2(b.X, b.Z); found = true; }
            }
            if (GM.Intercity != null && GM.Intercity.HasRoads
                && GM.Intercity.NearestRoadPoint(new Vector3(x, 0f, z), maxR, out var q))
            {
                float d2 = (q.x - x) * (q.x - x) + (q.z - z) * (q.z - z);
                if (d2 < bd) { bd = d2; pt = new Vector2(q.x, q.z); found = true; }
            }
            return found;
        }

        // ---------- 建模 ----------
        private GameObject BuildCar(Color c, int kind)
        {
            var go = new GameObject(kind==2?"Bus":(kind==1?"Truck":"Car"));
            var body = ShaderHelper.Pbr(c, 0.15f, 0.55f, 960 + (int)(c.r * 99), 0.5f);
            void B(string n, Vector3 pos, Vector3 sc, Material m)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = n; b.transform.SetParent(go.transform, false);
                b.transform.localPosition = pos; b.transform.localScale = sc;
                b.GetComponent<Renderer>().material = m;
                Object.Destroy(b.GetComponent<Collider>());
            }
            void W(Vector3 pos, float r=0.34f)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                w.name = "Wheel"; w.transform.SetParent(go.transform, false);
                w.transform.localPosition = pos; w.transform.localScale = new Vector3(r, 0.16f, r);
                w.transform.localRotation = Quaternion.Euler(0, 0, 90);
                w.GetComponent<Renderer>().material = _tire; Object.Destroy(w.GetComponent<Collider>());
            }
            if (kind==1) // 卡车
            {
                B("Cargo", new Vector3(0, 0.75f, 0.25f), new Vector3(1.25f, 0.95f, 1.7f), _bodyDark);
                B("Cab", new Vector3(0, 0.65f, -1.05f), new Vector3(1.15f, 0.8f, 0.9f), body);
                B("Windshield", new Vector3(0, 0.85f, -1.51f), new Vector3(0.9f, 0.4f, 0.06f), _glass);
                W(new Vector3(-0.62f, 0.3f, -0.75f)); W(new Vector3(0.62f, 0.3f, -0.75f));
                W(new Vector3(-0.62f, 0.3f, 0.75f));  W(new Vector3(0.62f, 0.3f, 0.75f));
            }
            else if (kind==2) // 公交巴士：白色长车身 + 蓝条 + 一排车窗，6 轮
            {
                B("BusBody", new Vector3(0,0.62f,0), new Vector3(1.35f,0.95f,4.2f), _busWhite);
                B("BusStripe", new Vector3(0,0.5f,0), new Vector3(1.37f,0.22f,4.25f), _busBlue);
                B("BusRoof", new Vector3(0,1.12f,-0.1f), new Vector3(1.2f,0.16f,3.6f), _busWhite);
                for(float zz=-1.6f; zz<=1.6f; zz+=0.64f)
                {
                    B("WinL", new Vector3(-0.69f,0.92f,zz), new Vector3(0.05f,0.34f,0.46f), _glass);
                    B("WinR", new Vector3( 0.69f,0.92f,zz), new Vector3(0.05f,0.34f,0.46f), _glass);
                }
                B("BusFront", new Vector3(0,0.85f,-2.12f), new Vector3(1.0f,0.5f,0.06f), _glass);
                foreach(float zz in new[]{-1.5f,0f,1.5f})
                { W(new Vector3(-0.72f,0.32f,zz)); W(new Vector3(0.72f,0.32f,zz)); }
            }
            else if (kind==3) // 消防车：红车身 + 白腰线 + 器材箱 + 车顶红警灯
            {
                B("Cab", new Vector3(0,0.7f,-1.15f), new Vector3(1.25f,0.9f,1.0f), _fireRed);
                B("Tank", new Vector3(0,0.85f,0.55f), new Vector3(1.3f,1.1f,2.3f), _fireRed);
                B("Stripe", new Vector3(0,0.55f,0.55f), new Vector3(1.32f,0.18f,2.32f), _fireWhite);
                B("Windshield", new Vector3(0,0.95f,-1.66f), new Vector3(1.0f,0.42f,0.06f), _glass);
                B("Ladder", new Vector3(0,1.5f,0.55f), new Vector3(0.16f,0.12f,2.6f), _metal);
                var br = GameObject.CreatePrimitive(PrimitiveType.Cube);
                br.name="BeaconR"; br.transform.SetParent(go.transform,false);
                br.transform.localPosition=new Vector3(0,1.3f,-1.15f); br.transform.localScale=new Vector3(0.5f,0.16f,0.28f);
                br.GetComponent<Renderer>().material=_beaconRed; Object.Destroy(br.GetComponent<Collider>());
                foreach(float zz in new[]{-1.15f,0.9f})
                { W(new Vector3(-0.68f,0.34f,zz),0.4f); W(new Vector3(0.68f,0.34f,zz),0.4f); }
            }
            else if (kind==4) // 警车：白车身 + 蓝腰线 + 红蓝双警灯
            {
                B("Body", new Vector3(0,0.48f,0), new Vector3(1.2f,0.55f,2.5f), _policeWhite);
                B("Cabin", new Vector3(0,0.9f,-0.15f), new Vector3(1.0f,0.5f,1.2f), _policeWhite);
                B("Stripe", new Vector3(0,0.5f,0), new Vector3(1.22f,0.2f,2.52f), _policeBlue);
                B("WindshieldF", new Vector3(0,0.92f,-0.78f), new Vector3(0.88f,0.34f,0.05f), _glass);
                B("WindshieldB", new Vector3(0,0.92f,0.48f), new Vector3(0.88f,0.34f,0.05f), _glass);
                var br = GameObject.CreatePrimitive(PrimitiveType.Cube);
                br.name="BeaconR"; br.transform.SetParent(go.transform,false);
                br.transform.localPosition=new Vector3(-0.22f,1.22f,-0.15f); br.transform.localScale=new Vector3(0.34f,0.14f,0.26f);
                br.GetComponent<Renderer>().material=_beaconRed; Object.Destroy(br.GetComponent<Collider>());
                var bb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bb.name="BeaconB"; bb.transform.SetParent(go.transform,false);
                bb.transform.localPosition=new Vector3(0.22f,1.22f,-0.15f); bb.transform.localScale=new Vector3(0.34f,0.14f,0.26f);
                bb.GetComponent<Renderer>().material=_beaconBlue; Object.Destroy(bb.GetComponent<Collider>());
                W(new Vector3(-0.62f,0.3f,-0.78f)); W(new Vector3(0.62f,0.3f,-0.78f));
                W(new Vector3(-0.62f,0.3f,0.78f));  W(new Vector3(0.62f,0.3f,0.78f));
            }
            else if (kind==5) // 救护车：白车身 + 红腰线 + 红色十字 + 红警灯
            {
                B("Body", new Vector3(0,0.62f,0), new Vector3(1.3f,0.95f,2.9f), _ambWhite);
                B("Stripe", new Vector3(0,0.5f,0), new Vector3(1.32f,0.2f,2.92f), _ambRed);
                B("CrossV", new Vector3(-0.66f,0.85f,0.3f), new Vector3(0.05f,0.5f,0.16f), _ambRed);
                B("CrossH", new Vector3(-0.66f,0.85f,0.3f), new Vector3(0.05f,0.16f,0.5f), _ambRed);
                B("WindshieldF", new Vector3(0,0.9f,-1.46f), new Vector3(1.0f,0.4f,0.06f), _glass);
                var br = GameObject.CreatePrimitive(PrimitiveType.Cube);
                br.name="BeaconR"; br.transform.SetParent(go.transform,false);
                br.transform.localPosition=new Vector3(0,1.18f,-0.9f); br.transform.localScale=new Vector3(0.5f,0.14f,0.26f);
                br.GetComponent<Renderer>().material=_beaconRed; Object.Destroy(br.GetComponent<Collider>());
                W(new Vector3(-0.66f,0.32f,-0.95f),0.38f); W(new Vector3(0.66f,0.32f,-0.95f),0.38f);
                W(new Vector3(-0.66f,0.32f,0.95f),0.38f);  W(new Vector3(0.66f,0.32f,0.95f),0.38f);
            }
            else // 轿车
            {
                B("Body", new Vector3(0, 0.45f, 0), new Vector3(1.15f, 0.5f, 2.3f), body);
                B("Cabin", new Vector3(0, 0.85f, -0.15f), new Vector3(0.95f, 0.5f, 1.15f), body);
                B("WindshieldF", new Vector3(0, 0.88f, -0.74f), new Vector3(0.85f, 0.34f, 0.05f), _glass);
                B("WindshieldB", new Vector3(0, 0.88f, 0.44f), new Vector3(0.85f, 0.34f, 0.05f), _glass);
                W(new Vector3(-0.62f, 0.3f, -0.75f)); W(new Vector3(0.62f, 0.3f, -0.75f));
                W(new Vector3(-0.62f, 0.3f, 0.75f));  W(new Vector3(0.62f, 0.3f, 0.75f));
            }
            return go;
        }

        private void SpawnCar(int kind, float x, float z)
        {
            var col = CarColors[Random.Range(0, CarColors.Length)];
            var view = BuildCar(col, kind);
            view.transform.SetParent(Root, false);
            float h = _terrain.HeightAt(x, z);
            view.transform.position = new Vector3(x, h, z);
            view.transform.localScale = kind==2?Vector3.one*1.05f:(kind==1?Vector3.one*1.1f:Vector3.one);
            var car = new Car { View = view, X = x, Z = z, H = h, TX = x, TZ = z, Kind=kind,
                Speed = kind==2?Random.Range(4f,5.5f):(kind==1?Random.Range(3.5f,5f):Random.Range(5f,8f)) };
            PickWander(car);
            _cars.Add(car);
        }

        private GameObject BuildPlane(Color stripe)
        {
            var go = new GameObject("Airliner");
            var white = ShaderHelper.Pbr(new Color(0.93f, 0.94f, 0.95f), 0.1f, 0.5f, 970, 0.5f);
            var sm = ShaderHelper.Pbr(stripe, 0.1f, 0.5f, 971, 0.5f);
            void B(string n, Vector3 pos, Vector3 sc, Material m, Quaternion rot = default)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = n; b.transform.SetParent(go.transform, false);
                b.transform.localPosition = pos; b.transform.localScale = sc;
                if (rot != default) b.transform.localRotation = rot;
                b.GetComponent<Renderer>().material = m; Object.Destroy(b.GetComponent<Collider>());
            }
            var fus = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fus.name = "Fuselage"; fus.transform.SetParent(go.transform, false);
            fus.transform.localPosition = Vector3.zero; fus.transform.localScale = new Vector3(0.45f, 2.6f, 0.45f);
            fus.transform.localRotation = Quaternion.Euler(90, 0, 0);
            fus.GetComponent<Renderer>().material = white; Object.Destroy(fus.GetComponent<Collider>());
            var nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nose.name = "Nose"; nose.transform.SetParent(go.transform, false);
            nose.transform.localPosition = new Vector3(0, 0, -2.55f); nose.transform.localScale = new Vector3(0.45f, 0.45f, 0.7f);
            nose.GetComponent<Renderer>().material = white; Object.Destroy(nose.GetComponent<Collider>());
            B("Wing", new Vector3(0, -0.05f, -0.2f), new Vector3(6.2f, 0.12f, 1.3f), _metal);
            B("TailWing", new Vector3(0, 0.25f, 2.25f), new Vector3(2.2f, 0.1f, 0.7f), _metal);
            B("Tail", new Vector3(0, 0.75f, 2.3f), new Vector3(0.12f, 1.1f, 0.7f), _metal);
            B("Stripe", new Vector3(0, 0.12f, 0), new Vector3(0.46f, 0.12f, 4.6f), sm, Quaternion.Euler(90, 0, 0));
            return go;
        }
        private void SpawnPlane(BuildingEntity ap) => SpawnPlane(ap, null);
        /// <summary>V9.0.9：dest 非空时建立机场对直飞航线；为空时单机盘旋。</summary>
        private void SpawnPlane(BuildingEntity ap, BuildingEntity dest)
        {
            var col = CarColors[Random.Range(0, 5)];
            var view = BuildPlane(col);
            view.transform.SetParent(Root, false);
            float alt = Random.Range(22f, 28f);
            if (dest != null)
            {
                _planes.Add(new Plane { View = view, CX = ap.X, CZ = ap.Z,
                    Route = true, HX = ap.X, HZ = ap.Z, TX = dest.X, TZ = dest.Z,
                    R = 0f, R0 = 0f, Alt = alt, Alt0 = alt, CruiseAlt = alt,
                    Ang = Mathf.Atan2(dest.X-ap.X, dest.Z-ap.Z), AngV = 0f,
                    Phase = 0, PT = 0f });
            }
            else
            {
                float r = Random.Range(20f, 32f);
                _planes.Add(new Plane { View = view, CX = ap.X, CZ = ap.Z,
                    R = r, R0 = r, Alt = alt, Alt0 = alt, CruiseAlt = alt,
                    Ang = Random.value * Mathf.PI * 2f, AngV = Random.Range(0.10f, 0.18f),
                    Phase = 0, PT = Random.Range(18f, 40f) });
            }
        }

        // ---------- 拥堵 ----------
        private static int CellKey(float x,float z) => Mathf.FloorToInt(x/Cell)*73856093 ^ Mathf.FloorToInt(z/Cell)*19349663;
        private void RecomputeCongestion()
        {
            _cong.Clear();
            var cap = new Dictionary<int,float>();
            foreach(var b in S.Buildings)
            {
                if(!IsRoad(b.Type)) continue;
                int k=CellKey(b.X,b.Z);
                cap.TryGetValue(k,out float c); cap[k]=c+RoadCapacity(b.Type);
            }
            var veh = new Dictionary<int,int>();
            foreach(var c in _cars)
            {
                if(c.View==null) continue;
                int k=CellKey(c.X,c.Z);
                veh.TryGetValue(k,out int n); veh[k]=n+1;
            }
            float sum=0; int cells=0; _maxCong=0f;
            foreach(var kv in veh)
            {
                cap.TryGetValue(kv.Key,out float cp);
                float v = kv.Value/(cp*1.2f + 0.5f);
                _cong[kv.Key]=v; sum+=v; cells++;
                if(v>_maxCong) _maxCong=v;
            }
            _avgCong = cells>0? sum/cells : 0f;

            // V9.0.8 地铁分流：每座地铁站全城拥堵 ×(1-18%)，4 座封顶缓解 65%
            int sub = 0;
            foreach (var b in S.Buildings) if (b != null && b.Type == "subway") sub++;
            float relief = Mathf.Min(0.65f, sub * 0.18f);
            if (relief > 0f && _cong.Count > 0)
            {
                var keys = new List<int>(_cong.Keys);
                foreach (var k in keys) _cong[k] *= (1f - relief);
                _avgCong *= (1f - relief);
                _maxCong *= (1f - relief);
            }
        }
        public float AvgCongestion => _avgCong;
        public float MaxCongestion => _maxCong;

        /// <summary>所在格拥堵系数（>0.8 视为拥堵）。</summary>
        private float CongestionAt(float x,float z) =>
            _cong.TryGetValue(CellKey(x,z), out float v) ? v : 0f;

        // ---------- 运动 ----------
        private bool LandOK(float x, float z)
        {
            if (_terrain == null) return true;
            bool br = GM.Bridge != null && GM.Bridge.IsBridgeAt(x, z);
            return (br || !_terrain.IsWater(x, z)) && _terrain.InsideFrontier(x, z);
        }
        // 就近道路等级车速倍率（找不到路则略慢于道路）
        private float RoadSpeedAt(float x,float z)
        {
            float best = 0.85f, bestD = 3.6f*3.6f;
            foreach(var b in S.Buildings)
            {
                if(!IsRoad(b.Type)) continue;
                float dx=b.X-x, dz=b.Z-z, d2=dx*dx+dz*dz;
                if(d2>bestD) continue;
                float m = b.Type switch
                {
                    "interchange" => 1.7f,
                    "highway_modern" => 1.6f,
                    "arterial" => 1.35f,
                    "highway" => 1.15f,
                    _ => 1.0f
                };
                if(m>best){ best=m; bestD=d2; }
            }
            return best;
        }
        private void PickWander(Car c)
        {
            c.Wander -= 1f; if (c.Wander > 0) return;
            c.Wander = 3f;
            // V9.1.1 车辆只在道路上：目标点必须落在铺装公路（建筑道路 / 城际 4 车道马路）上，否则原地停车，绝不越野
            for (int k = 0; k < 10; k++)
            {
                float tx, tz;
                if (GM.Intercity != null && GM.Intercity.HasRoads && Random.value < 0.7f)
                {
                    if (GM.Intercity.RandomRoadPoint(out var rp)) { tx = rp.x; tz = rp.z; }
                    else if (SnapRoad(c.X, c.Z, 120f, out var sp)) { tx = sp.x; tz = sp.y; }
                    else { c.TX = c.X; c.TZ = c.Z; return; }
                }
                else
                {
                    var roads = new List<BuildingEntity>();
                    foreach (var b in S.Buildings) if (IsRoad(b.Type)) roads.Add(b);
                    if (roads.Count == 0) { c.TX = c.X; c.TZ = c.Z; return; } // 没有道路：停车，不越野
                    var rd = roads[Random.Range(0, roads.Count)];
                    tx = rd.X + Random.Range(-4f, 4f); tz = rd.Z + Random.Range(-2f, 2f);
                }
                if (SnapRoad(tx, tz, 8f, out var on)) { c.TX = on.x; c.TZ = on.y; return; }
            }
            c.TX = c.X; c.TZ = c.Z;
        }
        private void TickCars(float dt)
        {
            foreach (var c in _cars)
            {
                if (c.View == null) continue;
                float dx = c.TX - c.X, dz = c.TZ - c.Z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist < 2f) { PickWander(c); continue; }
                float ux = dx / dist, uz = dz / dist;
                float roadMul = RoadSpeedAt(c.X,c.Z);
                float cong = CongestionAt(c.X,c.Z);
                float congMul = cong>0.8f ? 0.45f : 1f;
                float step = Mathf.Min(1.8f, c.Speed * roadMul * congMul * dt);
                float nx = c.X + ux * step, nz = c.Z + uz * step;
                if (!LandOK(nx, nz))
                {   // 贴岸转向，绝不下水
                    bool slid = false;
                    for (int turn = 30; turn <= 150 && !slid; turn += 30)
                    {
                        float rad = turn * Mathf.Deg2Rad, cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
                        for (int side = -1; side <= 1 && !slid; side += 2)
                        {
                            float rx = ux * cs - side * uz * sn, rz = uz * cs + side * ux * sn;
                            float sx = c.X + rx * step, sz = c.Z + rz * step;
                            if (LandOK(sx, sz)) { nx = sx; nz = sz; slid = true; }
                        }
                    }
                    if (!slid) { PickWander(c); continue; }
                }
                // V9.1.1 硬约束：始终贴在铺装公路中线；离开道路（越野/下水）则投影回最近道路，无路可走则停车重选
                if (SnapRoad(nx, nz, 7f, out var rp)) { nx = rp.x; nz = rp.y; }
                else { PickWander(c); continue; }
                c.X = nx; c.Z = nz;
                c.H = (GM.Bridge != null && GM.Bridge.IsBridgeAt(c.X, c.Z)) ? GM.Bridge.DeckHeightAt(c.X, c.Z) : _terrain.HeightAt(c.X, c.Z);
                c.View.transform.position = new Vector3(c.X, c.H, c.Z);
                float yaw = Mathf.Atan2(c.TX - c.X, c.TZ - c.Z) * Mathf.Rad2Deg;
                c.View.transform.rotation = Quaternion.Slerp(c.View.transform.rotation, Quaternion.Euler(0, yaw, 0), 0.2f);
            }
        }
        // ===== V9.0.6 应急车辆：最近站点 →（沿道路）→ 事件点 → 处置 → 返回站点 =====
        private BuildingEntity NearestStation(string stationType, float x, float z)
        {
            BuildingEntity best = null; float bd = float.MaxValue;
            foreach (var b in S.Buildings)
            {
                if (b == null || b.MapId != "home" || b.Type != stationType) continue;
                float d = (b.X-x)*(b.X-x)+(b.Z-z)*(b.Z-z);
                if (d < bd) { bd = d; best = b; }
            }
            return best;
        }
        private bool NearestRoadPoint(float x, float z, out Vector2 pt)
        {
            // V9.1.1 同时识别建筑道路与城际 4 车道马路（地球模式道路是纯视图，不在 S.Buildings）
            bool found = SnapRoad(x, z, 120f, out pt);
            return found;
        }
        /// <summary>城市事件触发：从最近的对应站点派出应急车。stationType=fire_station/police_station/hospital。</summary>
        public bool DispatchEmergency(string stationType, float tx, float tz)
        {
            if (_terrain == null || _em.Count >= MAX_EMERGENCY) return false;
            var st = NearestStation(stationType, tx, tz);
            if (st == null) return false;
            int kind = stationType == "fire_station" ? 3 : stationType == "police_station" ? 4 : 5;
            var view = BuildCar(Color.white, kind);
            view.transform.SetParent(Root, false);
            var e = new Emergency { Kind = kind, View = view, X = st.X, Z = st.Z,
                H = _terrain.HeightAt(st.X, st.Z), Phase = 0 };
            // 赶赴路径：站点 → 就近道路 → 事件点就近道路 → 事件点
            if (NearestRoadPoint(st.X, st.Z, out var r1)) e.Path.Add(r1);
            if (NearestRoadPoint(tx, tz, out var r2)) e.Path.Add(r2);
            e.Path.Add(new Vector2(tx, tz));
            view.transform.position = new Vector3(e.X, e.H, e.Z);
            var tbr = view.transform.Find("BeaconR"); var tbb = view.transform.Find("BeaconB");
            if (tbr != null) { e.BeaconR = tbr.gameObject; }
            if (tbb != null) { e.BeaconB = tbb.gameObject; }
            _em.Add(e);
            return true;
        }
        private void TickEmergency(float dt)
        {
            for (int i = _em.Count - 1; i >= 0; i--)
            {
                var e = _em[i];
                if (e.View == null) { _em.RemoveAt(i); continue; }
                // 生命周期硬兜底：单车 150 模拟秒未归队即视为返队销毁，防止极端卡位耗尽出动配额
                if (e.Phase != 1) { e.Age += dt; if (e.Age > 150f) { Object.Destroy(e.View); _em.RemoveAt(i); _emDone++; continue; } }
                // 警灯闪烁
                e.Flash += dt;
                bool lit = Mathf.FloorToInt(e.Flash / 0.22f) % 2 == 0;
                if (e.BeaconR != null) e.BeaconR.SetActive(lit);
                if (e.BeaconB != null) e.BeaconB.SetActive(!lit);

                if (e.Phase == 1)   // 现场处置
                {
                    e.T -= dt;
                    if (e.T <= 0f)
                    {
                        // 返回路径：事件点 → 就近道路 → 最近同类站点
                        e.Phase = 2; e.Wp = 0; e.Path.Clear();
                        if (NearestRoadPoint(e.X, e.Z, out var rb)) e.Path.Add(rb);
                        var st = NearestStation(e.Kind == 3 ? "fire_station" : e.Kind == 4 ? "police_station" : "hospital", e.X, e.Z);
                        if (st != null) e.Path.Add(new Vector2(st.X, st.Z));
                        else { Object.Destroy(e.View); _em.RemoveAt(i); _emDone++; continue; }
                    }
                    continue;
                }
                if (e.Wp >= e.Path.Count)
                {
                    if (e.Phase == 0) { e.Phase = 1; e.T = 3f; continue; }       // 抵达事件点
                    Object.Destroy(e.View); _em.RemoveAt(i); _emDone++; continue; // 回到站点
                }
                var wp = e.Path[e.Wp];
                float dx = wp.x - e.X, dz = wp.y - e.Z;
                float dist = Mathf.Sqrt(dx*dx + dz*dz);
                if (dist < 3.2f) { e.Wp++; e.Stuck = 0f; continue; }
                float ux = dx/dist, uz = dz/dist;
                float speed = 10f * RoadSpeedAt(e.X, e.Z);
                float step = Mathf.Min(2.4f, speed * dt);
                float nx = e.X + ux*step, nz = e.Z + uz*step;
                if (!LandOK(nx, nz))
                {
                    bool slid = false;
                    for (int turn = 30; turn <= 150 && !slid; turn += 30)
                    {
                        float rad = turn*Mathf.Deg2Rad, cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
                        for (int side = -1; side <= 1 && !slid; side += 2)
                        {
                            float rx = ux*cs - side*uz*sn, rz = uz*cs + side*ux*sn;
                            float sx = e.X + rx*step, sz = e.Z + rz*step;
                            if (LandOK(sx, sz)) { nx = sx; nz = sz; slid = true; }
                        }
                    }
                    if (!slid) { e.Stuck += dt; if (e.Stuck > 4f) { e.Wp++; e.Stuck = 0f; } continue; }
                }
                float beforeX = e.X, beforeZ = e.Z;
                e.X = nx; e.Z = nz;
                if (Mathf.Abs(e.X-beforeX)+Mathf.Abs(e.Z-beforeZ) < step*0.25f) { e.Stuck += dt; if (e.Stuck > 4f) { e.Wp++; e.Stuck = 0f; } }
                else e.Stuck = 0f;
                e.H = (GM.Bridge != null && GM.Bridge.IsBridgeAt(e.X, e.Z)) ? GM.Bridge.DeckHeightAt(e.X, e.Z) : _terrain.HeightAt(e.X, e.Z);
                e.View.transform.position = new Vector3(e.X, e.H, e.Z);
                float yaw = Mathf.Atan2(dx, dz)*Mathf.Rad2Deg;
                e.View.transform.rotation = Quaternion.Slerp(e.View.transform.rotation, Quaternion.Euler(0, yaw, 0), 0.25f);
            }
        }

        /// <summary>V9.0.9 航线飞机：机场对间直线往返（巡航→直线进近降落→停靠→选新目的地起飞爬升）。</summary>
        private void TickRoutePlane(Plane p, float dt)
        {
            const float speed = 16f;
            float ground = _terrain != null ? _terrain.HeightAt(p.CX, p.CZ) : 0f;
            if (p.Phase == 0) // 高空直飞目的地
            {
                float dx = p.TX - p.CX, dz = p.TZ - p.CZ;
                float dist = Mathf.Sqrt(dx*dx + dz*dz);
                if (dist > 0.01f)
                {
                    p.Ang = Mathf.Atan2(dx, dz);
                    float step = Mathf.Min(dist, speed*dt);
                    p.CX += dx/dist*step; p.CZ += dz/dist*step;
                }
                if (dist <= 1.2f) { p.Phase = 1; p.PT = 0f; }
                ground = _terrain != null ? _terrain.HeightAt(p.CX, p.CZ) : 0f;
                p.View.transform.position = new Vector3(p.CX, ground + p.CruiseAlt, p.CZ);
                p.View.transform.rotation = Quaternion.Slerp(p.View.transform.rotation, Quaternion.Euler(0, p.Ang*Mathf.Rad2Deg, 6f), 0.06f);
                return;
            }
            if (p.Phase == 1) // 8 秒直线进近下降
            {
                p.PT = Mathf.Clamp01(p.PT + dt/8f);
                float e = p.PT*p.PT*(3f-2f*p.PT);
                float back = 8f*(1f-e);
                float x = p.TX - Mathf.Sin(p.Ang)*back, z = p.TZ - Mathf.Cos(p.Ang)*back;
                float alt = Mathf.Lerp(p.CruiseAlt, 1.2f, e);
                float gy = _terrain != null ? _terrain.HeightAt(x, z) : 0f;
                p.CX = x; p.CZ = z;
                p.View.transform.position = new Vector3(x, gy + alt, z);
                p.View.transform.rotation = Quaternion.Slerp(p.View.transform.rotation, Quaternion.Euler(0, p.Ang*Mathf.Rad2Deg, 0f), 0.1f);
                if (p.PT >= 1f) { p.Phase = 2; p.PT = 5f; p.CX = p.TX; p.CZ = p.TZ; }
                return;
            }
            if (p.Phase == 2) // 地面停靠 5 秒，随后选另一座机场
            {
                p.PT -= dt;
                float gy0 = _terrain != null ? _terrain.HeightAt(p.TX, p.TZ) : 0f;
                p.View.transform.position = new Vector3(p.TX, gy0 + 1.2f, p.TZ);
                if (p.PT <= 0f)
                {
                    var aps = Airports();
                    BuildingEntity here = null;
                    foreach (var a in aps) if (Mathf.Abs(a.X-p.TX)<2f && Mathf.Abs(a.Z-p.TZ)<2f) { here = a; break; }
                    BuildingEntity dest = null;
                    for (int k=0;k<6 && aps.Count>1;k++)
                    {
                        var cand = aps[Random.Range(0, aps.Count)];
                        if (cand != here) { dest = cand; break; }
                    }
                    if (dest == null) // 机场被拆到不足 2 座：退回单机盘旋
                    {
                        p.Route = false; p.CX = p.TX; p.CZ = p.TZ;
                        p.R = p.R0 = 24f; p.Alt = p.Alt0 = p.CruiseAlt; p.AngV = Random.Range(0.10f,0.18f);
                        p.Phase = 0; p.PT = Random.Range(18f, 40f);
                        return;
                    }
                    p.HX = p.TX; p.HZ = p.TZ; p.TX = dest.X; p.TZ = dest.Z;
                    p.Ang = Mathf.Atan2(p.TX-p.HX, p.TZ-p.HZ);
                    p.Phase = 3; p.PT = 0f;
                }
                return;
            }
            // 起飞：8 秒沿新航向滑出 8 单位并爬升到巡航层
            p.PT = Mathf.Clamp01(p.PT + dt/8f);
            float outE = p.PT*p.PT;
            float ox = p.HX + Mathf.Sin(p.Ang)*8f*p.PT, oz = p.HZ + Mathf.Cos(p.Ang)*8f*p.PT;
            float oAlt = Mathf.Lerp(1.2f, p.CruiseAlt, outE);
            float og = _terrain != null ? _terrain.HeightAt(ox, oz) : 0f;
            p.CX = ox; p.CZ = oz;
            p.View.transform.position = new Vector3(ox, og + oAlt, oz);
            p.View.transform.rotation = Quaternion.Slerp(p.View.transform.rotation, Quaternion.Euler(0, p.Ang*Mathf.Rad2Deg, 6f), 0.1f);
            if (p.PT >= 1f) { p.Phase = 0; p.PT = 0f; }
        }

        private void TickPlanes(float dt)
        {
            foreach (var p in _planes)
            {
                if (p.View == null) continue;
                if (p.Route) { TickRoutePlane(p, dt); continue; }
                float ground0 = _terrain != null ? _terrain.HeightAt(p.CX, p.CZ) : 0f;
                if (p.Phase == 0) // 巡航盘旋
                {
                    p.Ang += p.AngV * dt;
                    p.PT -= dt;
                    if (p.PT <= 0f) { p.Phase = 1; p.PT = 0f; }
                }
                else if (p.Phase == 1) // 降落：螺旋收半径、降高度，8 秒落到跑道
                {
                    p.PT = Mathf.Clamp01(p.PT + dt/8f);
                    p.Ang += p.AngV * dt * (1f - p.PT*0.6f);
                    if (p.PT >= 1f) { p.Phase = 2; p.PT = 5f; p.R = 0f; p.Alt = 1.2f; }
                }
                else if (p.Phase == 2) // 地面停靠 5 秒
                {
                    p.PT -= dt;
                    if (p.PT <= 0f) { p.Phase = 3; p.PT = 0f; }
                }
                else // 起飞：螺旋放半径、爬高度，8 秒回到巡航层
                {
                    p.PT = Mathf.Clamp01(p.PT + dt/8f);
                    p.Ang += p.AngV * dt * (0.4f + p.PT*0.6f);
                    if (p.PT >= 1f) { p.Phase = 0; p.PT = Random.Range(22f, 46f); p.R = p.R0; p.Alt = p.Alt0; }
                }
                float ease = p.Phase == 1 ? p.PT*p.PT*(3f-2f*p.PT) : p.Phase == 3 ? p.PT*p.PT : 0f;
                float radial = p.Phase == 1 ? Mathf.Lerp(p.R0, 0f, ease) : p.Phase == 3 ? Mathf.Lerp(0f, p.R0, ease) : (p.Phase == 2 ? 0f : p.R);
                float alt    = p.Phase == 1 ? Mathf.Lerp(p.Alt0, 1.2f, ease) : p.Phase == 3 ? Mathf.Lerp(1.2f, p.Alt0, ease) : (p.Phase == 2 ? 1.2f : p.Alt);
                p.R = radial; p.Alt = alt;
                float x = p.CX + Mathf.Cos(p.Ang)*radial, z = p.CZ + Mathf.Sin(p.Ang)*radial;
                float vx = -Mathf.Sin(p.Ang), vz = Mathf.Cos(p.Ang);
                float yaw = Mathf.Atan2(vx, vz)*Mathf.Rad2Deg;
                float ground = _terrain != null ? _terrain.HeightAt(x, z) : 0f;
                float bank = p.Phase == 2 ? 0f : 12f;
                p.View.transform.position = new Vector3(x, (p.Phase==2?ground0:ground) + alt, z);
                p.View.transform.rotation = Quaternion.Slerp(p.View.transform.rotation, Quaternion.Euler(0, yaw, bank), 0.05f);
            }
        }

        // ---------- V9.0.6 探针/调试 ----------
        public int ActiveEmergency => _em.Count;
        public int EmergencyDone => _emDone;
        public string FlightInfo()
        {
            int cruise=0, land=0, ground=0, takeoff=0, route=0;
            foreach (var p in _planes)
            { if (p.Route) route++; if (p.Phase==0) cruise++; else if (p.Phase==1) land++; else if (p.Phase==2) ground++; else takeoff++; }
            return $"planes={_planes.Count}(航线{route}/巡航{cruise}/降落{land}/地面{ground}/起飞{takeoff}) airports={Airports().Count}";
        }
        /// <summary>探针：补齐 3 架并强制【盘旋机】进入降落，便于回归起降循环；航线机走自身往返状态机。</summary>
        public void DebugForceFlightCycle()
        {
            BuildingEntity ap = null;
            foreach (var b in S.Buildings) if (b.Type == "airport") { ap = b; break; }
            if (ap == null) return;
            int circling = 0; foreach (var p in _planes) if (!p.Route) circling++;
            int guard = 0;
            while (circling < 3 && guard++ < 6) { SpawnPlane(ap, null); circling++; }
            foreach (var p in _planes) if (!p.Route) { p.Phase = 1; p.PT = 0f; }
        }

        // ---------- 探针诊断 ----------
        public string Diagnose()
        {
            if(S==null) return "[ROAD] no state";
            int cars=0,trucks=0,buses=0,badWater=0,emWater=0;
            foreach(var c in _cars){
                if(c.Kind==1)trucks++; else if(c.Kind==2)buses++; else cars++;
                if(_terrain!=null && _terrain.IsWater(c.X,c.Z) && !(GM.Bridge!=null&&GM.Bridge.IsBridgeAt(c.X,c.Z))) badWater++;
            }
            int fire=0,police=0,amb=0;
            foreach(var e in _em){
                if(e.Kind==3)fire++; else if(e.Kind==4)police++; else amb++;
                if(_terrain!=null && _terrain.IsWater(e.X,e.Z) && !(GM.Bridge!=null&&GM.Bridge.IsBridgeAt(e.X,e.Z))) emWater++;
            }
            var sb=new StringBuilder();
            sb.Append($"[ROAD] year={S.Year}(AD{S.Year-3000}) era={S.Era} pop={S.Pop} | ");
            sb.Append($"roads total={RoadCount()} 驰道={CountRoad("road")} 大马路={CountRoad("highway")} 主干道={CountRoad("arterial")} 高速={CountRoad("highway_modern")} 立交={CountRoad("interchange")} | ");
            sb.Append($"veh car={cars} truck={trucks}(want{WantTrucks()}) bus={buses}(want{WantBuses()}) inWater={badWater} | cong avg={_avgCong:F2} max={_maxCong:F2} subway={SubwayCount()}(relief{Mathf.Min(0.65f,SubwayCount()*0.18f):F2})");
            sb.Append($" | EM active={_em.Count}(消防{fire}/警{police}/救护{amb}) done={_emDone} emInWater={emWater} | {FlightInfo()}");
            return sb.ToString();
        }
    }
}
