using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.Systems;
using PixelToCivilization.World;
using PixelToCivilization.AI;
using PixelToCivilization.UI;
using PixelToCivilization.Actors;
using PixelToCivilization.Buildings;

namespace PixelToCivilization.Core
{
    /// <summary>
    /// 全局游戏管理器：装配所有子系统，驱动主循环（Tick），并提供 WebGL 无参入口。
    /// V9.5.3：存档面板重做配套（自动存档槽0 + 手动槽1-5 + 导出/导入）；Debug 面板全部资源+100万；
    /// 事件横幅/冷冻倒计时/九神面板键输入窗口在对应面板。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public GameState State;
        public TimeSystem Time;
        public EconomySystem Economy;
        public PopulationSystem Population;
        public MilitarySystem Military;
        public NavalSystem Naval;
        public GroundWarfareSystem Ground;
        public CombatSystem Combat;
        public ColonizationSystem Colonization;
        public ExpeditionSystem Expedition;
        public BridgeSystem Bridges;
        public OceanExpansionSystem Ocean;
        public SpaceExpansionSystem Space;
        public BuildingSystem Buildings;
        public TechSystem Techs;
        public PolicySystem Policies;
        public PhilosophySystem Philosophy;
        public CultureSystem Culture;
        public EducationSystem Education;
        public WeatherSystem Weather;
        public EnvSystem Env;
        public CameraRig Cam;
        public SaveSystem SaveSystem;
        public CouncilSystem Council;
        public AudioSys Audio;
        public EraDatabase Eras;

        public float MaxSpeed => State.DebugLevel>=2 ? 1000f : State.DebugLevel>=1 ? 300f : GameConstants.SpeedMaxNormal;
        public bool SoundOn => State!=null && State.SoundOn;

        private readonly List<SystemBase> _systems = new();
        private float _tick;
        private float _cryoUntil=-1f; private float _cryoTotal; private int _speedBeforeCryo;
        private float _nextAutosave;
        private const float AutosaveInterval = 300f;   // 5 分钟自动存档（槽0）
        private StringBuilder _sb=new StringBuilder(256);

        private void Awake()
        {
            Instance=this;
            DontDestroyOnLoad(gameObject);
            State=new GameState();
            Eras=new EraDatabase();
            Time=new TimeSystem(this);
            Economy=new EconomySystem(this);
            Population=new PopulationSystem(this);
            Military=new MilitarySystem(this);
            Naval=new NavalSystem(this);
            Ground=new GroundWarfareSystem(this);
            Combat=new CombatSystem(this);
            Colonization=new ColonizationSystem(this);
            Expedition=new ExpeditionSystem(this);
            Bridges=new BridgeSystem(this);
            Ocean=new OceanExpansionSystem(this);
            Space=new SpaceExpansionSystem(this);
            Buildings=new BuildingSystem(this);
            Techs=new TechSystem(this);
            Policies=new PolicySystem(this);
            Philosophy=new PhilosophySystem(this);
            Culture=new CultureSystem(this);
            Education=new EducationSystem(this);
            Weather=new WeatherSystem(this);
            Env=new EnvSystem(this);
            SaveSystem=new SaveSystem(this);
            Council=new CouncilSystem(this);
            Audio=new AudioSys(this);
            Cam=FindObjectOfType<CameraRig>();
            _systems.AddRange(new SystemBase[]{Time,Economy,Population,Military,Naval,Ground,Combat,Colonization,Expedition,Bridges,Ocean,Space,Buildings,Techs,Policies,Philosophy,Culture,Education,Weather,Env,SaveSystem,Council,Audio});
            State.Speed=1f;
        }

        private void Start()
        {
            Audio.Init();
            State.Year=GameConstants.StartYear;
            State.Era=Eras.EraOf(State.Year);
            State.Dynasty=Eras.DynastyOf(State.Year);
            Env.GenerateWorld();
            Population.InitPopulation();
            Buildings.InitStartBuildings();
            Military.InitFactions();
            Naval.InitShips();
            Ground.Init();
            Council.ResetDaily();
            AddEvent("good","文明纪元开启。九神归位，护佑万民。");
            SaveSystem.LoadLastAuto();
        }

