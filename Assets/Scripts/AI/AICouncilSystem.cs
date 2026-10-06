using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using Random = UnityEngine.Random;

namespace PixelToCivilization.AI
{
    /// <summary>
    /// V6.1.8 九智能体共治（AI 多智能体 / AI Council）。
    /// 九位职能"神灵"各管一域、自主决策，由神庭按紧迫度仲裁后执行白名单动作；
    /// 【离线规则引擎永远在线】是文明不灭绝的硬保证；【联网 DeepSeek 大模型（默认·官方直连支持浏览器 CORS；方舟可手动配置）】仅作可选增强：
    /// 大模型只能在同一动作白名单内选择/加权，任何失败、跨域、超时都静默回退离线，绝不阻塞游戏、绝不越界改数。
    /// 设计目标：无人工干预下，允许大饥荒/灾难/动乱让文明倒退，但 SafetyNet 把人口/粮食/住房/民心/军力钳在存续线之上并注入恢复条件，使文明 5000~10000 年兴衰而不断绝。
    /// </summary>
    public class AICouncilSystem : GameSystemBase
    {
        // ============ 运行开关与计量（可存档） ============
        public bool Enabled = true;                 // 九神共治总开关（默认开，实现"无人工干预自动运行"）
        public bool Online = true;                  // 离线/联网（已配置可用方舟Key，默认联网；离线规则引擎永久兜底，联网失败静默回退）
        public long TokensUsed;                     // 共用 Token 额度累计消耗
        public int IntervalYears = 3;               // 议政间隔（游戏年）
        public int LastCouncilYear;
        public int SafetyCount;                     // 存亡续绝（灭绝兜底）触发次数
        public string ApiKey = ""; // 开源版不内置任何密钥；启动时从环境变量 PXC_ARK_API_KEY 读取，留空则离线规则引擎兜底
        public string KeyId = "";   // 方舟控制台凭证ID（仅本地标识，不参与HTTP鉴权；开源版留空）
        public string KeyName = "";      // 方舟控制台凭证名称（仅本地标识；开源版留空）
        public string Endpoint = "https://api.deepseek.com/chat/completions"; // V9.3.9 DeepSeek 官方直连（实测 CORS 预检 200）；方舟通道可手动改回 ark.cn-beijing.volces.com/api/v3/chat/completions
        // V9.3.9：用户指定 DeepSeek-V4.1-Flash；官方 API 实测（2026-10-03）支持模型 ID：deepseek-flash / deepseek-v4-pro，故默认 deepseek-flash（即 V4.1-Flash 在线档）
        public string Model = "deepseek-flash";
        public bool NetOk = true;                   // 最近一次联网是否成功（失败自动离线降级）
        public string LastNetError = "";

        // ============ 存续线（硬保证） ============
        public int PopFloor => Mathf.Max(12, Mathf.RoundToInt(GameConstants.StartPop * 0.15f)); // 人口硬底=12
        public float Continuity { get; private set; } = 100f;  // 文明存续健康分 0~100
        public string PerfNote = "性能神监测中";
        public List<AIGod> Gods = new();
        // V9.4.0 文明历史日志（环形，最近 32 条，进存档）：神决策/大事件/灾难入账，供各神"以史为鉴"调整后续决策
        public List<string> History = new();
        public const int HistoryMax = 32;
        private float _safetyRealCd;                // 现实秒兜底检查节流
        private float _fpsSmooth = 60f;
        private int _qualityCooldown;
        private bool _requesting;                    // LLM 请求在途（防重入）

        // V9.4.0 神庭协作计数（本轮已执行的跨神动作，议政每轮重置）
        private int _synergyUsed;

        public void LogHistory(string ev)
        {
            History.Add(ev);
            if (History.Count > HistoryMax) History.RemoveRange(0, History.Count - HistoryMax);
        }
        string LastSameDomain(string domain)
        {
            for (int i=History.Count-1;i>=0;i--) if (History[i].StartsWith(domain)) return History[i];
            return null;
        }

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            // V9.3.9 密钥读取（源码/仓库不含任何密钥）：① 游戏内 PlayerPrefs（九神面板可填）② 环境变量（PXC_AI_KEY，兼容旧 PXC_ARK_API_KEY）；WebGL 交付包由 index.html 加载器 JS 自动 SendMessage(WebAISetKey) 注入 PlayerPrefs（构建时环境变量 PXC_AI_KEY 写入 meta ai-key）；全空则离线规则自治兜底
            if(string.IsNullOrEmpty(ApiKey))
            {
                ApiKey = UnityEngine.PlayerPrefs.GetString("PXC_AI_KEY","");
                if(string.IsNullOrEmpty(ApiKey))
                    ApiKey = System.Environment.GetEnvironmentVariable("PXC_AI_KEY")
                            ?? System.Environment.GetEnvironmentVariable("PXC_ARK_API_KEY") ?? "";
            }
            Online = Online && !string.IsNullOrEmpty(ApiKey);
            Debug.Log("[AICouncil] V9.3.9 AI 神通道："+(string.IsNullOrEmpty(ApiKey)?"离线规则自治（未配置密钥）":"DeepSeek 联网就绪 model="+Model+" endpoint="+Endpoint));
            BuildGods();
        }


