using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.World;
using PixelToCivilization.Data;
using PixelToCivilization.Rendering;

namespace PixelToCivilization.Systems
{
    /// <summary>船型静态定义（对齐 v5.9.9 SHIP_DEFS）</summary>
    public class ShipDef
    {
        public string Id, Name, Icon, AttackType;
        public int Capacity, Housing, Durability, Defense, Era;
        public float Speed, Attack, Range;
        public float Radar;   // V9.4.3 雷达范围（格）：分船型（侦察/驱逐/航母远，小艇近），随等级+15%/级
        public Dictionary<string,int> Cost;
        public bool Military;
        public long ColorHex;
        public int SizeCls;   // V9.3.3 规模档 0小/1中/2大/3巨/4航母级（现代船更大更长）
    }

    /// <summary>
    /// 水军海战系统 —— 对齐 v5.9.9：8种船型、船员加成、建造宝船、敌方舰队、战船交火、随时代升级。
    /// </summary>
    public class NavalSystem : GameSystemBase
    {
        public readonly Dictionary<string,ShipDef> Defs = new();
        public List<ShipEntity> EnemyShips = new();
        private float _spawnCd;
        private bool _pirateEngaged;   // V6.1.5 本轮是否有敌舰/海盗，肃清后发护航赏金
        private Transform _root;
        private WorldGenerator _terrain;

        // V9.2.3 军舰自主巡逻（运行时态、不进存档）：无敌舰时沿外海航点环航
        class PatrolRoute { public readonly List<Vector2> Pts = new(); public int Idx; public bool Inited; }
        readonly Dictionary<ShipEntity,PatrolRoute> _patrol = new();
        // V9.3.5 主动载人（运行时态）：载人态船的目标闲人航点（0.5s 节流刷新；沉舰/退役同步清理）
        readonly Dictionary<ShipEntity,Vector2> _loadGoal = new();
        private float _loadCd;
        // V9.3.8 靠岸等待（运行时态）：载人态船到点停泊 _dockWait 秒，期间不动，等 EmbarkSystem 吸附岸边人员
        readonly Dictionary<ShipEntity,float> _dockWait = new();
        // V9.3.8 自动编队巡航（运行时态）：满员军用船无敌舰时分桶 3-7 艘成队，领队巡航、成员跟随；三模式每 10 现实分钟切换
        // V9.3.9 编队阵型：随模式同步轮换 5 种阵型（0倒V / 1V / 2纵列 / 3横排 / 4半圆包围），成员按领队航向相对排布
        class CruiseFormation { public readonly List<ShipEntity> Members=new(); public int Mode; public int Formation; public float LeadYaw; public readonly List<Vector2> Waypoints=new(); public int WpIdx; public bool Inited; }
        readonly Dictionary<ShipEntity,CruiseFormation> _formation = new();
        private static readonly string[] FormationNames={"倒V","V字","纵列","横排","半圆"};
        private float _cruiseSwitchCd;
        private float _reformTimer;   // V9.4.3 战斗/巡航统一低频重组编队（20s），保"持续编队+编队追击"
        private float _sepTimer;      // V9.4.6 船体积碰撞分离节流（4Hz）
        private const float CruiseSwitchInterval=600f;   // 现实 10 分钟切换巡航模式（unscaled，不受倍速影响）
        private static readonly Vector2[] FollowOffsets={
            new(0f,0f),new(2.5f,0f),new(-2.5f,0f),new(0f,2.5f),new(0f,-2.5f),new(5f,3f),new(-5f,3f)};
        private static readonly string[] CruiseModeNames={"绕大陆","岛间巡逻","随机坐标"};
        // V9.3.9 编队阵型偏移（成员编号 mi、阵型 Formation；间距 8 世界单位；前进方向为 +Z、右舷 +X，运行时按领队航向旋转）
        private static Vector2 FormationOffset(int mi,CruiseFormation f)
        {
            int n=f.Members.Count; if(n<2) return Vector2.zero;
            const float S=8f;
            int half=mi/2; int side=(mi%2==0)?1:-1;
            switch(f.Formation)
            {
                case 0: return new Vector2(side*(S*0.5f+half*S), -S*(1+half));              // 倒V：两翼向后展开
                case 1: return new Vector2(side*(S*0.5f+half*S), S*(1+half));               // V字：两翼向前张开
                case 2: return new Vector2(0f, -S*mi);                                      // 纵列：1字
                case 3: return new Vector2(side*(S*0.5f+half*S), 0f);                       // 横排：一字
                default: { float ang=Mathf.Deg2Rad*(150f-120f*mi/(n-1)); float r=10f+6f*half; // 半圆包围：领队后方扇形
                           return new Vector2(Mathf.Cos(ang)*r, Mathf.Sin(ang)*r*0.6f); }
            }
        }

        // ===== V9.3.3 海上帝阵营（3-5 个敌对阵营，敌舰按阵营着色/命名；运行时随机启用）=====
        static readonly (string name,long color)[] FactionDefs =
        {
            ("红海海盗",0xFF6347L),("黑旗帮",0x4169E1L),("南蛮水师",0x32CD32L),("北洋余部",0x9370DBL),("联合舰队",0xFFD700L)
        };
        readonly List<(string name,long color)> _enemyFactions = new();
        bool _factionsInited;

        // ===== V9.4.1 航母舰载机实体（自动发射/追击/返航，运行时态不进存档，随等级成长）=====
        // 等级表：Lv1 单机伤害60 同时2架 攻击半径8格 冷却3s；Lv2 99 / 3架 / 12格 / 2.5s；Lv3 144 / 4架 / 16格 / 2s
        // 挂载比例：战斗机:直升机:喷气机 = 5:2:1（Lv3 才解锁喷气机；挂载总数 8/14/22 架）
        class CarrierStrike
        {
            public GameObject View; public int Kind;           // 0战斗机 1直升机 2喷气机
            public ShipEntity Carrier, Target;
            public float T; public int Phase;                  // 0起飞 1巡航 2俯冲 3返航 4完成
            public float H0;                                   // 起飞高度基准
        }
        readonly List<CarrierStrike> _strikes = new();
        private float _carrierCd;                              // 弹射节流（等级冷却，scaled dt）

        // 航母出击数量/半径/冷却（按等级）
        static int CarrierAirborneCount(int lvl)   => lvl>=3?4 : lvl>=2?3 : 2;
        static float CarrierStrikeRange(int lvl)   => lvl>=3?16f : lvl>=2?12f : 8f;   // 格
        static float CarrierStrikeCd(int lvl)      => lvl>=3?2f : lvl>=2?2.5f : 3f;   // 秒
        static int CarrierHitDamage(int lvl, int baseAtk) => Mathf.RoundToInt(baseAtk * (lvl>=3?0.6f : lvl>=2?0.55f : 0.5f));

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _root=EntityViewFactory.EnsureRoot("Navy",gm.transform);
            _terrain=Object.FindObjectOfType<WorldGenerator>();
            LoadDefs();
        }
        // V6.3.4：船只只能在水面（无地形数据时不阻拦，避免副本/异常空引用）
        private bool OnWater(float x,float z)=> _terrain==null || (_terrain.IsOceanWater(x,z) && _terrain.InsideFrontier(x,z)); // V6.8.3 船只只准在外海（排除内河+淡水湖）
        // V6.8.3 船只永留外海（每帧每船调用一次）：
        //  · 在外海：不干预；
        //  · 退潮露出的"外海潮滩"（基准水位下本是海水、当前临时干涸、群系仍为外海 Default）：原地坐滩，随涨潮自动复浮，绝不水平拖行；
        //  · 一旦出现在内河/淡水湖/陆地上（退潮被河道引入、地图扩展、强风、旧存档等任何成因）：螺旋搜索最近外海并一次性归位。
        private void KeepAtSea(ShipEntity s)
        {
            if(_terrain==null) return;
            if(_terrain.IsOceanWater(s.X,s.Z)) return;
            bool dry=!_terrain.IsWater(s.X,s.Z);
            bool seaBiome=_terrain.BiomeAt(s.X,s.Z)==BiomeKind.Default;
            bool wouldBeSea=_terrain.HeightAt(s.X,s.Z)<GameConstants.WaterLevel;
            if(dry && seaBiome && wouldBeSea) return; // 外海潮滩：坐滩等涨潮，不做水平移动
            float tile=GameConstants.Tile,bx=s.X,bz=s.Z,best=float.MaxValue; bool found=false;
            for(int ring=1;ring<=200 && !found;ring++)
            {
                float r=ring*tile;
                for(int a=0;a<24;a++)
                {
                    float ang=a/24f*Mathf.PI*2f;
                    float cx=s.X+Mathf.Cos(ang)*r, cz=s.Z+Mathf.Sin(ang)*r;
                    // V7.0.6 归位点必须是"当前确有海水"的外海连通格（掩码=1 且当下是水、在疆域内），
                    // 河湖掩码为 0 被排除，退潮干滩也不会被选为归位点——杜绝把船拖进大陆湖或搁在滩上。
                    if(!_terrain.IsOceanWater(cx,cz)||!_terrain.IsWater(cx,cz)||!_terrain.InsideFrontier(cx,cz)) continue;
                    if(r<best){best=r;bx=cx;bz=cz;found=true;}
                }
            }
            if(found){ s.X=bx; s.Z=bz; } // 一次性归位最近外海（自愈，杜绝滞留内河/湖泊）
        }

        /// <summary>船的贴合高度：在水里随潮位起伏；坐滩时托在滩面与水面的较高者，不悬空、不陷地、不被拖走。</summary>
        private float ShipRestY(ShipEntity s)
        {
            float surf=GM.Tide!=null?GM.Tide.SurfaceY(s.X,s.Z):GameConstants.WaterLevel;
            if(_terrain!=null && !_terrain.IsOceanWater(s.X,s.Z))
            {
                float ground=_terrain.HeightAt(s.X,s.Z);
                return Mathf.Max(surf,ground)+0.12f; // 退潮露出：稳坐滩面，潮涨水面没过即复浮
            }
            return surf+0.08f+Mathf.Sin(Time.time*1.6f+s.HomeX+s.HomeZ)*0.13f;
        }

        private GameObject ShipView(ShipEntity s, long colorHex, float scale)
        {
            // V6.1.1：方块替换为程序化舰船（民用帆船 / 军用战舰）；V9.3.3 敌舰按阵营着色、我方保持船型本色
            if (s.FactionColor!=0) colorHex=s.FactionColor;
            var kind=s.Military?PixelToCivilization.Actors.VehicleKind.Warship:PixelToCivilization.Actors.VehicleKind.SailBoat;
            float lvlScale=1f+(s.Level-1)*0.1f;   // V9.3.3 等级放大：普通1.0/精良1.1/传奇1.2
            var v=EntityViewFactory.SpawnVehicle("Ship_"+s.Name,_root,kind,EntityViewFactory.Hex(colorHex),scale*0.85f*lvlScale,s.ShipTypeId);
            v.transform.position=new Vector3(s.X,0.1f,s.Z);
            // V6.1.1 船只可点击：载具工厂默认移除了碰撞体，这里在根节点补一个包围盒 + 点击桥（对齐建筑 BuildingClick）
            var col=v.AddComponent<BoxCollider>();
            bool huge=(s.ShipTypeId=="treasure_ship"||s.ShipTypeId=="treasure_warship"||s.ShipTypeId=="aircraft_carrier"||s.ShipTypeId=="cruise_liner");
            col.size=huge?new Vector3(9f,4.5f,8f):(s.Military?new Vector3(7f,3.2f,4.2f):new Vector3(5f,2.4f,3f)); col.center=new Vector3(0,1.2f,0);
            // V6.3.4 船只三级 LOD：占比>0.1%(0.001)显 LV3 高精且同屏最近≤30，0.01%~0.1% 为 LV2 简化，<0.01% 为 LV1 旧模
            PixelToCivilization.Rendering.LODKit.Attach(v, s.Military?7.5f:5.2f, 3, 0.001f, 0.00002f, "Ship", 30, 0.0001f);
            var click=v.AddComponent<ShipClick>(); click.Ship=s;
            click.OnClicked=ship=>PixelToCivilization.UI.UIManager.Instance?.ShowShip(ship);
            return v;
        }