        private void Update()
        {
            float dt=Time.deltaTime;
            _tick+=dt;
            // 加速冷冻：收到解冻指令前累计加速 >100 年→冻结到剩余 300 秒，期间最大 10×
            if(_cryoUntil>0)
            {
                if(Time.time>=_cryoUntil){ _cryoUntil=-1f; State.Speed=Mathf.Min(State.Speed,10f); AddEvent("good","冷冻解除，加速恢复（最高 10×）"); }
                else if(State.Speed>10f){ State.Speed=10f; }
            }
            float step=dt*State.Speed;
            if(step>0)
            {
                foreach(var s in _systems)
                {
                    if(s!=null) { try{ s.Tick(step); }catch(Exception ex){ Debug.LogError("[SYSTEM] "+s.GetType().Name+" : "+ex.Message); } }
                }
            }
            // 自动存档（槽0，每 5 分钟）
            if(Time.time>=_nextAutosave){ _nextAutosave=Time.time+AutosaveInterval; SaveSystem.AutoSave(); }
            // 事件横幅循环
            if(_eventBanner!=null && _eventUntil>0 && Time.time>=_eventUntil) HideEventBanner();
        }

        public void SetSpeedClamped(float v)
        {
            if(_cryoUntil>0 && v>10f) v=10f;
            State.Speed=Mathf.Clamp(v,0f,MaxSpeed);
            if(State.Speed>=GameConstants.SpeedThresholdCryo) TryCryo();
        }
        private void TryCryo()
        {
            if(_cryoUntil>0)return;
            _cryoTotal+=State.Speed*(60f/GameConstants.DaySeconds)/60f; // 近似游戏年
            if(_cryoTotal>=100f){ _cryoUntil=Time.time+300f; _speedBeforeCryo=(int)State.Speed; AddEvent("cryo","加速累计 100 年，进入冷冻 300 秒（期间最高 10×）"); }
        }
        public void WebCryoFreeze(){ _cryoUntil=Time.time+300f; _cryoTotal=0; State.Speed=10f; AddEvent("cryo","Debug：强制冷冻 300 秒"); }
        public void ResetCryo(){ _cryoTotal=0; _cryoUntil=-1f; }

        public int YearNow => State.Year;
        public string EraName => Eras.Name(State.Era);
        public string DynastyName => State.Dynasty;
        public int PopNow => State.Pop;
        public int HousingNow => State.Housing;
        public int FoodNow => (int)State.Resources[GameConstants.ResourceFood];
        public int GoldNow => (int)State.Resources[GameConstants.ResourceGold];
        public int WoodNow => (int)State.Resources[GameConstants.ResourceWood];
        public int StoneNow => (int)State.Resources[GameConstants.ResourceStone];
        public int IronNow => (int)State.Resources[GameConstants.ResourceIron];
        public int BronzeNow => (int)State.Resources[GameConstants.ResourceBronze];
        public int SteelNow => (int)State.Resources[GameConstants.ResourceSteel];
        public int CementNow => (int)State.Resources[GameConstants.ResourceConcrete];
        public int FusionNow => (int)State.Resources[GameConstants.ResourceFusion];
        public int HeliumNow => (int)State.Resources[GameConstants.ResourceHelium3];
        public int CultureNow => (int)State.Resources[GameConstants.ResourceCulture];
        public int ResearchNow => (int)State.Resources[GameConstants.ResourceResearch];
        public int PowerNow => (int)State.Resources[GameConstants.ResourcePower];
        public int GoodsNow => (int)State.Resources[GameConstants.ResourceGoods];

        // ===== 事件 =====
        public void AddEvent(string kind,string msg)
        {
            State.Events.Enqueue(new GameEvent{Kind=kind,Msg=msg,Year=State.Year});
            if(State.Events.Count>12)State.Events.Dequeue();
            ShowEventBanner(kind,msg);
        }
        public string PopEvent()=>State.Events.Count>0?State.Events.Dequeue().Msg:null;