        void BuildGods()
        {
            Gods.Clear();
            // id, 名称, 职责, 关注, 代表色, 性格倾向(0.4 温和 ~ 1.1 强势) —— V9.4.0 各神加四维性格向量
            Gods.Add(new AIGod("pop",  "人口神",     "人口·婚姻·生育·健康", "生育/瘟疫/移民", 0xFF8A80, 0.7f+Random.value*0.4f)
                { Intervene=Random.Range(0.5f,0.95f), Expand=Random.Range(0.3f,0.8f),  Prudent=Random.Range(0.3f,0.8f) });
            Gods.Add(new AIGod("farm", "土地农神",   "农业·土地·天气",       "丰收/饥荒/赈灾", 0x9CCC65, 0.7f+Random.value*0.4f)
                { Intervene=Random.Range(0.4f,0.9f), Expand=Random.Range(0.4f,0.85f), Prudent=Random.Range(0.3f,0.8f) });
            Gods.Add(new AIGod("tech", "技术工业神", "科技·工业·时代",       "研发/工业/进阶", 0x4FC3F7, 0.6f+Random.value*0.4f)
                { Intervene=Random.Range(0.5f,0.9f), Expand=Random.Range(0.5f,0.95f), Prudent=Random.Range(0.3f,0.7f) });
            Gods.Add(new AIGod("time", "时间事件神", "时间·朝代·历史",       "朝代更迭/民心", 0xFFD54F, 0.6f+Random.value*0.4f)
                { Intervene=Random.Range(0.5f,1.0f), Expand=Random.Range(0.3f,0.7f),  Prudent=Random.Range(0.4f,0.9f) });
            Gods.Add(new AIGod("res",  "资源神",     "资源·贸易·经济",       "储备/贸易路线", 0xA1887F, 0.6f+Random.value*0.4f)
                { Intervene=Random.Range(0.4f,0.9f), Expand=Random.Range(0.5f,0.9f),  Prudent=Random.Range(0.5f,1.0f) });
            Gods.Add(new AIGod("edu",  "教育文化神", "教育·文化·思想",       "学派/科技传播", 0xBA68C8, 0.5f+Random.value*0.4f)
                { Intervene=Random.Range(0.5f,0.95f), Expand=Random.Range(0.4f,0.8f), Prudent=Random.Range(0.3f,0.7f) });
            Gods.Add(new AIGod("org",  "组织神",     "制度·行政·建筑",       "政策/营建/效率", 0x7986CB, 0.6f+Random.value*0.4f)
                { Intervene=Random.Range(0.5f,0.95f), Expand=Random.Range(0.5f,0.9f),  Prudent=Random.Range(0.4f,0.8f) });
            Gods.Add(new AIGod("mil",  "军事外交神", "军事·外交·战争",       "战争/联盟/朝贡", 0xE57373, 0.7f+Random.value*0.5f)
                { Intervene=Random.Range(0.5f,1.0f), Expand=Random.Range(0.3f,0.7f),  Prudent=Random.Range(0.4f,0.9f) });
            Gods.Add(new AIGod("perf", "性能神",     "性能·画质·流畅",       "帧率/自动降档", 0x90A4AE, 0.8f)
                { Intervene=0.9f, Expand=0.2f, Prudent=0.7f });
        }

        // ===================== 主循环 =====================
        public override void OnYear(int year)
        {
            if (!Enabled) return;
            SafetyNet();                                  // 每年先做存续兜底（最高优先）
            if (year - LastCouncilYear >= IntervalYears)
            {
                LastCouncilYear = year;
                Council(year);
            }
            Continuity = ComputeContinuity();
        }

        public override void Tick(float dt)
        {
            // 性能神：平滑帧率（现实时间，不受暂停/倍速影响）
            if (Time.unscaledDeltaTime > 1e-4f)
                _fpsSmooth = Mathf.Lerp(_fpsSmooth, 1f / Time.unscaledDeltaTime, 0.08f);
            _qualityCooldown--;
            if (Enabled)
            {
                _safetyRealCd -= Time.unscaledDeltaTime;
                if (_safetyRealCd <= 0f) { _safetyRealCd = 2f; SafetyNet(); } // 现实秒兜底，防极端倍速下年内崩盘
                if (Gods.Count>0) Gods[8].Note = PerfTune();
            }
        }

        // ===================== 神庭议政：九神各自评估→仲裁→执行 =====================
        public void Council(int year)
        {
            foreach (var g in Gods)
            {
                float urg = Urgency(g.Id);
                g.Urgency = urg;
                // 紧迫度越高越必然出手；低紧迫也有小概率做"发展型"动作，让文明持续前进
                // V9.4.0 性格向量 Intervene：干预倾向高的神在低紧迫时也更常出手
                bool act = urg >= 0.45f || Random.value < 0.35f + g.Intervene * 0.15f;
                if (g.Id=="perf") { g.Note = PerfTune(); g.LastYear=year; g.Actions++; continue; }
                if (!act) { g.Status="休养"; continue; }
                g.Status = urg>=0.7f ? "预警" : "治理";
                var actText = OfflineDecide(g, urg);
                g.LastYear=year; g.Actions++;
                if(!string.IsNullOrEmpty(actText)) g.Note=actText;
            }
            // 联网增强（可选）：离线决策已保证存续，LLM 只做白名单内的二次微调与叙事
            if (Online && !_requesting && !string.IsNullOrEmpty(ApiKey)) StartCoroutine(RequestCounsel(year));
            // V9.4.0 神庭协作：跨神动作（最多 2 条/轮），让九神从"各自补数"升级为"共治协同"
            _synergyUsed = 0;
            CouncilSynergy(year);
        }

