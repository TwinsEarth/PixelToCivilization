using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.Systems;
using PixelToCivilization.World;

namespace PixelToCivilization.UI
{
    /// <summary>
    /// V6.1.9(i) 界面系统重构（对齐 7 张参考图）：
    /// 1 建筑面板 10 分类 + 卡片 + 最大/最小化；2 顶部 9 资源 + 悬浮说明；3 底部常用按钮(按使用次数)；
    /// 4 帮助界面；5 左下按钮组；6 国家状态面板最大/最小化；7 世界建筑浮标 + 建筑详情(实拍内视图/寿命/耐久/升级拆除/居住/塔)。
    /// </summary>
    public partial class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        public GameManager GM => GameManager.Instance;
        public GameState S => GameManager.Instance?.State;

        private GameObject _hud, _splash, _splashMsg;
        private Text _eraTag, _dynastyTag, _timeText, _gregText, _popText, _envText, _speedText, _eventText, _saveCountdown;
        private GameObject _leftPanel, _rightRtGo;
        private Transform _buildList;
        private readonly Dictionary<string,Text> _resTexts = new();
        private string _activeCat = "居住";
        private string _tool = "select";
        private Slider _speedSlider; private bool _suppressSlider;
        private GameObject _oceanBtn, _spaceBtn;
        private bool[] _statOpen = { true, true, true, true };
        private Text _statBasic, _statMil, _statSoc, _statEra;
        private GameObject _buildingModal; private Text _bmName,_bmLv,_bmAge,_bmDura,_bmPop,_bmCap;
        private Button _bmUp,_bmDel,_bmClose; private BuildingEntity _bmB;
        private GameObject _shipCard; private Text _scName,_scCrew,_scDura,_scLv,_scCap,_scAtt; private Button _scUp,_scDel,_scClose; private ShipEntity _scShip;
        private GameObject _cartCard; private Text _ccName,_ccCrew,_ccCap,_ccDura; private Button _ccClose; private CartEntity _ccCart;
        private GameObject _treeCard; private Text _tcName,_tcAge; private Button _tcClose; private PixelToCivilization.World.BanyanTree _tcTree;
        private GameObject _cryoBar; private Text _cryoText;
        private GameObject _techModal,_policyModal,_godsModal,_oceanModal,_spaceModal,_campaignModal,_colonyModal,_cityModal,_philosophyModal,_wonderModal;
        private GameObject _saveModal,_debugModal;
        private float _refreshCd, _splashCd;
        private bool _splashArmed;

        private void Awake(){ Instance=this; }
        private void Start(){ BuildAll(); }

        private void BuildAll()
        {
            _hud=new GameObject("HUD"); _hud.transform.SetParent(transform,false);
            var hc=_hud.AddComponent<RectTransform>(); hc.anchorMin=Vector2.zero; hc.anchorMax=Vector2.one; hc.offsetMin=Vector2.zero; hc.offsetMax=Vector2.zero;
            BuildTopBarV2(_hud.transform);
            BuildLeftPanelV2(_hud.transform);
            BuildRightPanelV2(_hud.transform);
            BuildBottomBarV2(_hud.transform);
            BuildLeftBottomCluster(_hud.transform);
            BuildHelpModals(_hud.transform);
            BuildSaveModal(_hud.transform);
            BuildDebugModal(_hud.transform);
            BuildToastAndEra(_hud.transform);
            BuildMarkerLayer(_hud.transform);
            BuildShipCard(_hud.transform);
            BuildCartCard(_hud.transform);
            BuildTreeCard(_hud.transform);
            BuildCryoBar(_hud.transform);
            BuildSubModals(_hud.transform);
            Refresh();
        }

        // ============ 建造面板子模态：科技/政策/九神/城市/百家/殖民/海洋/太空/讨伐/奇观 ============
        private void BuildSubModals(Transform parent)
        {
            _techModal=MakeModal("TechModal","科技研究",out _,false);
            _policyModal=MakeModal("PolicyModal","政策与制度",out _,false);
            _godsModal=MakeModal("GodsModal","九神共治",out _,false);
            _cityModal=MakeModal("CityModal","城市管理",out _,false);
            _philosophyModal=MakeModal("PhilosophyModal","诸子百家",out _,false);
            _colonyModal=MakeModal("ColonyModal","海外殖民",out _,false);
            _oceanModal=MakeModal("OceanModal","海洋大开发",out _,false);
            _spaceModal=MakeModal("SpaceModal","太空探索",out _,false);
            _campaignModal=MakeModal("CampaignModal","出师讨伐",out _,false);
            _wonderModal=MakeModal("WonderModal","世界奇观",out _,false);
        }

