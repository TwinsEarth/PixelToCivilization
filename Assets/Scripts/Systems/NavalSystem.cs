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

        // ===== V9.3.3 海上帝阵营（3-5 个敌对阵营，敌舰按阵营着色/命名；运行时随机启用）=====
        static readonly (string name,long color)[] FactionDefs =
        {
            ("红海海盗",0xFF6347L),("黑旗帮",0x4169E1L),("南蛮水师",0x32CD32L),("北洋余部",0x9370DBL),("联合舰队",0xFFD700L)
        };
        readonly List<(string name,long color)> _enemyFactions = new();
        bool _factionsInited;

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
            Add("small_boat","小木船","🛶",1,2,50,2,0.04f,0,0,false,0xB5743C,new(){{"wood",15}});
            Add("medium_boat","帆船","⛵",5,8,100,5,0.035f,0,0,false,0xEBCFA0,new(){{"wood",100},{"stone",30}});
            Add("large_boat","大船","🚢",20,20,200,10,0.025f,0,0,false,0xE05A4E,new(){{"wood",500},{"stone",200},{"gold",100}});
            Add("treasure_ship","宝船","🛳️",50,50,400,20,0.02f,0,0,false,0xFFC23D,new(){{"wood",3000},{"stone",500},{"iron",200},{"gold",1000}});
            // 军用船只：V6.1.1 按四级锚点补居住（运兵10/战船15/火炮20/火船5/宝船战舰50）
            Add("troop_boat","运兵船","🚣",10,10,80,3,0.035f,0,0,true,0x7FA04E,new(){{"wood",150},{"stone",50},{"food",30}},2);
            Add("war_junk","战船","⛵",15,15,150,8,0.04f,15,12,true,0xE05A4E,new(){{"wood",300},{"stone",100},{"iron",30},{"gold",50}},2,"arrow");
            Add("cannon_ship","火炮船","🚢",20,20,250,15,0.03f,40,18,true,0x4E8290,new(){{"wood",500},{"stone",150},{"iron",80},{"gold",100}},3,"cannon");
            Add("fire_ship","火船","🔥",5,5,60,2,0.05f,60,6,true,0xFF7A1E,new(){{"wood",100},{"stone",20},{"iron",10},{"gold",20}},3,"fire");
            Add("treasure_warship","宝船战舰","🛳️",50,50,500,25,0.025f,50,20,true,0xFFC23D,new(){{"wood",3000},{"stone",500},{"iron",300},{"gold",1000}},4,"cannon");
            // ===== V9.3.3 现代舰船（公元1949=游戏年4949 起，军事/交通栏直接替换旧木船；T2-T4 造价锚点）=====
            Add("steamship","轮船","🚢",30,30,220,8,0.05f,0,0,false,0x4A5568,new(){{"wood",100},{"steel",30}},4,"",2);
            Add("oil_tanker","油轮","🛢️",40,40,320,10,0.04f,0,0,false,0x2F4F4F,new(){{"steel",150},{"gold",50}},4,"",2);
            Add("cruise_liner","邮轮","🛳️",60,60,400,12,0.035f,0,0,false,0xE8F0FE,new(){{"steel",250},{"gold",300}},4,"",3);
            Add("destroyer","驱逐舰","🚀",30,30,400,18,0.045f,60,22,true,0x5A7D9A,new(){{"steel",120},{"iron",80},{"gold",100}},4,"cannon",2);
            Add("submarine","潜水艇","🦑",20,20,300,14,0.05f,80,12,true,0x3B4A5A,new(){{"steel",180},{"iron",100},{"gold",120}},4,"cannon",1);
            Add("missile_ship","导弹舰","🚀",40,40,500,20,0.04f,90,34,true,0x6B8E9E,new(){{"steel",200},{"iron",120},{"gold",200}},4,"cannon",3);
            Add("aircraft_carrier","航空母舰","🛫",80,80,900,30,0.03f,120,44,true,0x8FA6AD,new(){{"steel",500},{"iron",300},{"gold",500}},4,"cannon",4);
        }
        private void Add(string id,string name,string icon,int cap,int house,int dur,int def,float speed,
            float atk,float range,bool mil,long color,Dictionary<string,int> cost,int era=0,string atkType="",int sizeCls=0)
        {
            Defs[id]=new ShipDef{Id=id,Name=name,Icon=icon,Capacity=cap,Housing=house,Durability=dur,Defense=def,
                Speed=speed,Attack=atk,Range=range,Military=mil,ColorHex=color,Cost=cost,Era=era,AttackType=atkType,SizeCls=sizeCls};
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
        public float SpeedOf(ShipEntity s)
        {
            if (!Defs.TryGetValue(s.ShipTypeId,out var d)) return 0;
            if (s.Crew==0) return 0;
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
            var fac=RandomFaction();
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

        private ShipEntity SpawnEnemyAt(string t,float ex,float ez)
        {
            var d=Defs[t];
            var fac=RandomFaction();
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
            // V9.3.3 敌舰数量=我方船只数对应（≤全局200上限）：时代2以后、有我方战船时周期遭遇敌方舰队
            bool hasWarship=false;
            foreach (var s in S.Ships) if (s.Military) hasWarship=true;
            _spawnCd-=dt;
            if (S.Era>=2 && hasWarship && _spawnCd<=0)
            {
                int headroom=MaxShips-S.Ships.Count-EnemyShips.Count;
                int target=Mathf.Min(S.Ships.Count,headroom);   // 敌方≤我方数量，且不突破200上限
                if (EnemyShips.Count<target && headroom>0)
                {
                    _spawnCd=20f;
                    if (Random.value<0.6f){ SpawnEnemyShip(); S.NavyBattleActive=true; GM.AddEvent("bad","⚓ 敌方舰队出现！（"+_enemyFactions.Count+" 阵营对峙）"); }
                }
            }
            }catch(System.Exception e){ Debug.LogError("[NAVSTAGE:B] "+e.GetType().Name+": "+e.Message); }
            try{ UpdateOurShips(dt); }
            catch(System.Exception e){ Debug.LogError("[NAVSTAGE:C] "+e.GetType().Name+": "+e.Message); }
            try{ UpdateEnemyShips(dt); }
            catch(System.Exception e){ Debug.LogError("[NAVSTAGE:D] "+e.GetType().Name+": "+e.Message); }
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
                    if(s.View!=null)Object.Destroy(s.View);
                    S.Ships.RemoveAt(i);
                    GM.AddEvent("bad","一艘"+s.Name+"超期服役，已退役（船龄 "+s.Age+" 年）");
                }
            }
        }

        private void UpdateOurShips(float dt)
        {
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
                    // 民用船：围绕家园锚点缓慢圆周巡游，让水面"活"起来
                    float phase=(s.HomeX*0.7f+s.HomeZ*0.5f)+Time.time*0.10f;
                    float rr=9f;
                    float tx=s.HomeX+Mathf.Cos(phase)*rr, tz=s.HomeZ+Mathf.Sin(phase)*rr;
                    float ox=s.X, oz=s.Z;
                    float nx=Mathf.Lerp(s.X,tx,dt*0.6f), nz=Mathf.Lerp(s.Z,tz,dt*0.6f);
                    float dvx=0f,dvz=0f;
                    if(GM.OceanFlow!=null){var dv=GM.OceanFlow.Drift(nx,nz,dt,1.2f);dvx=dv.x;dvz=dv.y;} // 洋流/海风漂流
                    // V6.3.4：巡游目标与洋流叠加后必须仍在水面，否则本帧不移动，杜绝被吹上陆地
                    if(OnWater(nx+dvx,nz+dvz)){ s.X=nx+dvx; s.Z=nz+dvz; }
                    float mvx=s.X-ox, mvz=s.Z-oz;
                    if (Mathf.Abs(mvx)+Mathf.Abs(mvz)>1e-4f){ lookYaw=Mathf.Atan2(mvx,mvz)*Mathf.Rad2Deg; hasLook=true; }
                    }catch(System.Exception ex){ Debug.LogError("[NAV:C3 ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
                }
                else
                {
                    try{ s.AttackCd-=dt; }catch(System.Exception ex){ Debug.LogError("[NAV:C4a ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
                    ShipEntity target=null;
                    try{ target=NearestEnemy(s); }
                    catch(System.Exception ex){ Debug.LogError("[NAV:C4 ship"+si+"] "+ex.GetType().Name+": "+ex.Message); }
                    try{
                    if (target!=null)
                    {
                        float d=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(target.X,target.Z));
                        float ox=s.X,oz=s.Z;
                        bool fire=s.ShipTypeId=="fire_ship";
                        float engage=fire?3.2f:RangeOf(s);   // V9.3.4 火船贴舷自爆；其他军舰在自身射程内开火（射程随等级成长）
                        if (d>engage){ Vector3 dir=(target.Pos-s.Pos).normalized; float sp=SpeedOf(s);
                            // V6.1.9(i) 洋流海风：顺流顺风加速、逆流逆风减速
                            if(GM.OceanFlow!=null) sp*=GM.OceanFlow.SailFactor(s.X,s.Z,new Vector2(dir.x,dir.z));
                            float nx=s.X+dir.x*sp*30*dt, nz=s.Z+dir.z*sp*30*dt;
                            if(OnWater(nx,nz)){ s.X=nx; s.Z=nz; } // V6.1.9 军舰也不得登上陆地
                        }
                        else if (fire){ DetonateOurFireShip(s); }
                        else if (s.AttackCd<=0 && AttackOf(s)>0){ OurShipHit(s,target,d); s.AttackCd=2f; }
                        float mvx=s.X-ox,mvz=s.Z-oz;
                        if (Mathf.Abs(mvx)+Mathf.Abs(mvz)>1e-4f){ lookYaw=Mathf.Atan2(mvx,mvz)*Mathf.Rad2Deg; hasLook=true; }
                    }
                    else { PatrolMove(s,dt,ref lookYaw,ref hasLook); } // V9.2.3 无敌舰：自主巡逻
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
        }

        private void UpdateEnemyShips(float dt)
        {
            for (int i=EnemyShips.Count-1;i>=0;i--)
            {
                var e=EnemyShips[i];
                try{ KeepAtSea(e); } // V6.8.3 敌舰同样永留外海
                catch(System.Exception ex){ Debug.LogError("[NAV:D1 idx"+i+"] "+ex.GetType().Name+": "+ex.Message); }
                ShipEntity target=null;
                try{ target=NearestOurs(e); }
                catch(System.Exception ex){ Debug.LogError("[NAV:D2 idx"+i+"] "+ex.GetType().Name+": "+ex.Message); }
                try{
                if (target!=null)
                {
                    float d=Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(target.X,target.Z));
                    bool eFire=e.ShipTypeId=="fire_ship";
                    float engage=eFire?3.2f:RangeOf(e);   // V9.3.4 敌舰同样：射程内开火（射程随等级成长）、距离衰减
                    if (d>engage){ Vector3 dir=(target.Pos-e.Pos).normalized;
                        float nx=e.X+dir.x*1.2f*dt, nz=e.Z+dir.z*1.2f*dt;
                        if(OnWater(nx,nz)){e.X=nx;e.Z=nz;} } // V6.8.3：敌舰只在外海移动
                    else if (eFire){ DetonateEnemyFireShip(e); }
                    else { e.AttackCd-=dt; if(e.AttackCd<=0){ EnemyShipHit(e,target,d);e.AttackCd=2.5f;} }
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
            S.Ships.RemoveAll(s=>{ if(s.Hp<=0){if(s.View!=null)Object.Destroy(s.View);return true;} return false; });
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

        // ===== V9.3.3 发现距离50格 + 射程内开火 + 距离衰减 =====
        public const float DetectRange = 50f;   // 敌我双向发现距离（硬约束）
        /// <summary>伤害效率：eff=clamp01((R-d)/(R*0.5))——射程R边界0%、半射程50%、半射程内100%（普通炮船R20：20格0%/15格50%/10格100%）</summary>
        public static float DamageEff(float range,float dist)
        {
            if (range<=0f) return 1f;
            return Mathf.Clamp01((range-dist)/(range*0.5f));
        }
        private ShipEntity NearestEnemy(ShipEntity s){ ShipEntity best=null;float bd=DetectRangeOf(s);foreach(var e in EnemyShips){float d=Vector2.Distance(new Vector2(s.X,s.Z),new Vector2(e.X,e.Z));if(d<bd){bd=d;best=e;}}return best; }
        private ShipEntity NearestOurs(ShipEntity e){ ShipEntity best=null;float bd=DetectRangeOf(e);foreach(var s in S.Ships){if(!s.Military)continue;float d=Vector2.Distance(new Vector2(e.X,e.Z),new Vector2(s.X,s.Z));if(d<bd){bd=d;best=s;}}return best; }

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

        // ===== V6.1.5 海战分型：弓箭拦截 / 火炮溅射 / 火船自爆 =====
        // V9.3.3 我方开火：按距离衰减后伤害结算（d≤R 内调用）
        private void OurShipHit(ShipEntity s, ShipEntity target, float dist)
        {
            int atk=Mathf.RoundToInt(AttackOf(s)*DamageEff(RangeOf(s),dist));
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
            int atk=Mathf.RoundToInt(AttackOf(e)*DamageEff(RangeOf(e),dist));
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
            S.Ships.Remove(s); S.OceanFleets.Remove(s);
            GM.AddEvent("info","已解散一艘"+s.Name);
        }
    }

    /// <summary>船只点击桥（对齐建筑 BuildingClick，依赖根节点 Collider）</summary>
    public class ShipClick : MonoBehaviour
    {
        public ShipEntity Ship;
        public System.Action<ShipEntity> OnClicked;
        private void OnMouseDown()=>OnClicked?.Invoke(Ship);
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