        /// <summary>V9.4.0 神庭协作扫描：有限容量的跨神动作，各条独立于单神决策、只做结构性治理</summary>
        void CouncilSynergy(int year)
        {
            if (S == null) return;
            // ① 人口神促生育 → 教育文化神教化新生代（文化+研究+）
            if (_synergyUsed < 2 && Random.value < 0.5f && S.Children > S.Pop * 0.3f)
            {
                AddCap("culture", Mathf.RoundToInt(6 + 8f), 200);
                AddCap("research", Mathf.RoundToInt(4f), 200);
                _synergyUsed++;
                LogHistory("[教育] 教化新生代·文化反哺科技（人口神协作）");
                GM.AddEvent("info","📚 教育文化神教化新生代，学派兴盛（人口神协作）");
            }
            // ② 技术神时代跃迁 → 组织神营建新 Era 基础设施（水井/道路，受经济保底约束）
            if (_synergyUsed < 2 && S.GetRes("food") > 80 && S.GetRes("wood") > 90 && Random.value < 0.45f)
            {
                string[] cand = S.Era >= 5 ? new[]{"well","water_mill"} : S.Era >= 3 ? new[]{"well","granary"} : new[]{"well"};   // V9.6.3 禁系统自动建路：候选剔除 road（道路只能玩家手动建）
                foreach (var type in cand)
                {
                    if (GM.Def(type) == null) continue;
                    if (GM.Building != null && GM.Building.FindAutoPosition(type,out var x,out var z) && GM.Building.PlaceBuilding(type,x,z))
                    {
                        _synergyUsed++;
                        LogHistory("[营建] 技术跃迁·组织神督建" + GM.Def(type).Name);
                        GM.AddEvent("good","🏗️ 组织神督建"+GM.Def(type).Name+"（技术工业神协作）");
                        break;
                    }
                }
            }
        }

        /// <summary>紧迫度 0~1：V9.4.0 多因子加权（该神关注指标偏离健康区间越远越紧迫），取各信号最大值</summary>
        float Urgency(string id)
        {
            float u=0.05f;
            float food=S.GetRes("food"), happy=S.Happiness, housingGap=S.Housing-S.Pop;
            switch(id)
            {
                case "pop":
                    u=Mathf.Max(u, S.Pop < S.MaxPop*0.35f ? 0.8f : housingGap<6 ? 0.6f : 0.2f);
                    // V9.4.0 年龄结构失衡（儿童占比过低=生育通道断裂）
                    if (S.Children < S.Pop*0.15f) u=Mathf.Max(u,0.65f);
                    break;
                case "farm":
                    u=Mathf.Max(u, food<40 ? 0.85f : food<90 ? 0.5f : 0.2f);
                    // V9.4.0 农田/水利缺口（有粮但无产出结构时提高干预）
                    if (food>=90 && S.Buildings.Count>0 && FoodBuildingCount()<2) u=Mathf.Max(u,0.5f);
                    break;
                case "tech":
                    u=Mathf.Max(u, S.GetRes("research")<8 ? 0.55f : 0.25f);
                    // V9.4.0 时代可研科技滞留（有可研究而未研）
                    if (GM.Tech!=null && string.IsNullOrEmpty(S.CurrentResearch)) u=Mathf.Max(u,0.45f);
                    break;
                case "time":
                    u=Mathf.Max(u, S.DynastyMorale<35||S.Corruption>60 ? 0.8f : S.Happiness<45?0.5f:0.2f);
                    // V9.4.0 朝代周期律预警：朝代寿命>250年 且 腐败>55 → 改革窗口
                    if (S.Year>250 && S.Corruption>55) u=Mathf.Max(u,0.7f);
                    break;
                case "res":
                    float low=Mathf.Min(S.GetRes("wood"),S.GetRes("stone"),S.GetRes("gold"));
                    u=Mathf.Max(u, low<15 ? 0.7f : low<40?0.4f:0.15f);
                    // V9.4.0 贸易路线可用性（大航海后船队闲置）
                    if (S.AgeOfSail && S.Ships.Count>0 && S.GetRes("gold")<160) u=Mathf.Max(u,0.5f);
                    break;
                case "edu":
                    u=Mathf.Max(u, S.GetRes("culture")<10 ? 0.55f : 0.2f);
                    // V9.4.0 教育建筑缺口（有文化但无学校/书院结构）
                    if (S.GetRes("culture")>=10 && CultureBuildingCount()<1) u=Mathf.Max(u,0.4f);
                    break;
                case "org":
                    u=Mathf.Max(u, housingGap<4 ? 0.8f : S.Buildings.Count<12?0.45f:0.2f);
                    // V9.4.0 基础设施缺口（无道路/水井时高紧迫）
                    if (InfraBuildingCount()<2 && S.Buildings.Count>=6) u=Mathf.Max(u,0.5f);
                    break;
                case "mil":
                    bool weak = S.MilSoldiers<6 && S.MilFirepower<12;
                    u=Mathf.Max(u, S.WarActive&&weak ? 0.95f : S.WarActive?0.6f:S.Era>=1?0.3f:0.15f);
                    // V9.4.0 军备缺口（Era>=3 无城防时提高紧迫）
                    if (S.Era>=3 && S.Pop>200 && MilitaryBuildingCount()==0) u=Mathf.Max(u,0.6f);
                    break;
                case "perf": u=0.2f; break;
            }
            return Mathf.Clamp01(u);
        }