        private void LoadDefs()
        {
            Add("small_boat","小木船","🛶",1,2,50,2,0.04f,0,0,false,0xB5743C,new(){{"wood",15}},0,"",0,120f);
            Add("medium_boat","帆船","⛵",5,8,100,5,0.035f,0,0,false,0xEBCFA0,new(){{"wood",100},{"stone",30}},0,"",0,140f);
            Add("large_boat","大船","🚢",20,20,200,10,0.025f,0,0,false,0xE05A4E,new(){{"wood",500},{"stone",200},{"gold",100}},0,"",0,160f);
            Add("treasure_ship","宝船","🛳️",50,50,400,20,0.02f,0,0,false,0xFFC23D,new(){{"wood",3000},{"stone",500},{"iron",200},{"gold",1000}},0,"",0,200f);
            // 军用船只：V6.1.1 按四级锚点补居住（运兵10/战船15/火炮20/火船5/宝船战舰50）
            Add("troop_boat","运兵船","🚣",10,10,80,3,0.035f,0,0,true,0x7FA04E,new(){{"wood",150},{"stone",50},{"food",30}},2,"",0,140f);
            Add("war_junk","战船","⛵",15,15,150,8,0.04f,15,12,true,0xE05A4E,new(){{"wood",300},{"stone",100},{"iron",30},{"gold",50}},2,"arrow",0,200f);
            Add("cannon_ship","火炮船","🚢",20,20,250,15,0.03f,40,18,true,0x4E8290,new(){{"wood",500},{"stone",150},{"iron",80},{"gold",100}},3,"cannon",0,240f);
            Add("fire_ship","火船","🔥",5,5,60,2,0.05f,60,6,true,0xFF7A1E,new(){{"wood",100},{"stone",20},{"iron",10},{"gold",20}},3,"fire",0,120f);
            Add("treasure_warship","宝船战舰","🛳️",50,50,500,25,0.025f,50,20,true,0xFFC23D,new(){{"wood",3000},{"stone",500},{"iron",300},{"gold",1000}},4,"cannon",0,260f);
            // ===== V9.3.3 现代舰船（公元1949=游戏年4949 起，军事/交通栏直接替换旧木船；T2-T4 造价锚点）=====
            Add("steamship","轮船","🚢",30,30,220,8,0.05f,0,0,false,0x4A5568,new(){{"wood",100},{"steel",30}},4,"",2,180f);
            Add("oil_tanker","油轮","🛢️",40,40,320,10,0.04f,0,0,false,0x2F4F4F,new(){{"steel",150},{"gold",50}},4,"",2,180f);
            Add("cruise_liner","邮轮","🛳️",60,60,400,12,0.035f,0,0,false,0xE8F0FE,new(){{"steel",250},{"gold",300}},4,"",3,200f);
            Add("destroyer","驱逐舰","🚀",30,30,400,18,0.045f,60,22,true,0x5A7D9A,new(){{"steel",120},{"iron",80},{"gold",100}},4,"cannon",2,280f);
            Add("submarine","潜水艇","🦑",20,20,300,14,0.05f,80,12,true,0x3B4A5A,new(){{"steel",180},{"iron",100},{"gold",120}},4,"cannon",1,240f);
            Add("missile_ship","导弹舰","🚀",40,40,500,20,0.04f,90,34,true,0x6B8E9E,new(){{"steel",200},{"iron",120},{"gold",200}},4,"cannon",3,320f);
            Add("aircraft_carrier","航空母舰","🛫",80,80,900,30,0.03f,120,44,true,0x8FA6AD,new(){{"steel",500},{"iron",300},{"gold",500}},4,"cannon",4,380f);
        }
        private void Add(string id,string name,string icon,int cap,int house,int dur,int def,float speed,
            float atk,float range,bool mil,long color,Dictionary<string,int> cost,int era=0,string atkType="",int sizeCls=0,float radar=200f)
        {
            Defs[id]=new ShipDef{Id=id,Name=name,Icon=icon,Capacity=cap,Housing=house,Durability=dur,Defense=def,
                Speed=speed,Attack=atk,Range=range,Radar=radar,Military=mil,ColorHex=color,Cost=cost,Era=era,AttackType=atkType,SizeCls=sizeCls};
        }

        // ===== V9.3.3 时代替换：公元1949（游戏年4949）前=木船9型；之后=现代7型（军事栏直接替换，不做灰显解锁）=====
        private static readonly string[] ModernShipIds = {"steamship","oil_tanker","cruise_liner","destroyer","submarine","missile_ship","aircraft_carrier"};
        public bool ModernEra => S.Year >= 4949;   // 公元1949 = 游戏年4949（游戏纪年=公元+3000；用户硬约束：1949年前木船、1949年起现代舰船）
        public IEnumerable<ShipDef> AvailableDefs
        {
            get
            {
                foreach (var kv in Defs)
                    if (ModernEra ? System.Array.IndexOf(ModernShipIds,kv.Key)>=0 : System.Array.IndexOf(ModernShipIds,kv.Key)<0)
                        yield return kv.Value;
            }
        }

        // ===== 属性公式（对齐源码）=====
        public int Capacity(ShipEntity s) => Mathf.FloorToInt((Defs.TryGetValue(s.ShipTypeId,out var d)?d.Capacity:1)*s.LevelMult);
        public int MaxDurability(ShipEntity s) => Mathf.FloorToInt((Defs.TryGetValue(s.ShipTypeId,out var d)?d.Durability:50)*s.LevelMult);
        public int AttackOf(ShipEntity s)
        {
            if (!Defs.TryGetValue(s.ShipTypeId,out var d)) return 0;
            // V9.3.4 满编战斗力：0人=50%、每多1%人员+0.6%、满员=110%（crewEff=0.5+min(1,Crew/Capacity)*0.6）
            float cap=Mathf.Max(1,Capacity(s));
            float ratio=Mathf.Clamp01(s.Crew/cap);
            float crewEff=0.5f+ratio*0.6f;
            float lvlBonus = 1+(s.Level-1)*0.25f;
            return Mathf.FloorToInt(d.Attack*crewEff*lvlBonus);
        }
        // V9.3.4 等级成长：攻击距离随等级 +15%/级（Lv1=基准、Lv2=1.15×、Lv3=1.3×）
        // V9.6.3f：所有船只攻击范围缩短一半（我方/敌方统一生效；等级成长与雷达范围不变）
        // V9.6.3f2：海军攻击半径硬上限 100 格（减半 × 等级成长后再 clamp ≤100，贴合"最强最高等级≤100格"）
        public float RangeOf(ShipEntity s)
        {
            if (!Defs.TryGetValue(s.ShipTypeId,out var d)) return 5f;
            return Mathf.Min(100f, d.Range*0.5f*(1f+(s.Level-1)*0.15f));
        }
        // V9.3.4 等级成长：发现范围/自动搜敌雷达随等级 +15%/级（Lv1=50、Lv2=57.5、Lv3=65）
        public float DetectRangeOf(ShipEntity s) => DetectRange*(1f+(s.Level-1)*0.15f);
        // V9.4.3 我方远距雷达：船型雷达×(1+0.15lv)（战船档200格：Lv1=200、Lv2=230、Lv3=260；驱逐280/导弹320/航母380 更远，小艇120 更近）
        public float MyDetectRangeOf(ShipEntity s)
        {
            if (!Defs.TryGetValue(s.ShipTypeId,out var d)) return 200f;
            return d.Radar*(1f+(s.Level-1)*0.15f);
        }
        // V9.4.3 敌方雷达：100格基线×(1+0.15lv)，大舰(SizeCls≥3)加20（Lv1=100/115/130…）
        public float EnemyDetectRangeOf(ShipEntity e)
        {
            if (!Defs.TryGetValue(e.ShipTypeId,out var d)) return 100f;
            float baseR=100f+(d.SizeCls>=3?20f:0f);
            return baseR*(1f+(e.Level-1)*0.15f);
        }
        // V9.3.5 任务态：军用船满编率>=50%=战斗态；低员军用+民用=载人态（用户条款：50%载量为分界）
        public bool BattlePriority(ShipEntity s) => s.Military && s.Crew >= Capacity(s)*0.5f;
        // V9.3.11 战斗优先：100 格内自动搜索/锁定/追击敌船（不再按载量分态；等级成长保留：Lv1=100、Lv2=115、Lv3=130）
        public float CombatDetectRangeOf(ShipEntity s) => (DetectRange*2f)*(1f+(s.Level-1)*0.15f);
        public float SpeedOf(ShipEntity s)
        {
            if (!Defs.TryGetValue(s.ShipTypeId,out var d)) return 0;
            if (s.Crew==0) return d.Speed*0.4f; // V9.3.5 空船0.4倍速巡航去载人，避免低员船0速死锁（原 return 0）
            float ratio=Mathf.Min(1,s.Crew/(float)Capacity(s));
            return d.Speed*(0.5f+ratio*0.5f)*(1+(s.Level-1)*0.1f);
        }

        /// <summary>V9.3.3 全局船只上限（我方+敌舰）</summary>
        public const int MaxShips = 200;

        /// <summary>建造船只（通用）——V9.3.3 时代替换：仅当前时代可见船型可建；上限200</summary>
        public bool BuildShip(string typeId, float x, float z)
        {
            if (!Defs.TryGetValue(typeId,out var d)) return false;
            if (S.Ships.Count+EnemyShips.Count>=MaxShips){ GM.AddEvent("bad","已达船只上限 200 艘，无法再建"+d.Name); return false; }
            bool visible=ModernEra ? System.Array.IndexOf(ModernShipIds,typeId)>=0 : System.Array.IndexOf(ModernShipIds,typeId)<0;
            if (!visible){ GM.AddEvent("bad", ModernEra?"木船时代已结束，"+d.Name+"退役停产（公元1949起为现代舰船）":"现代舰船尚未出现（公元1949年开启现代海军）"); return false; }
            if (!S.CanAfford(d.Cost)){ GM.AddEvent("bad","资源不足，无法建造"+d.Name); return false; }
            S.Pay(d.Cost);
            var s = new ShipEntity
            {
                ShipTypeId=typeId, Name=d.Name, Side="ours", X=x, Z=z, Level=1,
                Capacity=d.Capacity, Housing=d.Housing, MaxHp=d.Durability, Hp=d.Durability,
                BaseAttack=d.Attack, Range=d.Range, Military=d.Military, AttackType=d.AttackType
            };
            if (typeId=="aircraft_carrier") s.CarrierAir=8;   // V9.3.3 航母挂载：Lv1 8 机
            s.View=ShipView(s,d.ColorHex,d.Military?1.6f:1.2f);
            S.Ships.Add(s);
            GM.AddEvent("good",d.Icon+" 新"+d.Name+"建成！");
            return true;
        }

        /// <summary>开局村落免费赠船：不扣资源，停泊/巡游于指定水面点（带家园锚点供缓慢巡游）</summary>
        public ShipEntity SpawnInitialShip(string typeId,float x,float z)
        {
            if(!Defs.TryGetValue(typeId,out var d)) return null;
            var s=new ShipEntity{
                ShipTypeId=typeId,Name=d.Name,Side="ours",X=x,Z=z,HomeX=x,HomeZ=z,Level=1,
                Capacity=d.Capacity,Housing=d.Housing,MaxHp=d.Durability,Hp=d.Durability,
                BaseAttack=d.Attack,Range=d.Range,Military=d.Military,AttackType=d.AttackType,Crew=Mathf.Max(1,d.Capacity/2)};
            if (typeId=="aircraft_carrier") s.CarrierAir=8;   // V9.3.3 航母挂载载机
            s.View=ShipView(s,d.ColorHex,d.Military?1.6f:1.35f);
            S.Ships.Add(s); return s;
        }

        /// <summary>读档恢复船只：不扣资源，按类型/坐标/等级重建（含居住与视图）</summary>
        public ShipEntity RestoreShip(string typeId,float x,float z,int level,bool military)        {
            if(!Defs.TryGetValue(typeId,out var d))return null;
            var s=new ShipEntity{
                ShipTypeId=typeId,Name=d.Name,Side="ours",X=x,Z=z,Level=Mathf.Clamp(level,1,3),
                Capacity=d.Capacity,Housing=d.Housing,Military=military,AttackType=d.AttackType,
                BaseAttack=d.Attack,Range=d.Range};
            s.MaxHp=MaxDurability(s);s.Hp=s.MaxHp;
            ApplyCarrierAir(s);   // V9.3.3 读档重建航母载机
            s.View=ShipView(s,d.ColorHex,d.Military?1.6f:1.2f);
            S.Ships.Add(s);return s;
        }

        /// <summary>建造宝船（对齐 buildTreasureShip，木100铁10的简化舰队版）</summary>
        public bool BuildTreasureShip()
        {
            if (S.GetRes("wood")<100 || S.GetRes("iron")<10) return false;
            S.AddRes("wood",-100); S.AddRes("iron",-10);
            S.OceanFleets.Add(new ShipEntity{ShipTypeId="treasure_ship",Name="宝船",Side="ours",X=20,Z=50,MaxHp=400,Hp=400,Capacity=50,Housing=50});
            GM.AddEvent("good","🚢 新宝船建成！");
            return true;
        }

        private void EnsureFactions()
        {
            if (_factionsInited) return;
            _factionsInited=true; _enemyFactions.Clear();
            // 开局随机启用 3-5 个敌对阵营（硬约束：至少3、至多5）
            int n=Random.Range(3,6);
            var idx=new List<int>(); for(int i=0;i<FactionDefs.Length;i++) idx.Add(i);
            for(int i=idx.Count-1;i>0;i--){ int j=Random.Range(0,i+1); (idx[i],idx[j])=(idx[j],idx[i]); }
            for(int i=0;i<n;i++) _enemyFactions.Add(FactionDefs[idx[i]]);
            GM.AddEvent("bad","⚔ 海上帝阵营浮现："+string.Join("、",_enemyFactions.ConvertAll(f=>f.name).ToArray()));
        }
        private (string name,long color) RandomFaction()
        {
            EnsureFactions();
            return _enemyFactions[Random.Range(0,_enemyFactions.Count)];
        }
        /// <summary>V9.4.5 等比例分阵营：取当前敌舰最少的阵营（0 艘阵营优先），保证 3-5 个敌对阵营都有船且数量均衡</summary>
        private (string name,long color) LeastRepresentedFaction()
        {
            EnsureFactions();   // V9.4.7 修复：镜像/Showcase 首调时先初始化 3-5 敌阵营；原直接 return default，导致敌船 FactionId=null→注册归 PLAYER→与我方同阵营、海军永不交战
            if(_enemyFactions.Count==0) return default;
            (string name,long color) best=_enemyFactions[0]; int min=int.MaxValue;
            for(int i=0;i<_enemyFactions.Count;i++)
            {
                int cnt=0;
                foreach(var e in EnemyShips) if(e.FactionId==_enemyFactions[i].name) cnt++;
                if(cnt<min){ min=cnt; best=_enemyFactions[i]; }
            }
            return best;
        }

