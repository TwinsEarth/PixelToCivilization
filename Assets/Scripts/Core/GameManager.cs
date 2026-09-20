using System;
using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Data;
using PixelToCivilization.Systems;
using PixelToCivilization.Buildings;
using PixelToCivilization.AI;
using PixelToCivilization.World;
using PixelToCivilization.UI;

namespace PixelToCivilization.Core
{
    public enum GameStateType { Menu, Playing, Paused, Victory, Defeat }

    /// <summary>游戏主管理器：加载数据库、编排全部子系统、速度/暂停/Debug密码门</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("状态")]
        public GameState State = new();
        public GameStateType StateType = GameStateType.Menu;

        [Header("数据库")]
        public List<EraDefinition> Eras;
        public List<DynastyDefinition> Dynasties;
        public Dictionary<string, BuildingDefinition> Buildings = new();
        public Dictionary<string, TechDefinition> Techs = new();
        public Dictionary<string, PolicyDefinition> Policies = new();

        [Header("子系统")]
        public GameTime Time;
        public EconomySystem Economy;
        public PopulationSystem Population;
        public BuildingSystem Building;
        public TechSystem Tech;
        public PolicySystem Policy;
        public MilitarySystem Military;
        public NavalSystem Naval;
        public OceanExpansionSystem Ocean;
        public SpaceExpansionSystem Space;
        public CanalSystem Canal;
        public CartSystem Cart;
        public ModernTrafficSystem ModernTraffic;  // V9.0.1 现代交通：工业时代彩色轿车/卡车、现代机场飞机（纯装饰，有上限不存档）
        public CityServicesSystem CityServices;   // V9.0.1 城市公共设施：消防/警察/医院/学校/公园/超市/小区，组织神按人口自动配套
        public CityMetricsSystem CityMetrics;     // V9.0.5 健康/教育/治安/就业指标 + 现代人口结构 + 通勤失业
        public CityFinanceSystem CityFinance;     // V9.0.7 城市等级/财政预算/税率/地价
        public TrainSystem Trains;                // V9.0.1 五代铁路：1800蒸汽/1900内燃/1950电力/1990高铁/2010磁悬浮，按公元年代自动升级（纯装饰有上限不存档）
        public DistrictSystem Districts;          // V9.0.9 多城区/行政区聚类（派生数据不存档）
        public IntercityNetworkSystem Intercity;  // V9.0.9 城际公路/五代铁路组网（纯装饰有上限不存档）
        public BridgeSystem Bridge;        // V6.3.9 桥梁（材料/跨距随时代，同阵营相邻陆地自动最短建桥）
        public EmbarkSystem Embark;        // V6.3.7 Lv2 车船自动载人
        public InfrastructureSystem Infra;
        public CultureSystem Culture;
        public GodsSystem Gods;
        public EnvironmentSystem Env;
        public SaveSystem SaveSystem;
        // 策划书扩展系统
        public PhilosophySystem Philosophy;
        public DisasterSystem Disaster;
        public HistoryEventSystem HistoryEvent;
        public VictorySystem Victory;
        public NationSystem Nation;   // V6.1.3 多聚落 + 天下分合（分裂3-7国 ↔ 大一统王朝）
        public ColonizationSystem Colonization;   // V6.1.5 殖民时代
        public ExpeditionSystem Expedition;       // V6.1.6 海洋/太空网格探索副本
        public WonderSystem Wonder;               // V6.8.0 世界奇观·文明丰碑
        public AICouncilSystem Council;           // V6.1.8 九智能体共治（AI多智能体，离线硬保证+联网ARK可选增强）
        public TideSystem Tide;                   // V6.1.9(i) 月度潮汐（主涨它降反向）
        public OceanCurrentSystem OceanFlow;      // V6.1.9(i) 洋流&海风（帆船动力/鱼群洄游）
        public WeatherSystem Weather;             // V6.1.9(i) 天气（雨雪晴云雾晚霞雷电龙卷）

        // V6.1.2 底部工具栏当前工具：select 选择 / tree 种树 / npc 招民（对齐 v5.9.9 setTool）
        public string Tool = "select";

        public event Action<GameStateType> OnStateChanged;
        public event Action<LogEntry> OnEventLogged;

        private readonly List<GameSystemBase> _systems = new();

        private void Awake() => EnsureAwake();

        /// <summary>幂等初始化单例与数据库：运行时由 Awake 调用；Edit 模式/冒烟测试可在 AddComponent 后显式调用</summary>
        public void EnsureAwake()
        {
            if (Instance == this) return;
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            LoadDatabases();
            // 打包版恢复上次的显示偏好（编辑器下不改动 Game 视图）
            if (!Application.isEditor && PlayerPrefs.HasKey("fullscreen"))
                SetFullscreen(PlayerPrefs.GetInt("fullscreen") == 1, false);
        }

        public void LoadDatabases()
        {
            Eras = EraDatabase.CreateAll();
            Dynasties = DynastyDatabase.CreateAll();
            Buildings.Clear();
            foreach (var b in BuildingDatabase.CreateAll()) Buildings[b.Id] = b;
            Techs.Clear();
            foreach (var t in TechDatabase.CreateAll()) Techs[t.Id] = t;
            Policies.Clear();
            foreach (var p in PolicyDatabase.CreateAll()) Policies[p.Id] = p;
        }

        /// <summary>由Bootstrap在世界搭建完成后调用，装配全部子系统</summary>
        public void InstallSystems()
        {
            Time = gameObject.GetComponent<GameTime>() ?? gameObject.AddComponent<GameTime>();
            Time.Init(State, Eras, Dynasties);
            Time.OnYearAdvanced += year => { foreach (var s in _systems) s.OnYear(year); };
            Time.OnEraChanged += (n, o) => { foreach (var s in _systems) s.OnEra(n, o); };
            Time.OnDynastyChanged += (i, y) => { foreach (var s in _systems) s.OnDynasty(i, y); };

            Add(Economy = gameObject.GetComponent<EconomySystem>() ?? gameObject.AddComponent<EconomySystem>());
            Add(Population = gameObject.GetComponent<PopulationSystem>() ?? gameObject.AddComponent<PopulationSystem>());
            Add(Building = gameObject.GetComponent<BuildingSystem>() ?? gameObject.AddComponent<BuildingSystem>());
            Add(Tech = gameObject.GetComponent<TechSystem>() ?? gameObject.AddComponent<TechSystem>());
            Add(Policy = gameObject.GetComponent<PolicySystem>() ?? gameObject.AddComponent<PolicySystem>());
            Add(Military = gameObject.GetComponent<MilitarySystem>() ?? gameObject.AddComponent<MilitarySystem>());
            Add(Tide = gameObject.GetComponent<TideSystem>() ?? gameObject.AddComponent<TideSystem>());
        Add(OceanFlow = gameObject.GetComponent<OceanCurrentSystem>() ?? gameObject.AddComponent<OceanCurrentSystem>());
        Add(Naval = gameObject.GetComponent<NavalSystem>() ?? gameObject.AddComponent<NavalSystem>());
            Add(Ocean = gameObject.GetComponent<OceanExpansionSystem>() ?? gameObject.AddComponent<OceanExpansionSystem>());
            Add(Space = gameObject.GetComponent<SpaceExpansionSystem>() ?? gameObject.AddComponent<SpaceExpansionSystem>());
            Add(Canal = gameObject.GetComponent<CanalSystem>() ?? gameObject.AddComponent<CanalSystem>());
            Add(Cart = gameObject.GetComponent<CartSystem>() ?? gameObject.AddComponent<CartSystem>());
            Add(ModernTraffic = gameObject.GetComponent<ModernTrafficSystem>() ?? gameObject.AddComponent<ModernTrafficSystem>()); // V9.0.1
            Add(Trains = gameObject.GetComponent<TrainSystem>() ?? gameObject.AddComponent<TrainSystem>()); // V9.0.1 五代铁路
            Add(Districts = gameObject.GetComponent<DistrictSystem>() ?? gameObject.AddComponent<DistrictSystem>()); // V9.0.9 行政区
            Add(Intercity = gameObject.GetComponent<IntercityNetworkSystem>() ?? gameObject.AddComponent<IntercityNetworkSystem>()); // V9.0.9 城际网络
            Add(Bridge = gameObject.GetComponent<BridgeSystem>() ?? gameObject.AddComponent<BridgeSystem>());
            Add(Embark = gameObject.GetComponent<EmbarkSystem>() ?? gameObject.AddComponent<EmbarkSystem>());
            Add(Infra = gameObject.GetComponent<InfrastructureSystem>() ?? gameObject.AddComponent<InfrastructureSystem>());
            Add(Culture = gameObject.GetComponent<CultureSystem>() ?? gameObject.AddComponent<CultureSystem>());
            Add(CityServices = gameObject.GetComponent<CityServicesSystem>() ?? gameObject.AddComponent<CityServicesSystem>()); // V9.0.1 城市公共设施
            Add(CityMetrics = gameObject.GetComponent<CityMetricsSystem>() ?? gameObject.AddComponent<CityMetricsSystem>()); // V9.0.5 城市指标/就业
            Add(CityFinance = gameObject.GetComponent<CityFinanceSystem>() ?? gameObject.AddComponent<CityFinanceSystem>()); // V9.0.7 城市财政/等级/地价
            Add(Gods = gameObject.GetComponent<GodsSystem>() ?? gameObject.AddComponent<GodsSystem>());
            Add(Env = gameObject.GetComponent<EnvironmentSystem>() ?? gameObject.AddComponent<EnvironmentSystem>());
        Add(Weather = gameObject.GetComponent<WeatherSystem>() ?? gameObject.AddComponent<WeatherSystem>());
            Add(Philosophy = gameObject.GetComponent<PhilosophySystem>() ?? gameObject.AddComponent<PhilosophySystem>());
            Add(Disaster = gameObject.GetComponent<DisasterSystem>() ?? gameObject.AddComponent<DisasterSystem>());
            Add(HistoryEvent = gameObject.GetComponent<HistoryEventSystem>() ?? gameObject.AddComponent<HistoryEventSystem>());
            Add(Victory = gameObject.GetComponent<VictorySystem>() ?? gameObject.AddComponent<VictorySystem>());
            // V6.1.3 大地图随年代自然延展 + 大航海/宇宙大开发时代里程碑
            Add(gameObject.GetComponent<WorldExpansionSystem>() ?? gameObject.AddComponent<WorldExpansionSystem>());
            // V6.1.3 多聚落 + 天下分合（必须在 EnvironmentSystem.PopulateInitial 前就绪，后者调用 Nation.InitNations）
            Add(Nation = gameObject.GetComponent<NationSystem>() ?? gameObject.AddComponent<NationSystem>());
            // V6.1.5 殖民时代 / V6.1.6 海洋·太空网格探索副本
            Add(Colonization = gameObject.GetComponent<ColonizationSystem>() ?? gameObject.AddComponent<ColonizationSystem>());
            Add(Expedition = gameObject.GetComponent<ExpeditionSystem>() ?? gameObject.AddComponent<ExpeditionSystem>());
            // V6.8.0 世界奇观（在九神议会之前，供组织/技术神自动援建统筹）
            Add(Wonder = gameObject.GetComponent<WonderSystem>() ?? gameObject.AddComponent<WonderSystem>());
            // V6.1.8 九智能体共治（放在最后，可统筹全部既有系统；离线规则硬保证文明不灭绝）
            Add(Council = gameObject.GetComponent<AICouncilSystem>() ?? gameObject.AddComponent<AICouncilSystem>());

            foreach (var s in _systems) s.Init(this);
            SaveSystem = gameObject.GetComponent<SaveSystem>() ?? gameObject.AddComponent<SaveSystem>();
            SaveSystem.Init(this);
            Debug.Log("[GameManager] 子系统装配完成，数量=" + _systems.Count);
        }