        // ===== Debug 密码 =====
        public bool TryUnlockDebug(string pw)
        {
            if(pw==GameConstants.DebugPassword){ State.DebugLevel=Mathf.Max(State.DebugLevel,2); return true; }
            if(pw=="ToFuture"){ State.DebugLevel=Mathf.Max(State.DebugLevel,2); return true; }
            return false;
        }

        // ===== WebGL 无参入口（浏览器 SendMessage 只能绑无参/string 方法）=====
        public void WebQuickSave(){ SaveSystem.SaveToSlot(1); }
        public void WebQuickLoad(){ if(SaveSystem.LoadFromSlot(1)) RefreshAll(); }
        public void WebAdvanceEra(){ Time.AdvanceEra(); }
        public void WebNextDynasty(){ Time.NextDynasty(); }
        public void WebAddResources1M()
        {
            var r=State.Resources;
            for(int i=0;i<r.Count;i++)r[i]+=1000000f;
            AddEvent("good","Debug：全部资源 +100 万");
        }
        public void WebToggleWar(){ State.WarActive=!State.WarActive; Military.InitFactions(); }
        public void WebSpawnEnemyFleet(){ for(int i=0;i<3;i++)Naval.DebugSpawnEnemy(); }
        public void WebSpawnOwnShip(){ Naval.DebugSpawnOwnShip(); }
        public void WebForceWeather(){ Weather.ForceNext(); }
        public void WebClearTrees(){ Env.ClearTrees(); }
        public void WebUnlockOcean(){ Ocean.UnlockExpansion(); }
        public void WebUnlockSpace(){ Space.UnlockExploration(); }
        public void WebOceanAuto(){ Expedition.Prepare("ocean"); for(int i=0;i<5;i++)Expedition.AutoExplore("ocean"); }
        public void WebSpaceAuto(){ Expedition.Prepare("space"); for(int i=0;i<5;i++)Expedition.AutoExplore("space"); }
        public void WebColonizeHere(){ Expedition.ColonizeHere(); }
        public void WebOutpostHere(){ Expedition.BuildOutpostHere(); }
        public void WebReturnAll(){ Expedition.ReturnHome("ocean"); Expedition.ReturnHome("space"); }
        public void WebCouncilNow(){ Council.CouncilNow(); }
        public void WebCouncilToggle(){ Council.ToggleEnabled(); }
        public void WebCouncilOnline(){ Council.SetOnline(true); }
        public void WebCouncilOffline(){ Council.SetOnline(false); }
        public void WebVictory(){ State.Victory=true; State.VictoryType="debug"; }
        public void WebPopFill(){ State.Pop=GameConstants.MaxPop; }
        public void WebPopAdd(){ State.Pop=Mathf.Min(GameConstants.MaxPop,State.Pop+50); }
        public void WebMoraleFill(){ State.DynastyMorale=100; State.Happiness=Mathf.Max(State.Happiness,90); }
        public void WebHousingAdd(){ State.Housing+=200; }
        public void WebAllTechs(){ foreach(var t in Techs.Keys)State.ResearchedTechs.Add(t); }

        public void RefreshAll(){ if(UIManager.Instance)UIManager.Instance.Refresh(); }

        // ===== 声音 =====
        public void ToggleSound(){ State.SoundOn=!State.SoundOn; if(State.SoundOn)Audio.Resume(); else Audio.MuteAll(); }

        // ===== 事件横幅（顶部中央，v6.3.x 悬浮层级最顶层）=====
        private GameObject _eventBanner; private float _eventUntil; private Text _eventText;
        private void ShowEventBanner(string kind,string msg)
        {
            if(_eventBanner==null && UIManager.Instance!=null) { /* banner 由 UIManager 创建 */ }
        }
        private void HideEventBanner(){ }
    }
}