        // V9.4.0 结构信号计数（供紧迫度/结构动作判断）
        int FoodBuildingCount(){ int n=0; foreach(var b in S.Buildings) if(b.Def!=null&&b.Def.Cat=="食物") n++; return n; }
        int CultureBuildingCount(){ int n=0; foreach(var b in S.Buildings) if(b.Def!=null&&(b.Def.Cat=="文化"||b.Def.Cat=="科技")) n++; return n; }
        int InfraBuildingCount(){ int n=0; foreach(var b in S.Buildings) if(b.Def!=null&&(b.Def.Cat=="交通"||b.Type=="well")) n++; return n; }
        int MilitaryBuildingCount(){ int n=0; foreach(var b in S.Buildings) if(b.Def!=null&&b.Def.Cat=="军事") n++; return n; }

        // ===================== 离线规则决策（硬保证，永远可用） =====================
        string OfflineDecide(AIGod g, float urg)
        {
            float zeal = g.Zeal; // 性格力度
            switch (g.Id)
            {
                case "pop": { // 创造生育条件：补住房 + 移民补口（受住房上限约束，不暴涨）
                    // V9.2.2 人口周期律：过剩/崩溃期人口神不再催生育，转轻徭薄赋、劝农赈济；现代低生育率则鼓励生育
                    var popSys = GM.Population;
                    int cycle = popSys!=null ? popSys.Cycle : 1;
                    if (cycle>=2)
                    {
                        AddCap("food", Mathf.RoundToInt((15+urg*30f)*zeal), 150);
                        S.Corruption = Mathf.Max(0, S.Corruption-3f*zeal);
                        return S.Era>=5 ? "人满为患·倡优生优育、完善养老" : "人地矛盾·轻徭薄赋、劝农赈济流民";
                    }
                    int add=0;
                    if (S.Housing < S.Pop+8) { int h=Mathf.CeilToInt((8+urg*8f)*zeal); S.Housing+=h; add+=h; }
                    if (S.Pop < S.MaxPop*0.5f && S.Housing>S.Pop+2 && S.GetRes("food")>40)
                    { int baby=Mathf.Max(1,Mathf.RoundToInt((1+urg*3f)*zeal)); S.Pop=Mathf.Min((int)S.Housing,GameConstants.MaxPop,S.Pop+baby); }
                    return add>0? $"促生育·增住房 {add}，抚育民口":"巡视婚配·妇幼安康";
                }
                case "farm": {
                    if (S.GetRes("food")<120) { int f=Mathf.RoundToInt((25+urg*55f)*zeal); AddCap("food",f,150); return $"劝课农桑·屯粮 +{f}（赈灾备荒）"; }
                    // V9.4.0 结构动作：有粮但农田/水车不足时督建，把"注资"变成"基建"
                    string b=null;
                    if (FoodBuildingCount()<2) b=TryBuildStructure(new[]{"farm","water_mill"});
                    if (b!=null) return b+"·以农为本";
                    return "风调雨顺·劝耕";
                }
                case "tech": {
                    AddCap("research", Mathf.RoundToInt((6+urg*14f)*zeal), 200);
                    TryAutoResearch(); // 自动选择一个当前时代可研究科技
                    // V9.4.0 结构动作：研究有余时督建科技建筑（低时代官学/印刷坊，高时代数据中心/AI实验室）
                    if (S.GetRes("research")>=25)
                    {
                        string tb=TryBuildStructure(new[]{"data_center","ai_lab","printing_house","academy_pre"});
                        if (tb!=null) return "格物致知·"+tb;
                    }
                    return "格物致知·推进研发与工业";
                }
                case "time": {
                    string note="修史明纪·安定朝纲";
                    if (S.Corruption>40){ float c=Mathf.Min(S.Corruption,(10+urg*25f)*zeal); S.Corruption-=c; note=$"整顿吏治·反腐 -{Mathf.RoundToInt(c)}"; }
                    if (S.DynastyMorale<55){ S.DynastyMorale=Mathf.Min(100,S.DynastyMorale+(8+urg*16f*zeal)); }
                    if (!S.MonarchWise){ GM.Culture?.RollMonarch(); note="选贤任能·更立明君"; }
                    // V9.4.0 周期律预警：朝代>250年且腐败高 → 改革窗口（民心动荡但腐败快速下降）
                    if (S.Year>250 && S.Corruption>55){ S.Corruption=Mathf.Max(0,S.Corruption-12f*zeal); S.Happiness=Mathf.Max(20,S.Happiness-6f); LogHistory("[改革] 周期律预警·改革窗口开启（腐-民心-）"); note="变法图强·以改革渡周期之厄"; }
                    return note;
                }
                case "res": {
                    var sb=new StringBuilder(); int acted=0;
                    foreach(var r in new[]{"wood","stone","iron","gold"})
                        if (S.GetRes(r)<40){ int v=Mathf.RoundToInt((8+urg*22f)*zeal); AddCap(r,v, S.AgeOfSail?220:140); acted++; }
                    if (S.AgeOfSail && S.GetRes("gold")<160){ AddCap("gold",Mathf.RoundToInt(20*zeal),300); sb.Append("开通商路·"); }
                    // V9.4.0 结构动作：开市建驿站/市场/银行，把贸易从"注资"变成"机构"
                    if (S.GetRes("gold")>=60 || S.GetRes("wood")>=120)
                    {
                        string rb=TryBuildStructure(new[]{"market","caravanserai","bank"});
                        if (rb!=null) return (sb.Length>0?sb.ToString():"")+rb+"·通货有道";
                    }
                    return acted>0||sb.Length>0 ? sb+"互通有无·补足关键储备" : "仓廪充实·贸易平顺";
                }
                case "edu": {
                    AddCap("culture", Mathf.RoundToInt((6+urg*16f)*zeal), 200);
                    AddCap("research", Mathf.RoundToInt(3*zeal), 200); // 学派传播反哺科研
                    // V9.4.0 结构动作：办学兴教（学堂/官学/现代学校/祭坛）
                    if (CultureBuildingCount()<2)
                    {
                        string eb=TryBuildStructure(new[]{"modern_school","school","academy_pre","altar"});
                        if (eb!=null) return "兴学教化·"+eb;
                    }
                    return "兴学教化·传播科技与思想";
                }
                case "org": {
                    if (S.Housing < S.Pop+6){ int h=Mathf.CeilToInt((6+urg*8f)*zeal); S.Housing+=h; return $"营建民居·住房 +{h}（吸纳流民）"; }
                    // V9.4.0 结构动作：基础设施（道路/水井）先于抽象效率
                    if (InfraBuildingCount()<3)
                    {
                        string ob=TryBuildStructure(new[]{"well"});   // V9.6.3 禁系统自动建路：候选剔除 road（道路只能玩家手动建）
                        if (ob!=null) return "厘清建制·"+ob+"（行政效率提升）";
                    }
                    // 行政效率：轻微降腐败、提民心
                    S.Corruption=Mathf.Max(0,S.Corruption-2f*zeal);
                    return "厘清建制·提升行政效率";
                }
                case "mil": {
                    string fort=AutoFortify();               // V6.3.7 按人口规模&资源自动营建防御/攻击建筑
                    if (S.WarActive && S.MilSoldiers<6 && S.MilFirepower<12)
                    {
                        // 危急：先尝试"朝贡求和"止损（外交），国力太弱时优先止戈；否则募兵守土
                        if (S.Pop < GameConstants.StartPop*0.4f && S.GetRes("gold")>=30)
                        { AddCap("gold",-30,99999); S.WarActive=false; return "纳贡和亲·暂息兵戈以养民（金-30）"; }
                        S.MilSoldiers+=Mathf.RoundToInt(4*zeal)+2; S.MilFirepower+=Mathf.RoundToInt(6*zeal)+3;
                        return fort??"募兵守土·整军御敌";
                    }
                    if (fort!=null) return fort;
                    if (S.Era>=1 && S.MilFirepower<20){ S.MilFirepower+=3*zeal; return "讲武练兵·巩固边防"; }
                    return "四境安定·武备常修";
                }
            }
            return "";
        }

