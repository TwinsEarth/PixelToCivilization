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
        public float RangeOf(ShipEntity s)
        {
            if (!Defs.TryGetValue(s.ShipTypeId,out var d)) return 10f;
            return d.Range*(1f+(s.Level-1)*0.15f);
        }
        // V9.3.4 等级成长：发现范围/自动搜敌雷达随等级 +15%/级（Lv1=50、Lv2=57.5、Lv3=65）
        public float DetectRangeOf(ShipEntity s) => DetectRange*(1f+(s.Level-1)*0.15f);
        // V9.4.3 我方远距雷达：船型雷达×(1+0.15lv)（战船档200格：Lv1=200、Lv2=230、Lv3=260；驱逐280/导弹320/航母380 更远，小艇120 更近）
        public float MyDetectRangeOf(ShipEntity s)
        {
            if (!Defs.TryGetValue(s.ShipTypeId,out var d)) return 200f;
            return d.Radar*(1f+(s.Level-1)*0.15f);
        }
        // V9.4.3 敌方雷达：100格基线×(1+0.15lv)，大舰(SizeCls>=3)加20（Lv1=100/115/130…）
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
                for(float rr=10f;rr<=160f && !sea;rr+=10f)
                    for(int a=0;a<24;a++)
                    {
                        float ang=a/24f*Mathf.PI*2f;
                        float cx=Mathf.Cos(ang)*rr, cz=Mathf.Sin(ang)*rr;
                        if(_terrain.IsOceanWater(cx,cz)&&_terrain.InsideFrontier(cx,cz)){ ox=cx; oz=cz; sea=true; break; }
                    }
            }
            if(!sea){ GM.AddEvent("warn","Debug 海战演示：附近没有外海"); return null; }
            var own=SpawnInitialShip(ModernEra?"destroyer":"cannon_ship",ox,oz);
            if(own==null) return null;
            // 敌炮船落在我方 6~24 格内，立即接敌
            float a2=Random.value*Mathf.PI*2f, rr2=6f+Random.value*18f;
            var en=SpawnEnemyShip(); en.X=ox+Mathf.Cos(a2)*rr2; en.Z=oz+Mathf.Sin(a2)*rr2;
            if(en.View!=null) en.View.transform.position=new Vector3(en.X,0.1f,en.Z);
            GM.AddEvent("good","⚔ Debug 海战演示：我方 "+own.Name+" 遭遇 "+en.Name+"（"+Mathf.RoundToInt(rr2)+" 格）");
            S.NavyBattleActive=true;
            return own;
        }

        /// <summary>V9.4.5 敌舰确定性镜像：我方每 3 秒内、按我方在编数等比例补齐敌舰；每批≤5、每 0.5s 一批；敌我同型同数量（用户条款：敌方数量跟随我方）</summary>
        public void MirrorEnemyFleet()
        {
            if(GM==null||S==null) return;
            EnsureFactions();
            int mine=S.Ships.Count, enemies=EnemyShips.Count;
            int deficit=enemies<mine? mine-enemies : 0;
            if(deficit<=0) return;
            int batch=Mathf.Min(5,deficit);
            for(int i=0;i<batch;i++) SpawnEnemyShip();
        }

        /// <summary>V9.4.7 统一战斗目录接线：海军全部注册为"军舰"作战单位，敌我双方法射统一走 CombatSystem（保留旧 LiveFire 兜底）</summary>
        public void RegisterCombat()
        {
            if(GM==null||GM.Combat==null) return;
            foreach(var s in S.Ships)
            {
                if(s.FactionId==null) s.FactionId="PLAYER";
                if(s.FactionColor==0) s.FactionColor=0xFFD700L;
                GM.Combat.RegisterUnit("ship:"+s.GetHashCode(), "ship", s.X, s.Z, 1f, s.Military?"PLAYER":"PLAYER", s, null, MaxDurability(s));
            }
            foreach(var e in EnemyShips)
            {
                if(e.FactionId==null) e.FactionId="enemy";
                GM.Combat.RegisterUnit("ship:"+e.GetHashCode(), "ship", e.X, e.Z, 1f, e.FactionId, e, null, MaxDurability(e));
            }
        }

        /// <summary>V9.4.7 我方军舰开火（统一战斗目录，带等级成长与满编战力；兼容旧 LiveFire 调用）</summary>
        public void OurShipFire(ShipEntity s, ShipEntity target, float dt)
        {
            if(s==null||target==null) return;
            if(GM!=null && GM.Combat!=null) { GM.Combat.ShipFire(s,target,dt); return; }
            LiveFire(s,target);
        }
        /// <summary>V9.4.7 敌方军舰开火（同上）</summary>
        public void EnemyShipFire(ShipEntity s, ShipEntity target, float dt)
        {
            if(s==null||target==null) return;
            if(GM!=null && GM.Combat!=null) { GM.Combat.EnemyShipFire(s,target,dt); return; }
            LiveFire(s,target);
        }
        /// <summary>V9.4.7 统一伤害结算：被击中扣血；沉没时调用 GM.Combat.OnShipSunk 释放战利品与广播</summary>
        public void DamageShip(ShipEntity s, int dmg)
        {
            if(s==null) return;
            s.Hp-=dmg;
            if(s.Hp<=0) {
                if(s.Side=="enemy"){ EnemyShips.Remove(s); GM.AddEvent("good","⛵ 击沉敌方 "+s.Name+"！"); }
                else { S.Ships.Remove(s); GM.AddEvent("bad","💥 我方 "+s.Name+" 被击沉！"); }
                if(s.View!=null) Object.Destroy(s.View);
                if(GM!=null && GM.Combat!=null) GM.Combat.OnShipSunk(s);
            }
        }

        /// <summary>旧版实弹开火（无统一战斗目录时的兜底）：投射物/即时命中 + 火炮声光</summary>
        void LiveFire(ShipEntity s, ShipEntity t)
        {
            if(t==null||t.Hp<=0) return;
            int dmg=AttackOf(s);
            if(s.AttackType=="cannon")
            {
                // 抛物弹 + 炮口闪 + 点光 0.12s + 烟雾淡出 + 即时音效（CannonFx 自驱动，见下）
                if(s.View!=null && WeaponFxSystem.Instance!=null)
                {
                    Vector3 from=s.View.transform.position+new Vector3(0,1.2f,0);
                    Vector3 to=t.View!=null?t.View.transform.position:new Vector3(t.X,0.1f,t.Z);
                    WeaponFxSystem.Instance.CannonFx(from,to,0xFFAA33L);
                }
                t.Hp-=dmg;
            }
            else if(s.AttackType=="fire")
            {
                float d=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(t.X,t.Z));
                if(d<=s.Range+4f) t.Hp-=dmg;
            }
            else t.Hp-=dmg; // arrow 等：直击
            if(t.Hp<=0) DamageShip(t,0);
        }

        // ===== V9.4.1 航母舰载机 =====
        void ApplyCarrierAir(ShipEntity s)
        {
            if(s.ShipTypeId=="aircraft_carrier") s.CarrierAir=s.Level>=3?22:s.Level>=2?14:8;
        }
        int CarrierTypeFor(ShipEntity s,int idx)
        {
            // 挂载比例 5:2:1：idx 序 0-4=战斗机、5-6=直升机、7(仅Lv3)=喷气机；Lv3 总数22、Lv2 14、Lv1 8
            int n=s.CarrierAir; if(n<=0) return 0;
            int jet= s.Level>=3? n/8 : 0;
            int heli= n/4;
            if(idx < n-jet-heli) return 0;
            if(idx < n-jet) return 1;
            return 2;
        }

        /// <summary>航母自动弹射：等级决定同时架数与攻击半径；无目标时巡航盘旋，有目标俯冲攻击后返航回收</summary>
        void UpdateCarrierStrikes(float dt)
        {
            if(GM==null||S==null) return;
            for(int i=_strikes.Count-1;i>=0;i--)
            {
                var st=_strikes[i]; if(st==null||st.View==null){ _strikes.RemoveAt(i); continue; }
                st.T+=dt;
                ShipEntity target=null;
                // 目标失效/被击沉：重选
                if(st.Target==null||st.Target.Hp<=0||!S.Ships.Contains(st.Target)){
                    float best=CarrierStrikeRange(st.Carrier.Level)*GameConstants.Tile*2f; // 格→世界
                    foreach(var e in EnemyShips){
                        float d=Vector2.Distance(new Vector2(st.Carrier.X,st.Carrier.Z),new Vector2(e.X,e.Z));
                        if(e.Hp>0 && d<best){ best=d; target=e; }
                    }
                    st.Target=target;
                } else target=st.Target;

                Vector3 pos=st.View.transform.position;
                if(st.Phase==0) // 起飞：爬升
                {
                    pos.y=Mathf.MoveTowards(pos.y,st.H0+8f,dt*30f);
                    if(pos.y>=st.H0+7.9f) st.Phase=1;
                }
                else if(st.Phase==1) // 巡航：环绕母舰或接近目标
                {
                    if(target!=null){
                        Vector3 tgt=new Vector3(target.X,target.Z,0f)+new Vector3(0,0,0.1f);
                        float step=dt*18f;
                        if(Vector3.Distance(pos,tgt)>3f) pos=Vector3.MoveTowards(pos,tgt,step);
                        else st.Phase=2;
                    } else {
                        float ang=st.T*0.6f;
                        Vector3 orbit=new Vector3(Mathf.Cos(ang)*6f,st.H0+8f,Mathf.Sin(ang)*6f);
                        Vector3 baseP=new Vector3(st.Carrier.X,0,st.Carrier.Z);
                        pos=Vector3.MoveTowards(pos,baseP+orbit,dt*8f);
                    }
                }
                else if(st.Phase==2) // 俯冲攻击
                {
                    if(target!=null){
                        Vector3 tgt=new Vector3(target.X,target.Z,0f);
                        pos=Vector3.MoveTowards(pos,tgt,dt*40f);
                        if(Vector3.Distance(pos,tgt)<2f){
                            int dmg=CarrierHitDamage(st.Carrier.Level,st.Carrier.BaseAttack);
                            DamageShip(target,dmg);
                            if(GM!=null) GM.AddEvent("good","✈ 舰载机命中 "+target.Name+" -"+dmg);
                            st.Phase=3;
                        }
                    } else st.Phase=3;
                }
                else if(st.Phase==3) // 返航
                {
                    Vector3 deck=new Vector3(st.Carrier.X,st.H0+1f,st.Carrier.Z);
                    pos=Vector3.MoveTowards(pos,deck,dt*30f);
                    if(Vector3.Distance(pos,deck)<1.5f){ st.Phase=4; }
                }
                st.View.transform.position=pos;
                if(st.Phase==4){ Object.Destroy(st.View); _strikes.RemoveAt(i); }
            }

            // 弹射新机（冷却节流）
            _carrierCd-=dt;
            if(_carrierCd>0) return;
            foreach(var c in S.Ships)
            {
                if(c.ShipTypeId!="aircraft_carrier"||c.Hp<=0) continue;
                int airborne=0; foreach(var st in _strikes) if(st.Carrier==c) airborne++;
                if(airborne>=CarrierAirborneCount(c.Level)) continue;
                // 只在有敌对目标时出击
                ShipEntity tgt=null; float best=CarrierStrikeRange(c.Level)*GameConstants.Tile*2f;
                foreach(var e in EnemyShips){ float d=Vector2.Distance(new Vector2(c.X,c.Z),new Vector2(e.X,e.Z)); if(e.Hp>0&&d<best){best=d;tgt=e;} }
                if(tgt==null) continue;
                _carrierCd=CarrierStrikeCd(c.Level);
                var kind=CarrierTypeFor(c,airborne);
                var v=EntityViewFactory.SpawnVehicle("Strike_"+c.Name,_root,PixelToCivilization.Actors.VehicleKind.Fighter,Color.yellow,0.8f,kind==2?"jet":kind==1?"heli":"fighter");
                if(v==null) continue;
                v.transform.position=new Vector3(c.X,0.5f,c.Z);
                _strikes.Add(new CarrierStrike{View=v,Kind=kind,Carrier=c,Target=tgt,T=0,Phase=0,H0=0.5f});
            }
        }

        // ===== V9.4.6 船体积碰撞（同一格挤压分离；军船更大判距更宽）=====
        void SeparateShips(float dt)
        {
            _sepTimer-=dt; if(_sepTimer>0) return; _sepTimer=0.25f;
            var all=new List<ShipEntity>(); all.AddRange(S.Ships); all.AddRange(EnemyShips);
            for(int i=0;i<all.Count;i++) for(int j=i+1;j<all.Count;j++)
            {
                var a=all[i]; var b=all[j]; if(a==null||b==null) continue;
                float dx=a.X-b.X, dz=a.Z-b.Z; float d2=dx*dx+dz*dz;
                float radA=a.Military?2.4f:1.8f, radB=b.Military?2.4f:1.8f;
                float min=radA+radB; if(d2>=min*min||d2<0.0001f) continue;
                float d=Mathf.Sqrt(d2), push=(min-d)*0.5f;
                float nx=dx/d, nz=dz/d;
                a.X+=nx*push; a.Z+=nz*push; b.X-=nx*push; b.Z-=nz*push;
            }
            foreach(var s in all){ if(s.View!=null) s.View.transform.position=new Vector3(s.X,ShipRestY(s),s.Z); }
        }

        // ===== 主循环 =====
        public override void Tick(float dt)
        {
            if(GM==null||S==null||!S.Initialized) return;
            float sdt=dt;
            // 每帧：位置贴合、外海约束、船员同步、LOD、事件
            foreach(var s in S.Ships)
            {
                if(s.View==null) continue;
                KeepAtSea(s);
                s.View.transform.position=new Vector3(s.X,ShipRestY(s),s.Z);
                if(s.View.transform.localScale.x<0.01f) s.View.transform.localScale=Vector3.one*(s.Military?1.6f:1.2f)*(1f+(s.Level-1)*0.1f);
            }
            foreach(var e in EnemyShips)
            {
                if(e.View==null) continue;
                KeepAtSea(e);
                e.View.transform.position=new Vector3(e.X,ShipRestY(e),e.Z);
            }

            // 敌舰生成（V9.4.5 确定性镜像在 GM 主循环内 3 秒调用；此处为传统增量：每 6-10s 一舰，上限 200）
            _spawnCd-=sdt;
            if(_spawnCd<=0 && S.Ships.Count+EnemyShips.Count<MaxShips)
            {
                _spawnCd=6f+Random.value*4f;
                if(Random.value<0.35f) SpawnEnemyShip();
            }

            // 载人航点节流（0.5s）
            _loadCd-=sdt;
            if(_loadCd<=0){ _loadCd=0.5f; UpdateLoadGoals(); }

            // 敌我交战（统一战斗目录优先；旧式兜底）
            if(GM.Combat!=null){ GM.Combat.NavalTick(dt); }
            else { LegacyNavalCombat(sdt); }

            // 编队：巡航模式切换（10 现实分钟）+ 低频重组（20s）
            _cruiseSwitchCd-=Time.unscaledDeltaTime;
            if(_cruiseSwitchCd<=0){ _cruiseSwitchCd=CruiseSwitchInterval; SwitchCruiseMode(); }
            _reformTimer-=sdt; if(_reformTimer<=0){ _reformTimer=20f; ReformFormations(); }
            UpdateFormations(dt);

            // 巡逻（无战事时）
            UpdatePatrol(dt);

            // 载人态：靠岸等待 & 移动
            UpdateLoading(dt);

            // 航母舰载机
            UpdateCarrierStrikes(dt);

            // 体积碰撞分离
            SeparateShips(dt);

            // 清扫：死亡/无视图残留
            for(int i=S.Ships.Count-1;i>=0;i--){ if(S.Ships[i].Hp<=0){ var dead=S.Ships[i]; S.Ships.RemoveAt(i); if(dead.View!=null) Object.Destroy(dead.View); } }
            for(int i=EnemyShips.Count-1;i>=0;i--){ if(EnemyShips[i].Hp<=0){ var dead=EnemyShips[i]; EnemyShips.RemoveAt(i); if(dead.View!=null) Object.Destroy(dead.View); } }
        }

        void UpdateLoadGoals()
        {
            if(GM==null||S==null) return;
            // 统计空闲人口（不干活、不在船上）
            int idle=0; foreach(var p in S.People){ if(p!=null && !p.OnShip && p.HomeX<0) idle++; }
            // 载人目标：每个低员船找一个最密集闲人点（V9.3.5：人员<50% 优先载人；V9.3.11 载人次之）
            _loadGoal.Clear();
            foreach(var s in S.Ships)
            {
                if(s==null||s.View==null) continue;
                if(BattlePriority(s)) continue;       // 战斗态不载人
                if(s.Crew>=Capacity(s)) continue;
                Vector2 bestP=Vector2.zero; float bestD=float.MaxValue;
                foreach(var p in S.People)
                {
                    if(p==null||p.OnShip) continue;
                    float d=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(p.X,p.Z));
                    if(d<bestD && d<60f){ bestD=d; bestP=new Vector2(p.X,p.Z); }
                }
                if(bestD<float.MaxValue) _loadGoal[s]=bestP;
            }
        }

        void UpdateLoading(float dt)
        {
            if(GM==null||S==null) return;
            foreach(var s in S.Ships)
            {
                if(s==null||s.View==null) continue;
                if(s.Crew>=Capacity(s)) continue;
                if(BattlePriority(s)) continue;
                if(!_loadGoal.TryGetValue(s,out var goal)) continue;
                // 靠岸等待：到点停 6s 等 EmbarkSystem 吸附；期间不移动
                if(_dockWait.TryGetValue(s,out float w))
                {
                    _dockWait[s]=w-dt;
                    if(w-dt<=0) _dockWait.Remove(s);
                    continue;
                }
                float d=Vector2.Distance(new Vector2(s.X,s.Z),goal);
                if(d<4f){ _dockWait[s]=6f; continue; }
                // 航行去载人点（航速 0.4~1.0×）
                Vector2 dir=(goal-new Vector2(s.X,s.Z)).normalized;
                float sp=SpeedOf(s)*0.6f;
                s.X+=dir.x*sp*dt; s.Z+=dir.y*sp*dt;
                KeepAtSea(s);
                if(s.View!=null) s.View.transform.position=new Vector3(s.X,ShipRestY(s),s.Z);
            }
        }

        // ===== 巡航/编队（V9.3.8/9.3.9）=====
        void SwitchCruiseMode()
        {
            foreach(var kv in _formation) kv.Value.Mode=(kv.Value.Mode+1)%3;
            // 阵型随模式轮换
            foreach(var kv in _formation)
            {
                int f=(kv.Value.Mode==0)?0:(kv.Value.Mode==1)?(kv.Value.Formation+1)%3:(kv.Value.Formation+2)%5;
                kv.Value.Formation=f;
            }
        }
        void ReformFormations()
        {
            if(GM==null||S==null) return;
            _formation.Clear();
            var wars=new List<ShipEntity>();
            foreach(var s in S.Ships) if(s.Military && s.Hp>0) wars.Add(s);
            if(wars.Count<3) return;
            // 分桶 3-7 艘
            int size=Random.Range(3,8);
            int n=Mathf.Min(size,wars.Count);
            var group=wars.GetRange(0,n);
            var leader=group[0];
            var f=new CruiseFormation(); f.Members.AddRange(group); f.Mode=Random.Range(0,3);
            f.Formation=Random.Range(0,5); f.Inited=false;
            _formation[leader]=f;
            // 生成绕大陆航点（简化：8 个环形点）
            float r=40f;
            for(int i=0;i<8;i++){ float a=i/8f*Mathf.PI*2f; f.Waypoints.Add(new Vector2(leader.X+Mathf.Cos(a)*r, leader.Z+Mathf.Sin(a)*r)); }
            f.WpIdx=0;
        }
        void UpdateFormations(float dt)
        {
            if(_formation.Count==0) return;
            foreach(var kv in _formation)
            {
                var f=kv.Value; if(f.Members.Count<1) continue;
                var lead=f.Members[0]; if(lead==null||lead.View==null) continue;
                // 有敌情时解散编队进入战斗（由 Combat 驱动，此处只管理巡航）
                bool combat=false;
                if(GM!=null && GM.Combat!=null && GM.Combat.AnyNavalThreat(lead)) combat=true;
                if(combat) continue;
                // 巡航模式
                if(f.Waypoints.Count>0)
                {
                    Vector2 wp=f.Waypoints[f.WpIdx];
                    float d=Vector2.Distance(new Vector2(lead.X,lead.Z),wp);
                    if(d<5f){ f.WpIdx=(f.WpIdx+1)%f.Waypoints.Count; wp=f.Waypoints[f.WpIdx]; }
                    Vector2 dir=(wp-new Vector2(lead.X,lead.Z)).normalized;
                    float sp=SpeedOf(lead);
                    lead.X+=dir.x*sp*dt; lead.Z+=dir.y*sp*dt;
                    KeepAtSea(lead);
                    if(lead.View!=null) lead.View.transform.position=new Vector3(lead.X,ShipRestY(lead),lead.Z);
                    f.LeadYaw=Mathf.Atan2(dir.x,dir.y);
                }
                // 成员跟随（阵型偏移）
                for(int i=1;i<f.Members.Count;i++)
                {
                    var m=f.Members[i]; if(m==null||m.View==null) continue;
                    Vector2 off=FormationOffset(i,f);
                    float ca=Mathf.Cos(f.LeadYaw), sa=Mathf.Sin(f.LeadYaw);
                    Vector2 target=new Vector2(lead.X+off.x*ca-off.y*sa, lead.Z+off.x*sa+off.y*ca);
                    float md=Vector2.Distance(new Vector2(m.X,m.Z),target);
                    if(md>2f)
                    {
                        Vector2 mdir=(target-new Vector2(m.X,m.Z)).normalized;
                        float msp=SpeedOf(m);
                        m.X+=mdir.x*msp*dt; m.Z+=mdir.y*msp*dt;
                        KeepAtSea(m);
                        if(m.View!=null) m.View.transform.position=new Vector3(m.X,ShipRestY(m),m.Z);
                    }
                }
            }
        }

        // ===== 巡逻（无敌舰时沿外海航点环航；V9.2.3）=====
        void UpdatePatrol(float dt)
        {
            if(GM==null||S==null) return;
            bool anyThreat=false;
            if(GM.Combat!=null) anyThreat=GM.Combat.AnyNavalThreat();
            if(anyThreat) return;
            foreach(var s in S.Ships)
            {
                if(s==null||!s.Military||s.View==null) continue;
                if(!_patrol.TryGetValue(s,out var rt))
                {
                    rt=new PatrolRoute();
                    float r=30f+Random.value*30f;
                    for(int i=0;i<8;i++){ float a=i/8f*Mathf.PI*2f; rt.Pts.Add(new Vector2(s.X+Mathf.Cos(a)*r, s.Z+Mathf.Sin(a)*r)); }
                    rt.Idx=0; rt.Inited=true;
                    _patrol[s]=rt;
                }
                Vector2 wp=rt.Pts[rt.Idx];
                float d=Vector2.Distance(new Vector2(s.X,s.Z),wp);
                if(d<4f){ rt.Idx=(rt.Idx+1)%rt.Pts.Count; wp=rt.Pts[rt.Idx]; }
                Vector2 dir=(wp-new Vector2(s.X,s.Z)).normalized;
                float sp=SpeedOf(s)*0.5f;
                s.X+=dir.x*sp*dt; s.Z+=dir.y*sp*dt;
                KeepAtSea(s);
                if(s.View!=null) s.View.transform.position=new Vector3(s.X,ShipRestY(s),s.Z);
            }
        }

        // ===== 旧式海战（统一战斗目录不可用时兜底）=====
        void LegacyNavalCombat(float dt)
        {
            if(GM==null||S==null) return;
            // 我方：锁定最近敌舰，射程内开火
            foreach(var s in S.Ships)
            {
                if(s==null||!s.Military||s.Hp<=0) continue;
                ShipEntity tgt=null; float best=CombatDetectRangeOf(s)*GameConstants.Tile;
                foreach(var e in EnemyShips)
                {
                    if(e==null||e.Hp<=0) continue;
                    float d=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(e.X,e.Z));
                    if(d<best){ best=d; tgt=e; }
                }
                if(tgt==null) continue;
                float rng=RangeOf(s)*GameConstants.Tile;
                float dd=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(tgt.X,tgt.Z));
                if(dd<=rng)
                {
                    s.AttackCooldown-=dt;
                    if(s.AttackCooldown<=0){ s.AttackCooldown=1.5f; OurShipFire(s,tgt,dt); }
                }
                else // 追击
                {
                    Vector2 dir=(new Vector2(tgt.X,tgt.Z)-new Vector2(s.X,s.Z)).normalized;
                    float sp=SpeedOf(s)*1.2f;
                    s.X+=dir.x*sp*dt; s.Z+=dir.y*sp*dt;
                    KeepAtSea(s);
                    if(s.View!=null) s.View.transform.position=new Vector3(s.X,ShipRestY(s),s.Z);
                }
            }
            // 敌方：100 格雷达锁我，50 格靠近，射程内开火
            foreach(var e in EnemyShips)
            {
                if(e==null||e.Hp<=0) continue;
                ShipEntity tgt=null; float best=EnemyDetectRangeOf(e)*GameConstants.Tile;
                foreach(var s in S.Ships)
                {
                    if(s==null||s.Hp<=0) continue;
                    float d=Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(s.X,s.Z));
                    if(d<best){ best=d; tgt=s; }
                }
                if(tgt==null) continue;
                float rng=RangeOf(e)*GameConstants.Tile;
                float dd=Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(tgt.X,tgt.Z));
                if(dd<=rng)
                {
                    e.AttackCooldown-=dt;
                    if(e.AttackCooldown<=0){ e.AttackCooldown=2f; EnemyShipFire(e,tgt,dt); }
                }
                else if(dd<50f*GameConstants.Tile) // 50 格靠近
                {
                    Vector2 dir=(new Vector2(tgt.X,tgt.Z)-new Vector2(e.X,e.Z)).normalized;
                    float sp=SpeedOf(e);
                    e.X+=dir.x*sp*dt; e.Z+=dir.y*sp*dt;
                    KeepAtSea(e);
                    if(e.View!=null) e.View.transform.position=new Vector3(e.X,ShipRestY(e),e.Z);
                }
            }
        }
    }
}
