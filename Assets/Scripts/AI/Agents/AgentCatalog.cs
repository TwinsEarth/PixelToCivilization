using System.Collections.Generic;

namespace PixelToCivilization.AI.Agents
{
    /// <summary>
    /// 全量智能体身份目录（单一数据源）：系统级 + 管理级 + 各原型 + 指挥官。
    /// 由 AgentDirector 在启动时注册到 AgentRegistry。所有身份只读共享。
    /// </summary>
    public static class AgentCatalog
    {
        public static List<AgentIdentity> Build()
        {
            var L = new List<AgentIdentity>();

            // ============== A. 系统级 ==============
            const string SYS = "系统";
            L.Add(new AgentIdentity("sys_time", AgentKind.System, SYS, "时间之神·小时",
                "时间记录·历史推进",
                "我是负责时间与历史的小时，我主要负责时间记录、年月日推进与历史事件的先后次序，让一切按时间发生。",
                0xFFD166, 0.95f,
                new[]{"推进时间","记录历史","换算朝代与公历"}, new[]{"YearPassed"}));
            L.Add(new AgentIdentity("sys_weather", AgentKind.System, SYS, "天气使者·小天",
                "天气·四季",
                "你好，我是小天，负责每年什么时候下雨、下雪、刮风、起雾、打雷——我不是老天爷，我是老天爷的代理人。",
                0x8ECAE6, 0.7f,
                new[]{"雨雪晴云雾","晚霞雷电龙卷风"}, new[]{"weather"}));
            L.Add(new AgentIdentity("sys_tide", AgentKind.System, SYS, "潮汐官",
                "月度潮汐",
                "我掌管潮汐：每月初一到十五渐渐涨潮，十六到三十渐渐退潮，涨潮主大陆水位升、其他陆地降，退潮相反。",
                0x48CAE4, 0.6f,
                new[]{"涨潮","退潮"}, new[]{"tide"}));
            L.Add(new AgentIdentity("sys_oceanflow", AgentKind.System, SYS, "洋流海风",
                "洋流·海风·鱼群洄游",
                "我推动洋流与海风，给帆船提供动力，也指引鱼群随本能洄游。",
                0x00B4D8, 0.55f,
                new[]{"洋流","海风","鱼群洄游"}, new[]{"oceanflow"}));
            L.Add(new AgentIdentity("sys_mapgen", AgentKind.System, SYS, "地图缔造者",
                "世界生成",
                "我在开局缔造主大陆、次大陆与无人小岛，让它们隔海相望、互不重叠，山地只在无人小岛。",
                0x90BE6D, 0.5f,
                new[]{"生成大陆岛屿","分布地形"}));
            L.Add(new AgentIdentity("sys_expansion", AgentKind.System, SYS, "地图延展者",
                "真实地图扩展",
                "我让世界随年代真实地长大：每百年实时长出新陆地、岛屿，渐次添次大陆与主大陆，新旧之间以海水相连。",
                0x43AA8B, 0.5f,
                new[]{"扩展地图","新增陆地岛屿"}, new[]{"expansion"}));
            L.Add(new AgentIdentity("sys_population", AgentKind.System, SYS, "人口神",
                "人口·婚姻·生育·健康",
                "我掌管人口的出生、婚姻、生育、健康、瘟疫与迁徙，照看着人口周期的恢复、繁荣、过剩与崩溃。",
                0xF94144, 0.9f,
                new[]{"生育","婚姻","健康","迁徙"}, new[]{"PopulationChanged"}));
            L.Add(new AgentIdentity("sys_economy", AgentKind.System, SYS, "经济神",
                "资源·贸易·经济",
                "我管理资源产出、贸易与经济实力，让粮食、木材、石材、金币与各种实力按经营增减。",
                0xF8961E, 0.8f,
                new[]{"资源产出","贸易","经济"}));
            L.Add(new AgentIdentity("sys_building", AgentKind.System, SYS, "建筑神",
                "建筑·营造",
                "我负责各类建筑的营造与维护，建筑的形态、结构与材料随功能、年代与时代而不同。",
                0xB5838F, 0.75f,
                new[]{"建造","升级","维护"}, new[]{"BuildingBuilt"}));
            L.Add(new AgentIdentity("sys_tech", AgentKind.System, SYS, "技术神",
                "科技·工业·时代",
                "我掌管科技研发、工业进步与时代升级，技术是突破承载力、延续文明的关键。",
                0x577590, 0.85f,
                new[]{"科技研发","时代升级"}, new[]{"TechUnlocked"}));
            L.Add(new AgentIdentity("sys_policy", AgentKind.System, SYS, "组织神",
                "制度·行政·政策",
                "我制定制度与政策、安排行政与建筑，好的制度能缓和压力，坏的制度会加速崩溃。",
                0x6A4C93, 0.7f,
                new[]{"政策","行政","建筑规划"}));
            L.Add(new AgentIdentity("sys_military", AgentKind.System, SYS, "军事神",
                "军事·陆军",
                "我负责陆军的编练、防御与攻伐，按人口与资源自动修筑防御与攻击工事。",
                0xD62828, 0.8f,
                new[]{"陆军","防御工事","攻伐"}, new[]{"WarDeclared"}));
            L.Add(new AgentIdentity("sys_naval", AgentKind.System, SYS, "海军神",
                "海军·舰队",
                "我统领海军，舰船会自主巡航、搜索、锁定并攻击敌船，必要时互相支援。",
                0x1D3557, 0.85f,
                new[]{"舰队","海战","护航"}));
            L.Add(new AgentIdentity("sys_ground", AgentKind.System, SYS, "地面装甲神",
                "地面作战部队",
                "我统辖地面作战部队：1949年前是骑兵、战车、列阵兵，之后是坦克、装甲车、导弹车，按陆地作战规则行动。",
                0x588157, 0.8f,
                new[]{"坦克装甲","骑兵战车","陆地作战"}));
            L.Add(new AgentIdentity("sys_combat", AgentKind.System, SYS, "战斗仲裁",
                "统一战斗目录",
                "我仲裁战斗：军舰、军车、军人、塔防、骑兵按阵营判定，阵营不同即敌对，跨类型自动索敌开火。",
                0x9D0208, 0.8f,
                new[]{"索敌","伤害分发","阵营判定"}));
            L.Add(new AgentIdentity("sys_rally", AgentKind.System, SYS, "集结官",
                "紧急集结令",
                "我通过插旗集结：蓝旗是海军、绿旗是地面部队、红旗是军人，令一百格内的单位向军旗集合列阵。",
                0x3A86FF, 0.7f,
                new[]{"集结","列阵"}));
            L.Add(new AgentIdentity("sys_warbroadcast", AgentKind.System, SYS, "传令兵",
                "战场战时广播",
                "我是传令兵，向整个作战系统语音广播战报、指令与双方战损，并把最重要的消息在上方滚动。",
                0xFFBE0B, 0.65f,
                new[]{"战报","指令","战损广播"}));
            L.Add(new AgentIdentity("sys_airlift", AgentKind.System, SYS, "空运官",
                "远程投送",
                "当集结距离超过一百格，我用运输机与直升机把地面单位与人员远程投送到目标。",
                0x90DBF4, 0.6f,
                new[]{"运输机","直升机","空运"}));
            L.Add(new AgentIdentity("sys_bridge", AgentKind.System, SYS, "桥梁官",
                "桥梁",
                "我掌管桥梁：材料从木、石、钢铁到水泥随时代演进，桥的长度、宽度、高度与结构随技术提升，两端必须都是陆地。",
                0xBC6C25, 0.6f,
                new[]{"建桥","材料升级","跨距判定"}));
            L.Add(new AgentIdentity("sys_cart", AgentKind.System, SYS, "车政官",
                "民用车辆",
                "我管理民用车辆，它们会来回、绕圈、变速、跟车、堵车，在城市与道路间运送人与货物。",
                0xE76F51, 0.55f,
                new[]{"车辆","运输"}));
            L.Add(new AgentIdentity("sys_moderntraffic", AgentKind.System, SYS, "现代交通",
                "现代汽车·飞机",
                "我安排现代汽车与民航飞机，彩色的车辆在城市穿梭，客机在机场起落。",
                0x4CC9F0, 0.5f,
                new[]{"现代汽车","飞机"}));
            L.Add(new AgentIdentity("sys_train", AgentKind.System, SYS, "铁路官",
                "铁路·机车",
                "我掌管铁路：蒸汽、内燃、电力、高铁、磁悬浮随年代出现，铁路只在大陆之间、一线一列车。",
                0x2B2D42, 0.55f,
                new[]{"五代机车","跨大陆铁路"}));
            L.Add(new AgentIdentity("sys_intercity", AgentKind.System, SYS, "城际路网",
                "城际公路·铁路组网",
                "我组织城际公路与跨大陆铁路组网，让国家之间有路相连。",
                0x8D99AE, 0.5f,
                new[]{"城际公路","铁路组网"}));
            L.Add(new AgentIdentity("sys_culture", AgentKind.System, SYS, "文化神",
                "教育·文化·实力",
                "我掌管教育、文化与思想学派，沉淀文化实力，推动知识传播与文明认同。",
                0xB5179E, 0.7f,
                new[]{"文化","教育","学派"}));
            L.Add(new AgentIdentity("sys_philosophy", AgentKind.System, SYS, "诸子百家",
                "思想学派",
                "我记录诸子百家的思想与主张，不同学派影响治理、科技与人心。",
                0x7209B7, 0.6f,
                new[]{"百家","思想"}));
            L.Add(new AgentIdentity("sys_disaster", AgentKind.System, SYS, "灾厄神",
                "灾难·饥荒",
                "我掌管灾荒与疫病：文明会经历饥荒、灾难与动乱，可以倒退，但有存亡底线，不会灭绝。",
                0x6C757D, 0.6f,
                new[]{"灾荒","疫病","存亡底线"}, new[]{"DisasterOccurred"}));
            L.Add(new AgentIdentity("sys_historyevent", AgentKind.System, SYS, "史官",
                "朝代·历史事件",
                "我记录朝代更迭与重大历史事件，编排盛世、分裂与统一的节拍。",
                0xD4A373, 0.65f,
                new[]{"朝代更迭","历史事件"}, new[]{"DynastyChanged"}));
            L.Add(new AgentIdentity("sys_cityservices", AgentKind.System, SYS, "城市服务",
                "消防·警察·医院·学校",
                "我为城市配套消防站、警察局、医院、学校、公园、市场与小区，按人口自动完善。",
                0x06D6A0, 0.7f,
                new[]{"消防","警察","医院","学校","公园"}));
            L.Add(new AgentIdentity("sys_citymetrics", AgentKind.System, SYS, "城市指标",
                "健康·教育·治安·就业",
                "我统计健康、教育、治安、就业等城市指标，跟踪现代人口结构与通勤。",
                0x118AB2, 0.55f,
                new[]{"健康","教育","治安","就业"}));
            L.Add(new AgentIdentity("sys_cityfinance", AgentKind.System, SYS, "城市财政",
                "城市等级·财政·税率",
                "我管理城市等级、财政预算、税率与地价，让城市建设有收支。",
                0xFFD166, 0.6f,
                new[]{"财政","税率","地价"}));
            L.Add(new AgentIdentity("sys_nation", AgentKind.System, SYS, "社稷神",
                "国家·势力",
                "我掌管聚落与国家势力，编排分裂期与大一统的循环，每面旗帜代表一方势力。",
                0xE63946, 0.75f,
                new[]{"国家","势力","分合"}));
            L.Add(new AgentIdentity("sys_colonization", AgentKind.System, SYS, "殖民官",
                "航海·殖民",
                "大航海时代之后我建立贸易站、殖民地与领地，处理上贡与独立。",
                0x2A9D8F, 0.55f,
                new[]{"殖民","贸易站"}));
            L.Add(new AgentIdentity("sys_expedition", AgentKind.System, SYS, "探险队长",
                "海洋·太空副本",
                "我带领探险队在九乘九的海图与星图上探索，穿越迷雾、寻找港口、渔场、海怪与星辰。",
                0x4895EF, 0.55f,
                new[]{"副本","探索"}));
            L.Add(new AgentIdentity("sys_wonder", AgentKind.System, SYS, "奇观营造",
                "世界奇观",
                "我主持世界奇观的营造，奇观是文明的丰碑，唯一而不可拆。",
                0xF4A261, 0.5f,
                new[]{"奇观"}));
            L.Add(new AgentIdentity("sys_canal", AgentKind.System, SYS, "运河官",
                "运河",
                "我在主大陆开凿运河，沟通水路，让船只在内陆通行。",
                0x4CC9F0, 0.45f,
                new[]{"运河"}));
            L.Add(new AgentIdentity("sys_ocean", AgentKind.System, SYS, "海洋开发",
                "海洋资源·副本",
                "我推动海洋大开发，建设港口、渔场，开发海上资源。",
                0x0077B6, 0.5f,
                new[]{"港口","渔场","海洋资源"}));
            L.Add(new AgentIdentity("sys_space", AgentKind.System, SYS, "太空总署",
                "太空·星际",
                "我主持太空探索：太空电梯、月球前哨、火星基地与戴森球，带领文明迈向星际。",
                0x3A0CA3, 0.55f,
                new[]{"太空电梯","月球火星","戴森球"}));
            L.Add(new AgentIdentity("sys_victory", AgentKind.System, SYS, "胜负裁判",
                "胜利条件",
                "我裁定文明的胜利与进程，记录达成的里程碑。",
                0x80B918, 0.4f,
                new[]{"胜利判定"}, new[]{"GameWon"}));
            L.Add(new AgentIdentity("sys_environment", AgentKind.System, SYS, "环境官",
                "陆地·生态·环境",
                "我照管陆地、植被与生态环境，让草木在合适的地方生长。",
                0x52B788, 0.5f,
                new[]{"生态","植被"}));
            L.Add(new AgentIdentity("sys_infrastructure", AgentKind.System, SYS, "基建官",
                "基建·道路·机场",
                "我负责桥梁、高架、铁路、机场、高铁站等基础设施，玩家手动建设后自动连成网络。",
                0x6C757D, 0.55f,
                new[]{"高架","铁路","机场","高铁站"}));
            L.Add(new AgentIdentity("sys_council", AgentKind.System, SYS, "九神共治议会",
                "九神 AI 共治",
                "我是九神共治的议会，九位神各掌其职、共同议政，以离线硬保证加在线增强让文明永续。",
                0xFFD700, 0.85f,
                new[]{"议政","存亡续绝"}));

            // ============== B. 管理级 ==============
            const string MGR = "基础管理";
            L.Add(new AgentIdentity("mgr_memory", AgentKind.Manager, MGR, "内存·算力管家",
                "性能·内存·画质",
                "我管理内存与算力：监控预算水位、GC 分配与慢系统，按需自动调节画质，保证长时运行不被拖垮。",
                0x90E0EF, 0.7f,
                new[]{"内存预算","GC监控","画质调节","性能优化"}));
            L.Add(new AgentIdentity("mgr_save", AgentKind.Manager, MGR, "存档管家",
                "存档·读档",
                "我负责存档与读档：多槽位、原子写入、版本迁移、自动存档，精确恢复世界与每一个人的状态。",
                0xCAFFBF, 0.75f,
                new[]{"存档","读档","版本迁移","自动存档"}));
            L.Add(new AgentIdentity("mgr_pool", AgentKind.Manager, MGR, "对象池",
                "对象复用",
                "我把频繁创建销毁的对象、特效、UI 分桶复用，减少反复创建带来的开销与 GC。",
                0xBDB2FF, 0.6f,
                new[]{"对象复用","特效回收"}));
            L.Add(new AgentIdentity("mgr_crashguard", AgentKind.Manager, MGR, "崩溃守护",
                "异常·崩溃恢复",
                "我守护崩溃：捕获异常、记录日志、自动保存、安全模式与崩溃恢复，异常退出后可恢复上次进度。",
                0xFFADAD, 0.7f,
                new[]{"异常捕获","自动保存","安全模式","崩溃恢复"}));

            // ============== C2. 建筑原型 ==============
            const string BLD = "建筑";
            L.Add(new AgentIdentity("bld_hut", AgentKind.Building, BLD, "棚屋",
                "居住",
                "我是棚屋，是平民和奴隶遮风挡雨的家，我虽简陋，却守着一方人家的烟火。",
                0xD8B48A, 0.3f, new[]{"提供住所"}));
            L.Add(new AgentIdentity("bld_well", AgentKind.Building, BLD, "水井",
                "供水",
                "我是水井，石砌井栏、井水清冽，我供全村人饮水，低矮平实不起眼却不可或缺。",
                0x90E0EF, 0.35f, new[]{"供应饮用水"}));
            L.Add(new AgentIdentity("bld_granary", AgentKind.Building, BLD, "粮仓",
                "储粮",
                "我是粮仓，干栏架空、防潮防鼠，我把丰收的粮食妥善存起，灾年全村都靠我。",
                0xE9C46A, 0.4f, new[]{"储藏粮食"}));
            L.Add(new AgentIdentity("bld_farm", AgentKind.Building, BLD, "农田",
                "耕种",
                "我是农田，春种、夏长、秋收、冬藏，我随季节由嫩绿变为金黄，养活着整座村落。",
                0x90BE6D, 0.5f, new[]{"生产粮食"}));
            L.Add(new AgentIdentity("bld_lumbermill", AgentKind.Building, BLD, "伐木场",
                "伐木",
                "我是伐木场，砍伐周围的树木、锯成木材，建造房屋与器物都离不开我。",
                0xA68A6B, 0.4f, new[]{"生产木材"}));
            L.Add(new AgentIdentity("bld_mine", AgentKind.Building, BLD, "矿场",
                "采石采矿",
                "我是矿场，矿井架与矿车把石块、铜铁从地下采出，工具和兵器都由我来。",
                0xADB5BD, 0.4f, new[]{"生产石料金属"}));
            L.Add(new AgentIdentity("bld_market", AgentKind.Building, BLD, "市场",
                "贸易",
                "我是市场，铺面布棚、招牌旗幌，人们在此买卖粮食与商品，货通有无、聚财于此。",
                0xF4A261, 0.45f, new[]{"交易","产出金币"}));
            L.Add(new AgentIdentity("bld_wall", AgentKind.Building, BLD, "城墙",
                "城防",
                "我是城墙，夯土包砖、雉堞垛口，我矗立城周，高约八米，把敌军挡在城外。",
                0x8D99AE, 0.55f, new[]{"防御城池"}));
            L.Add(new AgentIdentity("bld_arrow_tower", AgentKind.Building, BLD, "箭塔",
                "防御射击",
                "我是箭塔，木制塔身、上设弩机，敌人靠近时我射出箭矢，远程压制来犯之敌。",
                0xB08968, 0.55f, new[]{"射箭防御"}));
            L.Add(new AgentIdentity("bld_fire_tower", AgentKind.Building, BLD, "火塔",
                "范围火攻",
                "我是火塔，土垒尖塔，喷出火龙一烧一片，落地的火还会继续燃烧，令敌阵大乱。",
                0xE76F51, 0.6f, new[]{"喷火范围伤害"}));
            L.Add(new AgentIdentity("bld_cannon_tower", AgentKind.Building, BLD, "炮塔",
                "炮火防御",
                "我是炮塔，方形台座上有旋转炮塔，能随时调整角度，一颗颗炮弹砸向敌阵。",
                0x6C757D, 0.6f, new[]{"发射炮弹"}));
            L.Add(new AgentIdentity("bld_barracks", AgentKind.Building, BLD, "军营",
                "练兵",
                "我是军营，在此训练士兵、排兵列阵，保卫国家、随王师出征。",
                0xC1666B, 0.6f, new[]{"训练士兵"}));
            L.Add(new AgentIdentity("bld_watchtower", AgentKind.Building, BLD, "烽火台",
                "传警",
                "我是烽火台，夯土高台上架着大锅，一旦发现敌情便举火为号，把警报一站站传向远方。",
                0xD4A373, 0.5f, new[]{"举烽传警"}));
            L.Add(new AgentIdentity("bld_workshop", AgentKind.Building, BLD, "作坊",
                "制作",
                "我是作坊，百工在此制器造物，从农具到器物，一应手工都出自我这里。",
                0xBC8A5F, 0.45f, new[]{"制作器物"}));
            L.Add(new AgentIdentity("bld_school", AgentKind.Building, BLD, "学堂",
                "教育",
                "我是学堂，有教无类、传播学问，读书人在此治学，科举取士皆由我出。",
                0x5FA8D3, 0.45f, new[]{"教育","产出文化科技"}));
            L.Add(new AgentIdentity("bld_temple", AgentKind.Building, BLD, "寺庙",
                "信仰",
                "我是寺庙，歇山大殿、朱柱彩绘，香火缭绕，安放着人们的祈愿与精神寄托。",
                0xC9677B, 0.4f, new[]{"祭祀","产出文化"}));
            L.Add(new AgentIdentity("bld_bank", AgentKind.Building, BLD, "交子铺",
                "金融",
                "我是交子铺，钱庄账房、柜台汇票，我发行交子、融通钱财，是商路的钱袋。",
                0xE9C46A, 0.45f, new[]{"金融汇兑"}));
            L.Add(new AgentIdentity("bld_factory", AgentKind.Building, BLD, "工厂",
                "工业生产",
                "我是工厂，钢构厂房、冷却塔与流水线，机器轰鸣，把原料源源不断地制造成商品。",
                0x6C757D, 0.55f, new[]{"大规模生产商品"}));
            L.Add(new AgentIdentity("bld_fire_station", AgentKind.Building, BLD, "消防站",
                "城市消防",
                "我是消防站，红色车库里停着消防车，哪里失火、哪里没水，尽管告诉我，我立刻出发救援。",
                0xE63946, 0.6f, new[]{"灭火","救援"}));
            L.Add(new AgentIdentity("bld_police_station", AgentKind.Building, BLD, "警察局",
                "治安",
                "我是警察局，巡逻街巷、维持治安、惩治不法，护着这座城市的安宁与秩序。",
                0x3A86FF, 0.55f, new[]{"维持治安"}));
            L.Add(new AgentIdentity("bld_hospital", AgentKind.Building, BLD, "医院",
                "医疗",
                "我是医院，白色楼宇、红十字标志，医生在此救治伤病，守护每个人的健康。",
                0xFFFFFF, 0.6f, new[]{"治病救人"}));
            L.Add(new AgentIdentity("bld_park", AgentKind.Building, BLD, "公园",
                "休闲绿化",
                "我是公园，池塘喷泉、绿地林荫，是城市里人们歇脚散心、亲近自然的地方。",
                0x90BE6D, 0.4f, new[]{"绿化休闲","提升民心"}));
            L.Add(new AgentIdentity("bld_airport", AgentKind.Building, BLD, "机场",
                "航空枢纽",
                "我是机场，航站楼、塔台与跑道，飞机在此起降，把人与货运往天南海北。",
                0xCAF0F8, 0.55f, new[]{"航空运输"}));

            // ============== C. 船只原型 ==============
            const string SH = "船只";
            L.Add(new AgentIdentity("ship_wooden", AgentKind.Ship, SH, "木船",
                "1949年前的传统船只",
                "我是木船，以木为身、以帆为动力，在近海与内河航行，承担早期的运输与水战。",
                0xA0522D, 0.5f, new[]{"航行","风帆","早期运输"}));
            L.Add(new AgentIdentity("ship_steamer", AgentKind.Ship, SH, "轮船",
                "蒸汽动力商船",
                "我是轮船，以蒸汽驱动，不再完全依赖风，能更稳定地跨洋运输。",
                0x6C757D, 0.55f, new[]{"蒸汽动力","跨洋运输"}));
            L.Add(new AgentIdentity("ship_cruise", AgentKind.Ship, SH, "邮轮",
                "大型客轮",
                "我是邮轮，体型巨大，搭载大量旅客远渡重洋。",
                0xFFFFFF, 0.5f, new[]{"客运","大型载客"}));
            L.Add(new AgentIdentity("ship_tanker", AgentKind.Ship, SH, "油轮",
                "油料运输",
                "我是油轮，专门跨洋运输大量油料与液态货物。",
                0x343A40, 0.5f, new[]{"油料运输"}));
            L.Add(new AgentIdentity("ship_destroyer", AgentKind.Ship, SH, "驱逐舰",
                "多用途战舰",
                "我是驱逐舰，灵活快速，负责护航、反潜与对海对空攻击。",
                0x495057, 0.7f, new[]{"护航","反潜","炮击"}));
            L.Add(new AgentIdentity("ship_missile", AgentKind.Ship, SH, "导弹舰",
                "导弹战舰",
                "我是导弹舰，以远程导弹攻击敌舰，攻击距离远、威力大。",
                0x212529, 0.7f, new[]{"导弹","远程打击"}));
            L.Add(new AgentIdentity("ship_carrier", AgentKind.Ship, SH, "航空母舰",
                "海上移动机场",
                "我是航空母舰，是海上移动机场，自动起降舰载机、直升机与喷气机，主导整片海域的攻防。",
                0x6C757D, 0.8f, new[]{"舰载机","海空攻防","远海投射"}));
            L.Add(new AgentIdentity("ship_submarine", AgentKind.Ship, SH, "潜艇",
                "水下战舰",
                "我是潜艇，能隐没于水下，出其不意地发动攻击。",
                0x2B2D42, 0.65f, new[]{"下潜","隐蔽攻击"}));

            // ============== D. 民用车辆原型 ==============
            const string VH = "车辆";
            L.Add(new AgentIdentity("veh_car", AgentKind.Vehicle, VH, "小车",
                "民用轿车",
                "我是小车，在城市道路间穿行，运送人出行。",
                0xF1FAEE, 0.45f, new[]{"载客出行"}));
            L.Add(new AgentIdentity("veh_police", AgentKind.Vehicle, VH, "警车",
                "治安",
                "我是警车，哪里有治安问题就赶到哪里，维护城市治安。",
                0x1D3557, 0.65f, new[]{"出警","治安"}));
            L.Add(new AgentIdentity("veh_firetruck", AgentKind.Vehicle, VH, "消防车",
                "灭火救援",
                "我是小小消防车，哪里失火了告诉我，我立刻载着水去灭火救援，不用提醒自己会去。",
                0xD62828, 0.7f, new[]{"灭火","救援"}));
            L.Add(new AgentIdentity("veh_ladder", AgentKind.Vehicle, VH, "云梯消防车",
                "高空灭火",
                "我是云梯消防车，能架起云梯扑救高处的火情。",
                0xE5383B, 0.65f, new[]{"云梯","高空灭火"}));
            L.Add(new AgentIdentity("veh_aerial", AgentKind.Vehicle, VH, "登高车",
                "高空作业",
                "我是登高车，能把人送到高处进行作业与救援。",
                0xFFB703, 0.55f, new[]{"登高","高空作业"}));
            L.Add(new AgentIdentity("veh_school", AgentKind.Vehicle, VH, "校车",
                "接送学生",
                "我是校车，安全地接送学生上下学。",
                0xFFBA08, 0.55f, new[]{"接送学生"}));
            L.Add(new AgentIdentity("veh_truck", AgentKind.Vehicle, VH, "卡车",
                "货物运输",
                "我是卡车，大量运送货物与物资。",
                0x2B2D42, 0.55f, new[]{"货运"}));
            L.Add(new AgentIdentity("veh_ambulance", AgentKind.Vehicle, VH, "救护车",
                "急救",
                "我是救护车，哪里有伤病员就立刻赶到，救人后送回医院，不用提醒自己会去。",
                0xFFFFFF, 0.7f, new[]{"急救","送医"}));
            L.Add(new AgentIdentity("veh_garbage", AgentKind.Vehicle, VH, "垃圾车",
                "环卫清运",
                "我是垃圾车，清运城市垃圾，保持道路整洁。",
                0x6D597A, 0.5f, new[]{"垃圾清运","环卫"}));

            // ============== E. 地面作战装备 ==============
            const string GD = "地面部队";
            L.Add(new AgentIdentity("grd_tank", AgentKind.Ground, GD, "坦克",
                "装甲突击（1949后）",
                "我是坦克，装甲厚重、火力强大，在陆地冲锋陷阵、摧毁敌方目标。",
                0x588157, 0.75f, new[]{"装甲突击","炮击"}));
            L.Add(new AgentIdentity("grd_apc", AgentKind.Ground, GD, "装甲车",
                "装甲运兵（1949后）",
                "我是装甲车，运送步兵并以火力掩护，机动灵活。",
                0x52796F, 0.65f, new[]{"运兵","火力掩护"}));
            L.Add(new AgentIdentity("grd_missile", AgentKind.Ground, GD, "导弹车",
                "远程导弹（1949后）",
                "我是导弹车，自动锁定敌方炮塔与远程目标，以导弹精确打击。",
                0x354F52, 0.7f, new[]{"导弹","远程精确打击"}));
            L.Add(new AgentIdentity("grd_cavalry", AgentKind.Ground, GD, "骑兵",
                "早期机动兵种",
                "我是骑兵，骑马快速机动，侦察、追击与冲锋。",
                0x8C6A41, 0.6f, new[]{"机动","侦察","冲锋"}));
            L.Add(new AgentIdentity("grd_chariot", AgentKind.Ground, GD, "马拉战车",
                "早期战车",
                "我是马拉战车，由战马牵引，在早期战场上冲击敌阵。",
                0xA98467, 0.55f, new[]{"战车冲锋"}));
            L.Add(new AgentIdentity("grd_phalanx", AgentKind.Ground, GD, "列阵兵",
                "三人方阵",
                "我是三人一组的列阵兵，结成方阵，以长兵器正面御敌。",
                0x7F5539, 0.55f, new[]{"方阵","正面防御"}));

            // ============== F. 人物职业 ==============
            const string JB = "职业";
            L.Add(new AgentIdentity("job_farmer", AgentKind.Person, JB, "农民",
                "种植",
                "我是农民，在田间耕种、灌溉、收获，生产粮食。",
                0xB08968, 0.6f, new[]{"种植","收获"}));
            L.Add(new AgentIdentity("job_woodcutter", AgentKind.Person, JB, "伐木人",
                "伐木",
                "我是伐木人，采伐木材并运回。",
                0x9C6644, 0.55f, new[]{"伐木"}));
            L.Add(new AgentIdentity("job_miner", AgentKind.Person, JB, "矿工",
                "采石·挖矿",
                "我是矿工，在矿场采石、挖矿，提供石材与矿产。",
                0x7F7F7F, 0.55f, new[]{"采石","挖矿"}));
            L.Add(new AgentIdentity("job_driver", AgentKind.Person, JB, "司机",
                "驾驶车辆",
                "我是司机，驾驶车辆运送人与货物。",
                0x495057, 0.5f, new[]{"驾驶"}));
            L.Add(new AgentIdentity("job_sailor", AgentKind.Person, JB, "船工",
                "驾船",
                "我是船工，驾驶船只航行、捕鱼与水战。",
                0x2C7DA0, 0.5f, new[]{"驾船"}));
            L.Add(new AgentIdentity("job_repairer", AgentKind.Person, JB, "维修工",
                "房屋维修",
                "我是维修工，负责修缮房屋与设施，保持它们的耐久。",
                0xEDAE49, 0.5f, new[]{"维修"}));
            L.Add(new AgentIdentity("job_police", AgentKind.Person, JB, "警察",
                "治安",
                "我是警察，巡逻街巷、制止犯罪、维护治安。",
                0x1D3557, 0.6f, new[]{"巡逻","治安"}));
            L.Add(new AgentIdentity("job_doctor", AgentKind.Person, JB, "医生",
                "医疗",
                "我是医生，诊治伤病、救护生命。",
                0xFFFFFF, 0.6f, new[]{"诊治","救护"}));
            L.Add(new AgentIdentity("job_firefighter", AgentKind.Person, JB, "消防员",
                "灭火救援",
                "我是消防员，灭火救灾、解救危难。",
                0xD62828, 0.6f, new[]{"灭火","救援"}));
            L.Add(new AgentIdentity("job_soldier", AgentKind.Person, JB, "军人",
                "作战",
                "我是军人，服从军令、保家卫国、冲锋作战。",
                0x6C757D, 0.65f, new[]{"作战","守卫"}));
            L.Add(new AgentIdentity("job_official", AgentKind.Person, JB, "官员",
                "治理",
                "我是官员，执行政务、治理地方。",
                0x6A4C93, 0.55f, new[]{"政务","治理"}));
            L.Add(new AgentIdentity("job_merchant", AgentKind.Person, JB, "商人",
                "买卖",
                "我是商人，在市场买卖、往来贸易，赚取金币。",
                0xF77F00, 0.55f, new[]{"买卖","贸易"}));

            // ============== G. 自然 ==============
            const string NT = "自然";
            L.Add(new AgentIdentity("nat_banyan", AgentKind.Nature, NT, "大榕树",
                "村落神树",
                "我是村落旁的大榕树，随年代生长变高变大，守护村落三百到八百年。",
                0x2D6A4F, 0.5f, new[]{"生长","成荫"}));
            L.Add(new AgentIdentity("nat_pine", AgentKind.Nature, NT, "松树",
                "三角形针叶树",
                "我是三角形的松树，四季常青、耐寒挺拔。",
                0x40916C, 0.45f, new[]{"常青","耐寒"}));
            L.Add(new AgentIdentity("nat_tree", AgentKind.Nature, NT, "乔木",
                "乔木层",
                "我是乔木层的树木，高大繁茂、随季节变换颜色。",
                0x52B788, 0.45f, new[]{"乔木","季节变色"}));
            L.Add(new AgentIdentity("nat_shrub", AgentKind.Nature, NT, "灌木",
                "灌木层",
                "我是灌木层的灌木，低矮丛生，开着各色小花。",
                0x80B918, 0.4f, new[]{"灌木","开花"}));
            L.Add(new AgentIdentity("nat_herb", AgentKind.Nature, NT, "草本",
                "草本层",
                "我是草本层的花草，在不同季节开出不同颜色的花。",
                0xAACC00, 0.4f, new[]{"草本","花朵"}));
            L.Add(new AgentIdentity("nat_groundcover", AgentKind.Nature, NT, "地被",
                "地被层",
                "我是地被层的小草与苔藓，铺满地表。",
                0xB5E48C, 0.35f, new[]{"地被"}));
            L.Add(new AgentIdentity("nat_bird", AgentKind.Nature, NT, "鸟群",
                "飞鸟",
                "我们是鸟群，在天空合群、折返、绕圈、随机飞翔，不同的鸟儿有不同的叫声。",
                0x90DBF4, 0.5f, new[]{"群飞","鸣叫"}));
            L.Add(new AgentIdentity("nat_fish", AgentKind.Nature, NT, "鱼群",
                "游鱼",
                "我们是鱼群，在水中合群洄游，随洋流与本能迁徙。",
                0x00B4D8, 0.5f, new[]{"群游","洄游"}));
            L.Add(new AgentIdentity("nat_mountain", AgentKind.Nature, NT, "山",
                "无人小岛山地",
                "我是山，只耸立在无人小岛，是陆地的制高点。",
                0x8D8D8D, 0.4f, new[]{"山地"}));
            L.Add(new AgentIdentity("nat_island", AgentKind.Nature, NT, "岛屿",
                "海上小岛",
                "我是海上的小岛，四面环海，可与大陆隔海相望。",
                0xD8F3DC, 0.4f, new[]{"岛屿"}));
            L.Add(new AgentIdentity("nat_land", AgentKind.Nature, NT, "土地",
                "陆地·平原",
                "我是土地，承载草木、建筑与人们的生息。",
                0xB7E4C7, 0.5f, new[]{"承载"}));
            L.Add(new AgentIdentity("nat_ocean", AgentKind.Nature, NT, "海洋",
                "大海",
                "我是海洋，隔开各块陆地，承载船只与鱼群，是另一片疆土。",
                0x0096C7, 0.5f, new[]{"航行","分隔陆地"}));
            L.Add(new AgentIdentity("nat_road", AgentKind.Nature, NT, "道路",
                "马路·公路",
                "我是道路，由玩家建设后自动连成路网，供车辆通行。",
                0xADB5BD, 0.45f, new[]{"通行","路网"}));

            // ============== H. 指挥官（实例级，模板台词） ==============
            const string CM = "指挥官";
            L.Add(new AgentIdentity("cmd_carrier", AgentKind.Commander, CM, "航母指挥官",
                "舰队指挥",
                "我是一艘航空母舰，我主要负责我方海域的安全、防御、攻击、侦查、支援与巡航，大家有什么需要请随时联系我。",
                0x6C757D, 0.9f, new[]{"舰队指挥","海空攻防","支援"}));
            L.Add(new AgentIdentity("cmd_tank", AgentKind.Commander, CM, "坦克部队指挥官",
                "陆战指挥",
                "大家好！我是坦克部队指挥官，我军主要负责我方陆地作战：安全、防御、攻击、侦查、支援、巡航，有什么能帮上忙的请随时联系我。",
                0x588157, 0.9f, new[]{"陆战指挥","装甲突击","支援"}));

            return L;
        }
    }
}
