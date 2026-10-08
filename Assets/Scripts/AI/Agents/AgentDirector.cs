using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using PixelToCivilization.Core;
using PixelToCivilization.Systems;
using PixelToCivilization.AI;

namespace PixelToCivilization.AI.Agents
{
    public enum AgentRuntimeMode { Offline, Online, Hybrid }

    /// <summary>
    /// 智能体驱动层入口（MonoBehaviour）：注册全量身份、自主调度、消息协作、三模式与行为库。
    /// 非侵入：不重写玩法，"行动"委托给现有系统（战斗/集结/城市服务等已在运行）；
    /// 身份层负责"决策、协作表达、行为沉淀"，且全部异常都被捕获降级。
    /// </summary>
    public class AgentDirector : MonoBehaviour
    {
        public static AgentDirector Instance { get; private set; }

        public AgentRegistry Registry { get; private set; } = new AgentRegistry();
        public AgentMessageBus Bus { get; private set; } = new AgentMessageBus();
        public AgentBehaviorLibrary Library { get; private set; } = new AgentBehaviorLibrary();

        public AgentRuntimeMode Mode { get; private set; } = AgentRuntimeMode.Offline;
        public int TotalDecisions { get; private set; }
        public int LlmCalls { get; private set; }

        private GameManager _gm;
        private float _selfTimer;       // 自主调度（现实秒）
        private float _speakTimer;      // 自我介绍广播节流
        private int _selfCursor;
        private readonly List<AgentIdentity> _selfOrder = new();
        private readonly List<string> _llmPending = new();   // 正在等待 LLM 的情境 key
        private const string LibKey = "PXC_AgentLib_v980";
        private const string ModeKey = "PXC_AgentMode_v980";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        /// <summary>由 GameManager 在所有系统装配完成后调用（幂等）</summary>
        public void Bootstrap(GameManager gm)
        {
            _gm = gm;
            try
            {
                var catalog = AgentCatalog.Build();
                foreach (var id in catalog)
                {
                    Registry.Register(id);
                    if (id.Kind == AgentKind.System || id.Kind == AgentKind.Manager)
                        BindIdentityToSystem(id);
                }
                _selfOrder.AddRange(Registry.All);
                // 按优先级降序，自主调度从高优先级开始
                _selfOrder.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                LoadLibrary();
                LoadMode();
                Debug.Log("[AgentDirector] 身份注册完成，数量=" + Registry.Count + "，模式=" + Mode);
            }
            catch (Exception e) { Debug.LogError("[AgentDirector] bootstrap error: " + e.Message); }
        }

        // 把系统级身份 id 绑定到对应系统类型（按命名约定）
        private void BindIdentityToSystem(AgentIdentity id)
        {
            Type t = id.Id switch
            {
                "sys_time" => typeof(GameTime),
                "sys_weather" => typeof(WeatherSystem),
                "sys_tide" => typeof(TideSystem),
                "sys_oceanflow" => typeof(OceanCurrentSystem),
                "sys_expansion" => typeof(WorldExpansionSystem),
                "sys_population" => typeof(PopulationSystem),
                "sys_economy" => typeof(EconomySystem),
                "sys_building" => typeof(BuildingSystem),
                "sys_tech" => typeof(TechSystem),
                "sys_policy" => typeof(PolicySystem),
                "sys_military" => typeof(MilitarySystem),
                "sys_naval" => typeof(NavalSystem),
                "sys_ground" => typeof(GroundWarfareSystem),
                "sys_combat" => typeof(CombatSystem),
                "sys_rally" => typeof(RallySystem),
                "sys_warbroadcast" => typeof(WarBroadcastSystem),
                "sys_airlift" => typeof(AirLiftSystem),
                "sys_bridge" => typeof(BridgeSystem),
                "sys_cart" => typeof(CartSystem),
                "sys_moderntraffic" => typeof(ModernTrafficSystem),
                "sys_train" => typeof(TrainSystem),
                "sys_intercity" => typeof(IntercityNetworkSystem),
                "sys_culture" => typeof(CultureSystem),
                "sys_philosophy" => typeof(PhilosophySystem),
                "sys_disaster" => typeof(DisasterSystem),
                "sys_historyevent" => typeof(HistoryEventSystem),
                "sys_cityservices" => typeof(CityServicesSystem),
                "sys_citymetrics" => typeof(CityMetricsSystem),
                "sys_cityfinance" => typeof(CityFinanceSystem),
                "sys_nation" => typeof(NationSystem),
                "sys_colonization" => typeof(ColonizationSystem),
                "sys_expedition" => typeof(ExpeditionSystem),
                "sys_wonder" => typeof(WonderSystem),
                "sys_canal" => typeof(CanalSystem),
                "sys_ocean" => typeof(OceanExpansionSystem),
                "sys_space" => typeof(SpaceExpansionSystem),
                "sys_victory" => typeof(VictorySystem),
                "sys_environment" => typeof(EnvironmentSystem),
                "sys_infrastructure" => typeof(InfrastructureSystem),
                "sys_council" => typeof(AICouncilSystem),
                "mgr_memory" => typeof(MemoryBudgetManager),
                "mgr_save" => typeof(SaveSystem),
                "mgr_pool" => typeof(GameObjectPool),
                "mgr_crashguard" => typeof(CrashGuardSystem),
                _ => null
            };
            if (t != null) Registry.BindSystem(t, id.Id);
        }

