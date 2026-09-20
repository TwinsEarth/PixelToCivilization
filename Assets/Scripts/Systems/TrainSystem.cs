using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.0.1 五代铁路系统（纯装饰、有上限、不进存档；视图挂在 "Trains" 根，随清局销毁，丢失后自动重建）。
    /// 按公元年代（游戏年-3000）自动升级机车与轨道形制：
    ///   1800 蒸汽机车(木枕道砟+黑烟) / 1900 内燃机车(混凝土枕) / 1950 电力机车(接触网) /
    ///   1990 高速列车(高架无砟桥+接触网) / 2010 磁悬浮(高架导轨梁、悬浮、无轮无网)。
    /// 系统在聚落旁陆地自建直线铁路廊道并让列车往返；与 ModernTraffic 同范式，只在陆地/疆域内铺设。
    /// </summary>
    public class TrainSystem : GameSystemBase
    {
        private Transform _root;
        private WorldGenerator _terrain;
        private float _spawnCd;
        private int _tickTicks, _earlyReturn, _manageTicks;
        private readonly List<Corridor> _lines = new();
        private readonly List<Puff> _puffs = new();

        // 轨道材质
        private Material _ballast, _ballastClean, _sleeperWood, _sleeperConc, _rail, _conc, _concDark, _wire;
        // 列车常用材质
        private Material _black, _dark, _steel, _glass, _white, _silver, _blue, _green, _red, _brown, _orange;

        private class Corridor
        {
            public GameObject View; public Transform Train;
            public int Tier; public bool AxisX; public float L;
            public float U; public int Dir = 1; public float Speed; public float SmokeCd;
        }
        private class Puff { public Transform T; public float Age; }

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _terrain = Object.FindObjectOfType<WorldGenerator>();
            _ballast     = ShaderHelper.Pbr(new Color(0.30f, 0.27f, 0.24f), 0f, 0.1f, 1201, 0.6f);
            _ballastClean= ShaderHelper.Pbr(new Color(0.40f, 0.39f, 0.37f), 0f, 0.1f, 1202, 0.6f);
            _sleeperWood = ShaderHelper.Pbr(new Color(0.34f, 0.24f, 0.16f), 0f, 0.2f, 1203, 0.6f);
            _sleeperConc = ShaderHelper.Pbr(new Color(0.62f, 0.61f, 0.58f), 0f, 0.15f, 1204, 0.5f);
            _rail        = ShaderHelper.Pbr(new Color(0.45f, 0.47f, 0.50f), 0.8f, 0.5f, 1205, 0.3f);
            _conc        = ShaderHelper.Pbr(new Color(0.78f, 0.78f, 0.75f), 0f, 0.12f, 1206, 0.5f);
            _concDark    = ShaderHelper.Pbr(new Color(0.55f, 0.55f, 0.53f), 0f, 0.12f, 1207, 0.5f);
            _wire        = ShaderHelper.Pbr(new Color(0.20f, 0.20f, 0.22f), 0.6f, 0.4f, 1208, 0.2f);
            _black = ShaderHelper.Pbr(new Color(0.08f, 0.08f, 0.09f), 0.3f, 0.45f, 1210, 0.5f);
            _dark  = ShaderHelper.Pbr(new Color(0.16f, 0.16f, 0.18f), 0.3f, 0.4f, 1211, 0.5f);
            _steel = ShaderHelper.Pbr(new Color(0.66f, 0.69f, 0.73f), 0.7f, 0.5f, 1212, 0.3f);
            _glass = ShaderHelper.Pbr(new Color(0.30f, 0.55f, 0.78f), 0.1f, 0.9f, 1213, 0.2f);
            _white = ShaderHelper.Pbr(new Color(0.93f, 0.94f, 0.95f), 0.1f, 0.5f, 1214, 0.5f);
            _silver= ShaderHelper.Pbr(new Color(0.78f, 0.80f, 0.83f), 0.6f, 0.55f, 1215, 0.3f);
            _blue  = ShaderHelper.Pbr(new Color(0.12f, 0.38f, 0.72f), 0.2f, 0.5f, 1216, 0.5f);
            _green = ShaderHelper.Pbr(new Color(0.18f, 0.45f, 0.24f), 0.1f, 0.3f, 1217, 0.6f);
            _red   = ShaderHelper.Pbr(new Color(0.70f, 0.14f, 0.12f), 0.1f, 0.3f, 1218, 0.6f);
            _brown = ShaderHelper.Pbr(new Color(0.42f, 0.30f, 0.20f), 0f, 0.2f, 1219, 0.6f);
            _orange= ShaderHelper.Pbr(new Color(0.85f, 0.45f, 0.12f), 0.1f, 0.3f, 1220, 0.6f);
        }

        private Transform Root => _root = EntityViewFactory.EnsureRoot("Trains", GM.transform);

        /// <summary>五代机车：0=无(1800前) 1蒸汽 2内燃 3电力 4高铁 5磁悬浮</summary>
        public static int TierForYear(int gameYear)
        {
            int ad = gameYear - 3000;
            if (ad < 1800) return 0;
            if (ad < 1900) return 1;
            if (ad < 1950) return 2;
            if (ad < 1990) return 3;
            if (ad < 2010) return 4;
            return 5;
        }
        public static string TierName(int t) => t switch
        {
            1 => "蒸汽机车(1800)", 2 => "内燃机车(1900)", 3 => "电力机车(1950)",
            4 => "高速列车(1990)", 5 => "磁悬浮(2010)", _ => "无铁路"
        };
        public int LineCount => _lines.Count;
        public int CurrentTier => TierForYear(S != null ? S.Year : 0);
        /// <summary>立即按当前年代补建/重建铁路廊道（供时代跃迁、Debug 跳年后即时生效，不必等 3 秒冷却）。</summary>
        public void RefreshNow() { if (S != null && _terrain != null) ManageLines(); }
        private int TargetLines(int tier) => tier <= 2 ? 1 : tier == 3 ? 2 : 3;
        private float TierSpeed(int tier) => tier == 1 ? 7f : tier == 2 ? 12f : tier == 3 ? 18f : tier == 4 ? 36f : 46f;

        public override void Tick(float dt)
        {
            _tickTicks++;
            if (S == null || _terrain == null) { _earlyReturn++; return; }
            _spawnCd -= dt;
            if (_spawnCd <= 0f) { _spawnCd = 3f; ManageLines(); }
            TickTrains(dt);
            TickSmoke(dt);
        }

        public override void OnEra(int n, int o) { base.OnEra(n, o); RefreshNow(); }

        private void ManageLines()
        {
            _manageTicks++;
            // V9.1.1 地球模式：铁路只跨海峡，由 IntercityNetworkSystem 管理；停用聚落旁陆地廊道（大陆内部 0 铁路）。
            if (_terrain != null && _terrain.EarthMode)
            {
                if (_lines.Count > 0)
                {
                    foreach (var l in _lines) if (l.View != null) Object.Destroy(l.View);
                    _lines.Clear();
                }
                return;
            }
            if (S.Buildings.Count == 0) return;
            _lines.RemoveAll(l => l.View == null);
            int tier = TierForYear(S.Year);
            if (tier == 0) { foreach (var l in _lines) if (l.View != null) Object.Destroy(l.View); _lines.Clear(); return; }
            // 年代升级：重建全部廊道（轨道+机车形制随时代）
            foreach (var l in _lines)
                if (l.Tier != tier) { if (l.View != null) Object.Destroy(l.View); l.View = null; }
            _lines.RemoveAll(l => l.View == null);
            int want = TargetLines(tier);
            int guard = 0;
            while (_lines.Count < want && guard++ < 6) { if (!TryCreateLine(tier)) break; }
        }

        private bool TryCreateLine(int tier)
        {
            bool viaduct = tier >= 4;
            float maxBand = viaduct ? 5f : 1.4f;
            for (int k = 0; k < 24; k++)
            {
                var anchor = S.Buildings[Random.Range(0, S.Buildings.Count)];
                bool axisX = Random.value < 0.5f;
                float cx = anchor.X + Random.Range(-8f, 8f);
                float cz = anchor.Z + Random.Range(-8f, 8f);
                // V9.0.1 廊道长度按可用陆地自适应：从中心向两侧逐环生长，遇水/越界/起伏超限即止；
                // 小岛建短线(半长≥18)，疆域扩张后 ManageLines 补建/重建为长线(上限52)，避免“太难/太少”。
                GrowHalf(cx, cz, axisX, maxBand, out float half, out float topH);
                if (half < 18f) continue;
                float L = Mathf.Min(52f, half);
                float trackY = topH + (viaduct ? 1.3f : 0.22f);

                var view = new GameObject(axisX ? "RailX" : "RailZ");
                view.transform.SetParent(Root, false);
                view.transform.position = new Vector3(cx, trackY, cz);
                view.transform.rotation = Quaternion.Euler(0, axisX ? 90f : 0f, 0);
                BuildTrack(view.transform, tier, L);
                var train = BuildTrain(view.transform, tier);
                _lines.Add(new Corridor
                {
                    View = view, Train = train, Tier = tier, AxisX = axisX, L = L,
                    U = Random.Range(-L * 0.5f, L * 0.5f), Dir = 1, Speed = TierSpeed(tier)
                });
                return true;
            }
            return false;
        }

        /// <summary>从 (cx,cz) 沿轴向两侧逐环生长直线廊道，返回能连续保持陆地/疆域内/起伏≤maxBand 的最大半长 half 与该范围最高地面 topH。</summary>
        private void GrowHalf(float cx, float cz, bool axisX, float maxBand, out float half, out float topH)
        {
            float minH = float.MaxValue, maxH = float.MinValue; half = 0f;
            for (float step = 0f; step <= 52f; step += 2f)
            {
                bool ring = true;
                foreach (float sgn in step <= 0.01f ? new[]{1f} : new[]{-1f,1f})
                {
                    float u = sgn * step;
                    float wx = axisX ? cx + u : cx, wz = axisX ? cz : cz + u;
                    if (_terrain.IsWater(wx, wz) || !_terrain.InsideFrontier(wx, wz)) { ring = false; break; }
                    float hh = _terrain.HeightAt(wx, wz);
                    if (hh < minH) minH = hh; if (hh > maxH) maxH = hh;
                }
                if (!ring) break;
                if (maxH - minH > maxBand) break;
                half = step;
            }
            topH = maxH == float.MinValue ? 0f : maxH;
        }

        /// <summary>V9.0.1 浏览器诊断：遍历所有建筑×两轴向，统计可建廊道的最大半长与达标(≥18)数量，定位“建不出铁路”根因。</summary>
        public string Diagnose()
        {
            if (_terrain == null) return "terrain=null";
            int tier = TierForYear(S.Year); float maxBand = tier >= 4 ? 5f : 1.4f;
            float best = 0f; int ok = 0, waterBlock = 0, bandBlock = 0;
            foreach (var b in S.Buildings)
                foreach (bool axisX in new[]{true,false})
                {
                    GrowHalf(b.X, b.Z, axisX, maxBand, out float h, out _);
                    if (h > best) best = h;
                    if (h >= 18f) ok++;
                    else if (h <= 0.01f) waterBlock++;
                    else bandBlock++;
                }
            int before = _lines.Count;
            bool t1 = TryCreateLine(tier), t2 = TryCreateLine(tier), t3 = TryCreateLine(tier);
            return $"[TRAINDIAG] id={GetInstanceID()} tier={tier} buildings={S.Buildings.Count} bestHalf={best:F0} okAxes={ok} " +
                   $"waterBlock={waterBlock} bandBlock={bandBlock} activeHalf={_terrain.ActiveHalf:F0} " +
                   $"ticks={_tickTicks} early={_earlyReturn} manage={_manageTicks} lines={_lines.Count} " +
                   $"directTry=({t1},{t2},{t3}) lines {before}->{_lines.Count}";
        }

        // ================= 轨道（局部坐标，z 轴为线路方向，轨面 y=0）=================
        private void BuildTrack(Transform p, int tier, float L)
        {
            float len = 2f * L + 2f;
            if (tier == 5)
            {   // 磁悬浮：高架混凝土导轨梁 + 侧面导向轨，无接触网、无钢轨
                Box("Beam", new Vector3(0, -0.25f, 0), new Vector3(1.9f, 0.7f, len), _conc, p);
                Box("GuideL", new Vector3(-0.72f, 0.16f, 0), new Vector3(0.18f, 0.22f, len), _dark, p);
                Box("GuideR", new Vector3(0.72f, 0.16f, 0), new Vector3(0.18f, 0.22f, len), _dark, p);
                for (float z = -L; z <= L; z += 12f) Cyl("Pillar", new Vector3(0, -1.5f, z), new Vector3(0.5f, 2.2f, 0.5f), _concDark, p);
                return;
            }
            if (tier == 4)
            {   // 高铁：高架无砟桥 + 接触网
                Box("Deck", new Vector3(0, -0.2f, 0), new Vector3(2.7f, 0.5f, len), _conc, p);
                for (float z = -L; z <= L; z += 12f) Cyl("Pillar", new Vector3(0, -1.4f, z), new Vector3(0.55f, 2.0f, 0.55f), _concDark, p);
                Box("Slab", new Vector3(0, 0.08f, 0), new Vector3(2.0f, 0.12f, len), _concDark, p);
                Box("RailL", new Vector3(-0.55f, 0.18f, 0), new Vector3(0.12f, 0.12f, len), _rail, p);
                Box("RailR", new Vector3(0.55f, 0.18f, 0), new Vector3(0.12f, 0.12f, len), _rail, p);
                BuildCatenary(p, L);
                return;
            }
            if (tier == 3)
            {   // 电力：混凝土整体道床 + 接触网
                Box("Slab", new Vector3(0, -0.16f, 0), new Vector3(2.3f, 0.22f, len), _conc, p);
                Box("RailL", new Vector3(-0.55f, 0.02f, 0), new Vector3(0.12f, 0.12f, len), _rail, p);
                Box("RailR", new Vector3(0.55f, 0.02f, 0), new Vector3(0.12f, 0.12f, len), _rail, p);
                BuildCatenary(p, L);
                return;
            }
            // 蒸汽 / 内燃：道砟 + 枕木 + 双轨
            Box("Ballast", new Vector3(0, -0.28f, 0), new Vector3(2.4f, 0.34f, len), tier == 1 ? _ballast : _ballastClean, p);
            var sleeper = tier == 1 ? _sleeperWood : _sleeperConc;
            for (float z = -L; z <= L; z += 3f)
                Box("Sleeper", new Vector3(0, -0.07f, z), new Vector3(2.0f, 0.1f, 0.34f), sleeper, p);
            Box("RailL", new Vector3(-0.55f, 0.03f, 0), new Vector3(0.12f, 0.12f, len), _rail, p);
            Box("RailR", new Vector3(0.55f, 0.03f, 0), new Vector3(0.12f, 0.12f, len), _rail, p);
        }
        private void BuildCatenary(Transform p, float L)
        {
            for (float z = -L; z <= L; z += 9f)
            {
                Cyl("Mast", new Vector3(1.15f, 1.2f, z), new Vector3(0.07f, 2.4f, 0.07f), _dark, p);
                Box("Arm", new Vector3(0.58f, 2.35f, z), new Vector3(1.25f, 0.07f, 0.07f), _dark, p);
            }
            Box("Wire", new Vector3(0f, 2.4f, 0), new Vector3(0.03f, 0.03f, 2f * L + 2f), _wire, p);
        }

        // ================= 列车 =================
        private Transform BuildTrain(Transform parent, int tier)
        {
            var go = new GameObject("Train");
            go.transform.SetParent(parent, false);
            switch (tier)
            {
                case 1: BuildSteam(go.transform); break;
                case 2: BuildDiesel(go.transform); break;
                case 3: BuildElectric(go.transform); break;
                case 4: BuildHsTrain(go.transform, false); break;
                case 5: BuildHsTrain(go.transform, true); break;
            }
            return go.transform;
        }

        private void Wheel(Transform p, float x, float z, float r = 0.3f)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w.name = "Wheel"; w.transform.SetParent(p, false);
            w.transform.localPosition = new Vector3(x, r * 0.92f, z);
            w.transform.localScale = new Vector3(r, 0.12f, r);
            w.transform.localRotation = Quaternion.Euler(0, 0, 90);
            w.GetComponent<Renderer>().material = _dark; Object.Destroy(w.GetComponent<Collider>());
        }
        private void Bogie(Transform p, float z, float r = 0.3f)
        {
            Wheel(p, -0.5f, z, r); Wheel(p, 0.5f, z, r);
        }
        private void Coach(Transform p, float z, float len, Material body, bool windows = true)
        {
            Box("Coach", new Vector3(0, 0.95f, z), new Vector3(1.0f, 0.95f, len), body, p);
            Box("Roof", new Vector3(0, 1.46f, z), new Vector3(0.96f, 0.12f, len * 0.98f), _white, p);
            if (windows)
            {
                Box("WinBand", new Vector3(0, 1.12f, z), new Vector3(0.86f, 0.22f, len * 0.86f), _glass, p);
                Box("WinSideL", new Vector3(-0.51f, 1.12f, z), new Vector3(0.04f, 0.22f, len * 0.86f), _glass, p);
                Box("WinSideR", new Vector3(0.51f, 1.12f, z), new Vector3(0.04f, 0.22f, len * 0.86f), _glass, p);
            }
            Bogie(p, z - len * 0.32f); Bogie(p, z + len * 0.32f);
        }

        private void BuildSteam(Transform p)
        {
            // 机车（车头朝 +Z）
            Cyl("Boiler", new Vector3(0, 1.0f, 0.15f), new Vector3(0.56f, 1.55f, 0.56f), _black, p, Quaternion.Euler(90, 0, 0));
            Cyl("SmokeBox", new Vector3(0, 1.0f, 0.95f), new Vector3(0.58f, 0.25f, 0.58f), _dark, p, Quaternion.Euler(90, 0, 0));
            Cyl("Stack", new Vector3(0, 1.55f, 0.72f), new Vector3(0.13f, 0.5f, 0.13f), _black, p);
            Box("Cab", new Vector3(0, 1.0f, -0.62f), new Vector3(0.98f, 0.95f, 0.78f), _red, p);
            Box("CabRoof", new Vector3(0, 1.52f, -0.62f), new Vector3(1.04f, 0.12f, 0.84f), _black, p);
            Box("Headlamp", new Vector3(0, 1.05f, 1.06f), new Vector3(0.18f, 0.18f, 0.1f), ShaderHelper.Emissive(new Color(0.2f, 0.18f, 0.1f), new Color(1f, 0.9f, 0.5f)), p);
            foreach (float wz in new[] { 0.62f, 0f, -0.62f }) { Wheel(p, -0.58f, wz, 0.34f); Wheel(p, 0.58f, wz, 0.34f); }
            Box("RodL", new Vector3(-0.59f, 0.32f, 0f), new Vector3(0.06f, 0.06f, 1.5f), _steel, p);
            Box("RodR", new Vector3(0.59f, 0.32f, 0f), new Vector3(0.06f, 0.06f, 1.5f), _steel, p);
            // 煤水车 + 两节绿色客车
            Box("Tender", new Vector3(0, 0.85f, -1.7f), new Vector3(0.95f, 0.8f, 1.2f), _black, p);
            Box("Coal", new Vector3(0, 1.28f, -1.7f), new Vector3(0.8f, 0.25f, 0.95f), _dark, p);
            Bogie(p, -1.4f); Bogie(p, -2.0f);
            Coach(p, -3.4f, 2.2f, _green);
            Coach(p, -5.9f, 2.2f, _green);
        }

        private void BuildDiesel(Transform p)
        {
            Box("Loco", new Vector3(0, 0.95f, 0.3f), new Vector3(1.05f, 1.05f, 3.0f), _orange, p);
            Box("CabBand", new Vector3(0, 1.28f, 1.2f), new Vector3(0.98f, 0.34f, 0.7f), _glass, p);
            Box("Chassis", new Vector3(0, 0.4f, 0.3f), new Vector3(1.08f, 0.25f, 3.1f), _dark, p);
            Box("Headlamp", new Vector3(0, 0.8f, 1.83f), new Vector3(0.5f, 0.16f, 0.08f), ShaderHelper.Emissive(new Color(0.2f, 0.18f, 0.1f), new Color(1f, 0.95f, 0.7f)), p);
            Bogie(p, -0.9f); Bogie(p, 1.2f);
            Coach(p, -3.6f, 2.6f, _brown, false);   // 棚车/货车
            Coach(p, -6.5f, 2.6f, _concDark, false);
        }

        private void BuildElectric(Transform p)
        {
            Box("Loco", new Vector3(0, 1.0f, 0.2f), new Vector3(1.0f, 1.05f, 3.2f), _silver, p);
            Box("Belt", new Vector3(0, 0.75f, 0.2f), new Vector3(1.02f, 0.3f, 3.22f), _blue, p);
            Sph("Nose", new Vector3(0, 1.0f, 1.9f), new Vector3(0.5f, 0.5f, 0.42f), _silver, p);
            Box("CabWin", new Vector3(0, 1.3f, 1.35f), new Vector3(0.8f, 0.3f, 0.6f), _glass, p);
            // 受电弓
            Box("PantoBase", new Vector3(0, 1.56f, 0f), new Vector3(0.6f, 0.06f, 0.7f), _dark, p);
            Box("PantoA", new Vector3(-0.18f, 1.78f, 0f), new Vector3(0.05f, 0.5f, 0.05f), _dark, p);
            Box("PantoB", new Vector3(0.18f, 1.78f, 0f), new Vector3(0.05f, 0.5f, 0.05f), _dark, p);
            Box("PantoTop", new Vector3(0, 2.0f, 0f), new Vector3(0.6f, 0.04f, 0.12f), _dark, p);
            Bogie(p, -1.0f); Bogie(p, 1.2f);
            Coach(p, -3.6f, 2.6f, _silver);
            Coach(p, -6.5f, 2.6f, _silver);
        }

        // 高铁 / 磁悬浮：流线型白(银)车身 + 蓝色窗带，多节编组，车头球鼻
        private void BuildHsTrain(Transform p, bool maglev)
        {
            Material body = maglev ? _silver : _white;
            float noseZ = 3.3f;
            // 头车
            Box("Lead", new Vector3(0, 0.95f, 1.2f), new Vector3(0.95f, 0.95f, 3.6f), body, p);
            Sph("Nose", new Vector3(0, 0.9f, noseZ), new Vector3(0.48f, 0.46f, 0.95f), body, p);
            Box("BeltLead", new Vector3(0, 1.18f, 1.4f), new Vector3(0.82f, 0.24f, 3.0f), _blue, p);
            Box("Cockpit", new Vector3(0, 1.2f, 2.9f), new Vector3(0.7f, 0.24f, 0.7f), _glass, p);
            // 中车、尾车
            Box("Mid", new Vector3(0, 0.95f, -1.6f), new Vector3(0.95f, 0.95f, 2.6f), body, p);
            Box("BeltMid", new Vector3(0, 1.18f, -1.6f), new Vector3(0.82f, 0.24f, 2.2f), _blue, p);
            Box("Tail", new Vector3(0, 0.95f, -4.0f), new Vector3(0.95f, 0.95f, 2.2f), body, p);
            Box("BeltTail", new Vector3(0, 1.18f, -4.0f), new Vector3(0.82f, 0.24f, 1.8f), _blue, p);
            if (!maglev)
            {   // 高铁受电弓 + 转向架
                Box("PantoBase", new Vector3(0, 1.5f, -1.6f), new Vector3(0.6f, 0.06f, 0.6f), _dark, p);
                Box("PantoA", new Vector3(-0.18f, 1.7f, -1.6f), new Vector3(0.05f, 0.45f, 0.05f), _dark, p);
                Box("PantoB", new Vector3(0.18f, 1.7f, -1.6f), new Vector3(0.05f, 0.45f, 0.05f), _dark, p);
                Bogie(p, 0.2f, 0.26f); Bogie(p, 2.2f, 0.26f);
                Bogie(p, -1.6f, 0.26f); Bogie(p, -4.2f, 0.26f);
                Box("Skirt", new Vector3(0, 0.42f, 0), new Vector3(0.98f, 0.2f, 7.6f), _concDark, p);
            }
            else
            {   // 磁悬浮：包裹导轨梁的悬浮裙、无轮、悬浮间隙
                Box("SkirtL", new Vector3(-0.52f, 0.32f, 0), new Vector3(0.16f, 0.5f, 7.8f), body, p);
                Box("SkirtR", new Vector3(0.52f, 0.32f, 0), new Vector3(0.16f, 0.5f, 7.8f), body, p);
            }
        }

        // ================= 运动 =================
        private void TickTrains(float dt)
        {
            foreach (var l in _lines)
            {
                if (l.Train == null) continue;
                float lim = l.L - 4f;
                l.U += l.Dir * l.Speed * dt;
                if (l.U >= lim) { l.U = lim; l.Dir = -1; }
                else if (l.U <= -lim) { l.U = -lim; l.Dir = 1; }
                float baseY = l.Tier == 5 ? 0.55f + Mathf.Sin(UnityEngine.Time.time * 6f) * 0.03f : 0f;
                l.Train.localPosition = new Vector3(0, baseY, l.U);
                l.Train.localRotation = Quaternion.Euler(0, l.Dir > 0 ? 0f : 180f, 0);
                // 蒸汽烟
                if (l.Tier == 1)
                {
                    l.SmokeCd -= dt;
                    if (l.SmokeCd <= 0f)
                    {
                        l.SmokeCd = 0.14f;
                        EmitSmoke(l.Train);
                    }
                }
            }
        }

        private void EmitSmoke(Transform train)
        {
            if (_puffs.Count >= 20) return;
            var stack = train.Find("Stack");
            Vector3 pos = stack != null ? stack.position : train.position + train.up * 1.6f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Smoke"; Object.Destroy(go.GetComponent<Collider>());
            var m = ShaderHelper.Pbr(new Color(0.35f, 0.35f, 0.35f), 0f, 0f, 1299, 0f);
            go.GetComponent<Renderer>().material = m;
            go.transform.SetParent(Root, true);
            go.transform.position = pos; go.transform.localScale = Vector3.one * 0.22f;
            _puffs.Add(new Puff { T = go.transform, Age = 0f });
        }
        private void TickSmoke(float dt)
        {
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                var pu = _puffs[i];
                if (pu.T == null) { _puffs.RemoveAt(i); continue; }
                pu.Age += dt;
                pu.T.position += new Vector3(0.08f * dt, 1.1f * dt, 0f);
                float s = 0.22f + pu.Age * 0.5f;
                pu.T.localScale = Vector3.one * s;
                if (pu.Age >= 1.5f) { Object.Destroy(pu.T.gameObject); _puffs.RemoveAt(i); }
            }
        }

        // ================= primitive 辅助 =================
        private void Box(string n, Vector3 pos, Vector3 sc, Material m, Transform p)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n; g.transform.SetParent(p, false);
            g.transform.localPosition = pos; g.transform.localScale = sc;
            g.GetComponent<Renderer>().material = m; Object.Destroy(g.GetComponent<Collider>());
        }
        private void Cyl(string n, Vector3 pos, Vector3 sc, Material m, Transform p, Quaternion rot = default)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = n; g.transform.SetParent(p, false);
            g.transform.localPosition = pos; g.transform.localScale = sc;
            if (rot != default) g.transform.localRotation = rot;
            g.GetComponent<Renderer>().material = m; Object.Destroy(g.GetComponent<Collider>());
        }
        private void Sph(string n, Vector3 pos, Vector3 sc, Material m, Transform p)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = n; g.transform.SetParent(p, false);
            g.transform.localPosition = pos; g.transform.localScale = sc;
            g.GetComponent<Renderer>().material = m; Object.Destroy(g.GetComponent<Collider>());
        }
    }
}