        /// <summary>V9.3.3 敌舰按时代选型：木船时代=旧军用船；现代=现代军舰（含航母低概率）</summary>
        private string EnemyShipType()
        {
            string[] types;
            if (ModernEra) types=new[]{"destroyer","submarine","missile_ship","destroyer","missile_ship","aircraft_carrier"};
            else types=new[]{"war_junk","cannon_ship","fire_ship"};
            return types[Random.Range(0,types.Length)];
        }

        private ShipEntity SpawnEnemyShip()
        {
            string t=EnemyShipType(); var d=Defs[t];
            var fac=LeastRepresentedFaction();
            float ex=0,ez=0; // V6.3.4：敌舰出生环上找水面点，避免直接刷在陆地
            for(int k=0;k<24;k++){float a=Random.value*Mathf.PI*2,rr=60f+Random.value*30f;ex=Mathf.Cos(a)*rr;ez=Mathf.Sin(a)*rr;
                if(_terrain==null||_terrain.IsOceanWater(ex,ez))break;} // V6.8.3：敌舰只刷在外海
            var s=new ShipEntity{ShipTypeId=t,Name=fac.name+"·"+d.Name,Side="enemy",
                X=ex,Z=ez,Level=1,MaxHp=d.Durability,Hp=d.Durability,Housing=d.Housing,
                BaseAttack=d.Attack,Range=d.Range,Military=true,AttackType=d.AttackType,Capacity=d.Capacity,Crew=d.Capacity,
                FactionId=fac.name,FactionColor=fac.color};
            if (t=="aircraft_carrier") s.CarrierAir=8;
            s.View=ShipView(s,fac.color,1.6f);
            EnemyShips.Add(s); _pirateEngaged=true; return s;
        }

        /// <summary>V6.1.2 Debug：强制生成一艘敌方战船（海战演示）</summary>
        public ShipEntity DebugSpawnEnemy(){ S.NavyBattleActive=true; return SpawnEnemyShip(); }

        /// <summary>V6.1.2 Debug：在村址附近水域生成一艘我方战船（无消耗）；V9.3.3 现代时代生成现代军舰</summary>
        public ShipEntity DebugSpawnOwnShip()
        {
            string t = ModernEra ? (Random.value<0.35f?"aircraft_carrier":(Random.value<0.6f?"missile_ship":"destroyer"))
                       : S.Era>=4?"treasure_warship":S.Era>=3?"cannon_ship":"war_junk";
            if(!Defs.ContainsKey(t)) t="war_junk";
            for(int i=0;i<24;i++)
            {
                float ang=Random.value*Mathf.PI*2f, dist=12f+Random.value*24f;
                float x=Mathf.Cos(ang)*dist,z=Mathf.Sin(ang)*dist;
                var terrain=UnityEngine.Object.FindObjectOfType<WorldGenerator>();
                if(terrain!=null && !terrain.IsOceanWater(x,z)) continue; // V6.8.3：我方船只刷在外海
                var s=SpawnInitialShip(t,x,z);
                GM.AddEvent("good","🚢 Debug 生成我方"+Defs[t].Name);
                return s;
            }
            GM.AddEvent("warn","附近没有可停靠的水域");
            return null;
        }

        /// <summary>V9.2.3 海战演示：确定性螺旋找最近外海放我方火炮船，再在其 6~24 内放敌炮船，立即接敌→火炮声光（Debug 强制，绕过时代）。</summary>
        public ShipEntity DebugNavalShowcase()
        {
            float ox=0f,oz=0f; bool sea=false;
            if(_terrain!=null)
            {
                for(float rr=10f;rr<=130f&&!sea;rr+=2.5f)
                    for(int a=0;a<36;a++)
                    {
                        float ang=a/36f*Mathf.PI*2f;
                        float cx=Mathf.Cos(ang)*rr, cz=Mathf.Sin(ang)*rr;
                        if(_terrain.IsOceanWater(cx,cz)){ ox=cx;oz=cz;sea=true;break; }
                    }
            }
            else { sea=true; }
            if(!sea){ GM.AddEvent("warn","130内无外海，海战演示取消"); return null; }
            ShipEntity own=SpawnInitialShip("cannon_ship",ox,oz);
            GM.AddEvent("good","🚢 Debug 海战演示：我方火炮船就位（外海）");
            float ex=0f,ez=0f; bool found=false;
            for(float rr=6f;rr<=24f&&!found;rr+=1f)
                for(int a=0;a<36;a++)
                {
                    float ang=a/36f*Mathf.PI*2f;
                    float cx=own.X+Mathf.Cos(ang)*rr, cz=own.Z+Mathf.Sin(ang)*rr;
                    if(OnWater(cx,cz)){ ex=cx;ez=cz;found=true;break; }
                }
            if(!found){ GM.AddEvent("warn","我方炮船24内无外海点，敌舰未生成"); return own; }
            SpawnEnemyAt("cannon_ship",ex,ez);
            S.NavyBattleActive=true;
            return own;
        }

        /// <summary>V9.4.5 浏览器回归：外海环形扫描批量生成我方军舰（制造"我方&gt;敌舰"差额触发 3 秒镜像补齐）</summary>
        public int DebugSpawnOwnWarships(int n)
        {
            int made=0;
            for(int k=0;k<n;k++)
            {
                float ox=0f,oz=0f; bool sea=false;
                if(_terrain!=null)
                {
                    for(float rr=10f;rr<=130f&&!sea;rr+=2.5f)
                        for(int a=0;a<36;a++)
                        {
                            float ang=a/36f*Mathf.PI*2f;
                            float cx=Mathf.Cos(ang)*rr, cz=Mathf.Sin(ang)*rr;
                            if(_terrain.IsOceanWater(cx,cz)){ ox=cx;oz=cz;sea=true;break; }
                        }
                }
                else sea=true;
                if(!sea) break;
                string ownT = ModernEra ? (Random.value<0.25f?"aircraft_carrier":(Random.value<0.5f?"missile_ship":"destroyer")) : "cannon_ship"; // V9.4.7 修复：现代演示船用现代军舰（与敌方对称），原硬编码 cannon_ship 导致我方全是木炮船、迅速全灭
                if(!Defs.ContainsKey(ownT)) ownT="cannon_ship";
                var s=SpawnInitialShip(ownT,ox,oz);
                if(s!=null) made++;
            }
            if(made>0) GM.AddEvent("good","🚢 Debug 我方 "+made+" 艘军舰就位（外海）");
            return made;
        }

        private ShipEntity SpawnEnemyAt(string t,float ex,float ez)
        {
            var d=Defs[t];
            var fac=LeastRepresentedFaction();   // V9.4.5 分阵营均衡（原 RandomFaction 残留）
            var s=new ShipEntity{ShipTypeId=t,Name=fac.name+"·"+d.Name,Side="enemy",
                X=ex,Z=ez,Level=1,MaxHp=d.Durability,Hp=d.Durability,Housing=d.Housing,
                BaseAttack=d.Attack,Range=d.Range,Military=true,AttackType=d.AttackType,Capacity=d.Capacity,Crew=d.Capacity,
                FactionId=fac.name,FactionColor=fac.color};
            s.View=ShipView(s,fac.color,1.6f);
            EnemyShips.Add(s); _pirateEngaged=true;
            return s;
        }

        public override void Tick(float dt)
        {
            try{
            if(_patrol.Count>0) // V9.2.3 清理已移除军舰的巡逻条目（防长周期泄漏）
                foreach(var key in new List<ShipEntity>(_patrol.Keys))
                    if(!S.Ships.Contains(key)) _patrol.Remove(key);
            }catch(System.Exception e){ Debug.LogError("[NAVSTAGE:A] "+e.GetType().Name+": "+e.Message); }
            try{
            // V9.4.5 敌舰确定性镜像：我方有军舰时按缺口批量补齐（小缺口一次补完、大缺口每 0.5 秒补 5 艘），3 秒内达到敌方=我方船数（≤全局200上限）；分阵营等比例（LeastRepresentedFaction）
            bool hasWarship=false;
            foreach (var s in S.Ships) if (s.Military) hasWarship=true;
            _spawnCd-=dt;
            if (S.Era>=2 && hasWarship && _spawnCd<=0 && MemoryBudgetManager.EntitySpawnGate(MemoryBudgetManager.EntityKind.Ship, S.Ships.Count+EnemyShips.Count))   // V9.7.1 实体上限门控（90% 提前暂停镜像）
            {
                int headroom=MaxShips-S.Ships.Count-EnemyShips.Count;
                int target=Mathf.Min(S.Ships.Count,headroom);   // 敌方≤我方数量，且不突破200上限
                if (EnemyShips.Count<target && headroom>0)
                {
                    int gap=target-EnemyShips.Count;
                    int batch = gap<=3 ? gap : Mathf.Min(gap, 5);   // 每批最多 5 艘；30 艘缺口≈6 批×0.5s=3 秒补齐
                    for(int i=0;i<batch;i++) SpawnEnemyShip();
                    _spawnCd=0.5f;   // V9.4.5 0.5 秒下一批，等量前高频补齐（原20秒+60%概率导致敌舰长期缺失）
                    S.NavyBattleActive=true;
                    if(!_pirateEngaged){ _pirateEngaged=true; GM.AddEvent("bad","⚓ 敌方舰队出现！（"+_enemyFactions.Count+" 阵营对峙）"); }
                }
                else if(EnemyShips.Count>=target){ _spawnCd=5f; } // 等量后低频复查（我方新增船时立即触发补船）
            }
            }catch(System.Exception e){ Debug.LogError("[NAVSTAGE:B] "+e.GetType().Name+": "+e.Message); }
            try{ UpdateOurShips(dt); }
            catch(System.Exception e){ Debug.LogError("[NAVSTAGE:C] "+e.GetType().Name+": "+e.Message); }
            try{ UpdateEnemyShips(dt); }
            catch(System.Exception e){ Debug.LogError("[NAVSTAGE:D] "+e.GetType().Name+": "+e.Message); }
            try{ SeparateShips(dt); }   // V9.4.6 船/军舰体积碰撞（防重叠）
            catch(System.Exception e){ Debug.LogError("[NAVSTAGE:E] "+e.GetType().Name+": "+e.Message); }
        }

        /// <summary>V9.4.6 船体体积分离：我方+敌舰各自两两推开（4Hz），防编队/巡航重叠；不破坏编队形态（半径 7.5 ≈ 编队间距 8 的 94%）</summary>
        /// <summary>V9.5.0 新局清理：敌舰/舰载机/编队/巡航/靠岸等运行时状态不进 GameState，必须在此销毁视图并清空，防上一局海战残留。</summary>
        public void ResetForNewGame()
        {
            foreach (var e in EnemyShips) if (e.View != null) Object.Destroy(e.View);
            EnemyShips.Clear();
            foreach (var st in _strikes) if (st.View != null) Object.Destroy(st.View);
            _strikes.Clear();
            _patrol.Clear(); _loadGoal.Clear(); _dockWait.Clear(); _formation.Clear();
            _enemyFactions.Clear(); _factionsInited = false;
            _pirateEngaged = false;
            _loadCd = 0f; _spawnCd = 0f; _carrierCd = 0f;
            _cruiseSwitchCd = 0f; _reformTimer = 0f; _sepTimer = 0f;
        }

        private void SeparateShips(float dt)
        {
            if(Time.unscaledTime<_sepTimer) return;
            _sepTimer=Time.unscaledTime+0.25f;
            const float minSq=7.5f*7.5f;
            SeparateList(S.Ships,minSq,dt);
            SeparateList(EnemyShips,minSq,dt);
        }
        private void SeparateList(List<ShipEntity> list,float minSq,float dt)
        {
            for(int i=0;i<list.Count;i++)
            {
                var a=list[i]; if(a.View==null) continue;
                for(int j=i+1;j<list.Count;j++)
                {
                    var b=list[j]; if(b.View==null) continue;
                    float dx=b.X-a.X,dz=b.Z-a.Z; float d2=dx*dx+dz*dz;
                    if(d2>0.0001f&&d2<minSq)
                    {
                        float d=Mathf.Sqrt(d2); float push=(7.5f-d)*0.5f*4f*dt;
                        float ux=dx/d,uz=dz/d;
                        a.X-=ux*push; a.Z-=uz*push; b.X+=ux*push; b.Z+=uz*push;
                    }
                }
            }
        }