        /// <summary>V9.4.0 结构动作：真实营建建筑（成本由 PlaceBuilding 实扣），经济保底+候选逐项尝试，每次议政至多1座；返回"督建XX"或 null</summary>
        string TryBuildStructure(string[] cand)
        {
            if (GM.Building==null) return null;
            // 经济保底：不得掏空民生储备
            if (S.GetRes("food")<60 || S.GetRes("wood")<70 || S.GetRes("stone")<30) return null;
            foreach (var type in cand)
            {
                if (GM.Def(type)==null) continue;
                if (GM.Building.FindAutoPosition(type,out var x,out var z) && GM.Building.PlaceBuilding(type,x,z))
                {
                    LogHistory("[营建] 督建"+GM.Def(type).Name+"（人口"+S.Pop+"）");
                    return "督建"+GM.Def(type).Name;
                }
            }
            return null;
        }

        // V6.3.7 军事外交神：按人口规模定目标工事数、按时代选防御/攻击建筑，留经济保底后实建（每次议政至多1座）
        string AutoFortify()
        {
            if (GM.Building==null) return null;
            if (S.Era<1) return null;                                   // 三皇时期无城防
            // 经济保底：不得掏空民生储备
            if (S.GetRes("food")<60 || S.GetRes("wood")<70 || S.GetRes("stone")<30) return null;
            int target=Mathf.Clamp(Mathf.FloorToInt(S.Pop/60f)+(S.WarActive?2:0),1,12);
            int have=0; foreach(var b in S.Buildings) if(b.Def!=null&&b.Def.Cat=="军事") have++;
            if (have>=target) return null;
            // 候选：战时/已有2座后优先攻击塔，否则先立防御；按时代递进
            var cand=new System.Collections.Generic.List<string>();
            if (S.WarActive || have>=2)
            {
                if(S.Era>=5) cand.AddRange(new[]{"bunker","cannon_tower","fire_tower","arrow_tower"});
                else if(S.Era>=3) cand.AddRange(new[]{"cannon_tower","fire_tower","arrow_tower","wall"});
                else if(S.Era>=2) cand.AddRange(new[]{"fire_tower","arrow_tower","watchtower","wall"});
                else cand.AddRange(new[]{"arrow_tower","watchtower","wall"});
            }
            else cand.AddRange(new[]{"wall","watchtower","arrow_tower"});
            foreach(var type in cand)
            {
                if(GM.Def(type)==null) continue;
                if(GM.Building.FindAutoPosition(type,out var x,out var z) && GM.Building.PlaceBuilding(type,x,z))
                {
                    var d=GM.Def(type);
                    GM.AddEvent("good","🛡️ 军事外交神度势营建："+d.Name+"（人口"+S.Pop+"·军事建筑"+(have+1)+"/"+target+"）");
                    return "营建"+d.Name+"·巩固防御";
                }
            }
            return null;
        }

        void TryAutoResearch()
        {
            if (GM.Tech==null) return;
            if (!string.IsNullOrEmpty(S.CurrentResearch)) return;
            for (int era=S.Era; era<=Mathf.Min(S.Era+1,7); era++)
                foreach (var t in GM.Tech.TechsOfEra(era))
                    if (GM.Tech.CanResearch(t.Id,out _)) { GM.Tech.StartResearch(t.Id); return; }
        }

