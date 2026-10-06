using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.4.6 地面作战部队（V9.5.4 分代）：公元1949（游戏年4949）前为【古典】骑兵/列方阵兵/马拉战车；
    /// 1949 起【直接替换】为【现代】坦克/装甲车/导弹车（与船只 V9.3.3"直接替换而非解锁"同规则）。
    /// 战斗规则参照军舰闭环：雷达索敌（60 格）→ 射程内开火 → 射程外追击（航速+50%）→ 编队（3-7 辆）→ 巡航（无目标陆地巡逻）→ 敌方镜像（3 秒等比例补齐，3-5 阵营，敌方按我方当前时代换型）。
    /// 运行时态不进存档；陆地约束：IsWater 禁行；古典三型造价走木/粮/金（1949 前无钢）。
    /// </summary>
    public class GroundWarfareSystem : GameSystemBase
    {
        public class GroundDef
        {
            public string Id, Name, Icon, ColorHex;
            public float Durability, Attack, Speed;
            public int Range;              // 射程（格）
            public int CostSteel, CostGold;
            public int CostWood, CostFood; // V9.5.4 古典三型（1949 前）以木/粮/金列装，不消耗钢
            public bool Modern;            // true=坦克/装甲车/导弹车；false=骑兵/列方阵兵/马拉战车
            public string Desc="";         // V9.5.7 属性面板说明
        }
        /// <summary>V9.5.4 现代型标识集合（1949 后列装）</summary>
        static readonly HashSet<string> ModernTypes = new(){ "tank","apc","missile_vehicle" };
        /// <summary>V9.5.4 当前时代可列装的地面部队（军事栏直接遍历；古典↔现代按年份替换，不灰显不叠加）</summary>
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
            public float MaxHp, Hp, BaseAttack, Speed;
            public float BaseMaxHp;            // V9.5.7 升级基数（耐久线性成长 12%/级，防指数膨胀）
            public int BaseRange;              // V9.6.3 升级基数（射程 +1 格/级，上限 +5 格）
            public int Range;
            public string FactionId; public long FactionColor;
            public GameObject View;
            public float AttackCd, ThinkCd;
            public int Group;              // 编队组号（我方 0-31 / 敌方 32-63，V9.5.7 阵营分离防"跟着敌方跑"）
            public float RX, RZ;           // 巡航随机航点
            public bool HasRoute;
            public int Kills;              // V9.5.7 战绩（每 2 杀战功升 1 级）
            public bool Lifted;            // V9.6.1 远程投送中（运输机/直升机空运，原系统跳过 AI，坐标由 AirLiftSystem 接管）
        }

        public const int MaxGround = 60;   // 我方+敌方全局上限（地面部队规模显著小于舰队 200）
        public const float RadarGround = 60f;          // 发现距离（格）×Tile=世界单位
        public readonly Dictionary<string,GroundDef> Defs = new();
        public readonly List<GroundUnit> Ours = new();
        public readonly List<GroundUnit> Enemies = new();
        public bool Active;                // 公元1949 起激活（游戏年4949）

        // 地面部队敌对阵营（3-5，颜色与海军五阵营一致便于识别）
        static readonly (string name,long color)[] FactionDefs =
        {
            ("北方装甲军",0xFF6347L),("沙漠军团",0x4169E1L),("南方机械旅",0x32CD32L),("东方突击师",0x9370DBL),("联合远征军",0xFFD700L)
        };
        readonly List<(string name,long color)> _enemyFactions = new();
        bool _factionsInited;
        float _spawnCd, _sepAt, _reformTimer;
        WorldGenerator _terrain;
        Transform _root;
        Material _matTank,_matApc,_matMissile,_matDark,_matTrack;
        Material _matWood,_matHorse,_matHorseDk,_matBronze,_matPhalanx,_matCavalry;   // V9.5.4 古典三型材质

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _terrain = Object.FindObjectOfType<WorldGenerator>();
            _matTank    = ShaderHelper.Pbr(new Color(0.36f,0.50f,0.17f),0.25f,0.45f,9611,0.5f);    // 橄榄绿
            _matApc     = ShaderHelper.Pbr(new Color(0.54f,0.56f,0.48f),0.2f,0.4f,9612,0.5f);     // 沙灰
            _matMissile = ShaderHelper.Pbr(new Color(0.43f,0.31f,0.17f),0.2f,0.45f,9613,0.5f);    // 荒漠
            _matDark    = ShaderHelper.Pbr(new Color(0.12f,0.12f,0.14f),0.2f,0.5f,9614,0.4f);     // 履带/发射架
            _matTrack   = ShaderHelper.Pbr(new Color(0.08f,0.08f,0.09f),0f,0.3f,9615,0.3f);       // 履带
            _matWood    = ShaderHelper.Pbr(new Color(0.52f,0.36f,0.18f),0.15f,0.4f,9621,0.4f);    // 古典·战车车身
            _matHorse   = ShaderHelper.Pbr(new Color(0.45f,0.30f,0.16f),0.2f,0.45f,9622,0.4f);    // 古典·马匹棕
            _matHorseDk = ShaderHelper.Pbr(new Color(0.30f,0.19f,0.10f),0.2f,0.4f,9623,0.4f);     // 古典·马鬃/蹄
            _matBronze  = ShaderHelper.Pbr(new Color(0.72f,0.55f,0.20f),0.55f,0.5f,9624,0.4f);    // 古典·盾/矛头青铜
            _matPhalanx = ShaderHelper.Pbr(new Color(0.62f,0.18f,0.15f),0.15f,0.45f,9625,0.4f);   // 古典·方阵兵红袍
            _matCavalry = ShaderHelper.Pbr(new Color(0.20f,0.34f,0.28f),0.15f,0.45f,9626,0.4f);   // 古典·骑兵青甲
            Defs["tank"]            = new GroundDef{Id="tank",Name="坦克",Icon="🛡",ColorHex="5B7F2B",Durability=180,Attack=40,Range=12,Speed=3.5f,CostSteel=120,CostGold=40,Modern=true,Desc="重型装甲突击，高耐久中射程，攻坚主力"};
            Defs["apc"]             = new GroundDef{Id="apc",Name="装甲车",Icon="🚐",ColorHex="8A8F7A",Durability=100,Attack=15,Range=8,Speed=5.5f,CostSteel=60,CostGold=20,Modern=true,Desc="高速运兵装甲，机动侦察，支援步兵突击"};
            Defs["missile_vehicle"] = new GroundDef{Id="missile_vehicle",Name="导弹车",Icon="🚀",ColorHex="6E4F2B",Durability=120,Attack=70,Range=20,Speed=2.5f,CostSteel=100,CostGold=60,Modern=true,Desc="超远程导弹打击，射程 20 格火力压制"};
            // V9.5.4 古典三型（公元1949 前列装；木/粮/金造价，不耗钢）
            Defs["cavalry"]   = new GroundDef{Id="cavalry",Name="骑兵",Icon="🐎",ColorHex="8B5E3C",Durability=80,Attack=12,Range=2,Speed=6f,CostWood=40,CostGold=10,CostFood=20,Modern=false,Desc="古典高速突击骑兵，机动冲击敌阵"};
            Defs["phalanx"]   = new GroundDef{Id="phalanx",Name="列方阵兵",Icon="🛡",ColorHex="C0C0C8",Durability=140,Attack=18,Range=2,Speed=2f,CostWood=20,CostGold=10,CostFood=30,Modern=false,Desc="古典重装方阵，高耐久列阵防守"};
            Defs["chariot"]   = new GroundDef{Id="chariot",Name="马拉战车",Icon="🏇",ColorHex="7A5C2E",Durability=100,Attack=15,Range=3,Speed=5f,CostWood=80,CostGold=30,CostFood=20,Modern=false,Desc="古典马拉战车，冲锋践踏撕裂敌阵"};
            var go=new GameObject("GroundWarfare"); go.transform.SetParent(gm.transform,false);
            _root=go.transform;
        }

        /// <summary>V9.5.4 古典/现代时代匹配：现代型需 1949（游戏年4949）后；古典型需 1949 前</summary>
        bool EraMatch(string typeId)
        {
            bool modern=ModernTypes.Contains(typeId);
            return modern == (S.Year>=4949);
        }

        /// <summary>建造地面部队（玩家）：按时代列装——1949 前古典三型（骑兵/列方阵兵/马拉战车）、1949 起现代三型（坦克/装甲车/导弹车）；扣对应资源，上限 60，须列装在陆地</summary>
        public bool BuildGround(string typeId,float x,float z)
        {
            if(!Defs.TryGetValue(typeId,out var d)){ Debug.Log("[Ground] BuildGround reject: unknown type "+typeId); return false; }
            // V9.5.4 防坦克/装甲车/导弹车列装在水面
            if(_terrain==null) _terrain=Object.FindObjectOfType<WorldGenerator>();
            if(_terrain!=null && _terrain.IsWater(x,z)){ GM.AddEvent("bad","陆地载具不能列装在水面"); Debug.Log("[Ground] BuildGround reject: water at "+x.ToString("F1")+","+z.ToString("F1")); return false; }
            // V9.5.4 分代替换：现代型需 1949（游戏年4949）后、古典型需 1949 前；同时段只列装对应世代
            if(!EraMatch(typeId))
            {
                bool modern=ModernTypes.Contains(typeId);
                if(modern) GM.AddEvent("bad",d.Name+" 公元1949年才列装（当前 公元"+(S.Year-3000)+"，还需 "+(4949-S.Year)+" 年）");
                else       GM.AddEvent("bad","公元1949年起已列装现代地面部队（坦克/装甲车/导弹车），古典"+d.Name+"不再列装");
                Debug.Log("[Ground] BuildGround reject: era mismatch "+typeId+" year="+S.Year);
                return false;
            }
            if(Ours.Count+Enemies.Count>=MaxGround){ GM.AddEvent("bad","已达地面部队上限 "+MaxGround+" 辆"); Debug.Log("[Ground] BuildGround reject: max"); return false; }
            if(S.GetRes("steel")<d.CostSteel||S.GetRes("gold")<d.CostGold||S.GetRes("wood")<d.CostWood||S.GetRes("food")<d.CostFood)
            { GM.AddEvent("bad","资源不足，无法列装"+d.Name); Debug.Log("[Ground] BuildGround reject: resources"); return false; }
            S.AddRes("steel",-d.CostSteel); S.AddRes("gold",-d.CostGold); S.AddRes("wood",-d.CostWood); S.AddRes("food",-d.CostFood);
            var u=new GroundUnit{TypeId=typeId,Name=d.Name,Side="ours",X=x,Z=z,Level=1,
                BaseMaxHp=d.Durability,MaxHp=d.Durability,Hp=d.Durability,BaseAttack=d.Attack,BaseRange=d.Range,Range=d.Range,Speed=d.Speed};
            u.View=BuildView(u,d);
            Ours.Add(u); AssignGroup(u);
            GM.AddEvent("good",d.Icon+" 新"+d.Name+"列装！");
            Debug.Log("[Ground] BuildGround OK: "+d.Name+" at "+x.ToString("F1")+","+z.ToString("F1")+" ours="+Ours.Count);
            return true;
        }

        /// <summary>Debug/Web：无消耗造车（探针入口；按当前时代换型）。V9.6.3f：生成点改为出生地（主村落）附近 20–60 单位环，
        /// 不再绕世界原点 (0,0)——原实现导致"生成后不在出生地"（主大陆中心在出生地而非原点）。</summary>
        public int DebugBuildOwn(int n)
        {
            if(_terrain==null) return 0;
            int made=0;
            string[] types=S.Year>=4949
                ? new[]{"tank","apc","missile_vehicle"}
                : new[]{"cavalry","phalanx","chariot"};
            float ox = GM.State.VillageX.Count>0 ? GM.State.VillageX[0] : 0f;   // V9.6.3f 出生地中心
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
                    if(!_terrain.IsWater(x,z)) land=true;
                }
                if(!land) continue;
                var u=new GroundUnit{TypeId=t,Name=d.Name,Side="ours",X=x,Z=z,Level=1,
                    BaseMaxHp=d.Durability,MaxHp=d.Durability,Hp=d.Durability,BaseAttack=d.Attack,BaseRange=d.Range,Range=d.Range,Speed=d.Speed};
                u.View=BuildView(u,d);
                Ours.Add(u); AssignGroup(u); made++;
            }
            if(made>0) GM.AddEvent("good","🛡 Debug 我方地面部队 "+made+" 辆就位（出生地附近）");
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
            // V9.5.4 敌方镜像按我方当前时代换型（1949 前古典三型，1949 起现代三型）
            string[] types=S.Year>=4949
                ? new[]{"tank","apc","missile_vehicle","tank","apc"}
                : new[]{"cavalry","phalanx","chariot","cavalry","phalanx"};
            string t=types[Random.Range(0,types.Length)]; var d=Defs[t];
            var fac=LeastRepresentedFaction();
            // V9.6.3f：敌方镜像同样以出生地为中心（60–140 单位环），与我方出生地部队处于同一雷达作战半径内
            // （原绕原点 (0,0) 生成，出生地偏离原点时双方相距过远 → 互不可见 → "不主动寻敌/不战斗"）
            float ox = GM.State.VillageX.Count>0 ? GM.State.VillageX[0] : 0f;
            float oz = GM.State.VillageZ.Count>0 ? GM.State.VillageZ[0] : 0f;
            float x=0,z=0; bool land=false;
            for(int k=0;k<32&&!land;k++)
            { float a=Random.value*Mathf.PI*2, rr=60f+Random.value*80f;
              x=ox+Mathf.Cos(a)*rr; z=oz+Mathf.Sin(a)*rr;
              if(_terrain==null||!_terrain.IsWater(x,z)) land=true; }
            var u=new GroundUnit{TypeId=t,Name=fac.name+"·"+d.Name,Side="enemy",X=x,Z=z,Level=1,
                BaseMaxHp=d.Durability,MaxHp=d.Durability,Hp=d.Durability,BaseAttack=d.Attack,BaseRange=d.Range,Range=d.Range,Speed=d.Speed,
                FactionId=fac.name,FactionColor=fac.color};
            u.View=BuildView(u,d,fac.color);
            Enemies.Add(u); AssignGroup(u);
        }

        /// <summary>V9.5.0 新局清理：地面部队运行时列表不进 GameState，必须在此销毁视图并清空，防上一局坦克/装甲残留。</summary>
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
            // V9.5.4 地面部队自开局即可用（1949 前列装古典三型）；敌方镜像随我方数量触发
            if(!Active) Active=true;
            if(_terrain==null) _terrain=Object.FindObjectOfType<WorldGenerator>();
            // 敌方镜像：我方地面部队数 = 敌方数（3 秒补齐，≤60 上限）
            _spawnCd-=dt;
            if(_spawnCd<=0 && Ours.Count>0)
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
        bool _enemySeen;

        void UpdateOurs(float dt)
        {
            _reformTimer-=dt;
            if(_reformTimer<=0f){ _reformTimer=20f; RebuildGroups(); }
            for(int i=0;i<Ours.Count;i++)
            {
                var u=Ours[i]; if(u.View==null) continue;
                if (u.Lifted) continue;   // V9.6.1 远程投送中：不参与地面 AI（坐标/视图由 AirLiftSystem 接管）
                u.AttackCd-=dt;
                CombatTarget ct=GM.Combat.NearestHostile(u.X,u.Z,RadarGround*GameConstants.Tile,CombatSystem.PlayerKey);
                if(ct!=null)
                {
                    float d=Mathf.Sqrt((ct.X-u.X)*(ct.X-u.X)+(ct.Z-u.Z)*(ct.Z-u.Z));
                    float engage=u.Range*GameConstants.Tile;
                    if(d>engage)
                    {
                        // 追击：战斗航速 +50%；陆地约束防下水
                        MoveToward(u,ct.X,ct.Z,dt,1.5f);
                    }
                    else if(u.AttackCd<=0)
                    {
                        GroundFire(u,ct); u.AttackCd=2f;
                    }
                }
                // V9.6.0 紧急集结令：绿旗（地面部队）100 格内优先向军旗集结列阵（高于巡航）
                else if (GM.Rally!=null && GM.Rally.Active("ground"))
                {
                    var rp=GM.Rally.Target("ground");
                    if (rp.HasValue)
                    {
                        float rd=Mathf.Sqrt((rp.Value.x-u.X)*(rp.Value.x-u.X)+(rp.Value.y-u.Z)*(rp.Value.y-u.Z));
                        if (rd<=RallySystem.RallyRange*GameConstants.Tile)
                        {
                            if (rd>RallySystem.FormRange) MoveToward(u,rp.Value.x,rp.Value.y,dt,1f);
                            // 已入列阵圈：原地待命（不巡航）
                            continue;
                        }
                        // V9.6.2 超 100 格：运输机/直升机远程投送（未解锁广播提示；成功则等机，失败落巡航）
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
                e.AttackCd-=dt;
                CombatTarget ct=GM.Combat.NearestHostile(e.X,e.Z,RadarGround*GameConstants.Tile,e.FactionId);
                if(ct!=null)
                {
                    float d=Mathf.Sqrt((ct.X-e.X)*(ct.X-e.X)+(ct.Z-e.Z)*(ct.Z-e.Z));
                    float engage=e.Range*GameConstants.Tile;
                    if(d>engage) MoveToward(e,ct.X,ct.Z,dt,1.4f);
                    else if(e.AttackCd<=0)
                    {
                        EnemyGroundFire(e,ct); e.AttackCd=2.2f;
                    }
                }
                else Cruise(e,dt);
            }
        }

        /// <summary>V9.5.7 巡航+组阵：领队自由巡航；组员围绕领队按槽位环形阵型跟随（阵型半径 6），
        /// 距阵位 >3 格才向阵位移动，到位即保持——不再被强制拉回组中心点"原地徘徊"。
        /// 旧实现 Cruise 把组员目标强制覆盖为 GroupCenter（最后加入者）坐标，到达 d²<4 后 HasRoute=false，
        /// 下一帧重新选点又被覆盖回组中心 → 整组被拉到某坐标堆叠不动，且不与敌人接战。</summary>
        void Cruise(GroundUnit u,float dt)
        {
            if(_terrain==null) return;
            // 组员（非领队）：向"领队+槽位偏移"阵位移动，到位后原地待命保持阵型
            if(u.Group>=0 && GroupCenter.TryGetValue(u.Group,out var leader) && leader!=null && leader!=u)
            {
                int idx=GroupIndexIn(leader.Group,u);
                int cnt=GroupCount(leader.Group);
                float ang=(idx*360f/Mathf.Max(1,cnt))*Mathf.Deg2Rad;
                float sx=leader.X+Mathf.Sin(ang)*6f, sz=leader.Z+Mathf.Cos(ang)*6f;
                // 阵位落水则绕领队旋转找最近陆地（最多 12 次）
                for(int k=0;k<12&&_terrain.IsWater(sx,sz);k++)
                { ang+=0.52f; sx=leader.X+Mathf.Sin(ang)*6f; sz=leader.Z+Mathf.Cos(ang)*6f; }
                float ddx=sx-u.X, ddz=sz-u.Z;
                if(ddx*ddx+ddz*ddz>9f)                       // 距阵位 >3 格：向阵位移动
                {
                    MoveToward(u,sx,sz,dt,0.85f);
                    return;
                }
                u.HasRoute=true;                             // 已到位：保持待命状态，防下帧重新选点被拉走
                return;
            }
            // 领队 / 无组：陆地随机巡航。V9.6.3f：航点以单位自身为中心（40–100 单位环）——
            // 原绕世界原点 (0,0) 找点，出生地偏离原点时单位会被拉到原点附近"莫名坐标"，视觉即"被拉到某处不动"
            if(!u.HasRoute)
            {
                u.HasRoute=true;
                for(int k=0;k<24;k++)
                { float a=Random.value*Mathf.PI*2, rr=40f+Random.value*60f;
                  float nx=u.X+Mathf.Cos(a)*rr, nz=u.Z+Mathf.Sin(a)*rr;
                  if(!_terrain.IsWater(nx,nz)){ u.RX=nx; u.RZ=nz; break; } }
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
        /// <summary>同组同阵营中排在 u 之前的非领队个数（阵型槽位索引）</summary>
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

        /// <summary>V9.6.3 地面部队速度统一钳制：Lv1=0.01 格/秒 → Lv10=0.10 格/秒（最高 0.1、最低 0.01，随等级线性成长）。
        /// V9.6.3f：钳制整体上调 ×10 → Lv1=0.10 格/秒 → Lv10=1.00 格/秒。
        /// 根因：0.01–0.10 格/秒 ×Tile4 = 0.04–0.40 世界单位/秒，肉眼不可见移动，60 格雷达内追击需数十分钟
        /// → "生成后不主动寻敌、组队、战斗"（实际在动但极慢）。保留钳制语义与随等级成长，恢复可感知作战移动。</summary>
        public static float ClampedGroundSpeed(GroundUnit u)
        {
            float lv = Mathf.Clamp(u!=null ? u.Level : 1, 1, 10);
            return 0.10f + 0.90f * (lv - 1f) / 9f;
        }

        void MoveToward(GroundUnit u,float tx,float tz,float dt,float mul)
        {
            if(_terrain==null) return;
            float dx=tx-u.X,dz=tz-u.Z; float dist=Mathf.Sqrt(dx*dx+dz*dz);
            if(dist<0.5f) return;
            // V9.6.3 速度统一钳制 [0.01,0.10] 格/秒（×Tile=世界单位/秒，随等级成长）；去掉旧 30f 帧因子防"飞跑"
            float sp=ClampedGroundSpeed(u)*GameConstants.Tile*mul;
            float nx=u.X+dx/dist*sp*dt, nz=u.Z+dz/dist*sp*dt;
            // 陆地约束：不得下水；前方是水则贴岸转向
            if(_terrain.IsWater(nx,nz))
            {
                bool slid=false;
                for(int turn=30;turn<=150&&!slid;turn+=30)
                {
                    float rad=turn*Mathf.Deg2Rad, cs=Mathf.Cos(rad), sn=Mathf.Sin(rad);
                    for(int side=-1;side<=1&&!slid;side+=2)
                    {
                        float rx=dx/dist*cs-side*dz/dist*sn, rz=dz/dist*cs+side*dx/dist*sn;
                        float sx=u.X+rx*sp*dt, sz=u.Z+rz*sp*dt;   // V9.6.3 与世界速度一致（去 30f 帧因子）
                        if(!_terrain.IsWater(sx,sz)){ nx=sx; nz=sz; slid=true; }
                    }
                }
                if(!slid){ u.HasRoute=false; return; }
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

        GroundUnit NearestEnemy(GroundUnit u,float range)
        {
            GroundUnit best=null; float bd=range*range;
            foreach(var e in Enemies)
            {
                if(e.View==null) continue;
                float dx=e.X-u.X,dz=e.Z-u.Z,d=dx*dx+dz*dz;
                if(d<bd){ bd=d; best=e; }
            }
            return best;
        }
        GroundUnit NearestOurs(GroundUnit e,float range)
        {
            GroundUnit best=null; float bd=range*range;
            foreach(var o in Ours)
            {
                if(o.View==null) continue;
                float dx=o.X-e.X,dz=o.Z-e.Z,d=dx*dx+dz*dz;
                if(d<bd){ bd=d; best=o; }
            }
            return best;
        }

        void Hit(GroundUnit t,float atk)
        {
            if(t==null) return;
            t.Hp-=atk;
            if(t.View!=null) t.View.transform.localScale=new Vector3(1,1,1);   // 命中反馈占位（保持稳定）
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
            // V9.5.7 不再因 View==null 跳过注册：View 只是表现（被销毁/延迟生成），逻辑坐标与 HP 仍有效；
            // 旧实现 View 丢失单位不进战斗目录 → 敌人"看不见"它 → 不自动战斗。
            foreach(var u in Ours)
            {
                if(u==null||u.Hp<=0f) continue;
                c.Add(CombatSystem.K_GROUND,u,u.X,u.Z,u.Hp,u.BaseAttack,u.Range*GameConstants.Tile,CombatSystem.PlayerKey);
            }
            foreach(var e in Enemies)
            {
                if(e==null||e.Hp<=0f) continue;
                c.Add(CombatSystem.K_GROUND,e,e.X,e.Z,e.Hp,e.BaseAttack,e.Range*GameConstants.Tile,e.FactionId);
            }
        }

        public void DamageGround(GroundUnit u,float dmg)
        {
            if(u==null||u.Hp<=0f||dmg<=0f) return;
            u.Hp-=dmg;
        }

        // ===== V9.5.7 地面部队升级/得分 =====
        /// <summary>升级：耐久按基础值线性 +12%/级（防指数膨胀）、攻击 +8%/级、回血 30%；战斗中击杀达 2 的倍数自动战功升级</summary>
        public void LevelUp(GroundUnit u,bool free=false)
        {
            if(u==null||u.Level>=10){ if(!free&&u!=null) GM.AddEvent("bad","该部队已达最高等级 Lv10"); return; }
            u.Level++;
            u.MaxHp=Mathf.RoundToInt(u.BaseMaxHp*(1f+0.12f*(u.Level-1)));
            u.BaseAttack=Mathf.RoundToInt(u.BaseAttack*1.08f);
            u.Range=Mathf.Min(50, Mathf.Min(u.BaseRange+5, u.BaseRange+(u.Level-1)));   // V9.6.3 射程 +1 格/级（上限 +5）；V9.6.3f2 硬上限 50 格（用户要求"地面作战单位≤50格"）
            u.Hp=Mathf.Min(u.MaxHp, u.Hp+u.MaxHp*0.3f);
            GM.AddEvent("good","🎖 "+(free?"战功晋升":"升级")+"："+u.Name+" → Lv"+u.Level
                +"（攻击 "+u.BaseAttack+" 耐久 "+u.MaxHp+" 射程 "+u.Range+"）");
        }
        /// <summary>玩家资源升级造价：按当前等级线性上涨（钢/金按基础造价的 55%/级，古典型按木/粮/金）</summary>
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

        /// <summary>我方军车开火：同类型军车走 Hit+DestroyUnit；跨类型（舰/步/骑/建筑）走统一 Damage</summary>
        void GroundFire(GroundUnit u,CombatTarget ct)
        {
            // V9.5.4 统一武器特效：坦克炮口闪光+开火音效 / 命中爆炸+爆炸音效（桶池化）
            WeaponFxSystem.Muzzle(new Vector3(u.X,1.4f,u.Z),1.5f);
            WeaponFxSystem.Sfx("cannon_fire",new Vector3(u.X,1.4f,u.Z),0.65f);
            if(ct.Ref is GroundUnit tgt)
            {
                var hp=new Vector3(ct.X,1f,ct.Z);
                WeaponFxSystem.Explosion(hp); WeaponFxSystem.Sfx("cannon_explode",hp,0.8f);
                Hit(tgt,u.BaseAttack);
                if(tgt.Hp<=0f)
                {
                    DestroyUnit(tgt,Enemies);
                    // V9.5.7 战斗得分与战功升级：每 2 杀升 1 级（免费，战斗中即时生效）
                    u.Kills++;
                    GM.AddEvent("good","💥 击毁一辆敌"+tgt.Name+"！（"+u.Name+" 战绩 "+u.Kills+"）");
                    if(u.Kills%2==0 && u.Level<10) LevelUp(u,true);
                }
                return;
            }
            WeaponFxSystem.Explosion(new Vector3(ct.X,1f,ct.Z)); WeaponFxSystem.Sfx("cannon_explode",new Vector3(ct.X,1f,ct.Z),0.8f);
            GM.Combat.Damage(ct,u.BaseAttack);
        }

        /// <summary>敌方军车开火：目标为我方/他派军车走 Hit+DestroyUnit（敌方各派混战）；跨类型走统一 Damage</summary>
        void EnemyGroundFire(GroundUnit e,CombatTarget ct)
        {
            // V9.5.4 统一武器特效（敌方同样生效）
            WeaponFxSystem.Muzzle(new Vector3(e.X,1.4f,e.Z),1.5f);
            WeaponFxSystem.Sfx("cannon_fire",new Vector3(e.X,1.4f,e.Z),0.65f);
            if(ct.Ref is GroundUnit tgt)
            {
                var hp=new Vector3(ct.X,1f,ct.Z);
                WeaponFxSystem.Explosion(hp); WeaponFxSystem.Sfx("cannon_explode",hp,0.8f);
                Hit(tgt,e.BaseAttack);
                if(tgt.Hp<=0f)
                {
                    if(ct.Key==CombatSystem.PlayerKey){ DestroyUnit(tgt,Ours); GM.AddEvent("bad","💥 我方一辆"+tgt.Name+"被击毁！"); }
                    else { DestroyUnit(tgt,Enemies); GM.AddEvent("bad","💥 敌方"+tgt.Name+"在混战中被击毁！"); }
                }
                return;
            }
            WeaponFxSystem.Explosion(new Vector3(ct.X,1f,ct.Z)); WeaponFxSystem.Sfx("cannon_explode",new Vector3(ct.X,1f,ct.Z),0.8f);
            GM.Combat.Damage(ct,e.BaseAttack);
        }

        /// <summary>统一清理外部（舰/塔/步骑）击杀的军车，销毁视图后移除</summary>
        void CleanupDead()
        {
            Ours.RemoveAll(u=>{ if(u.Hp<=0f){ if(u.View!=null)Object.Destroy(u.View); return true; } return false; });
            Enemies.RemoveAll(u=>{ if(u.Hp<=0f){ if(u.View!=null)Object.Destroy(u.View); return true; } return false; });
        }

        /// <summary>V9.5.7 编队分配：我方组号 0-31、敌方组号 32-63（阵营分离，防止我方单位与敌方同组被领到敌方位置）；
        /// 组中心=本阵营首个加入者（领队），不再被"最后加入者"覆盖（旧实现组员全被拉到最后加入者坐标堆叠）。</summary>
        void AssignGroup(GroundUnit u)
        {
            bool ours=u.Side=="ours";
            var list=ours?Ours:Enemies;
            var cnt=new Dictionary<int,int>();
            foreach(var o in list){ if(o.Group>=0) cnt[o.Group]=cnt.TryGetValue(o.Group,out var c)?c+1:1; }
            int start=ours?0:32, end=ours?32:64, g=start;
            for(int k=start;k<end;k++){ if(!cnt.TryGetValue(k,out var c)||c<3){ g=k; break; } }
            u.Group=g;
            if(!GroupCenter.ContainsKey(g)) GroupCenter[g]=u;   // 领队=首个加入者
        }
        /// <summary>V9.5.7 重建组中心：保留各阵营首个单位作领队（我方 0-31 / 敌方 32-63 互不覆盖）</summary>
        void RebuildGroups()
        {
            GroupCenter.Clear();
            foreach(var o in Ours) if(!GroupCenter.ContainsKey(o.Group)) GroupCenter[o.Group]=o;
            foreach(var e in Enemies) if(!GroupCenter.ContainsKey(e.Group)) GroupCenter[e.Group]=e;
        }

        /// <summary>V9.4.6 地面部队体积分离（4Hz）：防编队/巡航重叠，半径 5</summary>
        void SeparateGround(float dt)
        {
            if(Time.unscaledTime<_sepAt) return;
            _sepAt=Time.unscaledTime+0.25f;
            const float minSq=5f*5f;
            SeparateList(Ours,minSq,dt); SeparateList(Enemies,minSq,dt);
        }
        void SeparateList(List<GroundUnit> list,float minSq,float dt)
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
                        float d=Mathf.Sqrt(d2); float push=(5f-d)*0.5f*4f*dt;
                        float ux=dx/d,uz=dz/d;
                        a.X-=ux*push; a.Z-=uz*push; b.X+=ux*push; b.Z+=uz*push;
                    }
                }
            }
        }

        // ===== 程序化视图（Cube 组合，顶点量安全；绝不用内置 Sphere）=====
        GameObject BuildView(GroundUnit u,GroundDef d,long?factionColor=null)
        {
            var go=new GameObject("Ground_"+d.Id);
            go.transform.SetParent(_root,false);
            var colorHex=factionColor!=null?("#"+((long)factionColor).ToString("X6")):d.ColorHex;
            // V9.5.4 古典三型材质：骑兵青甲 / 方阵兵红袍 / 战车木身；现代按原橄榄绿/沙灰/荒漠
            Material body = d.Id=="tank"?_matTank : d.Id=="apc"?_matApc
                         : d.Id=="missile_vehicle"?_matMissile
                         : d.Id=="cavalry"?_matCavalry : d.Id=="phalanx"?_matPhalanx : _matWood;
            ColorUtility.TryParseHtmlString(colorHex,out var fc);
            if(factionColor!=null) body=ShaderHelper.Pbr(fc,0.25f,0.5f,9700+(int)fc.GetHashCode()%200,0.5f);
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
            {   // 列方阵兵：红袍兵 + 巨盾 + 长矛（盾防正面）
                var shield=Prim(_matBronze,new Vector3(0.45f,1.5f,1.45f)); shield.transform.localPosition=new Vector3(0,0.75f,0.2f);
                var bodyGo=Prim(body,new Vector3(0.6f,0.9f,0.5f)); bodyGo.transform.localPosition=new Vector3(0,0.55f,-0.35f);
                var helm=Prim(_matBronze,new Vector3(0.4f,0.42f,0.4f)); helm.transform.localPosition=new Vector3(0,1.4f,-0.35f);
                var spear=Prim(_matDark,new Vector3(0.12f,2.0f,0.12f)); spear.transform.localPosition=new Vector3(0.3f,1.7f,-0.3f);
                var tip=Prim(_matBronze,new Vector3(0.2f,0.32f,0.2f)); tip.transform.localPosition=new Vector3(0.3f,2.65f,-0.3f);
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
            // V9.5.7 可点击选中：根节点补 BoxCollider + GroundClick（旧实现 Prim 销毁全部 Collider 且无 OnMouseDown，
            // 地面部队永远无法被鼠标拾取 → 无法查看属性/升级）。子部件仍销毁 Collider 防射线干扰。
            var col=go.AddComponent<BoxCollider>();
            col.size=new Vector3(2.8f,1.4f,2.0f); col.center=new Vector3(0,0.6f,0);
            var click=go.AddComponent<GroundClick>(); click.Unit=u;
            click.OnClicked=gu=>PixelToCivilization.UI.UIManager.Instance?.ShowGround(gu);
            return go;
        }
        GameObject Prim(Material m,Vector3 size,GameObject parent=null)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(g.GetComponent<Collider>());   // 防点击拾取干扰
            g.transform.SetParent(parent!=null?parent.transform:_root,false);   // V9.5.4 部件挂单位节点而非世界根：修复"部件钉原点、空壳移动"
            g.transform.localScale=size;
            var r=g.GetComponent<Renderer>(); r.material=m; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
        void TrackPair(GameObject parent,GameObject anchor)
        {
            var l=Prim(_matTrack,new Vector3(0.4f,0.35f,1.4f),anchor); l.transform.localPosition=new Vector3(-1.1f,-0.5f,0);
            var r=Prim(_matTrack,new Vector3(0.4f,0.35f,1.4f),anchor); r.transform.localPosition=new Vector3(1.1f,-0.5f,0);
        }

        /// <summary>V9.5.7 地面部队点击桥（对齐 ShipClick/CartClick，依赖根节点 Collider；UI/建造面板/放置模式不弹）</summary>
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

        /// <summary>V9.4.6 Web 探针</summary>
        public string Probe()
        {
            int f=0; var seen=new HashSet<string>();
            foreach(var e in Enemies) if(seen.Add(e.FactionId)) f++;
            return "groundActive="+(Active?"1":"0")+" ours="+Ours.Count+" enemy="+Enemies.Count+" factions="+f+" max="+MaxGround;
        }
        /// <summary>V9.6.3 浏览器回归：输出我方每单位 等级/坐标/钳制速度（ClampedGroundSpeed）与首单位 10 秒位移测速基线</summary>
        public string Probe963()
        {
            var sb=new System.Text.StringBuilder();
            sb.Append("G[");
            int n=0;
            foreach(var u in Ours){ if(n++>=4) break;
                sb.Append(u.TypeId).Append(":Lv").Append(u.Level).Append("(").Append(u.X.ToString("F1")).Append(",")
                  .Append(u.Z.ToString("F1")).Append(")spd").Append(ClampedGroundSpeed(u).ToString("F3")).Append(";"); }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