        // ============ 建造列表辅助 ============
        private void SetTool(string t){ _tool=t; S.Tool=t; if(t!="build")S.SelectedBuildType=null; }
        private void ToolBtn(Transform parent,string name,string iconKey)
        {
            var b=UITheme.Btn("tool_"+name,parent,"",12,UITheme.Chip);
            b.GetComponent<LayoutElement>().preferredWidth=46;b.GetComponent<LayoutElement>().preferredHeight=44;b.GetComponent<LayoutElement>().minWidth=46;
            var tx=b.transform.Find("Text"); if(tx)Destroy(tx.gameObject);
            var ic=UITheme.Icon(b.transform,iconKey,12); ic.rectTransform.anchorMin=Vector2.zero;ic.rectTransform.anchorMax=Vector2.one;
            ic.rectTransform.offsetMin=new Vector2(11,10);ic.rectTransform.offsetMax=new Vector2(-11,-10);
            string tip=name switch{"select"=>"选择：点选建筑/船只/树木查看属性","build"=>"建造：先点左侧分类，再点地面放置","tree"=>"种树：点击地面种植树木","npc"=>"招民：点击地面招募流民"};
            AddHover(b.gameObject,tip);
            b.onClick.AddListener(()=>{ SetTool(name); Toast(tip); });
        }
        private void BarSep(Transform parent)
        {
            var s=UITheme.Panel("sep",parent,UITheme.HexA(0x9aa3b2,0.5f));
            s.AddComponent<LayoutElement>().preferredWidth=2; s.AddComponent<LayoutElement>().preferredHeight=36;
        }
        private Text Pill(Transform parent,string txt,Color bg,float w,Color? fc=null)
        {
            var p=UITheme.Panel("pill",parent,bg);
            p.AddComponent<LayoutElement>().preferredWidth=w;
            var t=UITheme.Label("t",p.transform,txt,12,TextAnchor.MiddleCenter,fc??UITheme.Text);
            Stretch(t.gameObject); t.rectTransform.offsetMin=new Vector2(4,0);t.rectTransform.offsetMax=new Vector2(-4,0);
            return t;
        }

        // ============ 速度滑条 ============
        private void BuildSpeedSlider(Transform parent)
        {
            _speedSlider=UITheme.Slider("speed",parent,1f,GM.MaxSpeed,GM.State.Speed,GM.SetSpeedClamped);
            var le=_speedSlider.gameObject.AddComponent<LayoutElement>();le.preferredWidth=110;le.preferredHeight=40;
        }
        private void SyncSlider(){ _suppressSlider=true; _speedSlider.value=GM.State.Speed; _suppressSlider=false; }

        // ============ 通用模态骨架 ============
        private GameObject MakeModal(string name,string title,out GameObject box,bool autoFit=true)
        {
            var m=UITheme.Modal(name,_hud.transform,title,out box);
            return m;
        }
        private Transform ModalBody(GameObject modal)=>modal.transform.Find("Box/Body");
        private void Clear(Transform t){ for(int i=t.childCount-1;i>=0;i--)Destroy(t.GetChild(i).gameObject); }
        private void FitModal(GameObject m,float minH,float maxH)
        {
            if(m==null)return;
            var b=m.transform.Find("Box") as RectTransform; if(b==null)return;
            Canvas.ForceUpdateCanvases();
            var body=m.transform.Find("Box/Body") as RectTransform; if(body==null)return;
            float h=Mathf.Clamp(body.rect.height+56,minH,maxH);
            b.sizeDelta=new Vector2(b.sizeDelta.x,h);
        }
        private GameObject Row(Transform parent,float h)
        {
            var r=UITheme.Panel("row",parent,new Color(0,0,0,0));
            r.AddComponent<LayoutElement>().preferredHeight=h;
            var g=r.AddComponent<HorizontalLayoutGroup>();g.spacing=6;g.childControlWidth=true;g.childControlHeight=true;g.childForceExpandWidth=true;g.childForceExpandHeight=false;g.childAlignment=TextAnchor.MiddleCenter;
            return r;
        }
        private static void Stretch(GameObject go)
        {
            var r=go.GetComponent<RectTransform>(); r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=Vector2.zero; r.offsetMax=Vector2.zero;
        }

