using UnityEngine;

namespace PixelToCivilization.Rendering
{
    /// <summary>
    /// V6.1.1 环境导演：运行时搭建电影级自然光照——主方向光（软阴影）、三波段环境光、
    /// 程序化天空盒、指数高度雾；昼夜系数可由游戏年份/调试面板驱动，默认黄金时刻偏正午以保证观感。
    /// </summary>
    public class EnvironmentDirector : MonoBehaviour
    {
        public static EnvironmentDirector Instance { get; private set; }

        public Light Sun { get; private set; }
        Material _sky;
        public float DayFactor = 1f;          // 0=夜 1=昼
        public float SunAzimuth = 35f;        // 水平方位角
        public float SunElevation = 52f;      // 仰角
        public bool AutoDayNight = true;      // V9.0.8 默认开启昼夜循环（12 分钟一昼夜），夜景灯光随相位自动亮灭

        // 调色板
        static readonly Color ZenithDay = new(0.10f, 0.55f, 0.92f);  // V7.0.1
        static readonly Color HorizonDay = new(0.55f, 0.83f, 1.00f); // V7.0.1
        static readonly Color SunWarm = new(1.0f, 0.975f, 0.88f);
        static readonly Color FogDay = new(0.60f, 0.83f, 1.00f);    // V7.0.1

        void Awake() { Instance = this; Build(); }

        public void Build()
        {
            // 主方向光
            var lightGo = new GameObject("Sun_Directional");
            lightGo.transform.SetParent(transform);
            Sun = lightGo.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.5f;
            Sun.shadowBias = -0.0004f;
            Sun.shadowNormalBias = 0.4f;
            Sun.color = SunWarm;
            RenderSettings.sun = Sun;   // V6.7.1 显式指定 URP 主方向光，避免运行时主光丢失导致物体发黑

            // 天空盒
            var skyShader = Shader.Find("PxC/SkyDome");
            if (skyShader != null)
            {
                _sky = new Material(skyShader) { name = "RuntimeSky" };
                RenderSettings.skybox = _sky;
            }

            // 三波段环境光（天空/赤道/地面），PBR 间接光基础
            // 平坦地形法线严格朝上、会吃满天空环境光，若过亮会把草色冲淡成米白，故整体压低、保留饱和
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.85f, 0.96f);  // V7.0.1 提亮
            RenderSettings.ambientEquatorColor = new Color(0.80f, 0.78f, 0.70f);
            RenderSettings.ambientGroundColor = new Color(0.62f, 0.70f, 0.55f);
            RenderSettings.ambientIntensity = 1.35f;

            // 环境反射（让金属/水面有反射）
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;

            // 高度雾：指数平方，远处柔化地平线
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.00055f;  // V7.0.1 减雾
            RenderSettings.fogColor = FogDay;

            // 相机用天空盒清屏
            var cam = Camera.main;
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;

            ApplySun();
            // V6.7.1 一次性光照自检（发黑排查）：输出主光/环境光/URP Lit 是否被设备支持
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Debug.Log($"[LIGHT] device={SystemInfo.graphicsDeviceType} litFound={(lit!=null)} litSupported={(lit!=null&&lit.isSupported)} sun.intensity={Sun.intensity} sunEnabled={Sun.enabled} ambientMode={RenderSettings.ambientMode} ambientIntensity={RenderSettings.ambientIntensity} equator={RenderSettings.ambientEquatorColor}");
        }

        public bool HoldPhase = false;       // V9.0.8 探针/调试锁定相位时为 true，暂停自动昼夜推进

        void Update()
        {
            if (AutoDayNight && !HoldPhase)
            {
                // 一个完整昼夜约 12 分钟，便于观察；相位偏移 0.30 让开局落在清晨（仰角约 49°），避免玩家一进场就是深夜
                float phase = Mathf.Repeat(Time.time / 720f + 0.30f, 1f);
                SetPhase(phase);
            }
        }

        /// <summary>phase 0..1：0/0.5 为夜，0.25 为正午</summary>
        public void SetPhase(float phase)
        {
            float elev = Mathf.Sin(phase * Mathf.PI * 2f - Mathf.PI * 0.5f) * 0.5f + 0.5f; // 0..1
            SunElevation = Mathf.Lerp(-8f, 80f, elev);
            DayFactor = Mathf.Clamp01((SunElevation + 4f) / 14f);
            ApplySun();
        }

        public void SetDayFactor(float d) { DayFactor = Mathf.Clamp01(d); ApplySun(); }