        /// <summary>V9.5.7 只补不砍：AI 神仅在资源低于安全上限时补到上限，绝不在资源高于上限时"砍到 cap"。
        /// 旧实现 target=Min(cap, cur+delta)——cur 已高于 cap（如文化 500 万 > cap 200）时把资源直接砍到 cap
        /// （500万→200），正是用户长期上报"粮食/金币/文化/科技几百万骤降到几千"的根因；
        /// 基建消耗触发 AI 补资源阈值（劝课农桑/开通商路/学派传播/赈灾）即触发砍价。负 delta（纳贡扣金）原样扣减。</summary>
        void AddCap(string res, float delta, float cap)
        {
            if (delta > 0f)
            {
                float cur=S.GetRes(res);
                if (cur >= cap) return;                 // 已达/超过安全上限：完全不动，绝不砍资源
                S.AddRes(res, Mathf.Min(cap, cur+delta)-cur);
            }
            else
            {
                S.AddRes(res, delta);                   // 负 delta（如金 -30 纳贡和亲）：按绝对值扣，不受 cap 钳制
            }
        }

        // ===================== 存亡续绝 SafetyNet（最高优先·硬保证不灭绝） =====================
        public void SafetyNet()
        {
            // 1) 人口硬底 + 恢复条件（住房/粮/民心一并补齐，使其能重新繁衍，而非僵在底线）
            if (S.Pop < PopFloor)
            {
                S.Pop = PopFloor + Random.Range(4,9);
                S.Housing = Mathf.Max(S.Housing, S.Pop+12);
                AddCap("food",80,160);
                S.Happiness=Mathf.Max(S.Happiness,42f);
                SafetyCount++;
                LogHistory("[存续] 九神合议·存亡续绝（人口回升至 "+S.Pop+"）");
                GM.AddEvent("good","🕯️ 九神合议·存亡续绝：招抚流亡、休养生息，文明火种得以延续（人口回升至 "+S.Pop+"）");
            }
            // 2) 住房短缺：组织神补建，保证生育通道
            if (S.Housing < S.Pop+2) S.Housing += Mathf.CeilToInt((S.Pop+2-S.Housing)+4f);
            // 3) 饥荒线：土地农神保底赈粮，避免连锁饿死
            if (S.GetRes("food") < 20) { AddCap("food",60,150); LogHistory("[赈灾] 土地农神开仓赈灾"); GM.AddEvent("good","🌾 土地农神开仓赈灾，暂缓饥荒"); }
            // 4) 民心/天命崩溃：时间事件神维稳（允许动乱，但不允许民心归零而崩解）
            if (S.Happiness < 18){ S.Happiness=Mathf.Max(S.Happiness,46f); GM.AddEvent("info","⚖️ 九神抚民安定，民心止跌回升"); }
            if (S.DynastyMorale < 18) S.DynastyMorale=Mathf.Max(S.DynastyMorale,50f);
            if (S.Corruption > 75) S.Corruption=40f;
            // 5) 战时无兵无防即被灭国：军事外交神保底守土或朝贡止战
            if (S.WarActive && S.MilSoldiers<4 && S.MilFirepower<8)
            {
                if (S.GetRes("gold")>=30 && S.Pop<GameConstants.StartPop*0.5f){ AddCap("gold",-30,99999); S.WarActive=false; GM.AddEvent("info","🕊️ 军事外交神斡旋朝贡，暂止战端"); }
                else { S.MilSoldiers+=6; S.MilFirepower+=9; LogHistory("[存续] 危急存亡·征募义兵守土"); GM.AddEvent("good","🛡️ 危急存亡之秋，军事外交神征募义兵拱卫社稷"); }
            }
            // V9.4.0 6a) 财政线：战时国库枯竭 → 军事外交神强制止战（防军费拖垮财政而崩盘）
            if (S.WarActive && S.GetRes("gold")<12)
            {
                S.WarActive=false; LogHistory("[存续] 国库枯竭·军事外交神议和止战");
                GM.AddEvent("info","🕊️ 国库枯竭，军事外交神议和止战以保民生");
            }
            // V9.4.0 6b) 城防线：Era>=3 且人口>200 且无任何城防 → 组织神/军事神补建城墙
            if (S.Era>=3 && S.Pop>200 && MilitaryBuildingCount()==0)
            {
                if (GM.Building!=null && GM.Def("wall")!=null
                    && GM.Building.FindAutoPosition("wall",out var wx,out var wz) && GM.Building.PlaceBuilding("wall",wx,wz))
                {
                    LogHistory("[存续] 城防空窗·组织神督建城墙");
                    GM.AddEvent("good","🏰 城防空窗，组织神督建城墙拱卫都邑");
                }
            }
            // 6) 四项年龄结构归一，防止补口后结构失真
            GM.Population?.NormalizeAge();
        }

        /// <summary>文明存续健康分（面板显示）：人口/粮/住房冗余/民心/天命/无内战综合</summary>
        public float ComputeContinuity()
        {
            float pop = Mathf.Clamp01(S.Pop/(float)GameConstants.StartPop);
            float food= Mathf.Clamp01(S.GetRes("food")/120f);
            float house=Mathf.Clamp01((S.Housing-S.Pop+10)/30f);
            float happy=S.Happiness/100f, morale=S.DynastyMorale/100f;
            float war = S.WarActive?0.85f:1f;
            float v=100f*(0.30f*pop+0.18f*food+0.14f*house+0.16f*happy+0.12f*morale+0.10f*1f)*war;
            Continuity=Mathf.Clamp(v,0,100);
            return Continuity;
        }