        // ============ Debug 面板 ============
        private void BuildDebugModal(Transform parent)
        {
            _debugModal=MakeModal("DebugModal","Debug 控制台",out _,false);
        }
        private void OnClickDebug()
        {
            if(_debugModal==null)return;
            FillDebug(); _debugModal.SetActive(true);
            _debugModal.transform.SetAsLastSibling();
        }

        private void FillDebug()
        {
            var box=_debugModal.transform.Find("Box").GetComponent<RectTransform>();
            box.sizeDelta=new Vector2(816,560);   // V9.4.2 左右扩展20%：680×1.2=816
            var body=ModalBody(_debugModal); Clear(body);
            var dbgSr=UITheme.VerticalScroll("DbgScroll",body.transform,out var content,3);
            var cvg=content.GetComponent<VerticalLayoutGroup>();cvg.childControlHeight=true;
            // —— Debug 密码门（ToFuture 解锁 1000 倍速）——
            var gate=Row(content.transform,34);
            UITheme.Label("gt",gate.transform,"Debug 密码（解锁 1000× 与全部修改）",12,TextAnchor.MiddleLeft,UITheme.Gold).gameObject.AddComponent<LayoutElement>().flexibleWidth=1;
            var ip=UITheme.Input("pw",gate.transform,"输入密码…",14);ip.GetComponent<LayoutElement>().flexibleWidth=1;
            var ok=UITheme.Btn("unlock",gate.transform,"解锁",12);ok.onClick.AddListener(()=>{
                if(GM.TryUnlockDebug(ip.text)){ Refresh(); ip.text=""; FillDebug(); }
                else Toast("密码错误",false);
            });
            void Section(string e){ UITheme.Label("sec",content.transform,e,13,TextAnchor.MiddleLeft,UITheme.Gold,FontStyle.Bold).gameObject.AddComponent<LayoutElement>().preferredHeight=18; }
            void DBtn(string label,Action act){ var b=UITheme.Btn("d",content.transform,label,12); b.gameObject.AddComponent<LayoutElement>().flexibleWidth=1; b.onClick.AddListener(()=>{ try{act();}catch(Exception ex){ Debug.LogError("[DBTN] "+label+" : "+ex.Message); Toast("执行失败，见日志",false); } }); }
            if(S.DebugLevel>=2)
            {
                // —— 资源 ——
                Section("资源"); var g=Row(content.transform,34);
                var all=UITheme.Btn("all1m",g.transform,"全部资源 +100 万",12); all.gameObject.AddComponent<LayoutElement>().flexibleWidth=1; all.onClick.AddListener(()=>GM.WebAddResources1M());
                var tof=UITheme.Btn("tofuture",g.transform,"跳到公元 3000（Debug 档）",12); tof.gameObject.AddComponent<LayoutElement>().flexibleWidth=1; tof.onClick.AddListener(()=>{GM.Time.DebugJumpTo(6000);Toast("已跳到 6000 年");});
                // —— 时间 ——
                Section("时间"); var g2=Row(content.transform,34);
                var adv=UITheme.Btn("adv",g2.transform,"推进 1 时代",12); adv.onClick.AddListener(()=>GM.WebAdvanceEra());
                var next=UITheme.Btn("next",g2.transform,"下一个朝代",12); next.onClick.AddListener(()=>GM.WebNextDynasty());
                var cryo=UITheme.Btn("cryo",g2.transform,"加速冷冻（测试）",12); cryo.onClick.AddListener(()=>GM.WebCryoFreeze());
                // —— 海洋 / 太空 副本 ——
                Section("海洋大开发 · 太空探索 副本");
                DBtn("解锁海洋大开发",()=>GM.Ocean.UnlockExpansion());
                DBtn("解锁太空探索",()=>GM.Space.UnlockExploration());
                DBtn("开启大航海时代",()=>{S.AgeOfSail=true;if(S.Era<4)S.Era=4;GM.AddEvent("good","Debug：开启大航海时代");});
                DBtn("海图自动探索×5",()=>{GM.Expedition.Prepare("ocean");for(int i=0;i<5;i++)GM.Expedition.AutoExplore("ocean");});
                DBtn("星图自动探索×5",()=>{GM.Expedition.Prepare("space");for(int i=0;i<5;i++)GM.Expedition.AutoExplore("space");});
                DBtn("当前格建立殖民地",()=>{if(!GM.Expedition.ColonizeHere())Toast("此处不可殖民",false);});
                DBtn("当前格建前哨基地",()=>{if(!GM.Expedition.BuildOutpostHere())Toast("此处不可建前哨",false);});
                DBtn("两支队伍全部返航",()=>{GM.Expedition.ReturnHome("ocean");GM.Expedition.ReturnHome("space");});
                // —— 社会 ——
                Section("社会");
                DBtn("人口补满",()=>S.Pop=GameConstants.MaxPop);
                DBtn("人口 +50",()=>S.Pop=Mathf.Min(GameConstants.MaxPop,S.Pop+50));
                DBtn("民心/天命回满",()=>{S.DynastyMorale=100;S.Happiness=Mathf.Max(S.Happiness,90);});
                DBtn("完成全部科技",()=>{foreach(var t in GM.Techs.Keys)S.ResearchedTechs.Add(t);});
                DBtn("住房 +200",()=>S.Housing+=200);
                DBtn("九神强制议事一次",()=>GM.Council.CouncilNow());
                // —— 世界 / 军事 / AI ——
                Section("世界 · 军事 · 天气 · AI");
                DBtn("清除树木",()=>GM.Env.ClearTrees());
                DBtn("触发战争/群雄",()=>{GM.Military.InitFactions();S.WarActive=true;GM.AddEvent("bad","Debug：战争爆发！");});
                DBtn("敌方舰队 ×3",()=>{for(int i=0;i<3;i++)GM.Naval.DebugSpawnEnemy();});
                DBtn("我方战船",()=>GM.Naval.DebugSpawnOwnShip());
                DBtn("切换下一天气",()=>GM.Weather.ForceNext());
                DBtn("九智能体 开/关",()=>GM.Council.ToggleEnabled());
                DBtn("九智能体联网",()=>GM.Council.SetOnline(true));
                DBtn("九智能体离线",()=>GM.Council.SetOnline(false));
                // —— 结局 / 存档 ——
                Section("结局 / 存档");
                DBtn("直接胜利",()=>{S.Victory=true;S.VictoryType="debug";GM.AddEvent("good","Debug：达成文明胜利！");});
                DBtn("快速存档(槽1)",()=>GM.SaveSystem.SaveToSlot(1));
                DBtn("快速读档(槽1)",()=>{if(GM.SaveSystem.LoadFromSlot(1))Refresh();});
            }
            else
            {
                UITheme.Label("lock",content.transform,"输入上方密码解锁全部 Debug 功能（密码见帮助界面）",12,TextAnchor.MiddleLeft,UITheme.Sub)
                    .gameObject.AddComponent<LayoutElement>().preferredHeight=28;
            }
            var foot=Row(content.transform,30);
            var cb=UITheme.Btn("close",foot.transform,"关闭控制台",13);cb.onClick.AddListener(()=>_debugModal.SetActive(false));
            Canvas.ForceUpdateCanvases();dbgSr.verticalNormalizedPosition=1f;
            AutoBindHovers(_debugModal.transform);
            FitModal(_debugModal,360,520);
        }