        /// <summary>船龄按「游戏年」增长并到寿退役（旧实现误放在每帧 Tick 的 Age++，60fps 下约 3.3 秒即到寿 200 全部沉没）</summary>
        public override void OnYear(int year)
        {
            for(int i=S.Ships.Count-1;i>=0;i--)
            {
                var s=S.Ships[i];
                s.Age++;
                if (s.Age>s.MaxAge)
                {
                    _loadGoal.Remove(s); _dockWait.Remove(s); _formation.Remove(s);
                    if(s.View!=null)Object.Destroy(s.View);
                    S.Ships.RemoveAt(i);
                    GM.AddEvent("bad","一艘"+s.Name+"超期服役，已退役（船龄 "+s.Age+" 年）");
                }
            }
        }

        private void UpdateOurShips(float dt)
        {
            // V9.4.3 战斗/巡航统一低频重组编队（20s）：战斗态也保有编队，保证"编队追击（共享领队目标）"持续生效
            _reformTimer -= dt;
            if (_reformTimer <= 0f)
            {
                _reformTimer = 20f;
                try { RebuildFormations(); }
                catch (System.Exception ex) { Debug.LogError("[NAV:C0reform] "+ex.GetType().Name+": "+ex.Message); }
            }
            for(int si=0; si<S.Ships.Count; si++)
            {
                var s=S.Ships[si];
                float lookYaw=0f; bool hasLook=false;
                try{ KeepAtSea(s); } // V6.8.3 永留外海：退潮坐滩、误入内河/湖泊/陆地即归位最近外海
                catch(System.Exception ex){ Debug.LogError("[NAV:C1 ship"+si+"/"+s.ShipTypeId+"] "+ex.GetType().Name+": "+ex.Message); }
                bool wet=false;
                try{ wet=OnWater(s.X,s.Z); }
                catch(System.Exception ex){ Debug.LogError("[NAV:C2 ship"+si+"] "+ex.GetType().Name+": "+ex.Message); wet=true; }
                if(!wet)
                { // 退潮坐滩：仅垂直贴合潮位/滩面，不巡航、不追击、不漂移，涨潮自动复浮
                    try{ if(s.View!=null) s.View.transform.position=new Vector3(s.X,ShipRestY(s),s.Z); }
                    catch(System.Exception ex){ Debug.LogError("[NAV:C6b ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
                    continue;
                }
                if (!s.Military)
                {
                    try{
                    float ox=s.X, oz=s.Z;
                    // V9.3.5 民用船恒载人态：100格内有闲人优先驶向载人，无则围绕家园锚点缓慢圆周巡游
                    // V9.3.8 修复登船：到点停靠等待 6s；100格无闲人时驶向聚落海岸停靠点接人（人满/无人回家园巡游）
                    if(IsDocked(s)) { }
                    else if (s.Passengers>=s.EffectiveHousing) { HomeCruise(s,dt,ref lookYaw,ref hasLook,ox,oz); }
                    else if(!MoveToLoad(s,dt,ref lookYaw,ref hasLook,ox,oz)) CoastGoal(s,dt,ref lookYaw,ref hasLook,ox,oz);
                    }catch(System.Exception ex){ Debug.LogError("[NAV:C3 ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
                }
                else
                {
                    try{ s.AttackCd-=dt; }catch(System.Exception ex){ Debug.LogError("[NAV:C4a ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
                    CombatTarget ct=null;
                    try{ ct=GM.Combat.NearestHostile(s.X,s.Z, MyDetectRangeOf(s)*GameConstants.Tile, CombatSystem.KeyOf(s)); }
                    catch(System.Exception ex){ Debug.LogError("[NAV:C4 ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
                    ShipEntity support=null;
                    try{
                    // V9.4.7 编队共享目标：成员雷达内无敌时跟随领队锁定（统一目录，跨类型）
                    if (ct==null && _formation.TryGetValue(s,out var cf) && cf.Members.Count>1 && cf.Members[0]!=s)
                    {
                        var lead=cf.Members[0];
                        ct=GM.Combat.NearestHostile(lead.X,lead.Z, MyDetectRangeOf(lead)*GameConstants.Tile, CombatSystem.KeyOf(lead));
                    }
                    // V9.4.3 支援：雷达内低血友军优先护航（次于对敌战斗）
                    if (ct==null) support=NearestHurtAlly(s);
                    }catch(System.Exception ex){ Debug.LogError("[NAV:C4s ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
                    try{
                    if (ct!=null)
                    {
                        float d=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(ct.X,ct.Z));
                        float ox=s.X,oz=s.Z;
                        bool fire=s.ShipTypeId=="fire_ship";
                        // V9.3.5 射程(格)×Tile(4)=世界单位；V9.3.11 战斗优先：锁定即全力追击
                        float engage=fire?3.2f:RangeOf(s)*GameConstants.Tile;
                        if (d>engage){
                            Vector3 dir=new Vector3(ct.X-s.X,0f,ct.Z-s.Z).normalized; float sp=SpeedOf(s);
                            // V9.3.9 战斗航速：追击 +50%；30 格内近战加速 100% 但舰船受损（耐久掉至 50% 为止）
                            if(d<=30f*GameConstants.Tile){ sp*=2.0f; s.Hp=Mathf.Max(s.MaxHp*0.5f, s.Hp-s.MaxHp*0.08f*dt); }
                            else sp*=1.5f;
                            // V6.1.9(i) 洋流海风：顺流顺风加速、逆流逆风减速
                            if(GM.OceanFlow!=null) sp*=GM.OceanFlow.SailFactor(s.X,s.Z,new Vector2(dir.x,dir.z));
                            float nx=s.X+dir.x*sp*30*dt, nz=s.Z+dir.z*sp*30*dt;
                            if(OnWater(nx,nz)){ s.X=nx; s.Z=nz; } // V6.1.9 军舰不得登上陆地
                        }
                        else if (fire){ if(ct.Ref is ShipEntity) DetonateOurFireShip(s); }
                        else if (s.AttackCd<=0 && AttackOf(s)>0){ OurShipFire(s,ct,d); s.AttackCd=2f; }
                        if(ct.Ref is ShipEntity st) CarrierTick(s,st,dt);   // V9.4.1 航母弹射舰载机（目标为舰船）
                        float mvx=s.X-ox,mvz=s.Z-oz;
                        if (Mathf.Abs(mvx)+Mathf.Abs(mvz)>1e-4f){ lookYaw=Mathf.Atan2(mvx,mvz)*Mathf.Rad2Deg; hasLook=true; }
                    }
                    else {
                        // V9.4.3 优先级：战斗 > 支援低血友军 > 招人(50格) > 巡航
                        // V9.6.0 紧急集结令：蓝旗（海军）100 格内军舰优先向军旗集结列阵（高于支援/招人/巡航）
                        float ox=s.X,oz=s.Z;
                        bool rallied=false;
                        if (GM.Rally!=null && GM.Rally.Active("navy"))
                        {
                            var rp=GM.Rally.Target("navy");
                            if (rp.HasValue)
                            {
                                float rd=Vector2.Distance(new Vector2(s.X,s.Z),rp.Value);
                                if (rd<=RallySystem.RallyRange*GameConstants.Tile)
                                {
                                    rallied=true;
                                    if (rd>RallySystem.FormRange) MoveToward(s,rp.Value.x,rp.Value.y,dt,ref lookYaw,ref hasLook);
                                    // 已入列阵圈：停泊待命
                                }
                            }
                        }
                        if (!rallied)
                        {
                            if (support!=null)
                            {
                                float ds=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(support.X,support.Z));
                                if (ds>8f*GameConstants.Tile){ MoveToward(s,support.X,support.Z,dt,ref lookYaw,ref hasLook); }
                                else { MoveToward(s, support.X+Mathf.Cos(Time.time*0.5f)*12f, support.Z+Mathf.Sin(Time.time*0.5f)*12f, dt, ref lookYaw, ref hasLook); }
                            }
                            else if(IsDocked(s)) { }
                            else if(MoveToLoad(s,dt,ref lookYaw,ref hasLook,ox,oz)) { }
                            else if(!CruiseMove(s,dt,ref lookYaw,ref hasLook)) PatrolMove(s,dt,ref lookYaw,ref hasLook);
                        }
                    } // V9.2.3 无敌舰：自主巡逻
                    }catch(System.Exception ex){ Debug.LogError("[NAV:C5 ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
                }
                try{
                if (s.View!=null)
                {
                    float bob=ShipRestY(s); // V6.8.2：随潮起伏，退潮坐滩时托在滩面
                    s.View.transform.position=new Vector3(s.X,bob,s.Z);
                    if (hasLook) s.View.transform.rotation=Quaternion.Slerp(s.View.transform.rotation,Quaternion.Euler(0,lookYaw,0),0.12f);
                }
                }catch(System.Exception ex){ Debug.LogError("[NAV:C6 ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
            }
            try{ StrikeUpdate(dt); }   // V9.4.1 舰载机在飞状态驱动（起飞/巡航/俯冲/返航/回收）
            catch(System.Exception ex){ Debug.LogError("[NAV:C7 carrier] "+ex.GetType().Name+": "+ex.Message); }
        }

        private void UpdateEnemyShips(float dt)
        {
            for (int i=EnemyShips.Count-1;i>=0;i--)
            {
                var e=EnemyShips[i];
                try{ KeepAtSea(e); } // V6.8.3 敌舰同样永留外海
                catch(System.Exception ex){ Debug.LogError("[NAV:D1 idx"+i+"] "+ex.GetType().Name+": "+ex.Message); }
                CombatTarget ct=null;
                try{ ct=GM.Combat.NearestHostile(e.X,e.Z, EnemyDetectRangeOf(e)*GameConstants.Tile, e.FactionId); }
                catch(System.Exception ex){ Debug.LogError("[NAV:D2 idx"+i+"] "+ex.GetType().Name+": "+ex.Message); }
                try{
                if (ct!=null)
                {
                    float d=Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(ct.X,ct.Z));
                    bool eFire=e.ShipTypeId=="fire_ship";
                    float engage=eFire?3.2f:RangeOf(e)*GameConstants.Tile;
                    // V9.4.7 敌舰统一目录索敌：射程外逼近，射程内开火
                    if (d>engage){ EnemyMove(e,ct.X,ct.Z,dt); }
                    else if (eFire){ if(ct.Ref is ShipEntity) DetonateEnemyFireShip(e); }
                    else { e.AttackCd-=dt; if(e.AttackCd<=0){ EnemyShipFire(e,ct,d);e.AttackCd=2.5f;} }
                }
                else
                {
                    // V9.4.7 敌舰编队共享目标（统一目录，跨类型）
                    CombatTarget shared=null;
                    for(int j=0;j<EnemyShips.Count;j++)
                    {
                        var o=EnemyShips[j]; if(o==e) continue;
                        float dd=Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(o.X,o.Z));
                        if(dd>40f*GameConstants.Tile) continue;
                        var ot=GM.Combat.NearestHostile(o.X,o.Z, EnemyDetectRangeOf(o)*GameConstants.Tile, o.FactionId);
                        if(ot!=null){ shared=ot; break; }
                    }
                    if (shared!=null){ EnemyMove(e,shared.X,shared.Z,dt); }
                    else
                    {
                        // V9.4.3 敌舰支援：100格内低血敌舰靠拢护航
                        var hurt=NearestHurtEnemy(e);
                        if(hurt!=null){ EnemyMove(e,hurt.X,hurt.Z,dt); }
                        else CruiseEnemy(e,dt);
                    }
                }
                }catch(System.Exception ex){ Debug.LogError("[NAV:D3 idx"+i+"] "+ex.GetType().Name+": "+ex.Message); }
                try{ if (e.View!=null) e.View.transform.position=new Vector3(e.X,ShipRestY(e),e.Z); }
                catch(System.Exception ex){ Debug.LogError("[NAV:D4 idx"+i+"] "+ex.GetType().Name+": "+ex.Message); }
                try{
                if (e.Hp<=0){ if(e.View!=null)Object.Destroy(e.View); EnemyShips.RemoveAt(i); GM.AddEvent("good","💥 击沉一艘敌舰！"); }
                }catch(System.Exception ex){ Debug.LogError("[NAV:D5 idx"+i+"] "+ex.GetType().Name+": "+ex.Message); }
            }
            // 清理我方沉舰
            try{
            S.Ships.RemoveAll(s=>{ if(s.Hp<=0){_loadGoal.Remove(s); _dockWait.Remove(s); _formation.Remove(s); if(s.View!=null)Object.Destroy(s.View);return true;} return false; });
            }catch(System.Exception ex){ Debug.LogError("[NAV:D6] "+ex.GetType().Name+": "+ex.Message); }
            try{
            if (EnemyShips.Count==0)
            {
                S.NavyBattleActive=false;
                // V6.1.5 肃清海盗/敌舰：护航赏金 + 一段平静期
                if (_pirateEngaged)
                {
                    _pirateEngaged=false;
                    int gold=40+Random.Range(0,41);
                    S.AddRes("gold",gold); _spawnCd=Mathf.Max(_spawnCd,60f);
                    GM.AddEvent("good","🏴‍☠️ 肃清当前海域敌舰，护航赏金 "+gold+" 金，海疆暂宁");
                }
            }
            }catch(System.Exception ex){ Debug.LogError("[NAV:D7] "+ex.GetType().Name+": "+ex.Message); }
        }

        /// <summary>V9.4.5 敌舰统一移动：速度=SpeedOf×0.85（略慢于我方保留可追性），对齐我方 SpeedOf×30×dt 公式；仅限外海水面</summary>
        private void EnemyMove(ShipEntity e, float tx, float tz, float dt)
        {
            float sp=SpeedOf(e)*0.85f;
            if(sp<=0f) return;
            Vector3 dir=new Vector3(tx-e.X,0,tz-e.Z).normalized;
            float nx=e.X+dir.x*sp*30f*dt, nz=e.Z+dir.z*sp*30f*dt;
            if(OnWater(nx,nz)){ e.X=nx; e.Z=nz; }
        }
        /// <summary>V9.4.5 敌舰无目标游弋：每 8~14 秒选一个外海随机点巡航，到点换点；杜绝停靠岸边静止</summary>
        private void CruiseEnemy(ShipEntity e, float dt)
        {
            e.AttackCd-=dt;
            if(e.AttackCd<=0f)
            {
                e.AttackCd=8f+Random.value*6f;
                for(int k=0;k<24;k++)
                {
                    float ang=Random.value*Mathf.PI*2f, rr=40f+Random.value*70f;
                    float cx=e.X+Mathf.Cos(ang)*rr, cz=e.Z+Mathf.Sin(ang)*rr;
                    if(_terrain!=null && _terrain.IsOceanWater(cx,cz) && _terrain.InsideFrontier(cx,cz)){ e.HomeX=cx; e.HomeZ=cz; break; }
                }
            }
            float dx=e.HomeX-e.X, dz=e.HomeZ-e.Z;
            if(dx*dx+dz*dz > 4f) EnemyMove(e,e.HomeX,e.HomeZ,dt);
        }

        // ===== V9.3.3 发现距离50格 + 射程内开火 + 距离衰减 =====
        public const float DetectRange = 50f;   // 敌我双向发现距离（硬约束）
        /// <summary>伤害效率：eff=clamp01((R-d)/(R*0.5))——射程R边界0%、半射程50%、半射程内100%（普通炮船R20：20格0%/15格50%/10格100%）</summary>
        public static float DamageEff(float range,float dist)
        {
            if (range<=0f) return 1f;
            return Mathf.Clamp01((range-dist)/(range*0.5f));
        }
        // V9.4.3 我方搜索=200格档远距雷达（分船型/等级）；敌舰搜索=100格档雷达
        private ShipEntity NearestEnemy(ShipEntity s){ ShipEntity best=null;float bd=MyDetectRangeOf(s);foreach(var e in EnemyShips){float d=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(e.X,e.Z));if(d<bd){bd=d;best=e;}}return best; }
        private ShipEntity NearestOurs(ShipEntity e){ ShipEntity best=null;float bd=EnemyDetectRangeOf(e);foreach(var s in S.Ships){if(!s.Military)continue;float d=Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(s.X,s.Z));if(d<bd){bd=d;best=s;}}return best; }
        /// <summary>V9.4.3 支援：我方雷达内低血友军（Hp≤30%Max）——驶向护航</summary>
        private ShipEntity NearestHurtAlly(ShipEntity s)
        {
            ShipEntity best=null; float bd=float.MaxValue;
            float rr=MyDetectRangeOf(s)*GameConstants.Tile;
            for(int i=0;i<S.Ships.Count;i++){ var o=S.Ships[i]; if(o==s||!o.Military||o.Hp>o.MaxHp*0.3f) continue;
                float d=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(o.X,o.Z));
                if(d<rr&&d<bd){ bd=d; best=o; } }
            return best;
        }
        /// <summary>V9.4.3 敌舰支援：100格内低血敌舰（Hp≤30%Max）——驶向护航</summary>
        private ShipEntity NearestHurtEnemy(ShipEntity e)
        {
            ShipEntity best=null; float bd=float.MaxValue;
            float rr=100f*GameConstants.Tile;
            for(int i=0;i<EnemyShips.Count;i++){ var o=EnemyShips[i]; if(o==e||o.Hp>o.MaxHp*0.3f) continue;
                float d=Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(o.X,o.Z));
                if(d<rr&&d<bd){ bd=d; best=o; } }
            return best;
        }

        // ===== V9.2.3 军舰自主巡逻 =====
        private PatrolRoute GetPatrol(ShipEntity s)
        {
            if(!_patrol.TryGetValue(s,out var r)){ r=new PatrolRoute(); _patrol[s]=r; }
            if(!r.Inited)
            {
                r.Inited=true;
                const int K=6;
                for(int k=0;k<K;k++)
                {   // 每个方位在半径14~42环带找一个"当前确为外海"的航点；落陆地则该方位跳过
                    float ang=k/(float)K*Mathf.PI*2f;
                    for(float rr=14f;rr<=42f;rr+=4f)
                    {
                        float px=s.X+Mathf.Cos(ang)*rr, pz=s.Z+Mathf.Sin(ang)*rr;
                        if(OnWater(px,pz)){ r.Pts.Add(new Vector2(px,pz)); break; }
                    }
                }
            }
            return r;
        }

        /// <summary>无敌舰时军舰沿外海航点自主巡航；到点切下一航点循环。只在外海，不触发开火。</summary>
        private void PatrolMove(ShipEntity s, float dt, ref float lookYaw, ref bool hasLook)
        {
            var r=GetPatrol(s);
            if(r.Pts.Count==0) return;
            if(r.Idx>=r.Pts.Count) r.Idx=0;
            var wp=r.Pts[r.Idx];
            if(Vector2.Distance(new Vector2(s.X,s.Z),wp)<=3f){ r.Idx=(r.Idx+1)%r.Pts.Count; return; }
            float ox=s.X, oz=s.Z;
            Vector3 dir=(new Vector3(wp.x,0f,wp.y)-s.Pos).normalized;
            float sp=SpeedOf(s);
            if(sp<=0f && Defs.TryGetValue(s.ShipTypeId,out var pd)) sp=pd.Speed*0.5f; // Lv1未满员：骨架值守航速兜底
            if(GM.OceanFlow!=null) sp*=GM.OceanFlow.SailFactor(s.X,s.Z,new Vector2(dir.x,dir.z));
            float nx=s.X+dir.x*sp*30f*dt, nz=s.Z+dir.z*sp*30f*dt;
            float dvx=0f, dvz=0f;
            if(GM.OceanFlow!=null){ var dv=GM.OceanFlow.Drift(nx,nz,dt,0.8f); dvx=dv.x; dvz=dv.y; }
            if(OnWater(nx+dvx,nz+dvz)){ s.X=nx+dvx; s.Z=nz+dvz; }
            else if(OnWater(nx,nz)){ s.X=nx; s.Z=nz; }
            float mvx=s.X-ox, mvz=s.Z-oz;
            if(Mathf.Abs(mvx)+Mathf.Abs(mvz)>1e-4f){ lookYaw=Mathf.Atan2(mvx,mvz)*Mathf.Rad2Deg; hasLook=true; }
        }

        // ===== V9.3.5 主动载人：载人态船驶向最近空闲平民（100格=400世界单位内），到14世界单位内停靠交由EmbarkSystem吸附 =====
        /// <summary>100格内最近未登乘平民（平方距离裁剪；0.5s节流刷新目标）</summary>
        private Vector2? NearestIdleAgent(float x,float z,float rangeWorld)
        {
            Vector2? best=null; float bd=rangeWorld*rangeWorld;
            if(S.Agents==null) return null;
            for(int i=0;i<S.Agents.Count;i++)
            {
                var a=S.Agents[i];
                if(a==null||a.Boarded) continue;
                float dx=a.X-x,dz=a.Z-z,d=dx*dx+dz*dz;
                if(d<bd){ bd=d; best=new Vector2(a.X,a.Z); }
            }
            return best;
        }
        /// <summary>V9.3.9 载人优先判据：半径内未登船闲人数≥minCount（高员军船近岸大量人员时也先靠岸载人）</summary>
        private bool ManyIdleNear(ShipEntity s,float radius,int minCount)
        {
            if(S.Agents==null) return false;
            int cnt=0; float rr=radius*radius;
            for(int i=0;i<S.Agents.Count;i++){ var a=S.Agents[i]; if(a==null||a.Boarded) continue;
                float dx=a.X-s.X,dz=a.Z-s.Z; if(dx*dx+dz*dz<rr){ if(++cnt>=minCount) return true; } }
            return false;
        }
        /// <summary>载人态船驶向目标闲人；无闲人/已到达返回 false（调用方回退巡航或巡逻）。V9.3.11 招人次之：搜索半径 50 格，不设载量门槛</summary>
        private bool MoveToLoad(ShipEntity s,float dt,ref float lookYaw,ref bool hasLook,float ox,float oz)
        {
            if(_loadCd<=0f)
            {
                _loadCd=0.5f;
                var near=NearestIdleAgent(s.X,s.Z,50f*GameConstants.Tile);   // V9.3.11 50格内有人才去载人
                if(near.HasValue) _loadGoal[s]=near.Value; else _loadGoal.Remove(s);
            }
            if(!_loadGoal.TryGetValue(s,out var g)) return false;
            // V9.3.8 修复登船：到 14 世界单位内不立即开走，停靠等待 6s（EmbarkSystem 每 2.5s 以 400 世界单位吸附岸边人员）
            if(Vector2.Distance(new Vector2(s.X,s.Z),g)<=14f){ _dockWait[s]=6f; return true; }
            Vector3 dir=(new Vector3(g.x,0f,g.y)-s.Pos).normalized;
            float sp=SpeedOf(s)*0.8f;
            if(GM.OceanFlow!=null) sp*=GM.OceanFlow.SailFactor(s.X,s.Z,new Vector2(dir.x,dir.z));
            float nx=s.X+dir.x*sp*30f*dt, nz=s.Z+dir.z*sp*30f*dt;
            float dvx=0f,dvz=0f;
            if(GM.OceanFlow!=null){ var dv=GM.OceanFlow.Drift(nx,nz,dt,0.6f); dvx=dv.x; dvz=dv.y; }
            if(OnWater(nx+dvx,nz+dvz)){ s.X=nx+dvx; s.Z=nz+dvz; }
            else if(OnWater(nx,nz)){ s.X=nx; s.Z=nz; }
            float mvx=s.X-ox, mvz=s.Z-oz;
            if(Mathf.Abs(mvx)+Mathf.Abs(mvz)>1e-4f){ lookYaw=Mathf.Atan2(mvx,mvz)*Mathf.Rad2Deg; hasLook=true; }
            return true;
        }

        // ===== V9.3.8 靠岸停泊与聚落海岸接人 =====
        /// <summary>停泊计时：停泊期间船保持原位不动（EmbarkSystem 每 2.5s 吸附岸边人员）；到期移除。</summary>
        private bool IsDocked(ShipEntity s)
        {
            if(!_dockWait.TryGetValue(s,out var dw)) return false;
            dw-=Time.deltaTime;
            if(dw<=0f){ _dockWait.Remove(s); return false; }
            _dockWait[s]=dw; return true;
        }
        /// <summary>民用船满员/无人可载时的家园锚点圆周巡游（V9.2.3 原逻辑抽出复用）。</summary>
        private void HomeCruise(ShipEntity s,float dt,ref float lookYaw,ref bool hasLook,float ox,float oz)
        {
            float phase=(s.HomeX*0.7f+s.HomeZ*0.5f)+Time.time*0.10f;
            float rr=9f;
            float tx=s.HomeX+Mathf.Cos(phase)*rr, tz=s.HomeZ+Mathf.Sin(phase)*rr;
            float nx=Mathf.Lerp(s.X,tx,dt*0.6f), nz=Mathf.Lerp(s.Z,tz,dt*0.6f);
            float dvx=0f,dvz=0f;
            if(GM.OceanFlow!=null){var dv=GM.OceanFlow.Drift(nx,nz,dt,1.2f);dvx=dv.x;dvz=dv.y;}
            if(OnWater(nx+dvx,nz+dvz)){ s.X=nx+dvx; s.Z=nz+dvz; }
            float mvx=s.X-ox, mvz=s.Z-oz;
            if (Mathf.Abs(mvx)+Mathf.Abs(mvz)>1e-4f){ lookYaw=Mathf.Atan2(mvx,mvz)*Mathf.Rad2Deg; hasLook=true; }
        }
        /// <summary>聚落海岸停靠：载人态船 100 格内无闲人时，取未登船闲人质心，向外海螺旋找最近水面停靠点(≤60世界单位)驶向并停泊 8s 接人。</summary>
        private void CoastGoal(ShipEntity s,float dt,ref float lookYaw,ref bool hasLook,float ox,float oz)
        {
            float gx=0f,gz=0f; int n=0;
            if(S.Agents!=null)
                for(int i=0;i<S.Agents.Count;i++){ var a=S.Agents[i]; if(a!=null&&!a.Boarded){ gx+=a.X; gz+=a.Z; n++; } }
            if(n==0){ HomeCruise(s,dt,ref lookYaw,ref hasLook,ox,oz); return; }   // 无人可载：家园巡游兜底
            gx/=n; gz/=n;
            float tx=gx,tz=gz; bool found=false;
            for(float rr=0f;rr<=60f&&!found;rr+=2f)
            {
                for(int a=0;a<24;a++)
                {
                    float ang=a/24f*Mathf.PI*2f;
                    float cx=gx+Mathf.Cos(ang)*rr, cz=gz+Mathf.Sin(ang)*rr;
                    if(OnWater(cx,cz)){ tx=cx; tz=cz; found=true; break; }
                }
            }
            if(!found){ HomeCruise(s,dt,ref lookYaw,ref hasLook,ox,oz); return; }  // 60 内无外海：家园巡游兜底
            if(Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(tx,tz))<=4f){ _dockWait[s]=8f; return; }   // 到达停靠点：停泊 8s 等 Embark 吸附
            Vector3 dir=(new Vector3(tx,0f,tz)-s.Pos).normalized;
            float sp=SpeedOf(s); if(sp<=0f&&Defs.TryGetValue(s.ShipTypeId,out var pd)) sp=pd.Speed*0.5f;
            if(GM.OceanFlow!=null) sp*=GM.OceanFlow.SailFactor(s.X,s.Z,new Vector2(dir.x,dir.z));
            float nx=s.X+dir.x*sp*30f*dt, nz=s.Z+dir.z*sp*30f*dt;
            float dvx=0f,dvz=0f;
            if(GM.OceanFlow!=null){ var dv=GM.OceanFlow.Drift(nx,nz,dt,0.6f); dvx=dv.x; dvz=dv.y; }
            if(OnWater(nx+dvx,nz+dvz)){ s.X=nx+dvx; s.Z=nz+dvz; }
            else if(OnWater(nx,nz)){ s.X=nx; s.Z=nz; }
            float mvx=s.X-ox, mvz=s.Z-oz;
            if(Mathf.Abs(mvx)+Mathf.Abs(mvz)>1e-4f){ lookYaw=Mathf.Atan2(mvx,mvz)*Mathf.Rad2Deg; hasLook=true; }
        }

        // ===== V9.3.8 自动编队巡航（军用满员、无敌舰时触发；运行时态不进存档） =====
        private CruiseFormation GetFormation(ShipEntity s)
        {
            if(_formation.TryGetValue(s,out var f)) return f;
            RebuildFormations();
            return _formation.TryGetValue(s,out f)?f:null;
        }
        private int SizeClsOf(ShipEntity s) => Defs.TryGetValue(s.ShipTypeId,out var d)?d.SizeCls:0;
        /// <summary>V9.4.3 就近编队：空间聚类（格网40世界单位），每队≤30艘；队内领队=最大船（小的靠近大的）；<3艘的簇并入最近簇（少的靠近多的）；最终<3艘不编队由巡逻兜底。</summary>
        private void RebuildFormations()
        {
            _formation.Clear();
            var eligible=new List<ShipEntity>();
            // V9.3.11 巡航最次：所有军用船皆可入队（仅在"无敌船且无人可招"的巡航态被调用，故无需再判载量）
            for(int i=0;i<S.Ships.Count;i++){ var s=S.Ships[i]; if(s.Military) eligible.Add(s); }
            if(eligible.Count<3) return;   // 少于 3 艘不编队（单船由 PatrolMove 巡逻兜底）
            const float cell=40f;
            var clusters=new List<List<ShipEntity>>();
            foreach(var s in eligible)
            {
                List<ShipEntity> best=null; float bd=float.MaxValue;
                for(int c=0;c<clusters.Count;c++)
                {
                    var cl=clusters[c]; if(cl.Count>=30) continue;
                    var lead=cl[0];
                    float d=(s.X-lead.X)*(s.X-lead.X)+(s.Z-lead.Z)*(s.Z-lead.Z);
                    if(d<bd){ bd=d; best=cl; }
                }
                if(best!=null && bd<=cell*cell) best.Add(s);
                else clusters.Add(new List<ShipEntity>{s});
            }
            // 合并 <3 的簇到最近大簇（少的靠近多的）
            for(int c=clusters.Count-1;c>=0;c--)
            {
                if(clusters[c].Count>=3) continue;
                List<ShipEntity> best=null; float bd=float.MaxValue;
                var lead=clusters[c][0];
                for(int d=0;d<clusters.Count;d++)
                {
                    if(d==c||clusters[d].Count>=30) continue;
                    var dl=clusters[d][0];
                    float dd=(lead.X-dl.X)*(lead.X-dl.X)+(lead.Z-dl.Z)*(lead.Z-dl.Z);
                    if(dd<bd){ bd=dd; best=clusters[d]; }
                }
                if(best!=null) foreach(var s in clusters[c]){ if(best.Count<30) best.Add(s); }
            }
            foreach(var cl in clusters)
            {
                if(cl.Count<3) continue;   // 最终仍<3：单船巡逻兜底
                cl.Sort((a,b)=>SizeClsOf(b).CompareTo(SizeClsOf(a)));   // V9.4.3 领队=最大船（小靠大）
                var f=new CruiseFormation();
                foreach(var s in cl){ _formation[s]=f; f.Members.Add(s); }
            }
        }
        private bool CruiseMove(ShipEntity s,float dt,ref float lookYaw,ref bool hasLook)
        {
            var f=GetFormation(s);
            if(f==null) return false;   // 未入编队 → 单船巡逻
            _cruiseSwitchCd-=dt;
            if(_cruiseSwitchCd<=0f){ _cruiseSwitchCd=CruiseSwitchInterval; f.Mode=(f.Mode+1)%3; f.Formation=(f.Formation+1)%5; f.Inited=false; }   // V9.3.9 阵型随模式轮换
            if(!f.Inited){ f.Inited=true; f.WpIdx=0; GenCruiseWaypoints(f,s); }
            if(f.Members[0]==s)   // 领队：沿航点巡航
            {
                if(f.Waypoints.Count==0){ GenCruiseWaypoints(f,s); return true; }
                if(f.WpIdx>=f.Waypoints.Count) f.WpIdx=0;
                var wp=f.Waypoints[f.WpIdx];
                if(Vector2.Distance(new Vector2(s.X,s.Z),wp)<=3f){ f.WpIdx=(f.WpIdx+1)%f.Waypoints.Count; return true; }
                if(s.View!=null) f.LeadYaw=s.View.transform.rotation.eulerAngles.y;   // V9.3.9 领队航向供成员排阵
                MoveToward(s,wp.x,wp.y,dt,ref lookYaw,ref hasLook);
                return true;
            }
            // V9.3.9 成员按阵型相对领队排布（倒V/V/纵列/横排/半圆；落队>30 直奔领队归位）
            var lead=f.Members[0];
            int mi=f.Members.IndexOf(s);
            var off=FormationOffset(mi,f);
            float cyaw=-f.LeadYaw*Mathf.Deg2Rad, ca=Mathf.Cos(cyaw), sa=Mathf.Sin(cyaw);
            var target=new Vector2(lead.X+off.x*ca-off.y*sa, lead.Z+off.x*sa+off.y*ca);
            if(Vector2.Distance(new Vector2(s.X,s.Z),target)>30f) target=new Vector2(lead.X,lead.Z);
            MoveToward(s,target.x,target.y,dt,ref lookYaw,ref hasLook);
            return true;
        }
        private void GenCruiseWaypoints(CruiseFormation f,ShipEntity s)
        {
            f.Waypoints.Clear();
            switch(f.Mode)
            {
                case 0:   // M1 绕大陆航线：8 方位、半径 40~90 环带找外海航点
                    for(int k=0;k<8;k++)
                    {
                        float ang=k/8f*Mathf.PI*2f;
                        for(float rr=40f;rr<=90f;rr+=5f)
                        {
                            float px=s.X+Mathf.Cos(ang)*rr, pz=s.Z+Mathf.Sin(ang)*rr;
                            if(OnWater(px,pz)){ f.Waypoints.Add(new Vector2(px,pz)); break; }
                        }
                    }
                    break;
                case 1:   // M2 岛间巡逻：已揭示疆域内随机 3-4 个外海水面点
                    float br=_terrain!=null?Mathf.Max(21f,_terrain.RevealBase*0.9f):120f;
                    for(int k=0;k<4;k++)
                    {
                        for(int t=0;t<40;t++)
                        {
                            float rr=Random.Range(20f,br), ang=Random.value*Mathf.PI*2f;
                            float px=s.X+Mathf.Cos(ang)*rr, pz=s.Z+Mathf.Sin(ang)*rr;
                            if(OnWater(px,pz)){ f.Waypoints.Add(new Vector2(px,pz)); break; }
                        }
                    }
                    break;
                default:  // M3 随机坐标巡航：2-3 个随机外海水面点
                    for(int k=0;k<3;k++)
                    {
                        for(int t=0;t<40;t++)
                        {
                            float rr=Random.Range(25f,110f), ang=Random.value*Mathf.PI*2f;
                            float px=s.X+Mathf.Cos(ang)*rr, pz=s.Z+Mathf.Sin(ang)*rr;
                            if(OnWater(px,pz)){ f.Waypoints.Add(new Vector2(px,pz)); break; }
                        }
                    }
                    break;
            }
            if(f.Waypoints.Count==0) f.Waypoints.Add(new Vector2(s.X,s.Z));
        }
        private void MoveToward(ShipEntity s,float tx,float tz,float dt,ref float lookYaw,ref bool hasLook)
        {
            float ox=s.X, oz=s.Z;
            Vector3 dir=(new Vector3(tx,0f,tz)-s.Pos).normalized;
            float sp=SpeedOf(s); if(sp<=0f&&Defs.TryGetValue(s.ShipTypeId,out var pd)) sp=pd.Speed*0.5f;
            if(GM.OceanFlow!=null) sp*=GM.OceanFlow.SailFactor(s.X,s.Z,new Vector2(dir.x,dir.z));
            float nx=s.X+dir.x*sp*30f*dt, nz=s.Z+dir.z*sp*30f*dt;
            float dvx=0f,dvz=0f;
            if(GM.OceanFlow!=null){ var dv=GM.OceanFlow.Drift(nx,nz,dt,0.8f); dvx=dv.x; dvz=dv.y; }
            if(OnWater(nx+dvx,nz+dvz)){ s.X=nx+dvx; s.Z=nz+dvz; }
            else if(OnWater(nx,nz)){ s.X=nx; s.Z=nz; }
            float mvx=s.X-ox, mvz=s.Z-oz;
            if(Mathf.Abs(mvx)+Mathf.Abs(mvz)>1e-4f){ lookYaw=Mathf.Atan2(mvx,mvz)*Mathf.Rad2Deg; hasLook=true; }
        }
        /// <summary>V9.3.8 浏览器探针：船数/敌舰数/编队数/巡航模式/停泊中船数。</summary>
        public string DebugCruiseState()
        {
            int formations=0; var seen=new HashSet<CruiseFormation>();
            int mode=-1;
            foreach(var kv in _formation){ if(seen.Add(kv.Value)) formations++; if(mode<0) mode=kv.Value.Mode; }
            int docked=0, cruised=0;
            for(int i=0;i<S.Ships.Count;i++){ var s=S.Ships[i]; if(_dockWait.ContainsKey(s)) docked++; if(_formation.ContainsKey(s)) cruised++; }
            return "ships="+S.Ships.Count+" enemy="+EnemyShips.Count+" formations="+formations
                +" mode="+(mode>=0?CruiseModeNames[mode]:"-")+" cruising="+cruised+" docked="+docked;
        }

        // ===== V9.2.3 火炮声光 =====
        private void FireFx(Vector3 from, Vector3 to, bool cannon)
        {
            var go=new GameObject("CannonFx");
            go.transform.SetParent(_root,false);
            go.AddComponent<CannonFx>().Begin(from,to,cannon);
        }

        /// <summary>两舰交火：在炮口→目标甲板间生成火炮特效（仅炮船；弓箭战船不生成）。</summary>
        private void SpawnShotFx(ShipEntity s, ShipEntity t)
        {
            Vector3 dir=(t.Pos-s.Pos).normalized;
            Vector3 a=new Vector3(s.X,ShipRestY(s)+0.9f,s.Z)+dir*1.4f;
            Vector3 b=new(t.X,ShipRestY(t)+0.6f,t.Z);
            FireFx(a,b,true);
        }

        /// <summary>火船自爆/大爆炸：在该点生成爆炸特效与音效。</summary>
        private void SpawnExplosion(Vector3 at)
        {
            at.y+=0.8f;
            FireFx(at,at,true);
        }

        // ===== V9.4.7 统一战斗目录接线 =====
        /// <summary>V9.4.7 浏览器回归：把我方军舰一一传送到敌舰旁约 14-22 格（外海），强制进入雷达/射程交战</summary>
        public int ForceNavalBattle()
        {
            if (EnemyShips.Count == 0) return -1;
            int paired = 0;
            for (int i = 0; i < EnemyShips.Count && i < S.Ships.Count; i++)
            {
                var e = EnemyShips[i];
                float tx = e.X, tz = e.Z; bool ok = false;
                for (int k = 0; k < 12; k++)
                {
                    float ang = Random.value * Mathf.PI * 2f;
                    float dist = (14f + Random.value * 8f) * GameConstants.Tile;
                    float cx = e.X + Mathf.Cos(ang) * dist, cz = e.Z + Mathf.Sin(ang) * dist;
                    if (OnWater(cx, cz)) { tx = cx; tz = cz; ok = true; break; }
                }
                if (ok)
                {
                    var s = S.Ships[i]; s.X = tx; s.Z = tz;
                    if (s.View != null) s.View.transform.position = new Vector3(tx, ShipRestY(s), tz);
                    paired++;
                }
            }
            return paired;
        }

        public void RegisterCombat(CombatSystem c)
        {
            foreach (var s in S.Ships)
            {
                if (s == null || s.Hp <= 0f) continue;
                c.Add(CombatSystem.K_SHIP, s, s.X, s.Z, s.Hp, AttackOf(s), RangeOf(s)*GameConstants.Tile, CombatSystem.PlayerKey);
            }
            foreach (var e in EnemyShips)
            {
                if (e == null || e.Hp <= 0f) continue;
                c.Add(CombatSystem.K_SHIP, e, e.X, e.Z, e.Hp, AttackOf(e), RangeOf(e)*GameConstants.Tile, e.FactionId);
            }
        }

        /// <summary>统一目录伤害分发：军舰受击（击沉清理沿用 UpdateShips/UpdateEnemyShips 的 RemoveAll 与视图销毁）</summary>
        public void DamageShip(ShipEntity s, float dmg)
        {
            if (s == null || s.Hp <= 0f || dmg <= 0f) return;
            s.Hp -= dmg;
        }

        public static bool IsCannonType(string id)
        {
            return id == "cannon_ship" || id == "treasure_warship" || id == "destroyer"
                || id == "missile_ship" || id == "aircraft_carrier";
        }

        private void SpawnShotFxTo(ShipEntity s, float tx, float tz)
        {
            Vector3 dir = new Vector3(tx - s.X, 0f, tz - s.Z).normalized;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            Vector3 a = new Vector3(s.X, ShipRestY(s)+0.9f, s.Z) + dir*1.4f;
            Vector3 b = new Vector3(tx, 0.6f, tz);
            FireFx(a, b, true);
        }

        /// <summary>我方军舰开火（V9.4.7 兼容跨类型：舰对舰走原 OurShipHit；军舰/军车/步兵/建筑走统一 Damage）</summary>
        private void OurShipFire(ShipEntity s, CombatTarget ct, float dist)
        {
            if (ct.Ref is ShipEntity target) { OurShipHit(s, target, dist); return; }
            int atk = Mathf.RoundToInt(AttackOf(s)*DamageEff(RangeOf(s), dist/GameConstants.Tile));
            if (atk <= 0) return;
            GM.Combat.Damage(ct, atk);
            if (IsCannonType(s.ShipTypeId))
            {
                GM.Combat.DamageArea(ct.X, ct.Z, 4.5f, Mathf.RoundToInt(atk*0.5f), CombatSystem.KeyOf(s));
                SpawnShotFxTo(s, ct.X, ct.Z);
            }
        }

        /// <summary>敌方军舰开火（兼容跨类型：舰对舰走原 EnemyShipHit；其余走统一 Damage）</summary>
        private void EnemyShipFire(ShipEntity e, CombatTarget ct, float dist)
        {
            if (ct.Ref is ShipEntity target) { EnemyShipHit(e, target, dist); return; }
            int atk = Mathf.RoundToInt(AttackOf(e)*DamageEff(RangeOf(e), dist/GameConstants.Tile));
            if (atk <= 0) return;
            GM.Combat.Damage(ct, atk);
            if (IsCannonType(e.ShipTypeId))
            {
                GM.Combat.DamageArea(ct.X, ct.Z, 4.5f, Mathf.RoundToInt(atk*0.5f), e.FactionId);
                SpawnShotFxTo(e, ct.X, ct.Z);
            }
        }

        // ===== V6.1.5 海战分型：弓箭拦截 / 火炮溅射 / 火船自爆 =====
        // V9.3.3 我方开火：按距离衰减后伤害结算（d≤R 内调用）
        private void OurShipHit(ShipEntity s, ShipEntity target, float dist)
        {
            int atk=Mathf.RoundToInt(AttackOf(s)*DamageEff(RangeOf(s),dist/GameConstants.Tile)); // V9.3.5 世界单位→格口径（20格0%/15格50%/10格100%）
            bool cannon=s.ShipTypeId=="cannon_ship"||s.ShipTypeId=="treasure_warship"
                ||s.ShipTypeId=="destroyer"||s.ShipTypeId=="missile_ship"||s.ShipTypeId=="aircraft_carrier";
            if (s.ShipTypeId=="war_junk" && (target.ShipTypeId=="fire_ship"||target.ShipTypeId=="troop_boat"))
                atk=Mathf.RoundToInt(atk*1.5f);   // 弓箭战船快速拦截火船/运兵
            target.Hp-=atk;
            if (cannon)
                foreach (var e in EnemyShips)
                    if (e!=target && Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(e.X,e.Z))<=4.5f) e.Hp-=Mathf.RoundToInt(atk*0.5f);
            if (cannon) SpawnShotFx(s,target);   // V9.2.3 火炮声光
        }
        private void EnemyShipHit(ShipEntity e, ShipEntity target, float dist)
        {
            int atk=Mathf.RoundToInt(AttackOf(e)*DamageEff(RangeOf(e),dist/GameConstants.Tile)); // V9.3.5 世界单位→格口径
            target.Hp-=atk;
            bool cannon=e.ShipTypeId=="cannon_ship"||e.ShipTypeId=="treasure_warship"
                ||e.ShipTypeId=="destroyer"||e.ShipTypeId=="missile_ship"||e.ShipTypeId=="aircraft_carrier";
            if (cannon)
            {
                foreach (var s in S.Ships)
                    if (s.Military && s!=target && Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(s.X,s.Z))<=4.5f) s.Hp-=Mathf.RoundToInt(atk*0.5f);
                SpawnShotFx(e,target);          // V9.2.3 敌炮声光（可见来袭）
            }
        }
        private void DetonateOurFireShip(ShipEntity s)
        {
            float boom=Mathf.Max(60,AttackOf(s)*3f);
            foreach (var e in EnemyShips)
                if (Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(e.X,e.Z))<=5f) e.Hp-=boom;
            GM.AddEvent("bad","🔥 我军火船冲撞自爆，烈焰覆盖敌舰！");
            SpawnExplosion(new Vector3(s.X,0f,s.Z)); // V9.2.3 爆炸声光
            s.Hp=0;
        }
        private void DetonateEnemyFireShip(ShipEntity e)
        {
            float boom=Mathf.Max(60,AttackOf(e)*3f);
            foreach (var s in S.Ships)
                if (s.Military && Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(s.X,s.Z))<=5f) s.Hp-=boom;
            GM.AddEvent("bad","🔥 敌方火船贴舷自爆，冲撞我舰队！");
            SpawnExplosion(new Vector3(e.X,0f,e.Z)); // V9.2.3 爆炸声光
            e.Hp=0;
        }

        /// <summary>时代切换：我方所有船只升1级（对齐 upgradeShipsByEra）；V9.3.3 航母随等级增挂载机</summary>
        public void UpgradeShipsByEra(int newEra)
        {
            foreach (var s in S.Ships) if (s.Level<3) { s.Level++; s.MaxHp=MaxDurability(s); s.Hp=s.MaxHp; ApplyCarrierAir(s); }
        }

        /// <summary>V9.3.3 航母载机随等级：Lv1 8 机 / Lv2 14 机 / Lv3 22 机（舰载机/舰载直升机/舰载喷气机以挂载体现）</summary>
        private void ApplyCarrierAir(ShipEntity s)
        {
            if (s.ShipTypeId!="aircraft_carrier") return;
            s.CarrierAir = s.Level>=3?22 : s.Level>=2?14 : 8;
        }

        // ===== V9.4.1 航母舰载机实体：自动发射/巡航/俯冲打击/返航回收，随等级数量·半径·伤害成长 =====
        /// <summary>航母战斗分支：目标进入攻击半径（Lv1 8格/Lv2 12格/Lv3 16格）且冷却就绪时弹射出击（在飞≤6架）</summary>
        private void CarrierTick(ShipEntity s, ShipEntity target, float dt)
        {
            if (s.ShipTypeId!="aircraft_carrier" || target==null) return;
            _carrierCd -= dt;
            int lvl=s.Level;
            float range=CarrierStrikeRange(lvl)*GameConstants.Tile;
            float d=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(target.X,target.Z));
            if (_carrierCd<=0f && d<=range && AirborneCount()<6)
            {
                _carrierCd=CarrierStrikeCd(lvl);
                CarrierLaunch(s,target);
                GM.AddEvent("mil","✈ 航母弹射舰载机，锁定"+target.Name+"（射程"+CarrierStrikeRange(lvl)+"格）");
            }
        }