        // ===================== 性能神：按帧率/规模自动调档 =====================
        string PerfTune()
        {
            int ents=S.Buildings.Count+S.Agents.Count+S.Ships.Count;
            // V9.4.0 实体软上限：超限强制最低档（防 WebGL 内存/合批爆炸），恢复带迟滞防抖
            if (ents>=400)
            {
                QualitySettings.SetQualityLevel(0,false);
                Application.targetFrameRate=30;
                _qualityCooldown=240;
                return $"FPS {_fpsSmooth:F0}｜实体 {ents} 超预算→最低画质保稳定";
            }
            if (_qualityCooldown>0) return $"FPS {_fpsSmooth:F0}｜实体 {ents}";
            int lvl=QualitySettings.GetQualityLevel();
            if (_fpsSmooth<28f && lvl>0)
            {
                QualitySettings.DecreaseLevel(); Application.targetFrameRate=Mathf.Max(30,Application.targetFrameRate==-1?45:Application.targetFrameRate-15);
                _qualityCooldown=240; return $"FPS {_fpsSmooth:F0} 偏低→自动降档保流畅｜实体 {ents}";
            }
            if (_fpsSmooth>56f && lvl<QualitySettings.names.Length-1 && ents<220)
            {
                QualitySettings.IncreaseLevel(); _qualityCooldown=300;
                return $"FPS {_fpsSmooth:F0} 充裕→提升画质｜实体 {ents}";
            }
            return $"FPS {_fpsSmooth:F0}｜实体 {ents}｜画质档{lvl}";
        }

        // ===================== ARK 大模型联网增强（可选，失败静默降级离线） =====================
        IEnumerator RequestCounsel(int year)
        {
            _requesting=true; NetOk=true;
            string snapshot=BuildSnapshot(year);
            string sys="你是文明模拟中的AI执政官之一。只能从动作白名单选择，不得发明动作、不得使文明灭绝。"+
                       "输出紧凑JSON数组，每个元素 {\"god\":\"pop|farm|tech|time|res|edu|org|mil\",\"act\":\"addRes|addHousing|addPop|happy|morale|research|culture|defend|peace|agriculture|build|trade|urbanize|diplomacy|innovation\",\"amt\":数字,\"note\":\"20字内中文短评\"}。最多6条，amt保守。";
            // V9.3.9 实测（2026-10-03）：deepseek-flash 带可见思考链，必须 thinking.type=disabled 才能拿到 JSON 正文（禁用后≈259 tokens 返回6条动作；不传则思考链耗尽 max_tokens，content 恒空）
            var payload="{\"model\":\""+Model+"\",\"messages\":[{\"role\":\"system\",\"content\":\""+Esc(sys)+"\"},{\"role\":\"user\",\"content\":\""+Esc(snapshot)+"\"}],\"temperature\":0.6,\"max_tokens\":400,\"thinking\":{\"type\":\"disabled\"}}";
            using var req=new UnityWebRequest(Endpoint,"POST");
            byte[] body=Encoding.UTF8.GetBytes(payload);
            req.uploadHandler=new UploadHandlerRaw(body);
            req.downloadHandler=new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type","application/json");
            req.SetRequestHeader("Authorization","Bearer "+ApiKey);
            req.timeout=20;
            yield return req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
            bool fail=req.result!=UnityWebRequest.Result.Success;
#else
            bool fail=req.isNetworkError||req.isHttpError;
#endif
            if (fail)
            {
                NetOk=false; LastNetError=req.error; _requesting=false;
                Debug.Log("[AICouncil] 联网失败，已保持离线治理："+req.error);
                yield break;
            }
            try
            {
                string txt=req.downloadHandler.text;
                var tm=Regex.Match(txt,"\"total_tokens\"\\s*:\\s*(\\d+)");
                if(tm.Success) TokensUsed+=long.Parse(tm.Groups[1].Value);
                ApplyLLMActions(txt);
                NetOk=true;
            }
            catch(Exception e){ NetOk=false; LastNetError=e.Message; }
            _requesting=false;
        }

        string BuildSnapshot(int year)
        {
            var b=new StringBuilder();
            float fFood=S.GetRes("food"),fWood=S.GetRes("wood"),fStone=S.GetRes("stone"),fGold=S.GetRes("gold"),fResearch=S.GetRes("research"),fCulture=S.GetRes("culture");
            b.Append($"游戏年{year} 时代{S.Era} 人口{S.Pop}/上限{S.MaxPop} 住房{S.Housing} ");
            b.Append($"粮{fFood:F0} 木{fWood:F0} 石{fStone:F0} 金{fGold:F0} 研究{fResearch:F0} 文化{fCulture:F0} ");
            b.Append($"民心{S.Happiness:F0} 天命{S.DynastyMorale:F0} 腐败{S.Corruption:F0} 兵{S.MilSoldiers:F0}/火力{S.MilFirepower:F0} 战争{S.WarActive} 大航海{S.AgeOfSail}。");
            b.Append("目标：避免灭绝、允许短期倒退、稳健延续。请给下一步保守治理动作。");
            return b.ToString();
        }