        // ============ 存档界面（V9.5.3 完全重做，参照参考图）============
        private void BuildSaveModal(Transform parent)
        {
            _saveModal=MakeModal("SaveModal","存档管理",out _,false);
        }
        private void OpenSaveModal()
        {
            if(_saveModal==null)return;
            RenderSaveSlots();
            _saveModal.SetActive(true);
            _saveModal.transform.SetAsLastSibling();   // V9.5.3 置顶：存档界面永远位于最上层，不被其他浮窗遮挡
        }
        private void RenderSaveSlots()
        {
            if(_saveModal==null)return;
            var box=_saveModal.transform.Find("Box").GetComponent<RectTransform>();
            box.sizeDelta=new Vector2(888,780);   // V9.5.3 完全重做：参照参考图（三格空存档槽）加宽加高
            var body=ModalBody(_saveModal); Clear(body);
            var vg=body.gameObject.AddComponent<VerticalLayoutGroup>(); vg.spacing=4; vg.padding=new RectOffset(6,6,6,6);
            vg.childControlWidth=true; vg.childForceExpandWidth=true; vg.childControlHeight=true; vg.childForceExpandHeight=false;
            var top=Row(body.transform,34);
            UITheme.Btn("qs",top.transform,"快速存档",13).onClick.AddListener(()=>{GM.SaveSystem.SaveToSlot(1);RenderSaveSlots();Toast("已快速存档");});
            UITheme.Btn("ql",top.transform,"快速读档",13).onClick.AddListener(()=>{if(GM.SaveSystem.LoadFromSlot(1)){Refresh();RenderSaveSlots();Toast("已快速读档");}else Toast("该槽位为空",false);});
            UITheme.Btn("rf",top.transform,"刷新列表",13).onClick.AddListener(()=>RenderSaveSlots());
            UITheme.Btn("ex",top.transform,"导出 JSON",13).onClick.AddListener(()=>{GM.SaveSystem.ExportJson();Toast("已导出到本地（浏览器下载）");});
            UITheme.Btn("im",top.transform,"导入 JSON",13).onClick.AddListener(()=>{GM.SaveSystem.ImportJson();RenderSaveSlots();Toast("导入完成（若失败请用浏览器的打开文件选择）");});
            var auto=Row(body.transform,40);
            UITheme.Label("a",auto.transform,"自动存档（每 5 分钟自动覆盖，读档不中断）",12,TextAnchor.MiddleLeft,UITheme.Gold).gameObject.AddComponent<LayoutElement>().flexibleWidth=1;
            _saveCountdown=UITheme.Label("cd",auto.transform,"",12,TextAnchor.MiddleRight,UITheme.Sub); _saveCountdown.gameObject.AddComponent<LayoutElement>().preferredWidth=150;
            for(int i=1;i<=5;i++)
            {
                var slot=GM.SaveSystem.GetSlotMeta(i);
                var row=Row(body.transform,46);
                var info=slot==null?"【空】手动存档槽 "+i:"手动存档槽 "+i+"·第"+slot.Year+"年·"+slot.Dynasty+"·建筑"+slot.Buildings+"·"+slot.When;
                UITheme.Label("n",row.transform,info,12,TextAnchor.MiddleLeft).gameObject.AddComponent<LayoutElement>().flexibleWidth=1;
                int idx=i;
                UITheme.Btn("ov"+i,row.transform,"覆盖",11).onClick.AddListener(()=>{GM.SaveSystem.SaveToSlot(idx);RenderSaveSlots();Toast("已存入槽 "+idx);});
                UITheme.Btn("ld"+i,row.transform,"读档",11).onClick.AddListener(()=>{if(GM.SaveSystem.LoadFromSlot(idx)){Refresh();RenderSaveSlots();Toast("已从槽 "+idx+" 读档");}else Toast("该槽位为空",false);});
                UITheme.Btn("dl"+i,row.transform,"删除",11).onClick.AddListener(()=>{GM.SaveSystem.DeleteSlot(idx);RenderSaveSlots();Toast("已删除槽 "+idx);});
            }
            FitModal(_saveModal,520,860);
        }