        private void Add(GameSystemBase s) { if (!_systems.Contains(s)) _systems.Add(s); }

        public void StartNewGame()
        {
            State.Reset();
            // V9.0.1 开局公元1700（游戏年4700·清康熙·大航海殖民末期）：静默把朝代/时代对齐到 era4，不连发 0→4 时代切换事件
            Time?.SnapToStartYear();
            State.Running = true; State.Paused = false; State.Speed = 1f;
            StateType = GameStateType.Playing;
            OnStateChanged?.Invoke(StateType);
            AddEvent("info", "⛵ 公元1700年·大航海殖民时代：海岸城邦在新大陆的黎明中兴起（美国建国前后）");
        }

        /// <summary>
        /// V6.1.2：每次开始游戏都用随机种子重新生成大地图（海≥50%/陆≥30%/山≤10%，湖/山/沙漠/河适应性生成），
        /// 清理上一局全部实体视图，相机归位新村址，再生成初始聚落/人口/飞鸟。
        /// </summary>
        public static bool NextEarthMode=false;   // V9.1.0 开始页地图模式选择（经典随机/真实地球）

        public void StartNewRandomGame()
        {
            // 1) 清理上一局实体视图（数据由 State.Reset 清空，视图按固定根名回收）
            ClearWorldVisuals();

            // 2) 随机重建地形 + 植被
            var terrain = UnityEngine.Object.FindObjectOfType<World.WorldGenerator>();
            var veg = UnityEngine.Object.FindObjectOfType<World.VegetationSystem>();
            int seed = World.WorldGenerator.RandomSeed();
            Vector3 village = Vector3.zero;
            bool earth=NextEarthMode;   // V9.1.1 地球模式：先进经典地图片头，再黑场干净切换到真实地球（杜绝两图叠加）
            if (terrain != null)
            {
                village = terrain.Regenerate(seed);
                veg?.Regrow(terrain, seed);
                Debug.Log($"[StartNewRandomGame] 经典片头 seed={seed} 大陆半径={terrain.LandRadius:F0} " +
                          $"海{terrain.SeaRatio:P0} 陆{terrain.LandRatio:P0} 山{terrain.MountainRatio:P0} 沙漠{terrain.DesertRatio:P0} earthPending={earth}");
            }

            // 3) 相机归位新村址
            var rig = UnityEngine.Object.FindObjectOfType<World.CameraRig>();
            rig?.Retarget(village);

            // 4) 状态重置并进入游戏
            StartNewGame();
            Military?.ResetForNewGame();   // V9.1.1 新局清理上一局割据/远征军据点
            State.EarthMode=false;        // V9.1.1 片头阶段为经典地形，切换时由 SwitchToEarthImmediate 置 true

            // 5) 小地图地形底图作废重烘焙
            UIManager.Instance?.InvalidateMinimapBase();

            // 6) 初始聚落/职业人口/飞鸟群
            Env?.PopulateInitial();

            // 7) V9.1.1 真实地球模式：经典片头短暂展示后，黑场干净切换到真实地球（绝不两图叠加）
            if (earth) BeginEarthIntro(2.6f);
        }

        // ================= V9.1.1 经典 → 真实地球 干净切换 =================
        /// <summary>经典片头结束后启动黑场切换协程（delay 为经典片头展示的现实秒数）。</summary>
        public void BeginEarthIntro(float delay) { StartCoroutine(SwitchToEarthRoutine(delay)); }
        /// <summary>Web/调试无参入口：立即干净切换到真实地球（回归测试用，不等片头）。</summary>
        public void WebSwitchEarth(){ SwitchToEarthImmediate(); Debug.Log("[Web] SwitchToEarth EarthMode="+State.EarthMode); }

        private System.Collections.IEnumerator SwitchToEarthRoutine(float delay)
        {
            bool wasPaused = State.Paused; State.Paused = true;   // 切换期间冻结模拟
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            var fader = NewFader(out var img);
            yield return Fade(img, 1f, 0.4f);                    // 淡入黑场
            SwitchToEarthImmediate();
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Fade(img, 0f, 0.5f);                    // 淡出，露出真实地球
            Destroy(fader);
            State.Paused = wasPaused;
        }