        private int AirborneCount(){ int n=0; for(int i=_strikes.Count-1;i>=0;i--) if(_strikes[i].Phase<4) n++; return n; }

        private void CarrierLaunch(ShipEntity carrier, ShipEntity target)
        {
            int lvl=carrier.Level;
            int active=CarrierAirborneCount(lvl);   // 同时出击 2/3/4
            for(int k=0;k<active;k++)
            {
                // 挂载比例 5:2:1（战斗机为主，间隔出直升机，Lv3 才带喷气机）
                int kind = k%3==1?1 : k%3==2?2 : 0;
                if (kind==2 && lvl<3) kind=0;
                var st=new CarrierStrike{ Kind=kind, Carrier=carrier, Target=target, T=0f, Phase=0,
                    H0=2.5f+(k%2)*0.8f };
                st.View=BuildCarrierPlane(kind);
                st.View.transform.SetParent(_root,false);
                st.View.transform.position=new Vector3(carrier.X, st.H0, carrier.Z);
                _strikes.Add(st);
            }
        }

        /// <summary>驱动所有在飞载机：起飞爬升→巡航→俯冲打击→返航回收（单架生命周期约6-10秒）</summary>
        private void StrikeUpdate(float dt)
        {
            for(int i=_strikes.Count-1;i>=0;i--)
            {
                var st=_strikes[i];
                var carrier=st.Carrier; var target=st.Target;
                bool carrierGone=carrier==null||carrier.Hp<=0||!S.Ships.Contains(carrier);
                if (carrierGone){ if(st.View!=null)Object.Destroy(st.View); _strikes.RemoveAt(i); continue; }
                bool targetGone=target==null||target.Hp<=0||!EnemyShips.Contains(target);
                if (targetGone && st.Phase<3) st.Phase=3;   // 目标沉没→立即返航

                float sp=st.Kind==1?16f:22f;                // 直升机慢、固定翼快（世界单位/秒）
                st.T+=dt;
                var pos=st.View.transform.position;
                var cpos=new Vector3(carrier.X,0f,carrier.Z);
                var tpos=new Vector3(target!=null?target.X:carrier.X,0f,target!=null?target.Z:carrier.Z);
                switch(st.Phase)
                {
                    case 0: // 起飞：从甲板爬升到 14 高
                    {
                        pos.y=Mathf.Lerp(st.H0,14f,Mathf.Clamp01(st.T/0.8f));
                        pos.x=carrier.X; pos.z=carrier.Z;
                        if(st.T>=0.8f){ st.T=0f; st.Phase=1; }
                        break;
                    }
                    case 1: // 巡航：平飞逼近目标上空
                    {
                        var dv=new Vector3(tpos.x-pos.x,0f,tpos.z-pos.z);
                        float len=dv.magnitude;
                        if(len<4f){ st.T=0f; st.Phase=2; break; }
                        var step=dv/len*sp*dt;
                        pos.x+=step.x; pos.z+=step.z; pos.y=14f;
                        break;
                    }
                    case 2: // 俯冲打击：直线下压并逼近目标，命中结算
                    {
                        pos.y=Mathf.Lerp(14f,1.5f,Mathf.Clamp01(st.T/0.5f));
                        pos.x=Mathf.Lerp(pos.x,tpos.x,0.12f);
                        pos.z=Mathf.Lerp(pos.z,tpos.z,0.12f);
                        if(st.T>=0.5f)
                        {
                            if(target!=null&&target.Hp>0)
                            {
                                int dmg=CarrierHitDamage(carrier.Level,Mathf.RoundToInt(AttackOf(carrier)));
                                target.Hp=Mathf.Max(0,target.Hp-dmg);
                                SpawnExplosion(new Vector3(target.X,0f,target.Z));
                                GM.AddEvent("mil","✈ 舰载"+(st.Kind==1?"直升机":st.Kind==2?"喷气机":"战斗机")+"命中"+target.Name+"，造成 "+dmg+" 伤害");
                            }
                            st.T=0f; st.Phase=3;
                        }
                        break;
                    }
                    case 3: // 返航：飞回航母甲板，低空回收
                    {
                        var dv=new Vector3(cpos.x-pos.x,0f,cpos.z-pos.z);
                        float len=dv.magnitude;
                        if(len<3f){ st.T=0f; st.Phase=4; break; }
                        var step=dv/len*sp*dt;
                        pos.x+=step.x; pos.z+=step.z; pos.y=Mathf.Lerp(pos.y,4f,0.2f);
                        break;
                    }
                    default: // 回收完成：销毁
                    {
                        if(st.View!=null)Object.Destroy(st.View);
                        _strikes.RemoveAt(i);
                        continue;
                    }
                }
                float yaw=st.Phase==3
                    ? Mathf.Atan2(cpos.x-pos.x,cpos.z-pos.z)*Mathf.Rad2Deg
                    : Mathf.Atan2(tpos.x-pos.x,tpos.z-pos.z)*Mathf.Rad2Deg;
                st.View.transform.rotation=Quaternion.Slerp(st.View.transform.rotation,Quaternion.Euler(0,yaw,0),0.2f);
                st.View.transform.position=pos;
            }
        }