        // ================= 运行时自主调度 =================
        void Update()
        {
            // 现实秒节流（暂停时也能"思考"，但不产生玩法动作）
            _selfTimer += Time.unscaledDeltaTime;
            if (_selfTimer < 0.8f) return;
            _selfTimer = 0f;
            try
            {
                int year = _gm != null && _gm.State != null ? _gm.State.Year : 0;
                // 1) 处理待处理的求助/请求：让合适的智能体回应（协作）
                ResolvePending(year);
                // 2) 让一个智能体"自主行动"（轮流，受优先级影响；节流，单帧只处理一个）
                SelfActOne(year);
                // 3) 偶尔自我介绍（很低频，仅在智能体面板可见时有意义，这里记录即可）
                _speakTimer += 0.8f;
            }
            catch (Exception e) { Debug.LogError("[AgentDirector] update error: " + e.Message); }
        }

        // 让一个智能体自主行动
        private void SelfActOne(int year)
        {
            if (_selfOrder.Count == 0) return;
            var id = _selfOrder[_selfCursor % _selfOrder.Count];
            _selfCursor++;
            // 仅对系统/指挥官类做"自主决策"，原型类只在有实体时（玩法层）行动
            if (id.Kind != AgentKind.System && id.Kind != AgentKind.Commander
                && id.Kind != AgentKind.Manager) return;

            string situation = ReadSituation(id, year);
            string action = DecideAction(id, situation);
            TotalDecisions++;
            // 混合模式：在线决策沉淀；离线模式：记录"采用了已有行为"
            if (Mode == AgentRuntimeMode.Hybrid)
                Library.Record(id.Id, situation, action, "hybrid");
            else if (Mode == AgentRuntimeMode.Offline)
            {
                var recalled = Library.Recall(id.Id, situation);
                if (recalled != null) recalled.Uses++;
            }
        }

        // 读取智能体当前情境（极简、零分配压力，用字符串拼接在节流处少量发生）
        private string ReadSituation(AgentIdentity id, int year)
        {
            if (_gm == null || _gm.State == null) return "初始";
            var st = _gm.State;
            switch (id.Id)
            {
                case "sys_population":
                    return st.Pop > 0 && st.Pop < 200 ? "人口恢复期" : "人口常态";
                case "sys_naval":
                case "sys_combat":
                    return "巡防常态";
                case "sys_disaster":
                    return "无灾";
                case "sys_weather":
                    return "天气常态";
                default:
                    return "常态";
            }
        }