        // ============ 建筑 / 船只 / 车辆 / 树木 属性面板（V9.3.8 全物属性化）============
        private void BuildShipCard(Transform parent)
        {
            _shipCard=UITheme.Card("ShipCard",parent);
            _scName=AddRow(_shipCard.transform,"名称"); _scLv=AddRow(_shipCard.transform,"等级");
            _scCrew=AddRow(_shipCard.transform,"船员"); _scDura=AddRow(_shipCard.transform,"耐久");
            _scCap=AddRow(_shipCard.transform,"载量"); _scAtt=AddRow(_shipCard.transform,"攻击");
            var row=Row(_shipCard.transform,30);
            _scUp=UITheme.Btn("up",row.transform,"升级",12);_scUp.onClick.AddListener(()=>{GM.Naval.TryUpgradeShip(_scShip);RenderShipCard();});
            _scDel=UITheme.Btn("del",row.transform,"退役",12);_scDel.onClick.AddListener(()=>{GM.Naval.ScrapShip(_scShip);CloseShipCard();});
            _scClose=UITheme.Btn("x",row.transform,"关闭",12);_scClose.onClick.AddListener(CloseShipCard);
            _shipCard.SetActive(false);
        }
        private void BuildCartCard(Transform parent)
        {
            _cartCard=UITheme.Card("CartCard",parent);
            _ccName=AddRow(_cartCard.transform,"名称"); _ccCrew=AddRow(_cartCard.transform,"载客"); _ccCap=AddRow(_cartCard.transform,"载货"); _ccDura=AddRow(_cartCard.transform,"耐久");
            var row=Row(_cartCard.transform,30);
            _ccClose=UITheme.Btn("x",row.transform,"关闭",12);_ccClose.onClick.AddListener(CloseCart);
            _cartCard.SetActive(false);
        }
        private void BuildTreeCard(Transform parent)
        {
            _treeCard=UITheme.Card("TreeCard",parent);
            _tcName=AddRow(_treeCard.transform,"名称"); _tcAge=AddRow(_treeCard.transform,"树龄");
            var row=Row(_treeCard.transform,30);
            _tcClose=UITheme.Btn("x",row.transform,"关闭",12);_tcClose.onClick.AddListener(CloseTree);
            _treeCard.SetActive(false);
        }
        private Text AddRow(Transform parent,string label)
        {
            var r=Row(parent,24);
            UITheme.Label("l",r.transform,label,12,TextAnchor.MiddleLeft,UITheme.Sub).gameObject.AddComponent<LayoutElement>().preferredWidth=56;
            var t=UITheme.Label("v",r.transform,"",12,TextAnchor.MiddleLeft); t.horizontalOverflow=HorizontalWrapMode.Wrap; t.gameObject.AddComponent<LayoutElement>().flexibleWidth=1;
            return t;
        }
        public void ShowShip(ShipEntity s)
        {
            _scShip=s;_shipCard.SetActive(true);RenderShipCard();
            _shipCard.transform.SetAsLastSibling();
        }
        private void RenderShipCard()
        {
            if(_scShip==null)return;
            var s=_scShip;
            _scName.text=s.Name; _scLv.text="Lv"+s.Level;
            _scCrew.text=Mathf.RoundToInt(s.Crew)+"/"+GM.Naval.Capacity(s);
            _scDura.text=Mathf.RoundToInt(s.Hp)+"/"+GM.Naval.MaxDurability(s);
            _scCap.text=GM.Naval.Capacity(s)+" 人"; _scAtt.text=GM.Naval.AttackOf(s)+" 攻·"+GM.Naval.RangeOf(s).ToString("0")+" 射程";
        }
        public void CloseShipCard(){ if(_shipCard)_shipCard.SetActive(false); }
        public void ShowCart(CartEntity c){ _ccCart=c;_cartCard.SetActive(true);
            _ccName.text=c.Name; _ccCrew.text=c.Crew+"/"+c.Capacity+" 人"; _ccCap.text=c.Cargo+"/"+c.MaxCargo; _ccDura.text=c.Hp+"/"+c.MaxHp;
            _cartCard.transform.SetAsLastSibling(); }
        public void CloseCart(){ if(_cartCard)_cartCard.SetActive(false); }
        public void ShowTree(BanyanTree t){ _tcTree=t;_treeCard.SetActive(true);
            _tcName.text="大榕树"; _tcAge.text=t.Age.ToString("0")+" 年"; _treeCard.transform.SetAsLastSibling(); }
        public void CloseTree(){ if(_treeCard)_treeCard.SetActive(false); }

        // ============ 建筑内视图 / 属性 ============
        private void BuildBuildingModal(Transform parent)
        {
            _buildingModal=UITheme.Modal("BuildingModal","建筑",parent,out _);
        }

        // ============ 冷冻冷却条（V6.1.9）============
        private void BuildCryoBar(Transform parent)
        {
            _cryoBar=UITheme.Surface("CryoBar",parent,new Color(0.10f,0.14f,0.24f,0.92f));
            var rt=_cryoBar.GetComponent<RectTransform>();
            rt.anchorMin=new Vector2(0.5f,1);rt.anchorMax=new Vector2(0.5f,1);rt.pivot=new Vector2(0.5f,1);
            rt.anchoredPosition=new Vector2(0,-56);rt.sizeDelta=new Vector2(560,34);
            _cryoText=UITheme.Label("t",_cryoBar.transform,"",13,TextAnchor.MiddleCenter,UITheme.Hex(0x7fd0ff));
            Stretch(_cryoText.gameObject);
            _cryoBar.SetActive(false);
        }

        private void ToggleSound(){ GM.ToggleSound(); Toast(GM.SoundOn?"声音开":"声音关"); }
    }
}