        /// <summary>三类舰载机模型：0战斗机（灰蓝平直翼）/1直升机（白蓝+主旋翼+尾桨）/2喷气机（白红流线+尾焰）</summary>
        private GameObject BuildCarrierPlane(int kind)
        {
            var go=new GameObject(kind==1?"Heli":(kind==2?"Jet":"Fighter"));
            var body=ShaderHelper.Pbr(kind==2?new Color(0.9f,0.9f,0.92f):new Color(0.55f,0.58f,0.62f),0.3f,0.55f,976+kind,0.5f);
            var accent=ShaderHelper.Pbr(kind==2?new Color(0.85f,0.12f,0.10f):new Color(0.16f,0.42f,0.78f),0.2f,0.5f,980+kind,0.5f);
            var glass=ShaderHelper.Pbr(new Color(0.3f,0.55f,0.75f),0.1f,0.9f,984,0.3f);
            void B(string n,Vector3 pos,Vector3 sc,Material m)
            {
                var b=GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name=n; b.transform.SetParent(go.transform,false);
                b.transform.localPosition=pos; b.transform.localScale=sc;
                b.GetComponent<Renderer>().material=m; Object.Destroy(b.GetComponent<Collider>());
            }
            if (kind==1) // 直升机：机身+尾梁+主旋翼+尾桨+滑橇
            {
                B("Fuselage",new Vector3(0,0.3f,0),new Vector3(0.8f,0.5f,2.2f),body);
                B("Tail",new Vector3(0,0.45f,-1.7f),new Vector3(0.22f,0.22f,1.4f),body);
                B("TailRotor",new Vector3(0,0.55f,-2.5f),new Vector3(0.18f,0.04f,0.5f),accent);
                B("MainRotor",new Vector3(0,0.85f,0.1f),new Vector3(0.12f,0.03f,3.4f),accent);
                B("Cockpit",new Vector3(0,0.45f,0.7f),new Vector3(0.5f,0.25f,0.8f),glass);
                B("SkidL",new Vector3(-0.42f,0.05f,0.6f),new Vector3(0.08f,0.1f,1.4f),accent);
                B("SkidR",new Vector3(0.42f,0.05f,0.6f),new Vector3(0.08f,0.1f,1.4f),accent);
            }
            else if (kind==2) // 喷气机：细长机身+后掠翼+垂尾+尾焰
            {
                B("Fuselage",new Vector3(0,0.25f,0),new Vector3(0.5f,0.4f,3.6f),body);
                B("Nose",new Vector3(0,0.35f,1.9f),new Vector3(0.3f,0.2f,0.6f),body);
                B("WingL",new Vector3(-1.1f,0.3f,-0.1f),new Vector3(2.4f,0.06f,1.1f),body);
                B("WingR",new Vector3(1.1f,0.3f,-0.1f),new Vector3(2.4f,0.06f,1.1f),body);
                B("TailFin",new Vector3(0,0.75f,-1.7f),new Vector3(0.06f,1.0f,0.6f),accent);
                B("Stripe",new Vector3(0,0.32f,-0.4f),new Vector3(0.52f,0.12f,2.2f),accent);
                B("Afterburn",new Vector3(0,0.25f,-2.1f),new Vector3(0.4f,0.4f,0.5f),accent);
                B("Cockpit",new Vector3(0,0.5f,1.1f),new Vector3(0.34f,0.22f,0.8f),glass);
            }
            else // 战斗机：胖机身+平直翼+垂尾
            {
                B("Fuselage",new Vector3(0,0.3f,0),new Vector3(0.7f,0.5f,2.8f),body);
                B("WingL",new Vector3(-0.95f,0.35f,-0.2f),new Vector3(2.0f,0.07f,1.0f),body);
                B("WingR",new Vector3(0.95f,0.35f,-0.2f),new Vector3(2.0f,0.07f,1.0f),body);
                B("TailFin",new Vector3(0,0.8f,-1.3f),new Vector3(0.06f,0.8f,0.5f),accent);
                B("Stripe",new Vector3(0,0.38f,-0.2f),new Vector3(0.72f,0.14f,1.6f),accent);
                B("Cockpit",new Vector3(0,0.55f,0.75f),new Vector3(0.4f,0.26f,0.9f),glass);
            }
            go.transform.localScale=Vector3.one*2.2f;   // 空中可见度
            return go;
        }