        // 决策：在线/混合 → LLM（异步，结果进行为库）；离线 → 本地规则/行为库
        private string DecideAction(AgentIdentity id, string situation)
        {
            // 离线默认动作：按身份职责"各尽其职"（玩法层系统已在运行，这里表达决策）
            string offline = DefaultAction(id, situation);
            if (Mode == AgentRuntimeMode.Offline) return offline;
            // 在线/混合：若已有同情境行为则复用，否则异步问 LLM
            var known = Library.Recall(id.Id, situation);
            if (known != null) return known.Action;
            string key = id.Id + "|" + situation;
            if (!_llmPending.Contains(key))
            {
                _llmPending.Add(key);
                StartCoroutine(RequestAgentActionCoroutine(id, situation, offline));
            }
            return offline; // 本轮先用离线动作，LLM 结果回来后沉淀
        }

        private string DefaultAction(AgentIdentity id, string situation)
        {
            if (id.Capabilities != null && id.Capabilities.Length > 0)
                return "尽职：" + id.Capabilities[0];
            return "维持常态";
        }

        // ================= 求助 / 回应协作 =================
        // 处理总线上的求助与请求：找最合适的智能体回应
        private void ResolvePending(int year)
        {
            // 取最近的几条 Help/Request（反向遍历 Messages 副本）
            var msgs = Bus.Messages;
            int scanned = 0;
            for (int i = msgs.Count - 1; i >= 0 && scanned < 10; i--, scanned++)
            {
                var m = msgs[i];
                if (m.Type != AgentMessageType.Help && m.Type != AgentMessageType.Request) continue;
                // 检查是否已被回应（其后存在同 to 的 Reply）
                bool answered = false;
                for (int j = i + 1; j < msgs.Count; j++)
                {
                    if (msgs[j].Type == AgentMessageType.Reply && msgs[j].Topic == m.Topic)
                    { answered = true; break; }
                }
                if (answered) continue;

                // 找到能帮忙的智能体（同能力域，优先高优先级）
                var helper = FindHelper(m.Topic, m.From);
                if (helper != null)
                {
                    Bus.Reply(helper.Id, m.From, m.Topic,
                        "收到，我来支援。" + helper.Name + "已就位。",
                        year, Time.realtimeSinceStartup);
                    // 混合模式：把"求助→回应"沉淀为协作行为
                    if (Mode == AgentRuntimeMode.Hybrid)
                        Library.Record(helper.Id, "收到求助:" + m.Topic,
                            "前往支援 " + m.From, "hybrid");
                }
            }
        }

        // 按主题找能帮忙的智能体（能力标签或 id 命中主题）
        private AgentIdentity FindHelper(string topic, string excludeId)
        {
            AgentIdentity best = null;
            foreach (var a in Registry.All)
            {
                if (a.Id == excludeId) continue;
                bool hit = a.Id.Contains(topic) || a.Group != null && a.Group.Contains(topic);
                if (!hit && a.Capabilities != null)
                    foreach (var c in a.Capabilities)
                        if (c.Contains(topic) || topic.Contains(c)) { hit = true; break; }
                if (hit && (best == null || a.Priority > best.Priority)) best = a;
            }
            return best;
        }

        /// <summary>供玩法层/探针调用：某智能体主动求助（如某船在战斗中求援）</summary>
        public void CallForHelp(string agentId, string topic, string content)
        {
            int year = _gm != null && _gm.State != null ? _gm.State.Year : 0;
            Bus.Help(agentId, topic, content, year, Time.realtimeSinceStartup);
        }

        /// <summary>探针：立即处理一次总线上的求助/请求（外部手动触发，不等待 Update 节流）</summary>
        public void ResolvePendingProbe()
        {
            int year = _gm != null && _gm.State != null ? _gm.State.Year : 0;
            ResolvePending(year);
        }

