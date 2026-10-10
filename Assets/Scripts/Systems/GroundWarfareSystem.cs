using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.7.2 地面作战部队（V9.5.4 分代 + V9.7.2 十维参数与速度口径统一）：
    /// 公元1949（游戏年4949）前为【古典】骑兵 / 3人一组列方阵兵 / 马拉战车；
    /// 1949 起【直接替换】为【现代】坦克 / 装甲车 / 导弹车（与船只"直接替换而非解锁"同规则）。
    /// V9.7.2 三项修复：
    ///  1) 速度口径统一——实际移动速度 = 类型基础速度（世界单位/秒，骑兵快/方阵慢）× 等级成长系数，
    ///     旧 ClampedGroundSpeed 无视类型基础速度（只按等级、Lv1=0.1格/秒）且与属性面板 u.Speed 脱节；
    ///  2) 生成位置——新增列装驻留宽限 GraceT（20s 原地驻守只索敌不巡航），且巡航半径缩小到 20–45，
    ///     旧实现一造出来即被 40–100 环随机航点拉走（"不在原地，被拉到某坐标"）；
    ///  3) 十维参数——速度/耐久/攻击/防御/射程/射速/载人量/体积/军衔/智能等级，随等级差异化成长。
    /// 运行时态不进存档；陆地约束 IsStaticLand 禁行（静态基准水位，不受月度潮汐动态水位影响）；古典三型造价走木/粮/金（1949 前无钢）。
    /// </summary>
    public class GroundWarfareSystem : GameSystemBase
    {
        public class GroundDef
        {
            public string Id, Name, Icon, ColorHex;
            public float Speed;          // 基础移动速度（世界单位/秒，类型差异；与人员1.2、车辆3-8同口径）
            public float Durability;     // 基础耐久（最大HP）
            public float Attack;         // 基础攻击
            public float Defense;        // 基础防御（减伤：实际受击=伤害×100/(100+防御)）
            public int Range;            // 基础射程（格）
            public float FireInterval;   // 基础攻击间隔（秒/发；越小射速越快）
            public int Capacity;         // 载人量（装甲运兵/战车乘员）
            public float Footprint;      // 体积/占地（世界单位，决定碰撞半径与阵型间距）
            public string Rank;          // 军衔
            public int Intel;            // 智能等级（1-5，决定自主索敌/编队/追击能力）
            public int CostSteel, CostGold, CostWood, CostFood;
            public bool Modern;          // true=坦克/装甲车/导弹车；false=骑兵/列方阵兵/马拉战车
            public string Desc = "";
        }

        /// <summary>现代型标识集合（1949 后列装）</summary>
        static readonly HashSet<string> ModernTypes = new(){ "tank","apc","missile_vehicle" };
        /// <summary>当前时代可列装的地面部队（军事栏直接遍历；古典↔现代按年份替换，不灰显不叠加）</summary>
        public List<GroundDef> AvailableDefs
        {
            get
            {
                var list=new List<GroundDef>();
                foreach(var d in Defs.Values) if(d.Modern == (S.Year>=4949)) list.Add(d);
                return list;
            }
        }

        public class GroundUnit
        {
            public string TypeId, Name, Side;
            public float X, Z;
            public int Level;
            // 十维运行时（含基础基数，供等级成长重算）
            public float BaseSpeed;                 // 基础速度（世界单位/秒）
            public float BaseMaxHp, MaxHp, Hp;
            public float BaseAttack, Attack;
            public float BaseDefense, Defense;
            public int BaseRange, Range;
            public float BaseFire;                  // 基础攻击间隔
            public int Capacity;
            public float Footprint;
            public string Rank;
            public int Intel;
            public string FactionId; public long FactionColor;
            public GameObject View;
            public float AttackCd, ThinkCd;
            public int Group;                       // 编队组号（我方 0-31 / 敌方 32-63，阵营分离）
            public float RX, RZ;                    // 巡航随机航点
            public bool HasRoute;
            public int Kills;                       // 战绩（每 2 杀战功升 1 级）
            public bool Lifted;                     // 远程投送中（AirLiftSystem 接管，跳过 AI）
            public float GraceT;                    // V9.7.2 列装驻留宽限（秒）：原地驻守只索敌不巡航
        }

        public const int MaxGround = 60;
        public const float RadarGround = 60f;               // 基础发现距离（格）
        public const float GraceSeconds = 20f;               // V9.7.2 列装驻留宽限
        // V9.7.2 等级成长系数（参数随等级差异化，耐久线性防指数膨胀）
        public const float LvSpeed = 0.08f;                  // 速度 +8%/级
        public const float LvHp = 0.12f;                     // 耐久 +12%/级
        public const float LvAtk = 0.08f;                    // 攻击 +8%/级
        public const float LvDef = 0.10f;                     // 防御 +10%/级
        public const float LvRadar = 0.06f;                   // 雷达 +6%/级
        public const float LvFire = 0.03f;                    // 攻击间隔 -3%/级（射速变快）
        public const float MinFireInterval = 0.4f;
        public const int MaxRangeAdd = 5;                     // 射程最多 +5 格
        public const int GroundRangeHardCap = 50;             // 地面单位射程硬上限 50 格

        public readonly Dictionary<string,GroundDef> Defs = new();
        public readonly List<GroundUnit> Ours = new();
        public readonly List<GroundUnit> Enemies = new();
        public bool Active;

        // 地面部队敌对阵营（3-5，颜色与海军五阵营一致便于识别）
        static readonly (string name,long color)[] FactionDefs =
        {
            ("北方装甲军",0xFF6347L),("沙漠军团",0x4169E1L),("南方机械旅",0x32CD32L),("东方突击师",0x9370DBL),("联合远征军",0xFFD700L)
        };
        readonly List<(string name,long color)> _enemyFactions = new();
        bool _factionsInited;
        float _spawnCd, _sepAt, _reformTimer;
        bool _enemySeen;
        WorldGenerator _terrain;
        Transform _root;
        Material _matTank,_matApc,_matMissile,_matDark,_matTrack;
        Material _matWood,_matHorse,_matHorseDk,_matBronze,_matPhalanx,_matCavalry;

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _terrain = Object.FindObjectOfType<WorldGenerator>();
            _matTank    = ShaderHelper.Pbr(new Color(0.36f,0.50f,0.17f),0.25f,0.45f,9611,0.5f);
            _matApc     = ShaderHelper.Pbr(new Color(0.54f,0.56f,0.48f),0.2f,0.4f,9612,0.5f);
            _matMissile = ShaderHelper.Pbr(new Color(0.43f,0.31f,0.17f),0.2f,0.45f,9613,0.5f);
            _matDark    = ShaderHelper.Pbr(new Color(0.12f,0.12f,0.14f),0.2f,0.5f,9614,0.4f);
            _matTrack   = ShaderHelper.Pbr(new Color(0.08f,0.08f,0.09f),0f,0.3f,9615,0.3f);
            _matWood    = ShaderHelper.Pbr(new Color(0.52f,0.36f,0.18f),0.15f,0.4f,9621,0.4f);
            _matHorse   = ShaderHelper.Pbr(new Color(0.45f,0.30f,0.16f),0.2f,0.45f,9622,0.4f);
            _matHorseDk = ShaderHelper.Pbr(new Color(0.30f,0.19f,0.10f),0.2f,0.4f,9623,0.4f);
            _matBronze  = ShaderHelper.Pbr(new Color(0.72f,0.55f,0.20f),0.55f,0.5f,9624,0.4f);
            _matPhalanx = ShaderHelper.Pbr(new Color(0.62f,0.18f,0.15f),0.15f,0.45f,9625,0.4f);
            _matCavalry = ShaderHelper.Pbr(new Color(0.20f,0.34f,0.28f),0.15f,0.45f,9626,0.4f);

            // 现代三型（1949 起）
            Defs["tank"] = new GroundDef{Id="tank",Name="坦克",Icon="🛡",ColorHex="5B7F2B",
                Speed=3.5f,Durability=180,Attack=40,Defense=20,Range=12,FireInterval=2.4f,Capacity=3,Footprint=3.2f,
                Rank="装甲营长",Intel=3,CostSteel=120,CostGold=40,Modern=true,Desc="重型装甲突击，高耐久中射程，攻坚主力"};
            Defs["apc"] = new GroundDef{Id="apc",Name="装甲车",Icon="🚐",ColorHex="8A8F7A",
                Speed=5.0f,Durability=100,Attack=15,Defense=12,Range=8,FireInterval=1.8f,Capacity=8,Footprint=2.8f,
                Rank="侦察连长",Intel=3,CostSteel=60,CostGold=20,Modern=true,Desc="高速运兵装甲，机动侦察，支援步兵突击"};
            Defs["missile_vehicle"] = new GroundDef{Id="missile_vehicle",Name="导弹车",Icon="🚀",ColorHex="6E4F2B",
                Speed=2.5f,Durability=120,Attack=70,Defense=10,Range=20,FireInterval=3.5f,Capacity=3,Footprint=3.0f,
                Rank="导弹营长",Intel=4,CostSteel=100,CostGold=60,Modern=true,Desc="超远程导弹打击，射程 20 格火力压制"};
            // 古典三型（1949 前；木/粮/金列装，不耗钢）
            Defs["cavalry"] = new GroundDef{Id="cavalry",Name="骑兵",Icon="🐎",ColorHex="8B5E3C",
                Speed=3.2f,Durability=80,Attack=12,Defense=6,Range=2,FireInterval=1.8f,Capacity=1,Footprint=2.1f,
                Rank="骑兵都尉",Intel=2,CostWood=40,CostGold=10,CostFood=20,Modern=false,Desc="古典高速突击骑兵，机动冲击敌阵"};
            Defs["phalanx"] = new GroundDef{Id="phalanx",Name="列方阵兵",Icon="🛡",ColorHex="C0C0C8",
                Speed=1.2f,Durability=140,Attack=18,Defense=14,Range=2,FireInterval=2.2f,Capacity=3,Footprint=3.0f,
                Rank="百夫长",Intel=1,CostWood=20,CostGold=10,CostFood=30,Modern=false,Desc="古典 3 人一组重装方阵，高耐久列阵防守"};
            Defs["chariot"] = new GroundDef{Id="chariot",Name="马拉战车",Icon="🏇",ColorHex="7A5C2E",
                Speed=2.8f,Durability=100,Attack=15,Defense=8,Range=3,FireInterval=1.9f,Capacity=2,Footprint=2.6f,
                Rank="战车校尉",Intel=2,CostWood=80,CostGold=30,CostFood=20,Modern=false,Desc="古典马拉战车，冲锋践踏撕裂敌阵"};

            var go=new GameObject("GroundWarfare"); go.transform.SetParent(gm.transform,false);
            _root=go.transform;
        }

        bool EraMatch(string typeId)
        {
            bool modern=ModernTypes.Contains(typeId);
            return modern == (S.Year>=4949);
        }

        /// <summary>由 Def 构造一个十维单位（统一基数初始化；新单位带驻留宽限）。</summary>
        GroundUnit MakeUnit(GroundDef d,string side,float x,float z,string factionId="",long factionColor=0)
        {
            var u=new GroundUnit{
                TypeId=d.Id,Name=side=="enemy"?(factionId+"·"+d.Name):d.Name,Side=side,X=x,Z=z,Level=1,
                BaseSpeed=d.Speed, BaseMaxHp=d.Durability,MaxHp=d.Durability,Hp=d.Durability,
                BaseAttack=d.Attack,Attack=d.Attack,
                BaseDefense=d.Defense,Defense=d.Defense,
                BaseRange=d.Range,Range=d.Range,
                BaseFire=d.FireInterval,
                Capacity=d.Capacity,Footprint=d.Footprint,
                Rank=d.Rank,Intel=d.Intel,
                FactionId=factionId,FactionColor=factionColor,
                AttackCd=0f,GraceT=GraceSeconds
            };
            return u;
        }

        /// <summary>玩家列装地面部队：按时代列装，扣资源，上限 60，须在陆地；列装点即生成点（带驻留宽限，不会被立即拉走）。</summary>
        public bool BuildGround(string typeId,float x,float z)
        {
            if(!Defs.TryGetValue(typeId,out var d)){ Debug.Log("[Ground] BuildGround reject: unknown type "+typeId); return false; }
            if(_terrain==null) _terrain=Object.FindObjectOfType<WorldGenerator>();
            if(_terrain!=null && !_terrain.IsStaticLand(x,z)){ GM.AddEvent("bad","陆地载具不能列装在水面"); Debug.Log("[Ground] BuildGround reject: water"); return false; }
            // V9.8.3 生成点贴水校正：四方向 3 单位全是水（湖边贴水点/湖心小地块）→ 迁移最近陆地，防生成后被困湖里
            if(_terrain!=null && !_terrain.IsStaticLand(x+3f,z)&&!_terrain.IsStaticLand(x-3f,z)&&!_terrain.IsStaticLand(x,z+3f)&&!_terrain.IsStaticLand(x,z-3f))
            {
                var near=NearestLand(x,z);
                if(near.HasValue){ x=near.Value.x; z=near.Value.y; }
            }
            if(!EraMatch(typeId))
            {
                bool modern=ModernTypes.Contains(typeId);
                if(modern) GM.AddEvent("bad",d.Name+" 公元1949年才列装（当前 公元"+(S.Year-3000)+"）");
                else       GM.AddEvent("bad","公元1949年起已列装现代地面部队，古典"+d.Name+"不再列装");
                Debug.Log("[Ground] BuildGround reject: era mismatch "+typeId);
                return false;
            }
            if(Ours.Count+Enemies.Count>=MaxGround){ GM.AddEvent("bad","已达地面部队上限 "+MaxGround+" 辆"); return false; }
            if(S.GetRes("steel")<d.CostSteel||S.GetRes("gold")<d.CostGold||S.GetRes("wood")<d.CostWood||S.GetRes("food")<d.CostFood)
            { GM.AddEvent("bad","资源不足，无法列装"+d.Name); return false; }
            S.AddRes("steel",-d.CostSteel); S.AddRes("gold",-d.CostGold); S.AddRes("wood",-d.CostWood); S.AddRes("food",-d.CostFood);
            var u=MakeUnit(d,"ours",x,z);
            u.View=BuildView(u,d);
            Ours.Add(u); AssignGroup(u);
            GM.AddEvent("good",d.Icon+" 新"+d.Name+"列装！");
            Debug.Log("[Ground] BuildGround OK: "+d.Name+" at "+x.ToString("F1")+","+z.ToString("F1")+" effSpeed="+EffectiveSpeed(u).ToString("F2"));
            return true;
        }

        /// <summary>Debug/Web：无消耗列装（探针入口；按当前时代换型），生成点在出生地（主村落）附近 20–60 单位环陆地。</summary>
        public int DebugBuildOwn(int n)
        {
            if(_terrain==null) return 0;
            int made=0;
            string[] types=S.Year>=4949
                ? new[]{"tank","apc","missile_vehicle"}
                : new[]{"cavalry","phalanx","chariot"};
            float ox = GM.State.VillageX.Count>0 ? GM.State.VillageX[0] : 0f;
            float oz = GM.State.VillageZ.Count>0 ? GM.State.VillageZ[0] : 0f;
            for(int k=0;k<n;k++)
            {
                string t=types[Random.Range(0,types.Length)]; var d=Defs[t];
                if(Ours.Count+Enemies.Count>=MaxGround) break;
                float x=0,z=0; bool land=false;
                for(int i=0;i<32&&!land;i++)
                {
                    float ang=Random.value*Mathf.PI*2f, dist=20f+Random.value*40f;
                    x=ox+Mathf.Cos(ang)*dist; z=oz+Mathf.Sin(ang)*dist;
                    if(_terrain.IsStaticLand(x,z))
                    {
                        // V9.8.3 贴水校正：四方向 3 单位全是水（湖边贴水/湖心小块）→ 迁移最近陆地
                        if(!_terrain.IsStaticLand(x+3f,z)&&!_terrain.IsStaticLand(x-3f,z)&&!_terrain.IsStaticLand(x,z+3f)&&!_terrain.IsStaticLand(x,z-3f))
                        {
                            var near=NearestLand(x,z);
                            if(near.HasValue){ x=near.Value.x; z=near.Value.y; }
                        }
                        land=true;
                    }
                }
                if(!land) continue;
                var u=MakeUnit(d,"ours",x,z);
                u.View=BuildView(u,d);
                Ours.Add(u); AssignGroup(u); made++;
            }
            if(made>0)
            {
                // V9.8.1 出生广播带坐标：让玩家能定位部队（自动编组+雷达索敌+组阵巡航已自动生效）
                var first=Ours[Ours.Count-made];
                GM.AddEvent("good","🛡 Debug 我方地面部队 "+made+" 支列装（出生地附近 "+Mathf.RoundToInt(first.X)+","+Mathf.RoundToInt(first.Z)+"，已自动编组·雷达索敌·组阵巡航）");
            }
            return made;
        }

        void EnsureFactions()
        {
            if(_factionsInited) return;
            _factionsInited=true; _enemyFactions.Clear();
            int n=Random.Range(3,6);
            var idx=new List<int>(); for(int i=0;i<FactionDefs.Length;i++) idx.Add(i);
            for(int i=idx.Count-1;i>0;i--){ int j=Random.Range(0,i+1); (idx[i],idx[j])=(idx[j],idx[i]); }
            for(int i=0;i<n;i++) _enemyFactions.Add(FactionDefs[idx[i]]);
            GM.AddEvent("bad","⚔ 敌方装甲集团浮现："+string.Join("、",_enemyFactions.ConvertAll(f=>f.name).ToArray()));
        }
        (string name,long color) LeastRepresentedFaction()
        {
            EnsureFactions();
            if(_enemyFactions.Count==0) return default;
            (string name,long color) best=_enemyFactions[0]; int min=int.MaxValue;
            for(int i=0;i<_enemyFactions.Count;i++)
            {
                int cnt=0;
                foreach(var e in Enemies) if(e.FactionId==_enemyFactions[i].name) cnt++;
                if(cnt<min){ min=cnt; best=_enemyFactions[i]; }
            }
            return best;
        }
        void SpawnEnemy()
        {
            EnsureFactions();
            string[] types=S.Year>=4949
                ? new[]{"tank","apc","missile_vehicle","tank","apc"}
                : new[]{"cavalry","phalanx","chariot","cavalry","phalanx"};
            string t=types[Random.Range(0,types.Length)]; var d=Defs[t];
            var fac=LeastRepresentedFaction();
            // 敌方镜像以出生地为中心（60–140 环），与我方出生地部队在同一雷达作战半径内
            float ox = GM.State.VillageX.Count>0 ? GM.State.VillageX[0] : 0f;
            float oz = GM.State.VillageZ.Count>0 ? GM.State.VillageZ[0] : 0f;
            float x=0,z=0; bool land=false;
            for(int k=0;k<32&&!land;k++)
            { float a=Random.value*Mathf.PI*2, rr=60f+Random.value*80f;
              x=ox+Mathf.Cos(a)*rr; z=oz+Mathf.Sin(a)*rr;
              if(_terrain==null||_terrain.IsStaticLand(x,z))
              {
                  // V9.8.3 贴水校正：四方向 3 单位全是水 → 迁移最近陆地
                  if(_terrain!=null && !_terrain.IsStaticLand(x+3f,z)&&!_terrain.IsStaticLand(x-3f,z)&&!_terrain.IsStaticLand(x,z+3f)&&!_terrain.IsStaticLand(x,z-3f))
                  {
                      var near=NearestLand(x,z);
                      if(near.HasValue){ x=near.Value.x; z=near.Value.y; }
                  }
                  land=true;
              } }
            var u=MakeUnit(d,"enemy",x,z,fac.name,fac.color);
            u.View=BuildView(u,d,fac.color);
            Enemies.Add(u); AssignGroup(u);
        }

        /// <summary>新局清理：地面部队运行时列表不进 GameState，销毁视图并清空，防上一局残留。</summary>
        public void ResetForNewGame()
        {
            foreach (var u in Ours) if (u.View != null) Object.Destroy(u.View);
            foreach (var e in Enemies) if (e.View != null) Object.Destroy(e.View);
            Ours.Clear(); Enemies.Clear();
            GroupCenter.Clear();
            _enemyFactions.Clear(); _factionsInited = false;
            _enemySeen = false; Active = false;
            _spawnCd = 0f; _sepAt = 0f; _reformTimer = 0f;
        }

        public override void Tick(float dt)
        {
            if(!Active) Active=true;
            if(_terrain==null) _terrain=Object.FindObjectOfType<WorldGenerator>();
            _spawnCd-=dt;
            if(_spawnCd<=0 && Ours.Count>0 && MemoryBudgetManager.EntitySpawnGate(MemoryBudgetManager.EntityKind.Ground,Ours.Count+Enemies.Count))
            {
                int headroom=MaxGround-Ours.Count-Enemies.Count;
                int target=Mathf.Min(Ours.Count,headroom);
                if(Enemies.Count<target && headroom>0)
                {
                    int gap=target-Enemies.Count; int batch=gap<=3?gap:Mathf.Min(gap,5);
                    for(int i=0;i<batch;i++) SpawnEnemy();
                    _spawnCd=0.5f;
                    if(!_enemySeen){ _enemySeen=true; GM.AddEvent("bad","⚔ 敌方装甲部队出现！（"+_enemyFactions.Count+" 阵营对峙）"); }
                }
                else if(Enemies.Count>=target){ _spawnCd=5f; }
            }
            UpdateOurs(dt);
            UpdateEnemies(dt);
            SeparateGround(dt);
            CleanupDead();
        }

        void UpdateOurs(float dt)
        {
            _reformTimer-=dt;
            if(_reformTimer<=0f){ _reformTimer=20f; RebuildGroups(); }
            for(int i=0;i<Ours.Count;i++)
            {
                var u=Ours[i]; if(u.View==null) continue;
                if (u.Lifted) continue;
                // V9.8.4 水域脱困兜底：单位当前位置在水里（湖心/涨潮洼地/生成误入）→ 强制向最近陆地移动。
                // 不再 continue 跳过本帧索敌：困水单位上岸途中仍可发现敌人并交战（治"困湖后不战斗"）。
                if(_terrain!=null && !_terrain.IsStaticLand(u.X,u.Z))
                {
                    var land=NearestLand(u.X,u.Z);
                    if(land.HasValue) MoveToward(u,land.Value.x,land.Value.y,dt,2.0f);   // ×2 快速脱困
                }
                if (u.GraceT>0f) u.GraceT-=dt;   // 驻留宽限倒计时
                u.AttackCd-=dt;
                float radar=EffectiveRadar(u)*GameConstants.Tile;
                CombatTarget ct=GM.Combat.NearestHostile(u.X,u.Z,radar,CombatSystem.PlayerKey);
                if(ct!=null)
                {
                    float d=Mathf.Sqrt((ct.X-u.X)*(ct.X-u.X)+(ct.Z-u.Z)*(ct.Z-u.Z));
                    float engage=u.Range*GameConstants.Tile;
                    if(d>engage) MoveToward(u,ct.X,ct.Z,dt,1.5f);                 // 追击：战斗航速+50%
                    else if(u.AttackCd<=0)
                    {
                        GroundFire(u,ct); u.AttackCd=EffectiveFireInterval(u);
                    }
                }
                // 紧急集结令（绿旗=地面部队）：100 格内优先向军旗集结列阵（高于巡航）
                else if (GM.Rally!=null && GM.Rally.Active("ground"))
                {
                    var rp=GM.Rally.Target("ground");
                    if (rp.HasValue)
                    {
                        float rd=Mathf.Sqrt((rp.Value.x-u.X)*(rp.Value.x-u.X)+(rp.Value.y-u.Z)*(rp.Value.y-u.Z));
                        if (rd<=RallySystem.RallyRange*GameConstants.Tile)
                        {
                            if (rd>RallySystem.FormRange) MoveToward(u,rp.Value.x,rp.Value.y,dt,1f);
                            continue;
                        }
                        if (GM.AirLift!=null && GM.AirLift.RequestLiftAuto(u,rp.Value.x,rp.Value.y)) continue;
                    }
                    Cruise(u,dt);
                }
                else Cruise(u,dt);
            }
        }

        void UpdateEnemies(float dt)
        {
            for(int i=Enemies.Count-1;i>=0;i--)
            {
                var e=Enemies[i]; if(e.View==null) continue;
                if (e.Lifted) continue;
                // V9.8.4 敌方水域脱困兜底：困在水里 → 强制向最近陆地快速移动（×2）；不再 continue 跳过索敌
                if(_terrain!=null && !_terrain.IsStaticLand(e.X,e.Z))
                {
                    var land=NearestLand(e.X,e.Z);
                    if(land.HasValue) MoveToward(e,land.Value.x,land.Value.y,dt,2.0f);
                }
                if (e.GraceT>0f) e.GraceT-=dt;
                e.AttackCd-=dt;
                float radar=EffectiveRadar(e)*GameConstants.Tile;
                CombatTarget ct=GM.Combat.NearestHostile(e.X,e.Z,radar,e.FactionId);
                if(ct!=null)
                {
                    float d=Mathf.Sqrt((ct.X-e.X)*(ct.X-e.X)+(ct.Z-e.Z)*(ct.Z-e.Z));
                    float engage=e.Range*GameConstants.Tile;
                    if(d>engage) MoveToward(e,ct.X,ct.Z,dt,1.4f);
                    else if(e.AttackCd<=0)
                    {
                        EnemyGroundFire(e,ct); e.AttackCd=EffectiveFireInterval(e)+0.2f;
                    }
                }
                else Cruise(e,dt);
            }
        }

        /// <summary>巡航+组阵：① 驻留宽限内原地驻守（只索敌）；② 组员围绕领队按槽位环形阵型跟随；
        /// ③ 领队/无组在自身 20–45 小环陆地巡逻（V9.7.2 缩小，旧 40–100 环一造出来就跑没影）。</summary>
        void Cruise(GroundUnit u,float dt)
        {
            if(_terrain==null) return;
            // ① 驻留宽限：列装后原地驻守，不被巡航拉走
            if(u.GraceT>0f) return;
            // ② 组员：向"领队+槽位偏移"阵位移动，到位后保持
            if(u.Group>=0 && GroupCenter.TryGetValue(u.Group,out var leader) && leader!=null && leader!=u)
            {
                int idx=GroupIndexIn(leader.Group,u);
                int cnt=GroupCount(leader.Group);
                float ang=(idx*360f/Mathf.Max(1,cnt))*Mathf.Deg2Rad;
                float formR=4f+Mathf.Max(leader.Footprint,u.Footprint);   // 阵型间距随体积
                float sx=leader.X+Mathf.Sin(ang)*formR, sz=leader.Z+Mathf.Cos(ang)*formR;
                for(int k=0;k<12&&!_terrain.IsStaticLand(sx,sz);k++)
                { ang+=0.52f; sx=leader.X+Mathf.Sin(ang)*formR; sz=leader.Z+Mathf.Cos(ang)*formR; }
                float ddx=sx-u.X, ddz=sz-u.Z;
                if(ddx*ddx+ddz*ddz>9f){ MoveToward(u,sx,sz,dt,0.85f); return; }
                u.HasRoute=true;
                return;
            }
            // ③ 领队/无组：小环陆地巡逻
            if(!u.HasRoute)
            {
                u.HasRoute=true;
                for(int k=0;k<24;k++)
                { float a=Random.value*Mathf.PI*2, rr=20f+Random.value*25f;
                  float nx=u.X+Mathf.Cos(a)*rr, nz=u.Z+Mathf.Sin(a)*rr;
                  if(_terrain.IsStaticLand(nx,nz)){ u.RX=nx; u.RZ=nz; break; } }
            }
            float dx=u.RX-u.X,dz=u.RZ-u.Z;
            if(dx*dx+dz*dz<4f){ u.HasRoute=false; return; }
            MoveToward(u,u.RX,u.RZ,dt,0.85f);
        }
        readonly Dictionary<int,GroundUnit> GroupCenter = new();

        int GroupCount(int g)
        {
            int n=0;
            foreach(var o in Ours) if(o.Group==g) n++;
            foreach(var e in Enemies) if(e.Group==g) n++;
            return n;
        }
        int GroupIndexIn(int g,GroundUnit u)
        {
            int idx=0; var list=u.Side=="ours"?Ours:Enemies;
            for(int i=0;i<list.Count;i++)
            {
                if(list[i].Group!=g||list[i].Side!=u.Side) continue;
                if(list[i]==u) return idx;
                idx++;
            }
            return idx;
        }

        // ===== V9.7.2 实际生效参数（类型基础 × 等级成长；属性面板与移动/开火全部读这里，口径唯一）=====
        /// <summary>实际移动速度（世界单位/秒）= 基础速度 ×(1+8%×(lv-1))。</summary>
        public float EffectiveSpeed(GroundUnit u)
        {
            if(u==null) return 0f;
            return u.BaseSpeed*(1f+LvSpeed*(u.Level-1));
        }
        /// <summary>实际雷达/发现距离（格）= 60 ×(1+6%×(lv-1))。</summary>
        public float EffectiveRadar(GroundUnit u)
        {
            if(u==null) return RadarGround;
            return RadarGround*(1f+LvRadar*(u.Level-1));
        }
        /// <summary>实际攻击间隔（秒）= 基础间隔 ×(1-3%×(lv-1))，下限 0.4。</summary>
        public float EffectiveFireInterval(GroundUnit u)
        {
            if(u==null) return 2f;
            return Mathf.Max(MinFireInterval, u.BaseFire*(1f-LvFire*(u.Level-1)));
        }

        /// <summary>
        /// V9.8.4 找最近陆地坐标（水域脱困兜底）：以 (x,z) 为中心，半径 2→240 逐环扫描（角度步进 8/16/24 方向），
        /// 返回最近 IsStaticLand 点；找不到返回 null。半径上限 240 覆盖主大陆大湖泊（湖心距岸可超 100），
        /// 根治"大湖中心部队脱困失败原地卡死"。
        /// </summary>
        Vector2? NearestLand(float x, float z)
        {
            if (_terrain == null) return null;
            if (_terrain.IsStaticLand(x, z)) return new Vector2(x, z);
            for (float r = 2f; r <= 240f; )
            {
                int steps = r <= 10f ? 8 : r <= 48f ? 16 : 24;
                float step = r <= 48f ? 2f : 4f;
                for (int i = 0; i < steps; i++)
                {
                    float a = (i * 360f / steps) * Mathf.Deg2Rad;
                    float sx = x + Mathf.Cos(a) * r, sz = z + Mathf.Sin(a) * r;
                    if (_terrain.IsStaticLand(sx, sz)) return new Vector2(sx, sz);
                }
                r += step;
            }
            return null;
        }

        void MoveToward(GroundUnit u,float tx,float tz,float dt,float mul)
        {
            if(_terrain==null) return;
            float dx=tx-u.X,dz=tz-u.Z; float dist=Mathf.Sqrt(dx*dx+dz*dz);
            if(dist<0.5f) return;
            // V9.7.2 速度直接为世界单位/秒（类型差异 × 等级成长）；不再用旧 ClampedGroundSpeed×Tile
            float sp=EffectiveSpeed(u)*mul;
            float nx=u.X+dx/dist*sp*dt, nz=u.Z+dz/dist*sp*dt;
            // 陆地约束：不得下水；前方是水则贴岸转向；转向全部失败则强制向最近陆地移动（V9.8.3 防困水域原地卡死）
            if(!_terrain.IsStaticLand(nx,nz))
            {
                bool slid=false;
                for(int turn=30;turn<=150&&!slid;turn+=30)
                {
                    float rad=turn*Mathf.Deg2Rad, cs=Mathf.Cos(rad), sn=Mathf.Sin(rad);
                    for(int side=-1;side<=1&&!slid;side+=2)
                    {
                        float rx=dx/dist*cs-side*dz/dist*sn, rz=dz/dist*cs+side*dx/dist*sn;
                        float sx=u.X+rx*sp*dt, sz=u.Z+rz*sp*dt;
                        if(_terrain.IsStaticLand(sx,sz)){ nx=sx; nz=sz; slid=true; }
                    }
                }
                if(!slid)
                {
                    // V9.8.3 贴岸 5 档转向全失败（困在湖心/水洼）：找最近陆地走过去，找不到才放弃本步
                    var near=NearestLand(u.X,u.Z);
                    if(near.HasValue)
                    {
                        float ndx=near.Value.x-u.X, ndz=near.Value.y-u.Z;
                        float nd=Mathf.Sqrt(ndx*ndx+ndz*ndz);
                        if(nd>0.5f){ nx=u.X+ndx/nd*sp*dt; nz=u.Z+ndz/nd*sp*dt; }
                        else return;
                    }
                    else { u.HasRoute=false; return; }
                }
            }
            u.X=nx; u.Z=nz;
            if(u.View!=null)
            {
                float h=_terrain.HeightAt(nx,nz);
                u.View.transform.position=new Vector3(nx,h+0.5f,nz);
                float yaw=Mathf.Atan2(dx,dz)*Mathf.Rad2Deg;
                u.View.transform.rotation=Quaternion.Slerp(u.View.transform.rotation,Quaternion.Euler(0,yaw,0),0.2f);
            }
        }

        void DestroyUnit(GroundUnit u,List<GroundUnit> list)
        {
            if(u.View!=null) Object.Destroy(u.View);
            list.Remove(u);
            GroupCenter.Remove(u.Group);
        }

        // ===== V9.4.7 统一战斗目录接线 =====
        public void RegisterCombat(CombatSystem c)
        {
            // 不因 View==null 跳过：View 只是表现，逻辑坐标与 HP 仍有效
            foreach(var u in Ours)
            {
                if(u==null||u.Hp<=0f) continue;
                c.Add(CombatSystem.K_GROUND,u,u.X,u.Z,u.Hp,u.Attack,u.Range*GameConstants.Tile,CombatSystem.PlayerKey);
            }
            foreach(var e in Enemies)
            {
                if(e==null||e.Hp<=0f) continue;
                c.Add(CombatSystem.K_GROUND,e,e.X,e.Z,e.Hp,e.Attack,e.Range*GameConstants.Tile,e.FactionId);
            }
        }

        /// <summary>受到伤害（含防御减伤：实际受击=伤害×100/(100+防御)）。</summary>
        public void DamageGround(GroundUnit u,float dmg)
        {
            if(u==null||u.Hp<=0f||dmg<=0f) return;
            float mitigated=dmg*100f/(100f+Mathf.Max(0f,u.Defense));
            u.Hp-=mitigated;
        }

        // ===== V9.7.2 升级：十维全套随等级成长 =====
        /// <summary>升级：耐久+12%/级、攻击+8%/级、防御+10%/级、速度+8%/级、射程+1（上限+5、硬上限50）、射速间隔-3%/级；回血30%。</summary>
        public void LevelUp(GroundUnit u,bool free=false)
        {
            if(u==null||u.Level>=10){ if(!free&&u!=null) GM.AddEvent("bad","该部队已达最高等级 Lv10"); return; }
            u.Level++;
            u.MaxHp=Mathf.RoundToInt(u.BaseMaxHp*(1f+LvHp*(u.Level-1)));
            u.Attack=Mathf.RoundToInt(u.BaseAttack*Mathf.Pow(1f+LvAtk,u.Level-1));
            u.Defense=Mathf.RoundToInt(u.BaseDefense*(1f+LvDef*(u.Level-1)));
            u.Range=Mathf.Min(GroundRangeHardCap, Mathf.Min(u.BaseRange+MaxRangeAdd, u.BaseRange+(u.Level-1)));
            u.Hp=Mathf.Min(u.MaxHp, u.Hp+u.MaxHp*0.3f);
            GM.AddEvent("good","🎖 "+(free?"战功晋升":"升级")+"："+u.Name+" → Lv"+u.Level
                +"（攻击 "+u.Attack+" 耐久 "+u.MaxHp+" 防御 "+u.Defense+" 射程 "+u.Range+" 速度 "+EffectiveSpeed(u).ToString("0.0")+"）");
        }
        /// <summary>玩家资源升级造价：按当前等级线性（现代钢/金，古典木/粮/金）。</summary>
        public Dictionary<string,int> UpgradeGroundCost(GroundUnit u)
        {
            if(u==null||u.Level>=10) return null;
            if(!Defs.TryGetValue(u.TypeId,out var d)) return null;
            float lv=u.Level;
            if(d.Modern) return new Dictionary<string,int>{{"steel",Mathf.Max(10,Mathf.RoundToInt(d.CostSteel*0.55f*lv))},{"gold",Mathf.Max(5,Mathf.RoundToInt(d.CostGold*0.55f*lv))}};
            return new Dictionary<string,int>{{"wood",Mathf.Max(10,Mathf.RoundToInt(d.CostWood*0.5f*lv))},{"gold",Mathf.Max(5,Mathf.RoundToInt(d.CostGold*0.5f*lv))},{"food",Mathf.Max(10,Mathf.RoundToInt(d.CostFood*0.5f*lv))}};
        }
        public bool UpgradeGround(GroundUnit u)
        {
            var cost=UpgradeGroundCost(u);
            if(cost==null){ GM.AddEvent("bad","该部队已达最高等级 Lv10"); return false; }
            if(!S.CanAfford(cost)){ GM.AddEvent("bad","资源不足，无法升级"+u.Name); return false; }
            S.Pay(cost);
            LevelUp(u,false);
            return true;
        }

        // ===== 开火 =====
        void GroundFire(GroundUnit u,CombatTarget ct)
        {
            WeaponFxSystem.Muzzle(new Vector3(u.X,1.4f,u.Z),1.5f);
            WeaponFxSystem.Sfx("cannon_fire",new Vector3(u.X,1.4f,u.Z),0.65f);
            if(ct.Ref is GroundUnit tgt)
            {
                var hp=new Vector3(ct.X,1f,ct.Z);
                WeaponFxSystem.Explosion(hp); WeaponFxSystem.Sfx("cannon_explode",hp,0.8f);
                DamageGround(tgt,u.Attack);
                if(tgt.Hp<=0f)
                {
                    DestroyUnit(tgt,Enemies);
                    u.Kills++;
                    GM.AddEvent("good","💥 击毁一辆敌"+tgt.Name+"！（"+u.Name+" 战绩 "+u.Kills+"）");
                    if(u.Kills%2==0 && u.Level<10) LevelUp(u,true);
                }
                return;
            }
            WeaponFxSystem.Explosion(new Vector3(ct.X,1f,ct.Z)); WeaponFxSystem.Sfx("cannon_explode",new Vector3(ct.X,1f,ct.Z),0.8f);
            GM.Combat.Damage(ct,u.Attack);
        }
        void EnemyGroundFire(GroundUnit e,CombatTarget ct)
        {
            WeaponFxSystem.Muzzle(new Vector3(e.X,1.4f,e.Z),1.5f);
            WeaponFxSystem.Sfx("cannon_fire",new Vector3(e.X,1.4f,e.Z),0.65f);
            if(ct.Ref is GroundUnit tgt)
            {
                var hp=new Vector3(ct.X,1f,ct.Z);
                WeaponFxSystem.Explosion(hp); WeaponFxSystem.Sfx("cannon_explode",hp,0.8f);
                DamageGround(tgt,e.Attack);
                if(tgt.Hp<=0f)
                {
                    if(ct.Key==CombatSystem.PlayerKey){ DestroyUnit(tgt,Ours); GM.AddEvent("bad","💥 我方一辆"+tgt.Name+"被击毁！"); }
                    else { DestroyUnit(tgt,Enemies); GM.AddEvent("bad","💥 敌方"+tgt.Name+"在混战中被击毁！"); }
                }
                return;
            }
            WeaponFxSystem.Explosion(new Vector3(ct.X,1f,ct.Z)); WeaponFxSystem.Sfx("cannon_explode",new Vector3(ct.X,1f,ct.Z),0.8f);
            GM.Combat.Damage(ct,e.Attack);
        }

        void CleanupDead()
        {
            Ours.RemoveAll(u=>{ if(u.Hp<=0f){ if(u.View!=null)Object.Destroy(u.View); GroupCenter.Remove(u.Group); return true; } return false; });
            Enemies.RemoveAll(u=>{ if(u.Hp<=0f){ if(u.View!=null)Object.Destroy(u.View); GroupCenter.Remove(u.Group); return true; } return false; });
        }

        // ===== 编队 =====
        void AssignGroup(GroundUnit u)
        {
            bool ours=u.Side=="ours";
            var list=ours?Ours:Enemies;
            var cnt=new Dictionary<int,int>();
            foreach(var o in list){ if(o.Group>=0) cnt[o.Group]=cnt.TryGetValue(o.Group,out var c)?c+1:1; }
            int start=ours?0:32, end=ours?32:64, g=start;
            for(int k=start;k<end;k++){ if(!cnt.TryGetValue(k,out var c)||c<3){ g=k; break; } }
            u.Group=g;
            if(!GroupCenter.ContainsKey(g)) GroupCenter[g]=u;
        }
        void RebuildGroups()
        {
            GroupCenter.Clear();
            foreach(var o in Ours) if(!GroupCenter.ContainsKey(o.Group)) GroupCenter[o.Group]=o;
            foreach(var e in Enemies) if(!GroupCenter.ContainsKey(e.Group)) GroupCenter[e.Group]=e;
        }

        /// <summary>体积分离（4Hz）：防编队/巡航重叠，半径随 Footprint。</summary>
        void SeparateGround(float dt)
        {
            if(Time.unscaledTime<_sepAt) return;
            _sepAt=Time.unscaledTime+0.25f;
            SeparateList(Ours,dt); SeparateList(Enemies,dt);
        }
        void SeparateList(List<GroundUnit> list,float dt)
        {
            for(int i=0;i<list.Count;i++)
            {
                var a=list[i]; if(a.View==null) continue;
                for(int j=i+1;j<list.Count;j++)
                {
                    var b=list[j]; if(b.View==null) continue;
                    float dx=b.X-a.X,dz=b.Z-a.Z; float d2=dx*dx+dz*dz;
                    float minR=Mathf.Max(1f,(a.Footprint+b.Footprint)*0.5f);
                    float minSq=minR*minR;
                    if(d2>0.0001f&&d2<minSq)
                    {
                        float d=Mathf.Sqrt(d2); float push=(minR-d)*0.5f*4f*dt;
                        float ux=dx/d,uz=dz/d;
                        a.X-=ux*push; a.Z-=uz*push; b.X+=ux*push; b.Z+=uz*push;
                    }
                }
            }
        }

        // ===== 程序化视图（Cube 组合；phalanx 为 3 人一组；不用内置 Sphere）=====
        GameObject BuildView(GroundUnit u,GroundDef d,long?factionColor=null)
        {
            var go=new GameObject("Ground_"+d.Id);
            go.transform.SetParent(_root,false);
            var colorHex=factionColor!=null?("#"+((long)factionColor).ToString("X6")):d.ColorHex;
            Material body = d.Id=="tank"?_matTank : d.Id=="apc"?_matApc
                         : d.Id=="missile_vehicle"?_matMissile
                         : d.Id=="cavalry"?_matCavalry : d.Id=="phalanx"?_matPhalanx : _matWood;
            if(factionColor!=null)
            {
                ColorUtility.TryParseHtmlString(colorHex,out var fc);
                body=ShaderHelper.Pbr(fc,0.25f,0.5f,9700+(int)fc.GetHashCode()%200,0.5f);
            }
            if(d.Id=="cavalry")
            {   // 骑兵：青甲骑手 + 战马 + 长矛
                var horse=Prim(_matHorse,new Vector3(1.5f,0.8f,2.1f)); horse.transform.localPosition=new Vector3(0,0.4f,0);
                var head=Prim(_matHorse,new Vector3(0.45f,0.45f,0.55f)); head.transform.localPosition=new Vector3(0,0.85f,-1.25f);
                var mane=Prim(_matHorseDk,new Vector3(0.3f,0.5f,0.5f)); mane.transform.localPosition=new Vector3(0,1.0f,-0.15f);
                for(int leg=-1;leg<=1;leg+=2)
                { var l1=Prim(_matHorseDk,new Vector3(0.18f,0.7f,0.18f)); l1.transform.localPosition=new Vector3(0.45f*leg,0.05f,0.7f);
                  var l2=Prim(_matHorseDk,new Vector3(0.18f,0.7f,0.18f)); l2.transform.localPosition=new Vector3(0.45f*leg,0.05f,-0.7f); }
                var rider=Prim(body,new Vector3(0.6f,0.85f,0.6f)); rider.transform.localPosition=new Vector3(0,0.95f,0.35f);
                var helm=Prim(body,new Vector3(0.42f,0.4f,0.42f)); helm.transform.localPosition=new Vector3(0,1.55f,0.35f);
                var spear=Prim(_matDark,new Vector3(0.12f,1.9f,0.12f)); spear.transform.localPosition=new Vector3(0.45f,1.55f,0.3f);
                var tip=Prim(_matBronze,new Vector3(0.2f,0.3f,0.2f)); tip.transform.localPosition=new Vector3(0.45f,2.5f,0.3f);
            }
            else if(d.Id=="phalanx")
            {   // 列方阵兵：3 人一组并排（红袍兵 + 巨盾 + 长矛），V9.7.2 由单兵改为 3 人编组
                for(int m=-1;m<=1;m++)
                {
                    float off=m*0.95f;
                    var shield=Prim(_matBronze,new Vector3(0.4f,1.3f,1.2f)); shield.transform.localPosition=new Vector3(off,0.7f,0.2f);
                    var man=Prim(body,new Vector3(0.5f,0.8f,0.42f)); man.transform.localPosition=new Vector3(off,0.5f,-0.35f);
                    var helm=Prim(_matBronze,new Vector3(0.34f,0.36f,0.34f)); helm.transform.localPosition=new Vector3(off,1.25f,-0.35f);
                    var spear=Prim(_matDark,new Vector3(0.1f,1.8f,0.1f)); spear.transform.localPosition=new Vector3(off+0.22f,1.5f,-0.3f);
                    var stip=Prim(_matBronze,new Vector3(0.16f,0.26f,0.16f)); stip.transform.localPosition=new Vector3(off+0.22f,2.4f,-0.3f);
                }
            }
            else if(d.Id=="chariot")
            {   // 马拉战车：双马 + 车架 + 双轮 + 车夫
                var cart=Prim(_matWood,new Vector3(1.7f,0.45f,1.4f)); cart.transform.localPosition=new Vector3(0,0.42f,0.4f);
                var rail=Prim(_matWood,new Vector3(1.75f,0.55f,0.16f)); rail.transform.localPosition=new Vector3(0,0.92f,0.4f);
                for(int side=-1;side<=1;side+=2)
                { var w=Prim(_matHorseDk,new Vector3(0.65f,0.14f,0.65f)); w.transform.localPosition=new Vector3(0.85f*side,0.28f,0.4f); }
                var yoke=Prim(_matWood,new Vector3(0.16f,0.16f,1.9f)); yoke.transform.localPosition=new Vector3(0,0.75f,-1.0f);
                for(int h=-1;h<=1;h+=2)
                { var horse=Prim(_matHorse,new Vector3(1.1f,0.75f,1.7f)); horse.transform.localPosition=new Vector3(0.45f*h,0.45f,-1.35f);
                  var hhead=Prim(_matHorse,new Vector3(0.4f,0.4f,0.5f)); hhead.transform.localPosition=new Vector3(0.45f*h,0.8f,-2.2f);
                  for(int leg=-1;leg<=1;leg+=2){ var l=Prim(_matHorseDk,new Vector3(0.16f,0.6f,0.16f)); l.transform.localPosition=new Vector3(0.45f*h+0.25f*leg,0.05f,-1.2f); } }
                var driver=Prim(body,new Vector3(0.55f,0.8f,0.55f)); driver.transform.localPosition=new Vector3(0,1.05f,0.4f);
                var dhead=Prim(body,new Vector3(0.38f,0.38f,0.38f)); dhead.transform.localPosition=new Vector3(0,1.6f,0.4f);
            }
            else if(d.Id=="tank")
            {
                var bodyGo=Prim(body,new Vector3(2.2f,0.8f,1.4f),go);
                var turret=Prim(body,new Vector3(1.4f,0.6f,1.0f),go); turret.transform.localPosition=new Vector3(0,0.6f,0);
                var barrel=Prim(_matDark,new Vector3(0.25f,0.25f,1.8f),go); barrel.transform.localPosition=new Vector3(0,0.7f,1.2f);
                TrackPair(go,go);
            }
            else if(d.Id=="apc")
            {
                var bodyGo=Prim(body,new Vector3(2.4f,1.0f,1.6f),go);
                var top=Prim(body,new Vector3(1.6f,0.5f,1.0f),go); top.transform.localPosition=new Vector3(0,0.6f,0);
                TrackPair(go,go);
            }
            else
            {
                var bodyGo=Prim(body,new Vector3(2.6f,0.8f,1.5f),go);
                var rack=Prim(_matDark,new Vector3(0.3f,0.7f,2.4f),go); rack.transform.localPosition=new Vector3(0,0.6f,-0.3f);
                rack.transform.localRotation=Quaternion.Euler(0,0,-18f);
                TrackPair(go,go);
            }
            go.transform.position=new Vector3(u.X, _terrain!=null?_terrain.HeightAt(u.X,u.Z)+0.5f:0.5f, u.Z);
            var col=go.AddComponent<BoxCollider>();
            col.size=new Vector3(2.8f,1.4f,2.0f); col.center=new Vector3(0,0.6f,0);
            var click=go.AddComponent<GroundClick>(); click.Unit=u;
            click.OnClicked=gu=>PixelToCivilization.UI.UIManager.Instance?.ShowGround(gu);
            return go;
        }
        GameObject Prim(Material m,Vector3 size,GameObject parent=null)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent!=null?parent.transform:_root,false);
            g.transform.localScale=size;
            var r=g.GetComponent<Renderer>(); r.material=m; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
        void TrackPair(GameObject parent,GameObject anchor)
        {
            var l=Prim(_matTrack,new Vector3(0.4f,0.35f,1.4f),anchor); l.transform.localPosition=new Vector3(-1.1f,-0.5f,0);
            var r=Prim(_matTrack,new Vector3(0.4f,0.35f,1.4f),anchor); r.transform.localPosition=new Vector3(1.1f,-0.5f,0);
        }

        public class GroundClick : MonoBehaviour
        {
            public GroundUnit Unit;
            public System.Action<GroundUnit> OnClicked;
            private void OnMouseDown()
            {
                var gm=GameManager.Instance;
                if (gm!=null && gm.BlocksWorldClick()) return;
                OnClicked?.Invoke(Unit);
            }
        }

        // ===== Web 探针 =====
        public string Probe()
        {
            int f=0; var seen=new HashSet<string>();
            foreach(var e in Enemies) if(seen.Add(e.FactionId)) f++;
            return "groundActive="+(Active?"1":"0")+" ours="+Ours.Count+" enemy="+Enemies.Count+" factions="+f+" max="+MaxGround;
        }
        /// <summary>V9.7.2 回归：前 4 单位 等级/坐标/【实际生效速度】（EffectiveSpeed，与面板一致）。</summary>
        public string Probe963()
        {
            var sb=new System.Text.StringBuilder();
            sb.Append("G[");
            int n=0;
            foreach(var u in Ours){ if(n++>=4) break;
                sb.Append(u.TypeId).Append(":Lv").Append(u.Level).Append("(").Append(u.X.ToString("F1")).Append(",")
                  .Append(u.Z.ToString("F1")).Append(")spd").Append(EffectiveSpeed(u).ToString("F2")).Append(";"); }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