        void ApplySun()
        {
            if (Sun == null) return;
            // 由方位角/仰角算方向
            float az = SunAzimuth * Mathf.Deg2Rad, el = SunElevation * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az)).normalized;
            Sun.transform.rotation = Quaternion.LookRotation(-dir);
            Sun.intensity = Mathf.Lerp(0.10f, 1.65f, DayFactor);  // V7.0.1 明亮阳光   // V9.0.8 夜间压到月光级 0.10（旧值 0.30 导致深夜仍像白天），靠天空盒/环境光保底不发黑
            // 日出日落偏暖
            float warm = 1f - Mathf.Clamp01(Mathf.Abs(SunElevation - 30f) / 40f);
            Sun.color = Color.Lerp(new Color(0.5f, 0.58f, 0.8f), Color.Lerp(new Color(1f, 0.62f, 0.36f), SunWarm, DayFactor), DayFactor);
            Sun.color = Color.Lerp(Sun.color, new Color(1f, 0.6f, 0.35f), warm * 0.5f * DayFactor);
            Sun.enabled = SunElevation > -12f;

            if (_sky != null)
            {
                _sky.SetVector("_SunDir", new Vector4(dir.x, dir.y, dir.z, 0));
                _sky.SetFloat("_DayFactor", DayFactor);
            }

            // 雾与环境光随昼夜（V9.0.8 夜间环境光 0.62→0.34，让昼夜真正可辨，亮窗/路灯成为夜间主光源；0.34 保底不发黑）
            RenderSettings.fogColor = Color.Lerp(new Color(0.04f, 0.05f, 0.09f), FogDay, DayFactor);
            RenderSettings.ambientIntensity = Mathf.Lerp(0.34f, 1.25f, DayFactor);
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.30f, 0.36f, 0.55f), new Color(0.72f, 0.85f, 0.96f), DayFactor); // 夜间冷蓝月光环境

            // V9.0.8 全局昼夜系数：Unlit 的 PxC/VertexColor 地形与 PxC/WaterURP 水面在着色器内据此压暗
            Shader.SetGlobalFloat("_GlobalDay", DayFactor);

            // V9.0.8 夜景灯光：亮窗/路灯/发光标识按昼夜相位统一提亮压暗
            PixelToCivilization.World.ShaderHelper.ApplyNight(DayFactor);
        }

        // ===== V9.0.8 WebGL 浏览器回归探针（SendMessage 只能绑无参/string 方法）=====
        public void WebDay() { HoldPhase = true; SetPhase(0.5f); }
        public void WebNight() { HoldPhase = true; SetPhase(0.02f); }
        public void WebResumeAuto() { HoldPhase = false; }

        /// <summary>一键布置 V9.0.8 验收场景：5 摩天楼（地标应仅 3 座全高）+ 3 数据中心 + 2 地铁，并强制深夜。</summary>
        public void WebV908()
        {
            var gm = PixelToCivilization.Core.GameManager.Instance;
            if (gm == null) { Debug.Log("[V908] GameManager 未就绪"); return; }
            var S = gm.State;
            S.Era = 6; S.Year = 4960; S.Pop = 1600;
            foreach (var tech in new[] { "electricity", "computer", "ai_tech", "aviation", "high_speed_rail_tech" })
                if (!S.ResearchedTechs.Contains(tech)) S.ResearchedTechs.Add(tech);
            foreach (var r in new[] { "wood", "stone", "iron", "steel", "concrete", "gold", "food" })
                S.AddRes(r, 9999);

            int Guard(string id, int n)
            {
                int made = 0, guard = 0;
                while (made < n && guard++ < 60)
                {
                    if (gm.Building.FindAutoPosition(id, out float x, out float z) &&
                        gm.Building.PlaceBuilding(id, x, z)) made++;
                }
                return made;
            }
            int sky = Guard("skyscraper", 5);
            int dc = Guard("data_center", 3);
            int sub = Guard("subway", 2);
            gm.Infra?.RecomputeGrid();
            WebNight();

            int CountType(string id)
            {
                int n = 0;
                foreach (var b in S.Buildings) if (b != null && b.Type == id) n++;
                return n;
            }
            int skyTotal = CountType("skyscraper");
            int landmark = Mathf.Min(3, skyTotal);
            float dcMult = Mathf.Min(1.6f, 1f + 0.08f * CountType("data_center"));
            float relief = Mathf.Min(0.65f, 0.18f * CountType("subway"));
            Debug.Log($"[V908] dayFactor={DayFactor:F2} placedSky={sky} placedDC={dc} placedSubway={sub} | skyTotal={skyTotal} landmarkFullHeight={landmark} dcResearchMult={dcMult:F2} subwayCongRelief={relief:F2}");
            Debug.Log("[V908] " + (gm.ModernTraffic != null ? gm.ModernTraffic.Diagnose() : "traffic=null"));
        }

        /// <summary>只刷新诊断读数（地铁缓解需等交通系统 1s 重算后再调一次）。</summary>
        public void WebV908Info()
        {
            var gm = PixelToCivilization.Core.GameManager.Instance;
            if (gm == null) return;
            var S = gm.State;
            int CountType(string id)
            {
                int n = 0;
                foreach (var b in S.Buildings) if (b != null && b.Type == id) n++;
                return n;
            }
            float dcMult = Mathf.Min(1.6f, 1f + 0.08f * CountType("data_center"));
            float relief = Mathf.Min(0.65f, 0.18f * CountType("subway"));
            Debug.Log($"[V908] dayFactor={DayFactor:F2} sky={CountType("skyscraper")} dc={CountType("data_center")} subway={CountType("subway")} dcMult={dcMult:F2} relief={relief:F2}");
            Debug.Log("[V908] " + (gm.ModernTraffic != null ? gm.ModernTraffic.Diagnose() : "traffic=null"));
        }
    }
}