        void ApplyLLMActions(string txt)
        {
            var objs=Regex.Matches(txt,"\\{[^{}]*\\}");
            int applied=0;
            foreach(Match m in objs)
            {
                if(applied>=6) break;
                string o=m.Value;
                var gm=Regex.Match(o,"\"god\"\\s*:\\s*\"([a-z]+)\"");
                var am=Regex.Match(o,"\"act\"\\s*:\\s*\"([a-zA-Z]+)\"");
                var nm=Regex.Match(o,"\"amt\"\\s*:\\s*(-?[0-9.]+)");
                var qm=Regex.Match(o,"\"note\"\\s*:\\s*\"([^\"]*)\"");
                if(!gm.Success||!am.Success) continue;
                string god=gm.Groups[1].Value, act=am.Groups[1].Value;
                float amt=nm.Success?Mathf.Clamp(float.Parse(nm.Groups[1].Value),-40,60):10f;
                string note=qm.Success?qm.Groups[1].Value:"";
                // 白名单 + 数值钳制：LLM 只能在安全范围内微调，绝不能越界
                // V9.4.0 白名单扩展 +6：agriculture/build/trade/urbanize/diplomacy/innovation（均映射到离线结构动作同源实现）
                switch(act)
                {
                    case "addRes": AddCap("food", Mathf.Abs(amt), 1000); break;   // V9.1.3 修复：缺省按粮食落地（原空 break 导致国策提示成功但数值未变）
                    case "addHousing": S.Housing+=Mathf.Abs(amt); break;
                    case "addPop": if(S.Housing>S.Pop) S.Pop=Mathf.Min((int)S.Housing,S.Pop+Mathf.CeilToInt(Mathf.Abs(amt))); break;
                    case "happy": S.Happiness=Mathf.Clamp(S.Happiness+amt,0,100); break;
                    case "morale": S.DynastyMorale=Mathf.Clamp(S.DynastyMorale+amt,0,100); break;
                    case "research": AddCap("research",Mathf.Abs(amt),200); break;
                    case "culture": AddCap("culture",Mathf.Abs(amt),200); break;
                    case "defend": S.MilFirepower+=Mathf.Abs(amt); S.MilSoldiers+=1; break;
                    case "peace": if(S.WarActive){S.WarActive=false;} break;
                    case "agriculture": AddCap("food", Mathf.Abs(amt), 1000); break;          // 土地农神：屯粮赈灾（同 addRes 语义）
                    case "build": S.Housing+=Mathf.Abs(amt); break;                            // 组织神：补建民居（同 addHousing）
                    case "trade": AddCap("gold", Mathf.Abs(amt), 400); break;                  // 资源神：开市通商（金）
                    case "urbanize": S.Housing+=Mathf.Abs(amt); S.Happiness=Mathf.Clamp(S.Happiness+3,0,100); break; // 组织神：城市化
                    case "diplomacy": if(S.WarActive){S.WarActive=false;} S.Happiness=Mathf.Clamp(S.Happiness+5,0,100); break; // 军事外交神：议和
                    case "innovation": AddCap("research", Mathf.Abs(amt)*1.2f, 200); break;    // 技术工业神：创新窗口
                    default: continue;
                }
                var ag=Gods.Find(x=>x.Id==god);
                if(ag!=null){ ag.Note="🌐 "+note; ag.Actions++; }
                applied++;
            }
            if(applied>0) GM.AddEvent("info","🌐 九神联网议政，采纳 "+applied+" 条国策（共用Token 累计 "+TokensUsed+"）");
        }

        static string Esc(string s)=>s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n"," ").Replace("\r"," ");

        // ===================== UI / Web / 存档接口 =====================
        public void ToggleEnabled(){ Enabled=!Enabled; foreach(var g in Gods) g.Status=Enabled?"治理":"休眠"; }
        public void SetOnline(bool on){ Online=on; LastNetError=on?"":LastNetError; }

        /// <summary>V9.3.9 设置 AI 密钥（游戏内/Web 桥接调用）：写入本机 PlayerPrefs，不入存档不入仓库；空值=切回离线规则自治</summary>
        public void SetApiKey(string key)
        {
            key=(key??"").Trim();
            if(key.Length>0){ ApiKey=key; UnityEngine.PlayerPrefs.SetString("PXC_AI_KEY",key); Online=true; NetOk=true; LastNetError=""; }
            else { ApiKey=""; Online=false; LastNetError="密钥已清空·离线规则自治（不耗Token）"; UnityEngine.PlayerPrefs.SetString("PXC_AI_KEY",""); }
            UnityEngine.PlayerPrefs.Save(); // V9.3.9：立即持久化（WebGL 下不 Save 不落 localStorage，清空/填写后刷新会回读旧值）
            Debug.Log("[AICouncil] V9.3.9 密钥已"+(key.Length>0?"配置·切换 DeepSeek 联网":"清空·离线自治")+" model="+Model);
        }
        public void CouncilNow(){ Council(S.Year); SafetyNet(); Continuity=ComputeContinuity(); }
    }

    /// <summary>单个 AI 智能体（职能神）定义与运行态</summary>
    public class AIGod
    {
        public string Id,Name,Domain,Focus; public long Color; public float Zeal;
        // V9.4.0 性格向量（四维，每局随机、存档持久）：Zeal 出手力度 / Intervene 干预倾向 / Expand 扩张偏好 / Prudent 谨慎度（国库保底比例）
        public float Intervene=0.6f, Expand=0.5f, Prudent=0.5f;
        public int LastYear; public long Actions; public string Status="休眠"; public string Note=""; public float Urgency;
        public AIGod(string id,string name,string domain,string focus,long color,float zeal)
        { Id=id;Name=name;Domain=domain;Focus=focus;Color=color;Zeal=zeal; }
    }
}