        private System.Collections.IEnumerator Fade(UnityEngine.UI.Image img, float to, float dur)
        {
            float from = img.color.a, t = 0f;
            while (t < dur)
            {
                t += UnityEngine.Time.unscaledDeltaTime;   // V9.1.1 黑场用现实时间，避免与 GameTime 字段 Time 冲突
                var c = img.color; c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur)); img.color = c;
                yield return null;
            }
            var cc = img.color; cc.a = to; img.color = cc;
        }

        /// <summary>代码生成的全屏黑场（顶层 sortingOrder，不依赖 UIManager）。</summary>
        private GameObject NewFader(out UnityEngine.UI.Image img)
        {
            var go = new GameObject("EarthFade");
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 32767;
            go.AddComponent<UnityEngine.UI.CanvasScaler>(); go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            var rt = img.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        /// <summary>真正执行切换：销毁经典全部实体与地形/水/植被 → 生成真实地球 → 重置状态 → 落位七国聚落。任意时刻调用都不会与经典地图共存。</summary>
        public void SwitchToEarthImmediate()
        {
            ClearWorldVisuals();
            var terrain = UnityEngine.Object.FindObjectOfType<World.WorldGenerator>();
            var veg = UnityEngine.Object.FindObjectOfType<World.VegetationSystem>();
            // 防御性销毁经典地形/水面/植被根（RegenerateEarth 内部也会做，这里再兜一层，彻底杜绝叠加）
            if (terrain != null)
                foreach (var nm in new[] { "Terrain", "Water" })
                { var old = terrain.transform.Find(nm); if (old != null) DestroyImmediate(old.gameObject); }
            if (veg != null)
            { var ov = veg.transform.Find("Vegetation"); if (ov != null) DestroyImmediate(ov.gameObject); }

            int seed = World.WorldGenerator.RandomSeed();
            Vector3 home = terrain != null ? terrain.RegenerateEarth(seed) : Vector3.zero;
            veg?.Regrow(terrain, seed);

            var rig = UnityEngine.Object.FindObjectOfType<World.CameraRig>();
            rig?.Retarget(home);

            StartNewGame();
            Military?.ResetForNewGame();
            State.EarthMode = true;
            UIManager.Instance?.InvalidateMinimapBase();
            Env?.PopulateInitial();        // EarthMode=true → PopulateInitialEarth：七国城市落位
            Intercity?.RefreshNow();       // 立即按地球城市重建 4 车道公路 + 跨海铁路
            AddEvent("info", "🌍 已进入真实地球：一洲至多三国、四大阵营、国际铁路相连");
            Debug.Log("[SwitchToEarth] 干净切换完成 home=" + home +
                      " nations=" + (Nation != null && State.Nations != null ? State.Nations.Count : 0) +
                      " earthCities=" + (Nation != null ? Nation.EarthCities.Count : 0));
        }

        /// <summary>继续上次游戏：先搭好随机世界与各系统，再读取时间最新的有效存档（自动槽0或手动槽1..5）覆盖。无有效存档返回 false。</summary>
        public bool ContinueLastGame()
        {
            if (SaveSystem==null) { AddEvent("bad","没有可继续的存档"); return false; }
            int slot=SaveSystem.LatestSlot();
            if (slot<0) { AddEvent("bad","没有可继续的存档"); return false; }
            NextEarthMode=false;           // V9.1.1 读档不播经典→地球片头；地形模式由存档 d.EarthMode 决定
            StartNewRandomGame();          // 世界/系统/State 就绪（地形随后由存档种子还原）
            SaveSystem.LoadFromSlot(slot); // 覆盖为最新存档快照
            return true;
        }

        /// <summary>销毁各实体根节点下的全部视图子物体（根节点本身保留，系统仍持有引用）</summary>
        private void ClearWorldVisuals()
        {
            try { GameObjectPool.ClearAll(); } catch (System.Exception e) { Debug.LogWarning("[Pool] ClearAll: "+e.Message); } // V7.0.3 重建前清空对象池
            string[] roots = { "Buildings","Canals","Carts","ModernTraffic","Trains","Military","Navy","Environment","Birds","Fish","Wonders","Districts","Intercity" };
            // V9.1.1 地标根是 GM 直接子物体，遍历中立即销毁会跳过后续兄弟，先收集后销毁
            var landmarkRoots = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in transform)
                if (child != null && child.name.StartsWith("EarthLandmark")) landmarkRoots.Add(child.gameObject);
            foreach (var go in landmarkRoots) UnityEngine.Object.DestroyImmediate(go);
            foreach (Transform child in transform)
            {
                // V6.1.3 聚落节点命名 Settlement_0/1/...，按前缀一并清理
                if (System.Array.IndexOf(roots, child.name) < 0 && !child.name.StartsWith("Settlement")) continue;
                for (int i=child.childCount-1;i>=0;i--)
                {
                    var sub = child.GetChild(i);
                    // Environment 下还有 Trees/Agents 两层根，递归清它们的子物体；其余直接清
                    if (child.name=="Environment") { for(int j=sub.childCount-1;j>=0;j--) UnityEngine.Object.Destroy(sub.GetChild(j).gameObject); }
                    else UnityEngine.Object.Destroy(sub.gameObject);
                }
            }
        }

        private void Update()
        {
          try{
            HandleHotkeys();
            TickCryo(UnityEngine.Time.unscaledDeltaTime); // V6.1.9 冷冻冷却按现实秒走，暂停也计时
            if (StateType != GameStateType.Playing || State.Paused) return;
            float scaled = UnityEngine.Time.deltaTime * EffectiveSpeed; // 冷冻期实际倍速封顶10
            Time.Tick(scaled); // 年份推进（内部按游戏年份换算朝代/时代/公历）
            foreach (var s in _systems) s.Tick(scaled);
          }
          catch(System.Exception e){ Debug.LogError("[MARK_GM] "+e.GetType().Name+": "+e.Message+"\n"+e.StackTrace); }
        }

        // ===== V6.1.9 加速冷冻 =====
        /// <summary>冷冻冷却中实际生效的倍速（封顶 CryoMaxSpeed=10），非冷冻期等于设定倍速</summary>
        public float EffectiveSpeed => State.CryoActive ? Mathf.Min(State.Speed, GameConstants.CryoMaxSpeed) : State.Speed;
        private bool _cryoEntered;
        /// <summary>冷冻冷却按现实秒倒计时（不受暂停/倍速影响）；归零即自动解冻并重置累计年数</summary>
        private void TickCryo(float realDt)
        {
            if (!State.CryoActive) { _cryoEntered=false; return; }
            if (!_cryoEntered)
            {
                _cryoEntered=true;
                AddEvent("bad","❄️ 加速累计已满100年，进入冷冻冷却：300秒内最高10倍速");
                UIManager.Instance?.Toast("❄️ 冷冻冷却：300秒内最高10倍",false);
            }
            State.CryoRemainSec -= realDt;
            if (State.CryoRemainSec <= 0f) ThawCryo(true);
        }
        /// <summary>解冻：关闭冷冻、清零累计年数重新计算（倒计时归零自动调用，也供Web/调试手动解冻）</summary>
        public void ThawCryo(bool notify=true)
        {
            State.CryoActive=false; State.CryoRemainSec=0f; State.CryoAccumYears=0f; _cryoEntered=false;
            if(notify){ AddEvent("good","☀️ 冷冻结束，加速累计已重置，可继续加速"); UIManager.Instance?.Toast("☀️ 冷冻结束，已解冻",true); }
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if (Input.GetKeyDown(KeyCode.F11) ||
                (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Return))) ToggleFullscreen();
        }

        public void TogglePause()
        {
            State.Paused = !State.Paused;
            StateType = State.Paused ? GameStateType.Paused : GameStateType.Playing;
            OnStateChanged?.Invoke(StateType);
        }

        // ===== 全屏/窗口切换（F11、Alt+Enter 或底部栏按钮），偏好持久化 =====
        public void ToggleFullscreen() => SetFullscreen(Screen.fullScreenMode == FullScreenMode.Windowed || !Screen.fullScreen);

        public void SetFullscreen(bool full, bool notify = true)
        {
            if (full)
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = true;
            }
            else
            {
                var res = Screen.currentResolution;
                int w = Mathf.Clamp((int)(res.width * 0.85f), 1024, 1920);
                int h = Mathf.Clamp((int)(res.height * 0.85f), 576, 1080);
                Screen.SetResolution(w, h, FullScreenMode.Windowed);
            }
            PlayerPrefs.SetInt("fullscreen", full ? 1 : 0);
            PlayerPrefs.Save();
            if (!notify) return;
            AddEvent("info", full ? "⛶ 已切换全屏（F11 切回窗口）" : "⛶ 已切换窗口模式（F11 切回全屏）");
            UIManager.Instance?.Toast(full ? "⛶ 全屏模式" : "⛶ 窗口模式");
        }

        // ===== 速度（1:1 对齐 v5.9.9：连续整数倍速，分段加减，分级上限）=====
        /// <summary>当前 Debug 等级允许的最高倍速：普通100 / Debug1=300 / 密码解锁=1000</summary>
        public float MaxSpeed => State.DebugLevel >= 2 ? GameConstants.SpeedMaxDebug2
                              : State.DebugLevel >= 1 ? GameConstants.SpeedMaxDebug1
                              : GameConstants.SpeedMaxNormal;
        public float[] SpeedTiers => State.DebugLevel switch
        {
            2 => GameConstants.SpeedTiersDebug2,
            1 => GameConstants.SpeedTiersDebug1,
            _ => GameConstants.SpeedTiersNormal
        };
        public void CycleSpeed()
        {
            var tiers = SpeedTiers;
            int idx = Array.IndexOf(tiers, State.Speed);
            State.Speed = tiers[(idx + 1) % tiers.Length];
        }

        // ===== WebGL / SendMessage 友好入口（无参，供浏览器深链、外部页面与自动化回归调用；UI 按钮逻辑不受影响）=====
        public void WebQuickSave(){ SaveSystem?.SaveToSlot(1); }

        // ===== V6.8.0 世界奇观：浏览器回归入口（SendMessage 可绑 string） =====
        public void WebBuildWonder(string id){
            if(Wonder==null){Debug.Log("[WEB] Wonder system null");return;}
            // 回归便利：自动补足资源，避免被成本卡住
            foreach(var kv in Wonder.Def(id)?.Cost??new System.Collections.Generic.Dictionary<string,int>()) State.AddRes(kv.Key, kv.Value+50);
            var r=Wonder.TryBuild(id);
            Debug.Log($"[WEB] BuildWonder {id} ok={r.ok} msg={r.msg} count={State.Wonders.Count} resMul={Wonder.ResearchMul} goldMul={Wonder.GoldMul} housing+={Wonder.HousingAdd} ach={State.Achievements.Count}");
        }
        public void WebWonderInfo(){
            if(Wonder==null){Debug.Log("[WEB] Wonder null");return;}
            Debug.Log($"[WEB] WonderInfo count={State.Wonders.Count} era={State.Era} resMul={Wonder.ResearchMul} goldMul={Wonder.GoldMul} culMul={Wonder.CultureMul} fireMul={Wonder.FireCapMul} housing+={Wonder.HousingAdd} forcePower={Wonder.ForcePower} ach={State.Achievements.Count} auto={State.WonderAuto}");
        }
        public void WebWonderAuto(){ State.WonderAuto=!State.WonderAuto; Debug.Log("[WEB] WonderAuto="+State.WonderAuto); }

        // ===== V6.8.1 回归探针：强制生成中/大马车；回报平民迈腿动画状态 =====
        public void WebSpawnCarts(){
            if(Cart==null){Debug.Log("[WEB] Cart null");return;}
            float cx=State.VillageX!=null&&State.VillageX.Count>0?State.VillageX[0]:0f;
            float cz=State.VillageZ!=null&&State.VillageZ.Count>0?State.VillageZ[0]:0f;
            var m=Cart.SpawnCart("medium_cart",cx+6f,cz+4f,2);
            var l=Cart.SpawnCart("large_cart",cx-7f,cz+7f,3);
            Debug.Log($"[WEB] SpawnCarts medium={(m!=null)} large={(l!=null)} total={State.Carts.Count}");
        }
        public void WebAgentAnim(){
            int total=State.Agents!=null?State.Agents.Count:0, withAnim=0, moving=0, stopped=0, noView=0;
            foreach(var a in State.Agents){
                if(a.Boarded){continue;}
                if(a.View==null){noView++;continue;}
                if(a.Anim==null) a.Anim=a.View.GetComponentInChildren<PixelToCivilization.Actors.HumanoidAnimator>();
                if(a.Anim==null) continue;
                withAnim++;
                float sp=new Vector2(a.Anim.Velocity.x,a.Anim.Velocity.z).magnitude;
                if(sp>0.02f && a.Anim.Rig!=null && a.Anim.Rig.Moving) moving++; else stopped++;
            }
            Debug.Log($"[WEB] AgentAnim total={total} noView={noView} withAnim={withAnim} moving={moving} stopped={stopped}");
        }

        // ===== V9.0.1 现代文明回归探针：跳到2010年(磁悬浮)，自检平坦陆地把 7 设施+现代建筑各免费落一座，回读计数 =====
        public void WebV9Showcase()
        {
            Time?.DebugJumpTo(5010);                 // 公元2010年 -> era6、五代铁路到磁悬浮
            State.Era = 6; State.AgeOfSail = true;
            foreach (var k in new[]{"wood","stone","food","gold","iron","steel","concrete","bronze","goods"}) State.AddRes(k, 9000);
            var terr = UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            float cx = State.VillageX!=null&&State.VillageX.Count>0?State.VillageX[0]:0f;
            float cz = State.VillageZ!=null&&State.VillageZ.Count>0?State.VillageZ[0]:0f;
            var used = new List<Vector3>();
            bool FlatLand(float x, float z){
                if(terr==null) return true;
                if(terr.IsWater(x,z) || !terr.InsideFrontier(x,z)) return false;
                float h=terr.HeightAt(x,z);
                foreach(var o in new[]{(3f,0f),(-3f,0f),(0f,3f),(0f,-3f),(3f,3f),(-3f,-3f)})
                    if(terr.IsWater(x+o.Item1,z+o.Item2) || Mathf.Abs(terr.HeightAt(x+o.Item1,z+o.Item2)-h)>1.2f) return false;
                foreach(var u in used) if(Vector2.Distance(new Vector2(u.x,u.z),new Vector2(x,z))<15f) return false;
                return true;
            }
            string[] ids = {"fire_station","police_station","hospital","modern_school","park","supermarket","apartment",
                            "skyscraper","factory_modern","new_army","power_plant","data_center","ai_lab","airport","high_speed_rail"};
            int ok = 0; var sb = new System.Text.StringBuilder();
            foreach (var id in ids)
            {
                bool placed=false; float px=0,pz=0;
                for(int r=10; r<=320 && !placed; r+=6)
                    for(int a=0;a<360 && !placed;a+=15){
                        float x=cx+r*Mathf.Cos(a*Mathf.Deg2Rad), z=cz+r*Mathf.Sin(a*Mathf.Deg2Rad);
                        if(FlatLand(x,z)){ px=x; pz=z; placed=true; }
                    }
                if(placed){ var be=Building.PlaceInitial(id,px,pz); placed=be!=null; if(placed) used.Add(new Vector3(px,0,pz)); }
                sb.Append(id).Append('=').Append(placed?"Y":"N").Append(' ');
                if(placed) ok++;
            }
            Debug.Log($"[V9SHOW] placed {ok}/{ids.Length} :: {sb}");
            Trains?.RefreshNow();   // 跳年到现代后立即补建当代铁路（不必等 3 秒冷却/解除暂停）
            Debug.Log($"[V9SHOW] counts fire={State.CountBuilding("fire_station")} police={State.CountBuilding("police_station")} " +
                      $"hospital={State.CountBuilding("hospital")} school={State.CountBuilding("modern_school")} park={State.CountBuilding("park")} " +
                      $"supermarket={State.CountBuilding("supermarket")} apartment={State.CountBuilding("apartment")} " +
                      $"skyscraper={State.CountBuilding("skyscraper")} airport={State.CountBuilding("airport")} hsrail={State.CountBuilding("high_speed_rail")} " +
                      $"year={State.Year} trainTier={(Trains!=null?Trains.CurrentTier:-1)} trainLines={(Trains!=null?Trains.LineCount:-1)} buildings={State.Buildings.Count}");
        }

        /// <summary>V9.0.1 铁路建线诊断（无参 Web 探针）</summary>
        public void WebTrainDiag(){ Debug.Log(Trains!=null ? Trains.Diagnose() : "[TRAINDIAG] Trains=null"); }

        /// <summary>V9.0.9 城际网络：跳到当代，在两片相距约150单位的陆地各落一片城区+一座机场，
        /// 推进足够帧让行政区聚类、城际公路/铁路组网、航线组网完成，回读诊断（无参 Web 探针）。</summary>
        public void WebV909Network()
        {
            Time?.DebugJumpTo(5010); State.Era = 6; State.AgeOfSail = true;
            foreach (var k in new[]{"wood","stone","food","gold","iron","steel","concrete","bronze","goods"}) State.AddRes(k, 90000);
            var terr = UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            float cx = State.VillageX!=null&&State.VillageX.Count>0?State.VillageX[0]:0f;
            float cz = State.VillageZ!=null&&State.VillageZ.Count>0?State.VillageZ[0]:0f;
            var used = new List<Vector3>();
            bool FlatLand(float x, float z){
                if(terr==null) return true;
                if(terr.IsWater(x,z) || !terr.InsideFrontier(x,z)) return false;
                float h=terr.HeightAt(x,z);
                for(int dx=-2;dx<=2;dx+=2)for(int dz=-2;dz<=2;dz+=2)
                    if(terr.IsWater(x+dx,z+dz) || Mathf.Abs(terr.HeightAt(x+dx,z+dz)-h)>1.4f) return false;
                foreach(var u in used) if(Vector2.Distance(new Vector2(u.x,u.z),new Vector2(x,z))<7f) return false;
                return true;
            }
            int PlaceNear(string id, float ox, float oz, float rMax){
                for(int r=4;r<=rMax;r+=4)
                    for(int a=0;a<360;a+=12){
                        float x=cx+ox+r*Mathf.Cos(a*Mathf.Deg2Rad), z=cz+oz+r*Mathf.Sin(a*Mathf.Deg2Rad);
                        if(FlatLand(x,z)){ var be=Building.PlaceInitial(id,x,z); if(be!=null){ used.Add(new Vector3(x,0,z)); return 1; } }
                    }
                return 0;
            }
            int p=0;
            // 城区 A（村心附近）：8 住宅 + 1 机场
            for(int i=0;i<8;i++) p+=PlaceNear("apartment",0,0,60);
            p+=PlaceNear("airport",0,0,70);
            // 城区 B（约 150 单位外）：8 工厂/住宅 + 1 机场
            for(int i=0;i<6;i++) p+=PlaceNear("factory_modern",150,0,120);
            for(int i=0;i<4;i++) p+=PlaceNear("apartment",150,0,120);
            p+=PlaceNear("airport",150,0,140);
            Debug.Log($"[V909] placed={p} buildings={State.Buildings.Count}");
            Districts?.RefreshNow();
            Intercity?.RefreshNow();
            ModernTraffic.RefreshNow();
            for(int i=0;i<240;i++){ ModernTraffic.Tick(2f); Districts?.Tick(2f); Intercity?.Tick(2f); }
            Districts?.RefreshNow(); Intercity?.RefreshNow();
            for(int i=0;i<60;i++){ ModernTraffic.Tick(2f); Intercity?.Tick(2f); }
            Debug.Log(Districts!=null?Districts.Diagnose():"[DIST] null");
            Debug.Log(Intercity!=null?Intercity.Diagnose():"[INTERCITY] null");
            Debug.Log(ModernTraffic.Diagnose());
        }

        /// <summary>V9.0.2 城市公共服务：跳到当代，确定性落位一片紧凑现代社区（住宅+六类设施），
        /// 回报六类覆盖率，并走年度结算与强制城市事件，验证受控/失控分支不抛异常（无参 Web 探针）。</summary>
        // ===== V9.1.0 真实地球模式 WebGL 无参探针（SendMessage 无法绑定 int 形参） =====
        public void WebNewEarth(){ NextEarthMode=true; StartNewRandomGame(); Debug.Log("[Web] NewEarth EarthMode="+State.EarthMode); }
        public void WebNewClassic(){ NextEarthMode=false; StartNewRandomGame(); Debug.Log("[Web] NewClassic EarthMode="+State.EarthMode); }
        public void WebV910Diagnose()
        {
            var t=UnityEngine.Object.FindObjectOfType<World.WorldGenerator>();
            if(t==null){Debug.Log("[V910] terrain=null");return;}
            Debug.Log($"[V910] earth={t.EarthMode} K={t.ContinentCount} sea={t.SeaRatio:P1} landOnly={t.LandOnlyRatio:P1} mtn={t.MountainRatio:P1} desert={t.DesertRatio:P1} fresh={t.FreshWaterRatio:P1} home={t.HomeContinent} center={t.SettlementCenter} stateEarth={State.EarthMode}");
        }

        /// <summary>V9.1.1 国家阵营无参探针：13国/4阵营/首都陆块/地标数/远征军。</summary>
        public void WebV911Diagnose()
        {
            Debug.Log($"[V911] earth={State.EarthMode} phase={State.WorldPhase} nations={(State.Nations!=null?State.Nations.Count:0)}");
            if (State.Nations!=null)
                foreach(var n in State.Nations)
                    Debug.Log($"[V911] id={n.Id} {n.Name} F{n.Faction} hex={n.ColorHex} cid={n.ContinentId} alive={n.Alive} pos=({n.Cx:F1},{n.Cz:F1}) pop={n.Pop} cap={n.Note}");
            int lm=0; foreach(Transform tr in transform) if(tr!=null && tr.name.StartsWith("EarthLandmark_")) lm++;
            Debug.Log($"[V911] landmarks={lm} milFactions={(Military!=null?Military.Factions.Count:0)}");
            if (Military!=null)
                foreach(var f in Military.Factions)
                    Debug.Log($"[V911] mil {f.Name} hex={f.ColorHex:x} cid={f.HomeContinent} pos=({f.X:F1},{f.Z:F1}) destroyed={f.Destroyed}");
        }

        public void WebEarthProbe()
        {
            var t=UnityEngine.Object.FindObjectOfType<World.WorldGenerator>();
            if(t==null){Debug.Log("[V910P] terrain=null");return;}
            System.Func<float,float,string> S=(lon,lat)=>{
                float wx=PixelToCivilization.World.EarthMapData.LonToX(lon), wz=PixelToCivilization.World.EarthMapData.LatToZ(lat);
                return $"({lon},{lat}) cid={t.ContinentAt(wx,wz)} h={t.HeightAt(wx,wz):F2} biome={t.BiomeAt(wx,wz)} ocean={t.IsOceanWater(wx,wz)}";
            };
            Debug.Log("[V910P] sahara "+S(10f,22f));
            Debug.Log("[V910P] himalaya "+S(86f,31f));
            Debug.Log("[V910P] gibraltar "+S(-5.6f,35.95f));
            Debug.Log("[V910P] mediter "+S(17f,36f));
            Debug.Log("[V910P] redsea "+S(39f,20f));
            Debug.Log("[V910P] blacksea "+S(34f,45f));
            Debug.Log("[V910P] persian "+S(52f,27f));
            Debug.Log("[V910P] dover "+S(1.4f,50.9f));
            Debug.Log("[V910P] baltic "+S(20f,58f));
            Debug.Log("[V910P] bengal "+S(88f,12f));
            Debug.Log("[V910P] japansea "+S(135f,40f));
            Debug.Log("[V910P] caspian "+S(50f,42f));
            Debug.Log("[V910P] gulfmex "+S(-92f,24.5f));
            Debug.Log("[V910P] yangtze "+S(120f,31.5f));
            int desert=0,mtn=0; float mhv=0;
            for(float wx=-2380f;wx<=2380f;wx+=24f)for(float wz=-1180f;wz<=1180f;wz+=14.4f){
                float h=t.HeightAt(wx,wz); var b=t.BiomeAt(wx,wz);
                if(h>=GameConstants.WaterLevel){ if(b==PixelToCivilization.World.BiomeKind.Desert)desert++; if(h>=5.4f)mtn++; if(h>mhv)mhv=h; }
            }
            Debug.Log($"[V910P] sample desertCells={desert} mtnCells={mtn} maxH={mhv:F1}");
        }

        public void WebCityServices()
        {
            Time?.DebugJumpTo(5010); State.Era = 6; State.AgeOfSail = true;
            foreach (var k in new[]{"wood","stone","food","gold","iron","steel","concrete","bronze","goods"}) State.AddRes(k, 90000);

            var terr = UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            float cx = State.VillageX!=null&&State.VillageX.Count>0?State.VillageX[0]:0f;
            float cz = State.VillageZ!=null&&State.VillageZ.Count>0?State.VillageZ[0]:0f;
            var used = new List<Vector3>();
            bool FlatLand(float x, float z){
                if(terr==null) return true;
                if(terr.IsWater(x,z) || !terr.InsideFrontier(x,z)) return false;
                float h=terr.HeightAt(x,z);
                foreach(var o in new[]{(3f,0f),(-3f,0f),(0f,3f),(0f,-3f)})
                    if(terr.IsWater(x+o.Item1,z+o.Item2) || Mathf.Abs(terr.HeightAt(x+o.Item1,z+o.Item2)-h)>1.2f) return false;
                foreach(var u in used) if(Vector2.Distance(new Vector2(u.x,u.z),new Vector2(x,z))<7f) return false;
                return true;
            }
            int Place(string id, float rMax){
                for(int r=6; r<=rMax; r+=5)
                    for(int a=0;a<360;a+=12){
                        float x=cx+r*Mathf.Cos(a*Mathf.Deg2Rad), z=cz+r*Mathf.Sin(a*Mathf.Deg2Rad);
                        if(FlatLand(x,z)){ var be=Building.PlaceInitial(id,x,z); if(be!=null){ used.Add(new Vector3(x,0,z)); return 1; } }
                    }
                return 0;
            }
            int placed=0;
            // 设施先落在村心 30 单位内（覆盖率半径 45~75），住宅落在 6~48 单位环内，确保被覆盖
            for(int i=0;i<2;i++){ placed+=Place("fire_station",30); placed+=Place("police_station",30); placed+=Place("hospital",30); placed+=Place("modern_school",30); placed+=Place("supermarket",30); }
            for(int i=0;i<3;i++) placed+=Place("park",30);
            for(int i=0;i<12;i++) placed+=Place("apartment",48);
            Debug.Log($"[CITY] neighborhood placed={placed} center=({cx:F0},{cz:F0}) buildings={State.Buildings.Count}");

            CityServices.RecomputeCoverage();
            Debug.Log(CityServices.Diagnose());
            float happy0=State.Happiness, corr0=State.Corruption, res0=State.GetRes("research");
            CityServices.OnYear(State.Year);                 // 年度：覆盖给幸福/科研、警局压腐败、按概率城市事件
            CityServices.RollCityIncident(true);             // 强制一次最薄弱项事件（覆盖高时应受控无损失）
            CityServices.RollCityIncident(true);
            Debug.Log($"[CITY] after OnYear+2 forced: happy {happy0:F1}->{State.Happiness:F1} corruption {corr0:F1}->{State.Corruption:F1} research {res0}->{State.GetRes("research")} buildings={State.Buildings.Count} pop={State.Pop}");
            Debug.Log(CityServices.Diagnose());
        }

        /// <summary>V9.0.3 道路分级与城市车流：跳到当代，确定性落位各级道路+货运建筑，
        /// 触发自动修路与车辆/公交/卡车生成，回读道路分级数、车辆构成、拥堵度（无参 Web 探针）。</summary>
        public void WebRoadTraffic()
        {
            Time?.DebugJumpTo(5010); State.Era = 6; State.AgeOfSail = true;
            foreach (var k in new[]{"wood","stone","food","gold","iron","steel","concrete","bronze","goods"}) State.AddRes(k, 90000);

            var terr = UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            float cx = State.VillageX!=null&&State.VillageX.Count>0?State.VillageX[0]:0f;
            float cz = State.VillageZ!=null&&State.VillageZ.Count>0?State.VillageZ[0]:0f;
            var used = new List<Vector3>();
            bool FlatLand(float x, float z){
                if(terr==null) return true;
                if(terr.IsWater(x,z) || !terr.InsideFrontier(x,z)) return false;
                float h=terr.HeightAt(x,z);
                foreach(var o in new[]{(3f,0f),(-3f,0f),(0f,3f),(0f,-3f)})
                    if(terr.IsWater(x+o.Item1,z+o.Item2) || Mathf.Abs(terr.HeightAt(x+o.Item1,z+o.Item2)-h)>1.2f) return false;
                foreach(var u in used) if(Vector2.Distance(new Vector2(u.x,u.z),new Vector2(x,z))<6f) return false;
                return true;
            }
            int Place(string id, float rMax){
                for(int r=6; r<=rMax; r+=5)
                    for(int a=0;a<360;a+=12){
                        float x=cx+r*Mathf.Cos(a*Mathf.Deg2Rad), z=cz+r*Mathf.Sin(a*Mathf.Deg2Rad);
                        if(FlatLand(x,z)){ var be=Building.PlaceInitial(id,x,z); if(be!=null){ used.Add(new Vector3(x,0,z)); return 1; } }
                    }
                return 0;
            }
            int placed=0;
            for(int i=0;i<6;i++) placed+=Place("arterial",120);
            for(int i=0;i<4;i++) placed+=Place("highway_modern",150);
            placed+=Place("interchange",170);
            for(int i=0;i<3;i++){ placed+=Place("factory_modern",140); placed+=Place("supermarket",120); }
            for(int i=0;i<6;i++) placed+=Place("apartment",110);
            Debug.Log($"[ROAD] deterministic placed={placed} buildings={State.Buildings.Count}");

            ModernTraffic.RefreshNow();
            for(int i=0;i<160;i++) ModernTraffic.Tick(2f);
            Debug.Log(ModernTraffic.Diagnose());
        }

        /// <summary>V9.0.4 工业与能源链：A=有工厂无电厂（缺电+断链+高污染），B=补齐电厂/产业链/公园（满供+齐链+低污染），
        /// 回读电网供需比、缺电系数、产业链乘数、污染指数与商品产出率（无参 Web 探针）。</summary>
        public void WebIndustryEnergy()
        {
            Time?.DebugJumpTo(5010); State.Era = 6; State.AgeOfSail = true;
            foreach (var k in new[]{"wood","stone","food","gold","iron","steel","concrete","bronze","goods"}) State.AddRes(k, 90000);

            var terr = UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            float cx = State.VillageX!=null&&State.VillageX.Count>0?State.VillageX[0]:0f;
            float cz = State.VillageZ!=null&&State.VillageZ.Count>0?State.VillageZ[0]:0f;
            var used = new List<Vector3>();
            bool FlatLand(float x, float z){
                if(terr==null) return true;
                if(terr.IsWater(x,z) || !terr.InsideFrontier(x,z)) return false;
                float h=terr.HeightAt(x,z);
                foreach(var o in new[]{(3f,0f),(-3f,0f),(0f,3f),(0f,-3f)})
                    if(terr.IsWater(x+o.Item1,z+o.Item2) || Mathf.Abs(terr.HeightAt(x+o.Item1,z+o.Item2)-h)>1.2f) return false;
                foreach(var u in used) if(Vector2.Distance(new Vector2(u.x,u.z),new Vector2(x,z))<6f) return false;
                return true;
            }
            int Place(string id, float rMax){
                for(int r=6; r<=rMax; r+=5)
                    for(int a=0;a<360;a+=12){
                        float x=cx+r*Mathf.Cos(a*Mathf.Deg2Rad), z=cz+r*Mathf.Sin(a*Mathf.Deg2Rad);
                        if(FlatLand(x,z)){ var be=Building.PlaceInitial(id,x,z); if(be!=null){ used.Add(new Vector3(x,0,z)); return 1; } }
                    }
                return 0;
            }
            float GoodsRate(){ for(int i=0;i<5;i++) Economy.Tick(1f); return Economy.ProductionRate["goods"]; }

            // —— 场景 A：高用电工业+住宅，无电厂、无上游产业链、无绿化 ——
            int pa=0;
            for(int i=0;i<4;i++) pa+=Place("factory_modern",150);
            pa+=Place("data_center",150); pa+=Place("skyscraper",120);
            for(int i=0;i<4;i++) pa+=Place("apartment",120);
            for(int i=0;i<2;i++) pa+=Place("supermarket",120);
            for(int i=0;i<4;i++) Infra.OnYear(State.Year);
            float gA=GoodsRate();
            Debug.Log($"[IND-A] placed={pa} goodsRate={gA:F1} :: {Infra.Diagnose()}");

            // —— 场景 B：补 4 电厂（满供）+ 采集/加工/零售产业链 + 14 公园消解污染 ——
            int pb=0;
            for(int i=0;i<4;i++) pb+=Place("power_plant",170);
            pb+=Place("lumbermill",150); pb+=Place("mine",150);
            pb+=Place("workshop",150); pb+=Place("iron_smelter",150);
            for(int i=0;i<2;i++) pb+=Place("market",150);
            for(int i=0;i<14;i++) pb+=Place("park",150);
            for(int i=0;i<4;i++) Infra.OnYear(State.Year);
            float gB=GoodsRate();
            Debug.Log($"[IND-B] added={pb} goodsRate={gB:F1} goodsBoost={(gA>0?gB/gA:0):F2}x :: {Infra.Diagnose()}");
            Debug.Log($"[IND] epidemicFactor A->B 污染越大概率越高，当前={Infra.EpidemicFactor():F2}");
        }

        /// <summary>V9.0.5 城市指标：A=人口膨胀但无城镇岗位（高失业），B=配套+工厂+写字楼充分就业高覆盖，
        /// 回读学生/劳力/岗位/失业率与健康/教育/治安/幸福（无参 Web 探针）。</summary>
        public void WebCityMetrics()
        {
            Time?.DebugJumpTo(5010); State.Era = 6; State.AgeOfSail = true;
            foreach (var k in new[]{"wood","stone","food","gold","iron","steel","concrete","bronze","goods"}) State.AddRes(k, 90000);

            var terr = UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            float cx = State.VillageX!=null&&State.VillageX.Count>0?State.VillageX[0]:0f;
            float cz = State.VillageZ!=null&&State.VillageZ.Count>0?State.VillageZ[0]:0f;
            var used = new List<Vector3>();
            bool FlatLand(float x, float z){
                if(terr==null) return true;
                if(terr.IsWater(x,z) || !terr.InsideFrontier(x,z)) return false;
                float h=terr.HeightAt(x,z);
                foreach(var o in new[]{(3f,0f),(-3f,0f),(0f,3f),(0f,-3f)})
                    if(terr.IsWater(x+o.Item1,z+o.Item2) || Mathf.Abs(terr.HeightAt(x+o.Item1,z+o.Item2)-h)>1.2f) return false;
                foreach(var u in used) if(Vector2.Distance(new Vector2(u.x,u.z),new Vector2(x,z))<6f) return false;
                return true;
            }
            int Place(string id, float rMax){
                for(int r=6; r<=rMax; r+=5)
                    for(int a=0;a<360;a+=12){
                        float x=cx+r*Mathf.Cos(a*Mathf.Deg2Rad), z=cz+r*Mathf.Sin(a*Mathf.Deg2Rad);
                        if(FlatLand(x,z)){ var be=Building.PlaceInitial(id,x,z); if(be!=null){ used.Add(new Vector3(x,0,z)); return 1; } }
                    }
                return 0;
            }
            // —— 场景 A：人口膨胀到 1500，不补城镇岗位（现代农业兜底仅 0.18/人）——
            State.Pop=1500; Population.NormalizeAge();
            CityMetrics.OnYear(State.Year);
            Debug.Log($"[METRIC-A] pop=1500 无城镇岗位 :: {CityMetrics.Diagnose()}");

            // —— 场景 B：配套设施+工厂+写字楼+住宅，人口回落到 600 ——
            int placed=0;
            for(int i=0;i<2;i++){ placed+=Place("fire_station",150); placed+=Place("police_station",150); placed+=Place("hospital",150); placed+=Place("modern_school",150); }
            for(int i=0;i<4;i++) placed+=Place("park",150);
            for(int i=0;i<3;i++) placed+=Place("supermarket",150);
            for(int i=0;i<4;i++) placed+=Place("factory_modern",170);
            placed+=Place("skyscraper",150);
            for(int i=0;i<8;i++) placed+=Place("apartment",130);
            State.Pop=600; Population.NormalizeAge();
            CityServices.RecomputeCoverage();
            CityMetrics.OnYear(State.Year);
            CityMetrics.OnYear(State.Year);   // 第二次让平滑指标贴近目标
            Debug.Log($"[METRIC-B] placed={placed} pop=600 配套+工业+写字楼 :: {CityMetrics.Diagnose()}");
        }

        /// <summary>V9.0.6 应急车辆与航班：确定性落位消防/警局/医院/机场+道路，派出三类应急车驶向事件点再返回，
        /// 强制 3 架航班进入起降循环，分段回读在途/完成数、车辆是否下水、航班相位（无参 Web 探针）。</summary>
        public void WebEmergency()
        {
            Time?.DebugJumpTo(5010); State.Era = 6; State.AgeOfSail = true;
            foreach (var k in new[]{"wood","stone","food","gold","iron","steel","concrete","bronze","goods"}) State.AddRes(k, 90000);

            var terr = UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            float cx = State.VillageX!=null&&State.VillageX.Count>0?State.VillageX[0]:0f;
            float cz = State.VillageZ!=null&&State.VillageZ.Count>0?State.VillageZ[0]:0f;
            var used = new List<Vector3>();
            bool FlatLand(float x, float z){
                if(terr==null) return true;
                if(terr.IsWater(x,z) || !terr.InsideFrontier(x,z)) return false;
                float h=terr.HeightAt(x,z);
                foreach(var o in new[]{(3f,0f),(-3f,0f),(0f,3f),(0f,-3f)})
                    if(terr.IsWater(x+o.Item1,z+o.Item2) || Mathf.Abs(terr.HeightAt(x+o.Item1,z+o.Item2)-h)>1.2f) return false;
                foreach(var u in used) if(Vector2.Distance(new Vector2(u.x,u.z),new Vector2(x,z))<6f) return false;
                return true;
            }
            int Place(string id, float rMax){
                for(int r=6; r<=rMax; r+=5)
                    for(int a=0;a<360;a+=12){
                        float x=cx+r*Mathf.Cos(a*Mathf.Deg2Rad), z=cz+r*Mathf.Sin(a*Mathf.Deg2Rad);
                        if(FlatLand(x,z)){ var be=Building.PlaceInitial(id,x,z); if(be!=null){ used.Add(new Vector3(x,0,z)); return 1; } }
                    }
                return 0;
            }
            Place("fire_station",40); Place("police_station",40); Place("hospital",40); Place("airport",120);
            for(int i=0;i<8;i++) Place("arterial",120);
            ModernTraffic.RefreshNow();
            for(int i=0;i<10;i++) ModernTraffic.Tick(2f);   // 让机场生成满 3 架客机

            // 三类应急车：事件点在村心外 55~70 单位
            bool f=false,p=false,m=false;
            for(int g=0; g<24; g++){
                float a=g*15f*Mathf.Deg2Rad, tx=cx+62f*Mathf.Cos(a), tz=cz+62f*Mathf.Sin(a);
                if(terr!=null && (terr.IsWater(tx,tz)||!terr.InsideFrontier(tx,tz))) continue;
                if(!f){ f=ModernTraffic.DispatchEmergency("fire_station",tx,tz); if(f) continue; }
                if(!p){ p=ModernTraffic.DispatchEmergency("police_station",tx+6,tz); if(p) continue; }
                if(!m){ m=ModernTraffic.DispatchEmergency("hospital",tx-6,tz); }
                if(f&&p&&m) break;
            }
            Debug.Log($"[EM] dispatch fire={f} police={p} ambulance={m} :: {ModernTraffic.Diagnose()}");
            for(int i=0;i<6;i++) ModernTraffic.Tick(2f);
            Debug.Log($"[EM] en-route t≈12s :: {ModernTraffic.Diagnose()}");
            ModernTraffic.DebugForceFlightCycle();
            for(int i=0;i<5;i++) ModernTraffic.Tick(2f);
            Debug.Log($"[EM] flights landing t≈10s :: {ModernTraffic.Diagnose()}");
            for(int i=0;i<90;i++) ModernTraffic.Tick(2f);   // 应急车完成处置并回站；航班落地-停靠-起飞
            Debug.Log($"[EM] settled t≈180s :: {ModernTraffic.Diagnose()}");
        }

        /// <summary>V9.0.7 城市财政：落位配套+商业+道路，人口 1600（大都市阈值），三档税率各结算一年，
        /// 回读城市等级/收支/地价/幸福变化（无参 Web 探针）。</summary>
        public void WebCityFinance()
        {
            Time?.DebugJumpTo(5010); State.Era = 6; State.AgeOfSail = true;
            foreach (var k in new[]{"wood","stone","food","gold","iron","steel","concrete","bronze","goods"}) State.AddRes(k, 90000);

            var terr = UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            float cx = State.VillageX!=null&&State.VillageX.Count>0?State.VillageX[0]:0f;
            float cz = State.VillageZ!=null&&State.VillageZ.Count>0?State.VillageZ[0]:0f;
            var used = new List<Vector3>();
            bool FlatLand(float x, float z){
                if(terr==null) return true;
                if(terr.IsWater(x,z) || !terr.InsideFrontier(x,z)) return false;
                float h=terr.HeightAt(x,z);
                foreach(var o in new[]{(3f,0f),(-3f,0f),(0f,3f),(0f,-3f)})
                    if(terr.IsWater(x+o.Item1,z+o.Item2) || Mathf.Abs(terr.HeightAt(x+o.Item1,z+o.Item2)-h)>1.2f) return false;
                foreach(var u in used) if(Vector2.Distance(new Vector2(u.x,u.z),new Vector2(x,z))<6f) return false;
                return true;
            }
            int Place(string id, float rMax){
                for(int r=6; r<=rMax; r+=5)
                    for(int a=0;a<360;a+=12){
                        float x=cx+r*Mathf.Cos(a*Mathf.Deg2Rad), z=cz+r*Mathf.Sin(a*Mathf.Deg2Rad);
                        if(FlatLand(x,z)){ var be=Building.PlaceInitial(id,x,z); if(be!=null){ used.Add(new Vector3(x,0,z)); return 1; } }
                    }
                return 0;
            }
            int placed=0;
            for(int i=0;i<2;i++){ placed+=Place("fire_station",150); placed+=Place("police_station",150); placed+=Place("hospital",150); placed+=Place("modern_school",150); }
            for(int i=0;i<3;i++) placed+=Place("park",150);
            for(int i=0;i<3;i++) placed+=Place("supermarket",150);
            placed+=Place("skyscraper",150);
            for(int i=0;i<6;i++) Place("arterial",170);
            CityServices.RecomputeCoverage(); CityMetrics.OnYear(State.Year);
            State.Pop=1600; Population.NormalizeAge();
            foreach(int lvl in new[]{0,1,2})
            {
                State.CityTaxLevel=lvl;
                float gold0=State.GetRes("gold"), happy0=State.Happiness;
                CityFinance.OnYear(State.Year);
                Debug.Log($"[FIN] taxLvl={lvl}({CityFinanceSystem.TaxNames[lvl]}) placed={placed} pop=1600 " +
                          $"gold {gold0:F0}->{State.GetRes("gold"):F0}(净{State.GetRes("gold")-gold0:F0}) happy {happy0:F1}->{State.Happiness:F1} :: {CityFinance.Diagnose()}");
            }
        }

        // ===== V6.8.2 回归探针：强制退潮/涨潮，回读每艘船是否被拖进淡水湖或卡在陆地 =====
        public void WebTideEbb()
        {
            var terr=UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            if(terr==null||Naval==null||Tide==null){Debug.Log("[WEB] TideEbb missing refs");return;}
            // V6.8.3 自愈网确定性测试：把一艘真船分别放进大湖/内河，几帧内必须被 KeepAtSea 拉回外海
            if(State.Ships.Count>0){
                var vic=State.Ships[0]; float ox=vic.X,oz=vic.Z;
                float lakeX=0,lakeZ=0,rivX=0,rivZ=0; bool lk=false,rv=false;
                float TT=PixelToCivilization.Data.GameConstants.Tile;
                for(float gx=-380f; gx<=380f && (!lk||!rv); gx+=TT*3f)
                  for(float gz=-380f; gz<=380f && (!lk||!rv); gz+=TT*3f){
                    var bm=terr.BiomeAt(gx,gz);
                    if(!lk && bm==PixelToCivilization.World.BiomeKind.FreshWater && terr.IsWater(gx,gz)){lakeX=gx;lakeZ=gz;lk=true;}
                    if(!rv && bm==PixelToCivilization.World.BiomeKind.River && terr.IsWater(gx,gz)){rivX=gx;rivZ=gz;rv=true;}
                  }
                if(lk){ vic.X=lakeX;vic.Z=lakeZ; for(int k=0;k<12;k++) Naval.Tick(0.4f);
                        bool heal=terr.IsOceanWater(vic.X,vic.Z);
                        Debug.Log($"[WEB] Tide LAKE_HEAL={(heal?"PASS":"FAIL")} now=({vic.X:F0},{vic.Z:F0})"); }
                else Debug.Log("[WEB] Tide LAKE_HEAL=notile");
                if(rv){ vic.X=rivX;vic.Z=rivZ; for(int k=0;k<12;k++) Naval.Tick(0.4f);
                        bool heal=terr.IsOceanWater(vic.X,vic.Z);
                        Debug.Log($"[WEB] Tide RIVER_HEAL={(heal?"PASS":"FAIL")} now=({vic.X:F0},{vic.Z:F0})"); }
                else Debug.Log("[WEB] Tide RIVER_HEAL=notile");
                vic.X=ox;vic.Z=oz;
            }
            System.Action<string,int> report=(tag,day)=>{
                int ocean=0,beach=0,inland=0; string sample="";
                foreach(var sh in State.Ships){
                    bool sea=terr.IsOceanWater(sh.X,sh.Z);
                    bool dry=!terr.IsWater(sh.X,sh.Z);
                    bool tideFlat=dry && terr.BiomeAt(sh.X,sh.Z)==PixelToCivilization.World.BiomeKind.Default
                                      && terr.HeightAt(sh.X,sh.Z)<PixelToCivilization.Data.GameConstants.WaterLevel;
                    if(sea)ocean++; else if(tideFlat)beach++; else inland++;
                    if(sample.Length<170) sample+=($"({sh.X:F0},{sh.Z:F0}:{(sea?"sea":tideFlat?"flat":"INLAND")}) ");
                }
                Debug.Log($"[WEB] Tide {tag} day={day} ships={State.Ships.Count} ocean={ocean} beached={beach} IN_INLAND={inland} tide={State.MonthlyTide:F2} :: {sample}");
            };
            State.Day=22f;                                   // 退潮：主大陆近岸露出
            for(int i=0;i<1500;i++){ Tide.Tick(0.016f); Naval.Tick(0.4f); }
            report("EBB",22);
            State.Day=8f;                                    // 涨潮：近岸重新没水，坐滩船应复浮
            for(int i=0;i<900;i++){ Tide.Tick(0.016f); Naval.Tick(0.4f); }
            report("FLOOD",8);
        }
        public void WebQuickLoad(){ bool ok=SaveSystem!=null && SaveSystem.LoadFromSlot(1); Debug.Log("[Web] QuickLoad "+(ok?"OK":"FAIL")); }
        public void WebAdvanceEra(){ bool ok=Time!=null && Time.DebugAdvanceEra(); Debug.Log("[Web] AdvanceEra "+(ok?"OK":"FAIL")); }
        public void WebNextDynasty(){ bool ok=Time!=null && Time.DebugNextDynasty(); Debug.Log("[Web] NextDynasty "+(ok?"OK":"FAIL")); }
        public void WebProbeBridges(){ Bridge?.DebugProbe(); }
        public void WebForceBridge(){ bool ok=Bridge!=null&&Bridge.ForceNearest(); Debug.Log("[Web] ForceBridge "+(ok?"OK":"FAIL")); }
        public void WebToggleLeftPanel(){ UIManager.Instance?.WebToggleLeft(); }
        public void WebToggleRightPanel(){ UIManager.Instance?.WebToggleRight(); }

        // ===== V6.1.9(i) 天气/军事 Web 回归入口（无参）=====
        public void WebNextWeather(){ Weather?.ForceNext(); Debug.Log("[Web] NextWeather kind="+(Weather!=null?(int)Weather.Current:-1)); }
        public void WebTrainSquad(){ State.AddRes("food",800); State.Pop=Mathf.Max(State.Pop,40);
            bool a=Military.TrainSoldiers(), b=Military.TrainSoldiers();
            Debug.Log("[Web] TrainSquad a="+a+" b="+b+" pop="+State.Pop+" food="+State.GetRes("food")+" fu="+State.FriendlyUnits.Count); }

        // V6.3.7(真扩展) 浏览器自证探针：打印当前游戏年、扩张倍率、真实活动疆域边长/半幅/活动网格、全量上限
        public void WebProbeExpand(){
            var t=UnityEngine.Object.FindObjectOfType<World.WorldGenerator>();
            int yr=State!=null?State.Year:0;
            float fac=WorldExpansionSystem.ExpandFactor(yr);
            if(t==null){Debug.Log("[ExpandProbe] terrain null");return;}
            Debug.Log($"[ExpandProbe] year={yr} factor={fac:F3} activeHalf={t.ActiveHalf:F1} activeWorld={t.ActiveWorld:F1} activeN={t.ActiveN} fullGW={t.GW:F0} fullG={t.G} lands={t.LandmassCount} bridges={State.BridgeRuns.Count/8} pop={State.Pop}");
        }
        /// <summary>V6.5.4 实时增陆回归：记录初始陆块数→逐年跳到第2001年（触发10次百年岛/2次五百年次陆/1次千年主陆）→再探针</summary>
        public void WebGrowTest()
        {
            var t=UnityEngine.Object.FindObjectOfType<World.WorldGenerator>();
            int before=t!=null?t.LandmassCount:-1;
            Time?.DebugJumpTo(2001);
            int after=t!=null?t.LandmassCount:-1;
            float fac=WorldExpansionSystem.ExpandFactor(State.Year);
            Debug.Log($"[GROWTEST] lands {before}->{after} (新增{after-before}) year={State.Year} factor={fac:F3} activeHalf={t.ActiveHalf:F1} bridges={State.BridgeRuns.Count/8}");
        }

        // ===== V6.1.8 九智能体共治 Web 入口（无参，供 UI/浏览器/自动化回归）=====
        public void WebCouncilNow(){ Council?.CouncilNow(); Debug.Log("[Web] CouncilNow continuity="+ (Council!=null?Council.ComputeContinuity():-1)); }
        public void WebCouncilToggle(){ Council?.ToggleEnabled(); Debug.Log("[Web] Council enabled="+(Council!=null&&Council.Enabled)); }
        public void WebCouncilOnline(){ Council?.SetOnline(true); Debug.Log("[Web] Council online"); }
        public void WebCouncilOffline(){ Council?.SetOnline(false); Debug.Log("[Web] Council offline"); }
        /// <summary>V6.1.9 立即触发一次九神议政（含联网请求），用于浏览器验证 ARK 连通/CORS</summary>
        public void WebCouncilOnce(){ if(Council==null){Debug.Log("[Web] Council null");return;} Council.SetOnline(true); Council.CouncilNow(); Debug.Log("[Web] CouncilOnce online model="+Council.Model+" requesting, 请观察后续联网结果"); }
        // ---- V6.1.9 加速冷冻 Web 回归入口 ----
        /// <summary>立即进入冷冻冷却（300现实秒），验证限倍与倒计时</summary>
        public void WebCryoFreeze(){ State.CryoActive=true; State.CryoRemainSec=GameConstants.CryoCooldownSec; State.CryoAccumYears=GameConstants.CryoYearThreshold; Debug.Log("[Web] CryoFreeze active, speed will cap at "+EffectiveSpeed); }
        /// <summary>立即解冻并重置累计年数</summary>
        public void WebCryoThaw(){ ThawCryo(false); Debug.Log("[Web] CryoThaw active="+State.CryoActive+" accum="+State.CryoAccumYears+" eff="+EffectiveSpeed); }
        /// <summary>把加速累计年数设到阈值前1年，随后加速推进1年即应触发冷冻（验证自动触发）</summary>
        public void WebCryoArm(){ State.CryoAccumYears=GameConstants.CryoYearThreshold-1f; State.CryoActive=false; Debug.Log("[Web] CryoArm accum="+State.CryoAccumYears); }
        /// <summary>万年存续压测：从当前逐年补算到第10000游戏年，输出人口/存续分/兜底次数，验证文明不断绝</summary>
        public void WebMillenniumTest()
        {
            int pop0=State.Pop;
            Time?.DebugJumpTo(10000);
            Council?.SafetyNet(); float c=Council!=null?Council.ComputeContinuity():-1;
            int floor=Council!=null?Council.PopFloor:12;
            bool survive=State.Pop>=floor;
            Debug.Log($"[MILLENNIUM] 到第{State.Year}年 公历{Time?.GregorianYear} 人口{pop0}->{State.Pop}(硬底{floor}) 存续{c:F1} 兜底{Council?.SafetyCount} 存活={survive}");
        }

        /// <summary>V6.1.4→6.1.6 运行时自检（WebGL 自动化回归钩子；逐步 try，结果以 [SELFTEST] 打到控制台，不影响正常玩法）</summary>
        public void WebSelfTestV616()
        {
            int pass=0, fail=0; var log=new System.Text.StringBuilder();
            void Step(string name, System.Action act)
            {
                try{ act(); pass++; Debug.Log("[SELFTEST] OK "+name); }
                catch(System.Exception e){ fail++; Debug.Log("[SELFTEST] FAIL "+name+" => "+e.Message); }
            }
            var S=State;
            Step("给资源",()=>{ foreach(var k in new[]{"wood","stone","gold","food","iron","steel","fusion","carbon"}) S.AddRes(k,99999); });
            Step("进入航海时代",()=>{ S.Era=4; S.AgeOfSail=true; });
            Step("组建步骑机动部队",()=>{
                float hx=S.VillageX.Count>0?S.VillageX[0]:0f, hz=S.VillageZ.Count>0?S.VillageZ[0]:0f;
                S.FriendlyUnits.Add(new FriendlyUnit{Kind=0,X=hx,Z=hz,HomeX=hx,HomeZ=hz,Hp=50,MaxHp=50,Attack=6,Speed=1.6f});
                S.FriendlyUnits.Add(new FriendlyUnit{Kind=1,X=hx,Z=hz,HomeX=hx,HomeZ=hz,Hp=60,MaxHp=60,Attack=9,Speed=3.2f});
                if(S.FriendlyUnits.Count<2) throw new System.Exception("部队未入列");
            });
            Step("训练骑兵接口",()=>{ Debug.Log("[SELFTEST] TrainCavalry(无马厩可false)="+Military.TrainCavalry()); });
            Step("讨伐首个割据势力",()=>{
                Military.InitFactions();
                var f=Military.Factions.Find(x=>!x.Destroyed);
                if(f==null) throw new System.Exception("无割据势力");
                if(!Military.LaunchCampaign(f.Id)) throw new System.Exception("LaunchCampaign=false");
            });
            Step("海战生成我方战船",()=>{ if(Naval.DebugSpawnOwnShip()==null) throw new System.Exception("造舰失败"); });
            Step("建立殖民地",()=>{
                if(!Colonization.EraOpen) throw new System.Exception("殖民时代未开");
                if(!Colonization.FoundColony()){ string why; Colonization.CanFound(out why); throw new System.Exception("FoundColony=false:"+why); }
            });
            Step("殖民地升格与镇压",()=>{
                var c=S.Colonies[S.Colonies.Count-1];
                if(!Colonization.Upgrade(c)) throw new System.Exception("Upgrade=false");
                Colonization.Suppress(c);
            });
            Step("海洋副本探索",()=>{
                Expedition.Prepare("ocean");
                for(int i=0;i<6;i++) Expedition.Move("ocean", i%2==0?1:0, i%2==0?0:1);
                Expedition.AutoExplore("ocean"); Expedition.ColonizeHere(); Expedition.ReturnHome("ocean");
                if(!S.OceanExp.Inited) throw new System.Exception("海洋网格未初始化");
            });
            Step("太空副本探索",()=>{
                Expedition.Prepare("space");
                for(int i=0;i<6;i++) Expedition.AutoExplore("space");
                Expedition.BuildOutpostHere(); Expedition.ReturnHome("space");
                if(!S.SpaceExp.Inited) throw new System.Exception("太空网格未初始化");
            });
            Step("存档读档往返",()=>{
                int col=S.Colonies.Count, fu=S.FriendlyUnits.Count; bool oe=S.OceanExp.Inited;
                SaveSystem.SaveToSlot(3);
                if(!SaveSystem.LoadFromSlot(3)) throw new System.Exception("读档失败");
                if(S.Colonies.Count!=col) throw new System.Exception("殖民地未保留 "+S.Colonies.Count+"/"+col);
                if(S.FriendlyUnits.Count!=fu) throw new System.Exception("机动部队未保留 "+S.FriendlyUnits.Count+"/"+fu);
                if(oe && !S.OceanExp.Inited) throw new System.Exception("海洋副本网格未保留");
            });
            Debug.Log("[SELFTEST] ==== V616 RESULT pass="+pass+" fail="+fail+" :: "+log);
        }
        /// <summary>加速：0-9 一次+1；10-99 一次+10；≥100 一次+100（对齐 v5.9.9 speedUp），并解除暂停</summary>
        public void SpeedUp()
        {
            float s = State.Speed;
            if (s < 10f) s = Mathf.Min(10f, s + 1f);
            else if (s < 100f) s = Mathf.Min(100f, s + 10f);
            else s = Mathf.Min(MaxSpeed, s + 100f);
            State.Speed = s; State.Paused = false; StateType = GameStateType.Playing;
        }
        /// <summary>减速：>100 一次-100；>10 一次-10；>1 一次-1，最低1倍速（对齐 v5.9.9 speedDown）</summary>
        public void SpeedDown()
        {
            float s = State.Speed;
            if (s > 100f) s = Mathf.Max(100f, s - 100f);
            else if (s > 10f) s = Mathf.Max(10f, s - 10f);
            else if (s > 1f) s = Mathf.Max(1f, s - 1f);
            State.Speed = s;
        }
        /// <summary>滑条设速：取整并限制在 [1, MaxSpeed]</summary>
        public void SetSpeedClamped(float v) => State.Speed = Mathf.Clamp(Mathf.Round(v), 1f, MaxSpeed);
        public void SetSpeed(float v) => State.Speed = v;

        /// <summary>Debug密码门：ToFuture解锁1000倍速与全部修改功能</summary>
        public bool TryUnlockDebug(string password)
        {
            if (password == GameConstants.DebugPassword)
            {
                State.DebugLevel = 2;
                AddEvent("good", "🔓 Debug面板已解锁（1000倍速/全资源/时代跳转）");
                return true;
            }
            AddEvent("bad", "🔒 密码错误");
            return false;
        }

        // ===== 事件日志 =====
        public void AddEvent(string kind, string text)
        {
            var e = new LogEntry(State.Year, kind, text);
            State.EventLog.Insert(0, e);
            if (State.EventLog.Count > 200) State.EventLog.RemoveAt(State.EventLog.Count - 1);
            OnEventLogged?.Invoke(e);
        }

        public BuildingDefinition Def(string id) => Buildings.TryGetValue(id, out var d) ? d : null;
        public EraDefinition Era => Eras != null && State.Era < Eras.Count ? Eras[State.Era] : null;
    }
}
