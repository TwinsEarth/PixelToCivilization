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
        public GroundWarfareSystem Ground;   // V9.4.6 地面作战部队：坦克/装甲车/导弹车（1949 起，战斗规则参照军舰）
        public RallySystem Rally;            // V9.6.0 紧急集结令（插旗：蓝=海军/绿=地面/红=军人，100 格列阵）
        public WarBroadcastSystem War;       // V9.6.1 战场战时广播（传令兵语音/横幅/双方战损）
        public AirLiftSystem AirLift;        // V9.6.1 远程投送（>100 格集结：运输机/直升机空运）
        public CombatSystem Combat;   // V9.4.7 统一战斗目录：五类单位跨阵营索敌/伤害分发，建筑（除树）可破坏
        public OceanExpansionSystem Ocean;
        public SpaceExpansionSystem Space;
        public CanalSystem Canal;
        public World.VegetationSystem Veg;   // V9.3.8 树木属性面板：植被系统引用（Populate/读档后赋值）
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
        /// <summary>V9.6.4 内存架构中枢（预算水位/临界钳制/GC采样/慢系统/池报告）</summary>
        public MemoryBudgetManager MemBudget;
        /// <summary>V9.6.5 崩溃架构中枢（异常捕获/崩溃标记/环形日志/安全模式/恢复引导）</summary>
        public CrashGuardSystem Guard;
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
        Add(Ground = gameObject.GetComponent<GroundWarfareSystem>() ?? gameObject.AddComponent<GroundWarfareSystem>()); // V9.4.6 地面作战部队
        Add(Combat = gameObject.GetComponent<CombatSystem>() ?? gameObject.AddComponent<CombatSystem>()); // V9.4.7 统一战斗目录（置于作战单位之后，Tick 末段用最新位置重建）
        Add(Rally = gameObject.GetComponent<RallySystem>() ?? gameObject.AddComponent<RallySystem>()); // V9.6.0 紧急集结令（蓝旗海军/绿旗地面/红旗军人，100格集结）
        Add(War = gameObject.GetComponent<WarBroadcastSystem>() ?? gameObject.AddComponent<WarBroadcastSystem>()); // V9.6.1 战时广播（传令兵语音/战报/双方战损）
        Add(AirLift = gameObject.GetComponent<AirLiftSystem>() ?? gameObject.AddComponent<AirLiftSystem>()); // V9.6.1 远程投送（>100格集结→运输机/直升机空运）
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
            // V9.6.4 内存架构中枢：预算水位/临界钳制/GC采样/慢系统统计/池报告（非游戏系统，挂 GM 下用 Update 现实秒采样）
            MemBudget = gameObject.GetComponent<MemoryBudgetManager>() ?? gameObject.AddComponent<MemoryBudgetManager>();
            // V9.6.5 崩溃架构中枢：异常钩子（logMessageReceived + WebGL JS onerror）+ 崩溃标记 + 环形日志 + 安全模式
            Guard = gameObject.GetComponent<CrashGuardSystem>() ?? gameObject.AddComponent<CrashGuardSystem>();
            Guard.Init();
            // V9.6.6 存档架构：ISaveable 注册（崩溃环形日志随档携带）+ 启动清理原子写残留临时键
            SaveSystem.Register(Guard);
            SaveSystem.CleanupTempKeys();
            Debug.Log("[GameManager] 子系统装配完成，数量=" + _systems.Count);
        }

        private void Add(GameSystemBase s) { if (!_systems.Contains(s)) _systems.Add(s); }

        public void StartNewGame()
        {
            State.Reset();
            // V9.4.0 新文明新历史：清空九神历史日志与议政节流（持久单例跨局复用）
            if (Council != null) { Council.History.Clear(); Council.LastCouncilYear = 0; }
            // V9.5.0 作战系统运行时列表不进 GameState，新局必须销毁视图并清空（敌舰/舰载机/地面部队/编队），防上一局残留
            Military?.ResetForNewGame();
            Naval?.ResetForNewGame();
            Ground?.ResetForNewGame();
            // V9.6.1 战时广播/远程投送为运行时态，新局必须清零防上一局残留
            War?.ResetRuntime();
            AirLift?.ResetRuntime();
            // V9.0.1 开局公元1700（游戏年4700·清康熙·大航海殖民末期）：静默把朝代/时代对齐到 era4，不连发 0→4 时代切换事件
            Time?.SnapToStartYear();
            // V9.6.5 玩家主动开始新局 = 自愿放弃崩溃恢复 → 清崩溃标记
            CrashGuardSystem.MarkClean();
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
            Veg=veg;   // V9.3.8 树面板：缓存植被系统引用
            int seed = World.WorldGenerator.RandomSeed();
            Vector3 village = Vector3.zero;
            bool earth=NextEarthMode;   // V9.1.1 地球模式：先进经典地图片头，再黑场干净切换到真实地球（杜绝两图叠加）
            if (terrain != null)
            {
                Debug.Log("[S1] regen start");
                village = terrain.Regenerate(seed);
                Debug.Log("[S2] veg regrow");
                veg?.Regrow(terrain, seed);
                Debug.Log("[S3] veg done");
                Debug.Log($"[StartNewRandomGame] 经典片头 seed={seed} 大陆半径={terrain.LandRadius:F0} " +
                          $"海{terrain.SeaRatio:P0} 陆{terrain.LandRatio:P0} 山{terrain.MountainRatio:P0} 沙漠{terrain.DesertRatio:P0} earthPending={earth}");
            }

            // 3) 相机归位新村址
            Debug.Log("[S4] retarget");
            var rig = UnityEngine.Object.FindObjectOfType<World.CameraRig>();
            rig?.Retarget(village);

            // 4) 状态重置并进入游戏
            Debug.Log("[S5] StartNewGame");
            StartNewGame();
            Debug.Log("[S6] MilitaryReset");
            Military?.ResetForNewGame();   // V9.1.1 新局清理上一局割据/远征军据点
            State.EarthMode=false;        // V9.1.1 片头阶段为经典地形，切换时由 SwitchToEarthImmediate 置 true

            // 5) 小地图地形底图作废重烘焙
            Debug.Log("[S7] minimap invalidate");
            UIManager.Instance?.InvalidateMinimapBase();

            // 6) 初始聚落/职业人口/飞鸟群
            Debug.Log("[S8] PopulateInitial");
            Env?.PopulateInitial();
            Debug.Log("[S9] done");

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
            Veg=veg;   // V9.3.8 树面板：地球切换后刷新引用
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
            // V9.7.0 架构喂数（节流 0.5s）：SoA 人口聚合 + 存档脏标记 + 崩溃上下文
            if (UnityEngine.Time.unscaledTime - _v970ArchTimer > 0.5f)
            {
                _v970ArchTimer = UnityEngine.Time.unscaledTime;
                try
                {
                    int fCount = 1 + (State != null && State.EnemyFactions != null ? State.EnemyFactions.Count : 0);
                    SoAPopulationStore.Reset();
                    if (State != null && State.Pop > 0 && fCount > 0)
                    {
                        // 同源聚合：Total 恒等于 State.Pop；势力/年龄段/职业分布为统计假设（探针明示，不改模拟路径）
                        int per = State.Pop / fCount, rem = State.Pop % fCount;
                        for (int f = 0; f < fCount; f++) SoAPopulationStore.FeedOne(f, 1, 0, per + (f < rem ? 1 : 0));
                    }
                    SaveDirtySystem.UpdateCheck(State);
                    LocalCrashReporter.SetContext("dynasty", "dyn" + (State != null ? State.DynastyIdx.ToString() : "?"));
                    LocalCrashReporter.SetContext("year", State != null ? State.Year.ToString() : "?");
                    LocalCrashReporter.SetContext("pop", State != null ? State.Pop.ToString() : "?");
                    LocalCrashReporter.SetContext("factions", fCount.ToString());
                    LocalCrashReporter.SetContext("era", State != null ? State.Era.ToString() : "?");
                    LocalCrashReporter.SetContext("speed", State != null ? State.Speed.ToString("F1") : "?");
                }
                catch (System.Exception e) { Debug.LogError("[V970-ARCH] " + e.GetType().Name + ": " + e.Message); }
            }
            if (StateType != GameStateType.Playing || State.Paused) return;
            float scaled = UnityEngine.Time.deltaTime * EffectiveSpeed; // 冷冻期实际倍速封顶10
            try { Time.Tick(scaled); } // 年份推进（内部按游戏年份换算朝代/时代/公历）
            catch(System.Exception e){ Debug.LogError("[SYSERR:GameTime] "+e.GetType().Name+": "+e.Message); }
            // V9.6.4 每系统 Tick 耗时采样（Time.realtimeSinceStartup 差值，零分配；每 120 帧汇总 Top5 慢系统）
            var mb = MemoryBudgetManager.Instance;
            foreach (var s in _systems)
            {
                float t0 = UnityEngine.Time.realtimeSinceStartup;
                try { s.Tick(scaled); }
                catch(System.Exception e){ Debug.LogError("[SYSERR:"+s.GetType().Name+"] "+e.GetType().Name+": "+e.Message); }
                if (mb != null) mb.ProfileTick(s.GetType().Name, (UnityEngine.Time.realtimeSinceStartup - t0) * 1000f);
            }
          }
          catch(System.Exception e){ Debug.LogError("[MARK_GM] "+e.GetType().Name+": "+e.Message+"\n"+e.StackTrace); }
        }

        // ===== V6.1.9 加速冷冻 =====
        /// <summary>冷冻冷却中实际生效的倍速（封顶 CryoMaxSpeed=10），非冷冻期等于设定倍速</summary>
        public float EffectiveSpeed => State.CryoActive ? Mathf.Min(State.Speed, GameConstants.CryoMaxSpeed) : State.Speed;
        private bool _cryoEntered;
        private float _v970ArchTimer;   // V9.7.0 架构喂数节流（0.5s 一次）
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
            // V9.3.5 暂停防呆：Toast 显著提示（此前仅速度按钮文字"暂停"，玩家误触空格易误判时间卡死）
            if (UIManager.Instance!=null) UIManager.Instance.Toast(State.Paused ? "⏸ 已暂停（空格恢复）" : "▶ 已恢复", !State.Paused);
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
        // V9.6.1 Web 探针：战时广播战损统计 + 顶部滚动条状态 + 空运投送状态
        public void WebV961Probe()
        {
            string war = War != null ? ("war:" + War.EnemyKills + "/" + War.SelfLosses) : "warnull";
            string tick = "ticker:none";
            var ui = UnityEngine.Object.FindObjectOfType<PixelToCivilization.UI.UIManager>();
            if (ui != null) tick = ui.TickerState();
            string air = AirLift != null ? AirLift.Probe() : "airnull";
            Debug.Log("[WEB] " + war + "|" + tick + "|" + air);
        }
        // V9.3.11 Debug：全部资源 +100 万（无参，供浏览器 SendMessage 回归与 Debug 控制台按钮）
        public void WebAddResources1M(){
            foreach(var k in ResourceDatabase.Order) State.AddRes(k,1000000);
            AddEvent("good","[DEBUG] 全部资源 +100 万");
            Debug.Log("[WEB] AddResources1M done food="+State.GetRes("food")+" gold="+State.GetRes("gold"));
        }

        // V9.3.12 剪贴板桥：JS（PXC_ReadClipboard）读取系统剪贴板后 SendMessage 到此处；ApiKeyPrompt 弹窗激活时直接填入输入框
        public void WebApplyClipboard(string text){
            if(string.IsNullOrEmpty(text)) return;
            text=text.Trim();
            if(PixelToCivilization.UI.ApiKeyPrompt.Current!=null){
                PixelToCivilization.UI.ApiKeyPrompt.Current.SetText(text);
                Debug.Log("[WEB] Clipboard applied len="+text.Length);
            } else {
                Debug.Log("[WEB] Clipboard recv no-prompt len="+text.Length);
            }
        }

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

        /// <summary>V9.7.1 jslib 回调：IndexedDB 正文库预载完成（GameBootstrap 协程继续推进）。</summary>
        public void OnStorageReady(string _) => PixelToCivilization.Platform.PxcStorage.MarkReady();
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

        // ===== V9.6.5 崩溃架构：恢复 / 模拟崩溃 / 探针 =====
        /// <summary>安全模式恢复：按 auto→rollback0→1→2 自动载入最近健康档并进入 Playing；无档则留在主菜单提示。</summary>
        public void TryCrashRecovery()
        {
            if (SaveSystem==null || State==null) return;
            State.Reset();
            string used="";
            if (SaveSystem.LoadBestForRecovery(out used))
            {
                State.Running=true; State.Paused=false; State.Speed=1f;
                StateType=GameStateType.Playing;
                OnStateChanged?.Invoke(StateType);
                CrashGuardSystem.MarkClean();
                AddEvent("bad","⚠️ 安全模式：上次异常退出，已自动恢复至 "+used+"（阴影已关，请检查资源与建筑）");
                UIManager.Instance?.Toast("⚠️ 已从"+used+"恢复（安全模式）",false);
                Debug.Log("[CrashGuard] 恢复成功："+used);
            }
            else
            {
                AddEvent("info","⚠️ 安全模式：上次异常退出，无可用回滚存档，请开始新局");
                Debug.LogWarning("[CrashGuard] 无可用回滚存档，停留主菜单");
            }
        }

        /// <summary>V9.6.5 模拟崩溃（供浏览器回归）：
        /// mode=0 抛托管异常（验证异常钩子+崩溃标记）；mode=1 模拟硬崩溃（写 crashed 标记，刷新后进安全模式）；
        /// mode=2 先损坏自动档再抛异常（验证下次启动回滚恢复）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebCrashSimulate(int mode)
        {
            if (Guard==null || SaveSystem==null) { Debug.LogWarning("[CrashSim] Guard/Save 未就绪"); return; }
            string result = "mode="+mode;
            try
            {
                if (mode==0)
                {
                    throw new System.InvalidOperationException("[CrashSim] 模拟托管异常：崩溃钩子应捕获并写崩溃标记");
                }
                else if (mode==1)
                {
                    CrashGuardSystem.LastCrashSummary = "模拟硬崩溃(强杀)";
                    UnityEngine.PlayerPrefs.SetString(CrashGuardSystem.KeyState,"crashed");
                    UnityEngine.PlayerPrefs.SetString(CrashGuardSystem.KeyLast,CrashGuardSystem.LastCrashSummary);
                    UnityEngine.PlayerPrefs.Save();
                    result += "|ok:hardkill(刷新页面验证安全模式)";
                }
                else if (mode==2)
                {
                    SaveSystem.WebCrashCorruptAuto();
                    CrashGuardSystem.LastCrashSummary = "模拟崩溃：自动档已损坏，验证回滚恢复";
                    UnityEngine.PlayerPrefs.SetString(CrashGuardSystem.KeyState,"crashed");
                    UnityEngine.PlayerPrefs.SetString(CrashGuardSystem.KeyLast,CrashGuardSystem.LastCrashSummary);
                    UnityEngine.PlayerPrefs.Save();
                    result += "|ok:auto-corrupted(刷新验证回滚)";
                }
                else
                {
                    result += "|bad:unknown-mode";
                }
            }
            catch (System.Exception e)
            {
                result += "|thrown:" + e.GetType().Name;
            }
            Debug.Log("[CrashSim] " + result);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(result) + "');"); } catch { }
        }

        /// <summary>V9.6.5 崩溃探针：安全模式/崩溃状态/回滚槽/最近日志（读 window.pxcProbe）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebCrashProbe()
        {
            string rb = "roll:";
            if (SaveSystem!=null)
            {
                for (int i=0;i<SaveSystem.RollbackSlots;i++)
                {
                    string k="PxC_Roll_"+i;
                    rb += (UnityEngine.PlayerPrefs.HasKey(k)?"1":"0")+(i<SaveSystem.RollbackSlots-1?",":"");
                }
            }
            string s = Guard!=null ? Guard.Probe() : "guardnull";
            s = s + "|" + rb;
            Debug.Log("[CrashProbe] " + s);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(s) + "');"); } catch { }
        }

        /// <summary>V9.6.6 存档探针：槽位存在位/schema/临时键/备份/回滚（读 window.pxcProbe）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebSaveProbe()
        {
            string s = SaveSystem != null ? SaveSystem.WebSaveProbe() : "savenull";
            Debug.Log("[SaveProbe] " + s);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(s) + "');"); } catch { }
        }

        /// <summary>V9.6.6 异步存档探针（浏览器回归用；slot 传 JS 数字 0..5）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebSaveAsync(int slot)
        {
            if (SaveSystem == null) { WriteProbe("async:fail:nosave"); return; }
            SaveSystem.SaveAsync(slot, ok => WriteProbe("async:" + (ok ? "ok" : "fail") + ":slot" + slot));
        }

        /// <summary>V9.6.6 从写前备份恢复探针（浏览器回归用；slot 传 JS 数字 1..5）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebRestoreBackup(int slot)
        {
            bool ok = SaveSystem != null && SaveSystem.RestoreBackup(slot);
            WriteProbe("restore:" + (ok ? "ok" : "fail") + ":slot" + slot);
        }

        /// <summary>V9.6.6 探测：检查自动档是否带校验和（新档 schema=3）——验证校验架构生效。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebSaveProbe2()
        {
            string s = "autoschema:" + (SaveSystem != null ? SaveSystem.ReadSchemaForTest(0) : -1);
            WriteProbe(s);
        }

        /// <summary>V9.6.8 资源双精度探针：gold 置 1000000，逐次 +0.0625×16 → 读回精确值（float 在百万级 ULP≈0.0625 会部分丢失，double 无损）。
        /// 注：V9.5.6 已有 WebResProbe 综合资源探针，本探针专测双精度累加路径。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebResPrecisionProbe()
        {
            if (State == null) { WriteProbe("res:null"); return; }
            State.Res["gold"] = 1000000d;
            for (int i = 0; i < 16; i++) State.AddRes("gold", 0.0625f);
            double v = State.Res["gold"];
            WriteProbe("res:gold=" + v.ToString("F6") + " expect1000001.000000");
        }

        /// <summary>V9.7.1 自适应分段探针：输出年份/倍速/上帧年结数/单年结均耗/硬上限/剩余 Day（工作量预算动态决定年结数）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebTimeProbe()
        {
            if (State == null || Time == null) { WriteProbe("time:null"); return; }
            WriteProbe("time:y=" + State.Year + " spd=" + State.Speed.ToString("F1") + " day=" + State.Day.ToString("F1")
                + " ticks=" + Time.LastFrameTicks + " avgMs=" + Time.AvgYearTickMs.ToString("F3")
                + " hard=" + Core.GameTime.HardCapTicks + " cryo=" + (State.CryoActive ? "on" : "off"));
        }

        /// <summary>V9.6.8 帧年结计数（编辑器/浏览器统一验证单帧补算上限；GameTime.Tick 每帧推进后由本方法读 Year 增量）。
        /// 说明：WebGL 下 Unity 帧序稳定，探针两次采样 Year 差 ≤ MaxYearsPerFrame 即证明预算生效。</summary>
        int _lastProbeYear = int.MinValue;

        private void WriteProbe(string s)
        {
            Debug.Log("[Probe] " + s);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(s) + "');"); } catch { }
        }

        /// <summary>V9.6.5 手动进入安全模式（浏览器/调试测试用）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebSafeMode()
        {
            CrashGuardSystem.SafeMode = true;
            CrashGuardSystem.ApplySafeMode();
            AddEvent("bad","⚠️ 已手动进入安全模式（阴影关闭/LOD 0.5/自动保存 20s）");            Debug.Log("[CrashSim] SafeMode=ON");
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString("safemode:on") + "');"); } catch { }
        }

        /// <summary>WebGL JS window.onerror 转发入口（CrashGuard 桥；带参 SendMessage）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebJsError(string payload)
        {
            if (Guard!=null) Guard.WebJsError(payload);
        }
        public void WebAdvanceEra(){ bool ok=Time!=null && Time.DebugAdvanceEra(); Debug.Log("[Web] AdvanceEra "+(ok?"OK":"FAIL")); }
        // V9.3.5 时间推进探针：无参 Web 入口（SendMessage 可绑），回归直接读 Year/Day/Era/Paused/Speed/有效倍速
        public void WebYearProbe(){
            if(Time==null){ Debug.Log("[Web] YearProbe Time=null"); return; }
            Debug.Log($"[Web] YearProbe Year={State.Year} Day={State.Day:F2} Era={State.Era} Paused={State.Paused} Speed={State.Speed} Eff={EffectiveSpeed:F0} Greg={Time.GregorianText}");
        }
        // V9.5.6 资源综合探针：回归直读所有资源/兵力/粮食产出与消耗率，定位"资源乱扣"
        public void WebResProbe(){
            if(State==null){ Debug.Log("[Web] ResProbe State=null"); return; }
            var sb = new System.Text.StringBuilder();
            sb.Append($"Year={State.Year} Era={State.Era} Pop={State.Pop} Mil={State.MilSoldiers} Bld={State.Buildings.Count} Ships={State.Ships.Count} Carts={State.Carts.Count}; ");
            foreach(var id in ResourceDatabase.Order)
                sb.Append(id+"="+State.GetRes(id).ToString("F0")+" ");
            if(Economy!=null)
                sb.Append($"| foodProd={Economy.FoodProductionRate:F1} foodCons={Economy.FoodConsumptionRate:F1} goldRate={Economy.ProductionRate["gold"]:F1}");
            Debug.Log("[Web] ResProbe "+sb.ToString());
        }
        // V9.3.7 世界对象点击守卫（修正 V9.3.6 过度拦截）：
        // 仅两个条件屏蔽 OnMouseDown——建造放置模式（摆虚影中）、指针落在任意 UI 上（含左建造面板，
        // Glass 底板 raycastTarget=true 必命中，穿透防护仍成立）。不再因"面板展开"整图禁选，
        // 修复"V9.3.6 后无法选中建筑/船只"的回归。
        public bool BlocksWorldClick()
        {
            if (State!=null && !string.IsNullOrEmpty(State.SelectedBuildType)) return true;   // 建造放置模式（虚影摆放中）
            if (UnityEngine.EventSystems.EventSystem.current!=null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return true; // 指针在任意 UI 上
            return false;
        }
        // V9.3.7 点击守卫探针：浏览器回归直读守卫三态（指针不在UI上时应为 blocks=false 才能选中世界对象）
        public void WebClickGuardProbe()
        {
            string sel = State!=null ? (State.SelectedBuildType??"") : "null";
            string left = UIManager.Instance!=null ? (UIManager.Instance.LeftPanelOpen ? "open":"closed") : "no-ui";
            bool blocks = BlocksWorldClick();
            Debug.Log($"[Web] ClickGuard blocks={blocks} selectedBuild='{sel}' leftPanel={left} pointerOverUI={UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject()}");
        }
        // V9.5.6 射线探针：传入 client 坐标（左上角原点，CSS 像素，"x,y"），
        // 转 uGUI 屏幕坐标（左下角原点）后执行 EventSystem.RaycastAll，返回命中的元素链，
        // 用于诊断"点卡片无反应"——卡片是否被命中、是否被更上层透明元素遮挡。
        public void WebRaycastProbe(string clientXY)
        {
            var es=UnityEngine.EventSystems.EventSystem.current;
            if(es==null){ Debug.Log("[Web] Raycast no EventSystem"); return; }
            var parts=clientXY.Split(',');
            float cx=float.Parse(parts[0],System.Globalization.CultureInfo.InvariantCulture);
            float cy=float.Parse(parts[1],System.Globalization.CultureInfo.InvariantCulture);
            Vector3 sp=new Vector3(cx, Screen.height-cy, 0f);
            var ped=new UnityEngine.EventSystems.PointerEventData(es){ position=sp, button=UnityEngine.EventSystems.PointerEventData.InputButton.Left };
            var res=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            es.RaycastAll(ped,res);
            var sb=new System.Text.StringBuilder();
            sb.Append($"[Web] Raycast client=({cx},{cy}) screen=({sp.x},{sp.y}) Screen={Screen.width}x{Screen.height} n={res.Count} hits=");
            for(int i=0;i<res.Count;i++){ sb.Append(res[i].gameObject.name); if(i<res.Count-1) sb.Append(" > "); }
            Debug.Log(sb.ToString());
        }
        // V9.3.8 探针：船数/敌舰数/编队数/巡航模式/停泊中船数（配合 DebugSpawnOwnShip×4+DebugSpawnEnemy 回归编队巡航）
        public void WebV938Probe()
        {
            string naval = Naval!=null ? Naval.DebugCruiseState() : "no-naval";
            string trees = "no-veg";
            if (Veg!=null) trees = "trees="+Veg.Trees.Count;
            Debug.Log("[Web] V938 "+naval+" "+trees);
        }
        // V9.3.8 编队巡航浏览器演示：就地造 5 艘满员军用船（不跳年、不放敌舰），自动分桶编队巡航
        public void WebV938Fleet()
        {
            if (Naval==null){ Debug.Log("[Web] V938Fleet Naval=null"); return; }
            int made=0;
            for(int k=0;k<5;k++)
                for(int i=0;i<24;i++)
                {
                    float ang=UnityEngine.Random.value*Mathf.PI*2f, dist=14f+UnityEngine.Random.value*26f;
                    float x=Mathf.Cos(ang)*dist, z=Mathf.Sin(ang)*dist;
                    var terrain=FindObjectOfType<WorldGenerator>();
                    if(terrain!=null && !terrain.IsOceanWater(x,z)) continue;
                    if(Naval.SpawnInitialShip(State.Era>=3?"cannon_ship":"war_junk",x,z)!=null){ made++; break; }
                }
            Debug.Log("[Web] V938Fleet made="+made);
            AddEvent("good","🚢 V9.3.8 编队演示：我方 5 舰就位（无敌人，应自动编队巡航）");
        }
        public void WebNextDynasty(){ bool ok=Time!=null && Time.DebugNextDynasty(); Debug.Log("[Web] NextDynasty "+(ok?"OK":"FAIL")); }
        public void WebProbeBridges(){ Bridge?.DebugProbe(); }
        /// <summary>V9.7.1 浏览器回归：高架桥生命周期实证（建柱连片→老化→桥面格/柱清理）</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebViaductTest(){ Bridge?.WebViaductLifecycleTest(); }
        public void WebForceBridge(){ bool ok=Bridge!=null&&Bridge.ForceNearest(); Debug.Log("[Web] ForceBridge "+(ok?"OK":"FAIL")); }
        [UnityEngine.Scripting.Preserve]
        public void WebV923Naval(){ int m=Naval!=null?Naval.DebugSpawnOwnWarships(5):0; Debug.Log("[Web] Warships5 made="+m); Naval?.DebugNavalShowcase(); }   // V9.4.5 浏览器回归：外海批量造 5 艘我方军舰+演示舰
        [UnityEngine.Scripting.Preserve]
        public void WebSpawnWarships5(){ WebV923Naval(); }   // V9.4.5 别名入口
        // V9.4.6 地面作战部队浏览器入口：探针 + 无消耗造车演示（1949 时代直接激活）
        [UnityEngine.Scripting.Preserve]
        public void WebGroundProbe()
        {
            string g = Ground!=null ? Ground.Probe() : "no-ground";
            Debug.Log("[Web] Ground "+g);
        }
        /// <summary>V9.6.0 浏览器回归：紧急集结令探针（rally:kind|x,z / rally:none）</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebRallyProbe()
        {
            Debug.Log("[Web] " + (Rally!=null ? Rally.Probe() : "rally:no-system"));
        }
        /// <summary>V9.6.0 浏览器回归：直接落蓝旗（海军集结）于指定坐标</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebRallyNavy(float x, float z)
        {
            if (Rally==null){ Debug.Log("[Web] RallyNavy null"); return; }
            Rally.SetRally("navy", x, z);
            Debug.Log("[Web] "+Rally.Probe());
        }
        /// <summary>V9.6.2 浏览器回归：落绿旗（地面部队集结）于远侧坐标（单参，规避 SendMessage 双参静默失败）</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebRallyGround(float n)
        {
            if (Rally==null){ Debug.Log("[Web] RallyGround null"); return; }
            Rally.SetRally("ground", 700f, 500f + n);
            Debug.Log("[Web] "+Rally.Probe());
        }
        /// <summary>V9.6.2 浏览器回归：落红旗（军人集结）于远侧坐标（单参）。游戏内 kind 规范为 navy/ground/inf</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebRallyInf(float n)
        {
            if (Rally==null){ Debug.Log("[Web] RallyInf null"); return; }
            Rally.SetRally("inf", 700f, 300f + n);
            Debug.Log("[Web] "+Rally.Probe());
        }
        [UnityEngine.Scripting.Preserve]
        public void WebBuildGround()
        {
            if (Ground==null){ Debug.Log("[Web] BuildGround Ground=null"); return; }
            if (Time!=null && State.Year<4949) Time.DebugJumpTo(4949);   // 公元1949 激活地面部队
            int m=Ground.DebugBuildOwn(3);
            Debug.Log("[Web] BuildGround made="+m+" "+Ground.Probe());
        }
        /// <summary>V9.6.2 浏览器回归：集结与投送链实况（ground/军人投送中数量、机队、兜底复位计数）</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebV962Probe()
        {
            string g = Ground!=null ? Ground.Probe() : "no-ground";
            string a = AirLift!=null ? AirLift.Probe() : "no-airlift";
            string s = "[WEB] v962 ground("+g+")|"+a+"|rally:"+(Rally!=null?Rally.Probe():"no-rally");
            Debug.Log(s);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(s) + "');"); } catch (System.Exception ex) { Debug.Log("[WEB] eval fail "+ex.Message); }
        }
        /// <summary>V9.4.7 浏览器回归：强制我方军舰传送到敌舰旁交战</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebForceNavalBattle(){ int p=Naval!=null?Naval.ForceNavalBattle():-99; Debug.Log("[Web] ForceNavalBattle paired="+p); }
        /// <summary>V9.6.3 浏览器回归：地面部队速度钳制/车辆自由/系统零自动建路 综合探针</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebV963Probe()
        {
            string g = Ground!=null ? Ground.Probe963() : "no-ground";
            string t = ModernTraffic!=null ? ModernTraffic.Diagnose() : "no-traffic";
            string i = Intercity!=null ? Intercity.Diagnose() : "no-intercity";
            string s = "[WEB] v963 "+g+" | "+t+" | "+i;
            Debug.Log(s);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(s) + "');"); } catch (System.Exception ex) { Debug.Log("[WEB] eval fail "+ex.Message); }
        }
        /// <summary>V9.6.4 内存架构：水位/原生/托管/5s增量/钳制门/网格纹理/池统计/慢系统 Top5</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebMemoryProbe()
        {
            string m = MemBudget!=null ? MemBudget.Probe() : "no-mem";
            string e = "[MEM] "+m+"|ships="+(State!=null?State.Ships.Count:0)+"|blds="+(State!=null?State.Buildings.Count:0)+"|trees="+(State!=null&&State.Trees!=null?State.Trees.Count:0)+"|agents="+(State!=null&&State.Agents!=null?State.Agents.Count:0);
            Debug.Log(e);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(e) + "');"); } catch (System.Exception ex) { Debug.Log("[WEB] eval fail "+ex.Message); }
        }
        /// <summary>V9.6.3 浏览器回归：两次调用测 Ours[0] 实际位移速度（格/秒，期望 Lv1≈0.01..Lv10≈0.10）</summary>
        static float _v963px,_v963pz,_v963pt;
        [UnityEngine.Scripting.Preserve]
        public void WebV963Speed()
        {
            if (Ground==null || Ground.Ours==null || Ground.Ours.Count==0){ Debug.Log("[WEB] V963Speed no-ground"); return; }
            var u=Ground.Ours[0];
            float now=UnityEngine.Time.realtimeSinceStartup;
            if(_v963pt<=0f){ _v963px=u.X; _v963pz=u.Z; _v963pt=now;
                Debug.Log("[WEB] V963Speed sample1 lv="+u.Level+" x="+u.X.ToString("F2")+" z="+u.Z.ToString("F2")); return; }
            float ddx=u.X-_v963px, ddz=u.Z-_v963pz;
            float dist=Mathf.Sqrt(ddx*ddx+ddz*ddz);
            float sec=now-_v963pt;
            float gps=sec>0.01f ? dist/(sec*GameConstants.Tile) : 0f;
            Debug.Log("[WEB] V963Speed lv="+u.Level+" dist="+dist.ToString("F2")+" sec="+sec.ToString("F2")+" grid/s="+gps.ToString("F3")+" (期望 0.01@Lv1..0.10@Lv10)");
            _v963px=u.X; _v963pz=u.Z; _v963pt=now;
        }
        /// <summary>V9.6.3 浏览器回归：免费升级 Ours[0]（验证射程/耐久/速度随等级成长）</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebGroundUpgrade()
        {
            if (Ground==null || Ground.Ours==null || Ground.Ours.Count==0){ Debug.Log("[WEB] GroundUpgrade no-ground"); return; }
            var u=Ground.Ours[0];
            float before=Ground.EffectiveSpeed(u);
            int br=u.BaseRange, lv=u.Level;
            Ground.LevelUp(u,true);
            Debug.Log("[WEB] GroundUpgrade lv="+lv+"->"+u.Level+" range="+br+"->"+u.Range+" hp="+u.Hp+"/"+u.MaxHp+" spd="+before.ToString("F3")+"->"+Ground.EffectiveSpeed(u).ToString("F3"));
        }
        /// <summary>V9.6.3f2 浏览器回归：海军攻击半径（减半后 clamp ≤50 格，Lv1/Lv10 对比）+ 造 3 艘我方军舰</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebV963F2Naval()
        {
            if (Naval==null){ Debug.Log("[WEB] F2Naval no-naval"); return; }
            var sb=new System.Text.StringBuilder();
            foreach(var kv in Naval.Defs)
            {
                if(!kv.Value.Military) continue;
                float lv1=Mathf.Min(100f, kv.Value.Range*0.5f);
                float lv10=Mathf.Min(100f, kv.Value.Range*0.5f*(1f+9f*0.15f));
                sb.Append(kv.Key+"="+lv1.ToString("F1")+"/"+lv10.ToString("F1")+" ");
            }
            Debug.Log("[WEB] F2Naval range(half|clamp100 Lv1/Lv10): "+sb);
            int m=Naval.DebugSpawnOwnWarships(3);
            Debug.Log("[WEB] F2Naval warships3="+m);
            string f2 = "[WEB] f2naval "+sb.ToString().Trim();
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(f2) + "');"); } catch (System.Exception ex2) { Debug.Log("[WEB] eval fail "+ex2.Message); }
        }
        /// <summary>V9.6.3h 浏览器回归：村落旁依次建 5 类塔防各 1 座（验证分型模型 + 投射物 kind 接线）</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebBuildTowers()
        {
            string result="no-build";
            try
            {
                if (Building==null){ Debug.Log("[WEB] BuildTowers null"); return; }
                float ox = State.VillageX.Count>0 ? State.VillageX[0] : 0f;
                float oz = State.VillageZ.Count>0 ? State.VillageZ[0] : 0f;
                string[] ids={"arrow_tower","fire_tower","cannon_tower","bunker","watchtower"};
                int ok=0;
                for(int i=0;i<ids.Length;i++)
                {
                    bool placed=false;
                    for(int a=0;a<12 && !placed;a++)
                    { float ang=a*30f*Mathf.Deg2Rad;
                      float px=ox+40f*Mathf.Cos(ang), pz=oz+40f*Mathf.Sin(ang);
                      if(Building.PlaceInitial(ids[i],px,pz,"home")!=null) placed=true; }
                    if(placed) ok++;
                }
                string cnt="";
                foreach(var id in ids) cnt+=id+"="+State.CountBuilding(id)+" ";
                result="towers:"+ok+"/"+ids.Length+"|"+cnt.Trim();
            }
            catch(System.Exception ex){ result="towers-ex:"+ex.GetType().Name+":"+ex.Message; }
            Debug.Log("[WEB] BuildTowers "+result);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(result) + "');"); } catch (System.Exception ex2) { Debug.Log("[WEB] eval fail "+ex2.Message); }
        }
        /// <summary>V9.6.4 塔防几何回归：输出每塔包围盒高宽比（证明"塔不是楼"：矮墩台 h/w≈0.8-1.4，高楼≥3）、
        /// 世界坐标（供相机跳转视觉复核）与 2 层内部件名（Bolt/BowArm/Nozzle/FireMouth/TurretGun/MG0-3/Cauldron/Beacon 等）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebTowerBounds()
        {
            string result="no-tower";
            try
            {
                var sb=new System.Text.StringBuilder();
                foreach(var b in State.Buildings)
                {
                    if(!(b.Type.Contains("tower")||b.Type=="watchtower"||b.Type=="bunker")) continue;
                    if(b.View==null) continue;
                    var rs=b.View.GetComponentsInChildren<Renderer>(true);
                    if(rs.Length==0){ sb.Append(b.Type+"(norender)"); continue; }
                    Bounds bb=rs[0].bounds;
                    for(int i=1;i<rs.Length;i++) bb.Encapsulate(rs[i].bounds);
                    float wid=Mathf.Max(1e-4f,Mathf.Max(bb.size.x,bb.size.z));
                    float hw=bb.size.y/wid;
                    string parts="";
                    foreach(Transform c in b.View.transform)
                    {
                        if(c==null) continue;
                        foreach(Transform c2 in c)
                        { if(c2==null) continue; string n=c2.name;
                          if(!n.StartsWith("LOD")&&!n.StartsWith("LV")) parts+=(n.Length>6?n.Substring(0,6):n)+","; }
                    }
                    sb.Append(b.Type+"(h/w="+hw.ToString("0.00")+",xy="+b.X.ToString("0")+","+b.Z.ToString("0")+")["+parts.TrimEnd(',')+"] ");
                }
                result="towerbounds:"+sb.ToString().Trim();
            }
            catch(System.Exception ex){ result="towerbounds-ex:"+ex.GetType().Name+":"+ex.Message; }
            Debug.Log("[WEB] TowerBounds "+result);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(result) + "');"); } catch (System.Exception ex2) { Debug.Log("[WEB] eval fail "+ex2.Message); }
        }
        /// <summary>V9.6.3h 浏览器回归：输出全部塔防的 View 子物体结构（验证分型建模生效：箭塔弩机/火塔土垒/炮塔炮管/碉堡机枪/烽火台大锅）</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebTowerProbe()
        {
            string result="no-tower";
            try
            {
                var sb=new System.Text.StringBuilder();
                foreach(var b in State.Buildings)
                {
                    if(!(b.Type.Contains("tower")||b.Type=="watchtower"||b.Type=="bunker")) continue;
                    int kids = b.View!=null ? b.View.transform.childCount : -1;
                    string names="";
                    if(b.View!=null)
                        foreach(Transform c in b.View.transform)
                        { string n=c.name; names+=(n.Length>4?n.Substring(0,4):n)+"/"; }
                    sb.Append(b.Type+"("+kids+"){"+names.TrimEnd('/')+"} ");
                }
                result="towerstruct:"+sb.ToString().Trim();
            }
            catch(System.Exception ex){ result="towerstruct-ex:"+ex.GetType().Name+":"+ex.Message; }
            Debug.Log("[WEB] TowerProbe "+result);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(result) + "');"); } catch (System.Exception ex2) { Debug.Log("[WEB] eval fail "+ex2.Message); }
        }
        /// <summary>V9.6.3f2 浏览器回归：集结令压力测试——连续插旗 20 次（蓝/绿/红循环），验证不再卡死、旗模型不复用堆积（子物体恒 5）、
        /// NaN/水上落旗被拒绝；并输出地面射程上限（升级后 ≤50 格）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebRallyStress()
        {
            string result="no-rally";
            try
            {
                if (Rally==null){ Debug.Log("[WEB] RallyStress null"); return; }
                float ox = State.VillageX.Count>0 ? State.VillageX[0] : 0f;
                float oz = State.VillageZ.Count>0 ? State.VillageZ[0] : 0f;
                string[] kinds={"navy","ground","inf"};
                int ok=0,reject=0;
                for(int i=0;i<20;i++)
                {
                    string k=kinds[i%3];
                    float ang=UnityEngine.Random.value*Mathf.PI*2f, rr=30f+UnityEngine.Random.value*80f;
                    float x=ox+Mathf.Cos(ang)*rr, z=oz+Mathf.Sin(ang)*rr;
                    Rally.SetRally(k,x,z);
                    if(State.RallyKind.Length>0) ok++; else reject++;
                }
                // NaN 防御验证：传入 NaN 应被拒绝（旗状态保持上一旗不变）
                string lastKind = State.RallyKind;
                Rally.SetRally("ground", float.NaN, float.NaN);
                bool nanRejected = State.RallyKind==lastKind && lastKind.Length>0;
                int childCount = 0;
                var flagRoot = Rally.GetFlagRootForTest();
                if (flagRoot!=null) childCount = flagRoot.transform.childCount;
                string groundRange = Ground!=null && Ground.Ours!=null && Ground.Ours.Count>0
                    ? "lv"+Ground.Ours[0].Level+"r"+Ground.Ours[0].Range : "no-ground";
                result = "rally:"+Rally.Probe()+"|ok="+ok+"|children="+childCount+"|nanRejected="+nanRejected+"|"+groundRange;
            }
            catch(System.Exception ex){ result="rally-ex:"+ex.GetType().Name+":"+ex.Message; }
            Debug.Log("[WEB] RallyStress "+result);
            try { Application.ExternalEval("window.pxcProbe=decodeURIComponent('" + System.Uri.EscapeDataString(result) + "');"); } catch (System.Exception ex2) { Debug.Log("[WEB] eval fail "+ex2.Message); }
        }
        /// <summary>V9.4.7 浏览器回归：统一战斗目录实况（按 Kind/Key 分组）</summary>        [UnityEngine.Scripting.Preserve]
        public void WebCombatProbe()
        {
            if (Combat==null){ Debug.Log("[Web] CombatProbe null"); return; }
            var tg=Combat.Targets;
            int[] kc=new int[6];
            var keys=new System.Collections.Generic.Dictionary<string,int>();
            foreach(var t in tg){ kc[t.Kind]++; if(!keys.ContainsKey(t.Key)) keys[t.Key]=0; keys[t.Key]++; }
            string kinds="ship="+kc[0]+" ground="+kc[1]+" inf="+kc[2]+" tower="+kc[3]+" cav="+kc[4]+" bld="+kc[5];
            string ks="";
            foreach(var k in keys) ks+=k.Key+"="+k.Value+" ";
            Debug.Log("[Web] CombatProbe total="+tg.Count+" | "+kinds+" | "+ks);
        }
        /// <summary>V9.3.3 现代海军演示：静默跳到公元2000（游戏年5000）、生成我方现代舰队、触发敌舰阵营（Debug 强制，浏览器 SendMessage 无参入口）</summary>
        public void WebV933Modern()
        {
            if (Time!=null) Time.DebugJumpTo(5000);   // 游戏年5000 = 公元2000（era6），现代舰船时代
            if (Naval==null){ Debug.Log("[Web] V933 Naval=null"); return; }
            string[] fleet={"destroyer","missile_ship","aircraft_carrier","submarine","steamship","cruise_liner"};
            foreach(var t in fleet)
            {
                for(int i=0;i<18;i++)
                {
                    float ang=UnityEngine.Random.value*Mathf.PI*2f, dist=16f+UnityEngine.Random.value*30f;
                    float x=Mathf.Cos(ang)*dist, z=Mathf.Sin(ang)*dist;
                    var terrain=FindObjectOfType<WorldGenerator>();
                    if(terrain!=null && !terrain.IsOceanWater(x,z)) continue;
                    var s=Naval.SpawnInitialShip(t,x,z);
                    if(s!=null){ AddEvent("good","🚢 现代舰队就位："+t); break; }
                }
            }
            Naval.DebugSpawnEnemy();
            AddEvent("bad","⚔ 公元2000年·现代海军时代：敌国舰队以 3-5 阵营巡弋海疆！");
        }
        public void WebToggleLeftPanel(){ UIManager.Instance?.WebToggleLeft(); }
        public void WebToggleRightPanel(){ UIManager.Instance?.WebToggleRight(); }
        /// <summary>V9.3.9 WebGL 无参入口：打开帮助界面（浏览器回归密钥输入框用）</summary>
        public void WebOpenHelp(){ if(UIManager.Instance!=null){ UIManager.Instance.WebOpenHelp(); Debug.Log("[Web] OpenHelp"); } }
        /// <summary>V9.3.13 浏览器回归：按名称打开各面板（string 参数）</summary>
        public void WebOpenModal(string w){ UIManager.Instance?.WebOpenModal(w); }
        /// <summary>V9.5.3 浏览器回归：按名称切换建造分类（中英文别名，绕开 OCR 坐标漂移）</summary>
        public void WebOpenBuildTab(string w){ if(UIManager.Instance!=null) UIManager.Instance.WebOpenBuildTab(w); }
        /// <summary>V9.3.9 WebGL 无参入口：程序化验证密钥输入链（聚焦+赋值→探针日志）</summary>
        public void WebKeyTest(){ if(UIManager.Instance!=null) UIManager.Instance.WebKeyTest(); }

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
        /// <summary>V9.3.9 Web 桥接：浏览器 SendMessage 传 string 设置 AI 密钥（日志只记长度，不落明文）</summary>
        public void WebAISetKey(string key){ Council?.SetApiKey(key); Debug.Log("[Web] AISetKey len="+(key==null?0:key.Length)); }
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
        [UnityEngine.Scripting.Preserve]
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

        // ===== V9.7.0 架构探针（浏览器回归：对象池/SoA/脏标记/崩溃上下文） =====

        /// <summary>对象池状态探针：桶数 + 空闲/租出快照 + 适配层未释放计数。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebPoolProbe()
        {
            WriteProbe("pool:buckets=" + GameObjectPool.BucketCount() +
                       "|addrlive=" + AddressablesManager.LiveCount +
                       "|" + AddressablesManager.Stats());
        }

        /// <summary>SoA 人口聚合探针：Total 恒等于 State.Pop（同源），展示连续数组聚合结果。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebSoAProbe()
        {
            if (State == null) { WriteProbe("soa:null"); return; }
            WriteProbe(SoAPopulationStore.Probe() + "|statepop=" + State.Pop);
        }

        /// <summary>存档脏标记探针：启用/脏/跳过/保存计数。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebDirtyProbe()
        {
            WriteProbe(SaveDirtySystem.Probe());
        }

        /// <summary>崩溃上下文探针：朝代/年份/人口/势力/时代/倍速 + 落盘标记。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebCrashCtxProbe()
        {
            WriteProbe(LocalCrashReporter.Probe());
        }
    }
}