        // ================= 在线 LLM 决策 =================
        private IEnumerator<UnityWebRequestAsyncOperation> RequestAgentActionCoroutine(
            AgentIdentity id, string situation, string fallback)
        {
            string key = PlayerPrefs.GetString("PXC_AI_KEY", "");
            if (string.IsNullOrEmpty(key)) key = PlayerPrefs.GetString("PXC_ArkKey", "");
            if (string.IsNullOrEmpty(key))
            {
                LlmFallback(id, situation, fallback, "无密钥");
                yield break;
            }
            LlmCalls++;
            string endpoint = "https://api.deepseek.com/chat/completions";
            if (PlayerPrefs.GetString("PXC_AI_BASE", "") != "")
                endpoint = PlayerPrefs.GetString("PXC_AI_BASE");
            string model = PlayerPrefs.GetString("PXC_AI_MODEL", "deepseek-chat");

            var payload = new AgentLlmPayload
            {
                model = model,
                messages = new List<AgentLlmMsg>
                {
                    new AgentLlmMsg { role = "system",
                        content = "你是游戏中一个智能体。只输出一个简短动作（20字内），描述我此刻该做什么。" },
                    new AgentLlmMsg { role = "user",
                        content = "我是" + id.Name + "，职责是" + id.Domain + "。当前情境：" + situation + "。我该做什么？" }
                },
                max_tokens = 80,
                temperature = 0.6f
            };
            string body = JsonUtility.ToJson(payload);
            using var req = new UnityWebRequest(endpoint, "POST");
            byte[] data = Encoding.UTF8.GetBytes(body);
            req.uploadHandler = new UploadHandlerRaw(data);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + key);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                LlmFallback(id, situation, fallback, "LLM请求失败:" + req.error);
            }
            else
            {
                try
                {
                    var resp = JsonUtility.FromJson<AgentLlmResponse>(req.downloadHandler.text);
                    string action = resp.choices[0].message.content.Trim();
                    // 只沉淀有效动作
                    if (!string.IsNullOrEmpty(action))
                    {
                        Library.Record(id.Id, situation, action, "online");
                        if (Mode == AgentRuntimeMode.Hybrid)
                            Library.Record(id.Id, situation, action, "hybrid");
                        SaveLibrary();
                    }
                }
                catch (Exception e) { LlmFallback(id, situation, fallback, "解析失败:" + e.Message); }
            }
        }

        private void LlmFallback(AgentIdentity id, string situation, string fallback, string why)
        {
            // 降级离线：保证不阻断
            if (Mode == AgentRuntimeMode.Hybrid)
                Library.Record(id.Id, situation, fallback, "hybrid");
            string pkey = id.Id + "|" + situation;
            _llmPending.Remove(pkey);
        }

        // ================= 模式切换 =================
        public void SetMode(AgentRuntimeMode m)
        {
            Mode = m;
            PlayerPrefs.SetString(ModeKey, ((int)m).ToString());
            PlayerPrefs.Save();
            int year = _gm != null && _gm.State != null ? _gm.State.Year : 0;
            Bus.Broadcast("director", "mode", "智能体模式切换为：" + m, year, Time.realtimeSinceStartup);
        }

        public void CycleMode() => SetMode((AgentRuntimeMode)(((int)Mode + 1) % 3));

        private void LoadMode()
        {
            string s = PlayerPrefs.GetString(ModeKey, "0");
            if (int.TryParse(s, out int v) && v >= 0 && v < 3) Mode = (AgentRuntimeMode)v;
        }

        // ================= 行为库持久化 =================
        public void SaveLibrary()
        {
            try
            {
                var wrap = new AgentLibWrap { Lib = Library };
                PlayerPrefs.SetString(LibKey, JsonUtility.ToJson(wrap));
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogError("[AgentDirector] save lib error: " + e.Message); }
        }

        private void LoadLibrary()
        {
            try
            {
                string s = PlayerPrefs.GetString(LibKey, "");
                if (!string.IsNullOrEmpty(s))
                {
                    var wrap = JsonUtility.FromJson<AgentLibWrap>(s);
                    if (wrap != null && wrap.Lib != null) Library = wrap.Lib;
                }
            }
            catch (Exception e) { Debug.LogError("[AgentDirector] load lib error: " + e.Message); }
        }

        /// <summary>清空行为库（探针/设置）</summary>
        public void ClearLibrary() { Library.Clear(); PlayerPrefs.DeleteKey(LibKey); }

        // ================= 实体类型 → 身份 id 映射 =================
        public static string BuildingTypeToAgent(string buildingTypeId)
        {
            return buildingTypeId switch
            {
                "hut" or "house" or "residence" => "bld_hut",
                "well" => "bld_well",
                "granary" => "bld_granary",
                "farm" or "farmland" => "bld_farm",
                "lumbermill" or "lumber" => "bld_lumbermill",
                "mine" or "quarry" => "bld_mine",
                "market" => "bld_market",
                "wall" => "bld_wall",
                "arrow_tower" or "arrow" => "bld_arrow_tower",
                "fire_tower" or "fire" => "bld_fire_tower",
                "cannon_tower" or "cannon" => "bld_cannon_tower",
                "barracks" => "bld_barracks",
                "watchtower" => "bld_watchtower",
                "workshop" => "bld_workshop",
                "school" => "bld_school",
                "temple" => "bld_temple",
                "bank" or "money_shop" => "bld_bank",
                "factory_pre" or "factory_modern" or "factory" => "bld_factory",
                "fire_station" => "bld_fire_station",
                "police_station" => "bld_police_station",
                "hospital" => "bld_hospital",
                "park" => "bld_park",
                "airport" => "bld_airport",
                _ => "bld_hut"
            };
        }

        public static string ShipTypeToAgent(string shipTypeId)
        {
            return shipTypeId switch
            {
                "small_boat" or "fishing_boat" or "wooden" or "sail" => "ship_wooden",
                "steamer" => "ship_steamer",
                "cruise" or "passenger_liner" => "ship_cruise",
                "tanker" => "ship_tanker",
                "destroyer" => "ship_destroyer",
                "missile_ship" or "missile" => "ship_missile",
                "carrier" => "ship_carrier",
                "submarine" or "sub" => "ship_submarine",
                _ => "ship_wooden"
            };
        }

        public static string CartTypeToAgent(string cartTypeId)
        {
            return cartTypeId switch
            {
                "police" => "veh_police",
                "fire" or "firetruck" => "veh_firetruck",
                "ladder" => "veh_ladder",
                "aerial" => "veh_aerial",
                "school" => "veh_school",
                "truck" => "veh_truck",
                "ambulance" => "veh_ambulance",
                "garbage" => "veh_garbage",
                _ => "veh_car"
            };
        }

        public static string GroundTypeToAgent(string groundTypeId)
        {
            return groundTypeId switch
            {
                "tank" => "grd_tank",
                "apc" => "grd_apc",
                "missile_vehicle" => "grd_missile",
                "cavalry" => "grd_cavalry",
                "chariot" => "grd_chariot",
                "phalanx" => "grd_phalanx",
                _ => "grd_phalanx"
            };
        }

        public static string JobToAgent(string job)
        {
            return job switch
            {
                "farmer" => "job_farmer",
                "woodcutter" => "job_woodcutter",
                "miner" => "job_miner",
                "driver" => "job_driver",
                "sailor" => "job_sailor",
                "repairer" => "job_repairer",
                "police" => "job_police",
                "doctor" => "job_doctor",
                "firefighter" => "job_firefighter",
                "soldier" => "job_soldier",
                "official" => "job_official",
                "merchant" => "job_merchant",
                _ => "job_farmer"
            };
        }

        // 由实体（建筑/船/车/地面）取身份
        public AgentIdentity ForShip(ShipEntity s)
            => Registry.Get(ShipTypeToAgent(s != null ? s.ShipTypeId : ""));
        public AgentIdentity ForCart(CartEntity c)
            => Registry.Get(CartTypeToAgent(c != null ? c.CartTypeId : ""));

        // ================= 序列化包装（JsonUtility 需顶层类） =================
        [Serializable]
        private class AgentLibWrap { public AgentBehaviorLibrary Lib; }

        [Serializable]
        private class AgentLlmPayload
        {
            public string model;
            public List<AgentLlmMsg> messages;
            public int max_tokens;
            public float temperature;
        }
        [Serializable]
        private class AgentLlmMsg { public string role; public string content; }
        [Serializable]
        private class AgentLlmResponse
        {
            public List<AgentLlmChoice> choices;
        }
        [Serializable]
        private class AgentLlmChoice
        {
            public AgentLlmMsg message;
        }
    }
}