        public static readonly string[] LevelNames={"普通","精良","传奇"};
        public string LevelName(ShipEntity s)=>LevelNames[Mathf.Clamp(s.Level-1,0,2)];

        /// <summary>升1级花费（对齐 getShipUpgradeCost：基础cost * 当前等级 * 1.5），满级返回 null</summary>
        public Dictionary<string,int> UpgradeCost(ShipEntity s)
        {
            if (!Defs.TryGetValue(s.ShipTypeId,out var d) || s.Level>=3) return null;
            var cost=new Dictionary<string,int>();
            foreach (var kv in d.Cost) cost[kv.Key]=Mathf.FloorToInt(kv.Value*s.Level*1.5f);
            return cost;
        }

        /// <summary>手动升级单船：普通→精良→传奇，升级后耐久/居住随等级倍率提升（住房由 PopulationSystem 自动重算）</summary>
        public bool UpgradeShip(ShipEntity s)
        {
            var cost=UpgradeCost(s);
            if (cost==null){ GM.AddEvent("bad","该船已达传奇级"); return false; }
            if (!S.CanAfford(cost)){ GM.AddEvent("bad","资源不足，无法升级"+s.Name); return false; }
            S.Pay(cost); s.Level++; s.MaxHp=MaxDurability(s); s.Hp=s.MaxHp;
            ApplyCarrierAir(s);   // V9.3.3 航母载机随等级提升
            GM.AddEvent("good",$"⬆ {s.Name}升级为{LevelName(s)}（居住{s.EffectiveHousing}人）{(s.ShipTypeId=="aircraft_carrier"?"，载机升至 "+s.CarrierAir+" 架":"")}");
            return true;
        }

        /// <summary>解散/凿沉一艘我方船（视图销毁、住房自动减少）</summary>
        public void ScuttleShip(ShipEntity s)
        {
            if (s.View!=null) Object.Destroy(s.View);
            _loadGoal.Remove(s); _dockWait.Remove(s); _formation.Remove(s);   // V9.3.8 解散同步清理运行时态
            S.Ships.Remove(s); S.OceanFleets.Remove(s);
            GM.AddEvent("info","已解散一艘"+s.Name);
        }
    }

    /// <summary>船只点击桥（对齐建筑 BuildingClick，依赖根节点 Collider）</summary>
    public class ShipClick : MonoBehaviour
    {
        public ShipEntity Ship;
        public System.Action<ShipEntity> OnClicked;
        // V9.3.6 点击守卫：与建筑一致，UI/建造面板/放置模式不弹船只信息
        private void OnMouseDown()
        {
            var gm=GameManager.Instance;
            if (gm!=null && gm.BlocksWorldClick()) return;
            OnClicked?.Invoke(Ship);
        }
    }

    /// <summary>
    /// V9.2.3 火炮开火声光（自驱动、到期自毁、不进存档）：
    /// 炮口闪光(自发光Quad+点光源0.12s) + 烟雾(上升扩大淡出~1s) + 弹丸(抛物0.4s纯视觉) + 即时音效。
    /// 伤害仍由 NavalSystem 即时结算，本组件只做表现。
    /// </summary>
    public class CannonFx : MonoBehaviour
    {
        struct Smoke { public Transform T; public Material M; public float Age, Life; }
        Vector3 _a, _b;
        Transform _ball, _flash;
        Light _light;
        float _t, _ft;
        bool _landed;
        readonly List<Smoke> _smoke = new();
        Material _smokeBase, _ballMat;

        public void Begin(Vector3 a, Vector3 b, bool cannon)
        {
            _a=a; _b=b;
            // 炮口闪光（自发光 Quad，到期销毁，不改共享缓存材质）
            var fq=GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(fq.GetComponent<Collider>());
            fq.name="muzzle"; fq.transform.SetParent(transform,false);
            fq.transform.position=a+Vector3.up*0.1f; fq.transform.localScale=Vector3.one*1.3f;
            fq.GetComponent<MeshRenderer>().sharedMaterial=ShaderHelper.Emissive(new Color(1f,0.85f,0.35f),new Color(3f,2f,0.6f));
            _flash=fq.transform;
            var lg=new GameObject("mlight"); lg.transform.SetParent(transform,false); lg.transform.position=a;
            _light=lg.AddComponent<Light>(); _light.type=LightType.Point;
            _light.color=new Color(1f,0.8f,0.45f); _light.intensity=7f; _light.range=16f;
            // 弹丸
            _ballMat=ShaderHelper.Pbr(new Color(0.08f,0.07f,0.06f),0.3f,0.4f,9091,1f,false);
            var bq=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bq.GetComponent<Collider>());
            bq.name="ball"; bq.transform.SetParent(transform,false);
            bq.transform.localScale=Vector3.one*0.45f;
            bq.GetComponent<MeshRenderer>().sharedMaterial=_ballMat;
            _ball=bq.transform; _ball.position=a;
            // 烟雾基底（每团克隆材质以独立淡出）
            _smokeBase=ShaderHelper.Trans(new Color(0.28f,0.28f,0.28f,0.6f),0.6f);
            AddSmoke(a);
            // 即时音效：跨距开火播炮声；原地爆炸(a≈b)跳过炮声、只播爆炸
            if(Vector3.Distance(a,b)>1.5f)
                PixelToCivilization.Audio.AudioManager.I?.PlaySfx("cannon_fire",a,0.95f);
        }

        void AddSmoke(Vector3 p)
        {
            var s=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(transform,false);
            s.transform.position=p; s.transform.localScale=Vector3.one*0.6f;
            var mat=new Material(_smokeBase);
            s.GetComponent<MeshRenderer>().sharedMaterial=mat;
            _smoke.Add(new Smoke{T=s.transform,M=mat,Age=0f,Life=Random.Range(0.9f,1.3f)});
        }

        void Update()
        {
            float dt=Time.deltaTime;
            // 闪光：点光源强度骤衰，0.12s 销毁闪光
            _ft+=dt;
            if(_light!=null) _light.intensity=7f*Mathf.Max(0f,1f-_ft/0.12f);
            if(_ft>=0.12f)
            {
                if(_light!=null){Destroy(_light.gameObject);_light=null;}
                if(_flash!=null){Destroy(_flash.gameObject);_flash=null;}
            }
            else if(_flash!=null && Camera.main!=null)
                _flash.LookAt(Camera.main.transform);
            // 弹丸抛物
            _t+=dt; const float DUR=0.4f;
            float k=Mathf.Clamp01(_t/DUR);
            if(_ball!=null)
            {
                Vector3 p=Vector3.Lerp(_a,_b,k); p.y+=Mathf.Sin(k*Mathf.PI)*2.2f;
                _ball.position=p;
            }
            if(k>=1f && !_landed)
            {
                _landed=true;
                AddSmoke(_b);
                PixelToCivilization.Audio.AudioManager.I?.PlaySfx("cannon_explode",_b,0.8f);
                if(_ball!=null){Destroy(_ball.gameObject);_ball=null;}
            }
            // 烟雾上升扩大淡出
            for(int i=_smoke.Count-1;i>=0;i--)
            {
                var sm=_smoke[i];
                sm.Age+=dt;
                float t01=Mathf.Clamp01(sm.Age/sm.Life);
                sm.T.position+=Vector3.up*dt*1.7f;
                sm.T.localScale=Vector3.one*(0.6f+t01*1.9f);
                if(sm.M!=null){ var c=sm.M.color; c.a=(1f-t01)*0.6f; sm.M.color=c; }
                if(t01>=1f){ Destroy(sm.T.gameObject); Destroy(sm.M); _smoke.RemoveAt(i); }
                else _smoke[i]=sm;
            }
            if(_landed && _light==null && _flash==null && _ball==null && _smoke.Count==0)
            {
                // V9.2.3 修复：_ballMat/_smokeBase 来自 ShaderHelper 全局共享缓存，绝不可 Destroy。
                // 旧实现销毁后缓存仍返回被销毁的"假 null"材质，下一发炮 new Material(source)
                // 即抛 ArgumentNullException: source，表现为 C5/D3 每帧报错、火炮特效永久失效。
                // 各团烟雾是 new Material(_smokeBase) 的私有实例，已在上方淡出时自行销毁。
                _ballMat=null; _smokeBase=null;
                Destroy(gameObject);
            }
        }
    }
}
