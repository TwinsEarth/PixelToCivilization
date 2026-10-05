using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.4.6 地面作战部队：坦克/装甲车/导弹车（公元 1949 = 游戏年 4949 解锁，与军舰时代门槛一致）。
    /// 战斗规则参照军舰闭环：雷达索敌（60 格）→ 射程内开火 → 射程外追击（航速+50%）→ 编队（3-7 辆）→ 巡航（无目标陆地巡逻）→ 敌方镜像（3 秒等比例补齐，3-5 阵营）。
    /// 运行时态不进存档；陆地约束：IsWater 禁行。
    /// </summary>
    public class GroundWarfareSystem : GameSystemBase
    {
        public class GroundDef
        {
            public string Id, Name, Icon, ColorHex;
            public float Durability, Attack, Speed;
            public int Range;              // 射程（格）
            public int CostSteel, CostGold;
        }
        public class GroundUnit
        {
            public string TypeId, Name, Side;
            public float X, Z;
            public int Level;
            public float MaxHp, Hp, BaseAttack, Speed;
            public int Range;
            public string FactionId; public long FactionColor;
            public GameObject View;
            public float AttackCd, ThinkCd;
            public int Group;              // 编队组号
            public float RX, RZ;           // 巡航随机航点
            public bool HasRoute;
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

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _terrain = Object.FindObjectOfType<WorldGenerator>();
            _matTank    = ShaderHelper.Pbr(new Color(0.36f,0.50f,0.17f),0.25f,0.45f,9611,0.5f);    // 橄榄绿
            _matApc     = ShaderHelper.Pbr(new Color(0.54f,0.56f,0.48f),0.2f,0.4f,9612,0.5f);     // 沙灰
            _matMissile = ShaderHelper.Pbr(new Color(0.43f,0.31f,0.17f),0.2f,0.45f,9613,0.5f);    // 荒漠
            _matDark    = ShaderHelper.Pbr(new Color(0.12f,0.12f,0.14f),0.2f,0.5f,9614,0.4f);     // 履带/发射架
            _matTrack   = ShaderHelper.Pbr(new Color(0.08f,0.08f,0.09f),0f,0.3f,9615,0.3f);       // 履带
            Defs["tank"]            = new GroundDef{Id="tank",Name="坦克",Icon="🛡",ColorHex="5B7F2B",Durability=180,Attack=40,Range=12,Speed=3.5f,CostSteel=120,CostGold=40};
            Defs["apc"]             = new GroundDef{Id="apc",Name="装甲车",Icon="🚐",ColorHex="8A8F7A",Durability=100,Attack=15,Range=8,Speed=5.5f,CostSteel=60,CostGold=20};
            Defs["missile_vehicle"] = new GroundDef{Id="missile_vehicle",Name="导弹车",Icon="🚀",ColorHex="6E4F2B",Durability=120,Attack=70,Range=20,Speed=2.5f,CostSteel=100,CostGold=60};
            var go=new GameObject("GroundWarfare"); go.transform.SetParent(gm.transform,false);
            _root=go.transform;
        }

        /// <summary>建造地面部队（玩家）：公元1949 起解锁，扣钢/金，上限 60，须列装在陆地</summary>
        public bool BuildGround(string typeId,float x,float z)
        {
            if(!Defs.TryGetValue(typeId,out var d)) return false;
            // V9.5.3 防坦克/装甲车/导弹车列装在水面
            if(_terrain==null) _terrain=Object.FindObjectOfType<WorldGenerator>();
            if(_terrain!=null && _terrain.IsWater(x,z)){ GM.AddEvent("bad","陆地载具不能列装在水面"); return false; }
            if(S.Year<4949){ GM.AddEvent("bad","地面作战部队公元1949年才列装（当前 公元"+(S.Year-3000)+"）"); return false; }
            if(Ours.Count+Enemies.Count>=MaxGround){ GM.AddEvent("bad","已达地面部队上限 "+MaxGround+" 辆"); return false; }
            if(S.GetRes("steel")<d.CostSteel||S.GetRes("gold")<d.CostGold){ GM.AddEvent("bad","资源不足，无法列装"+d.Name); return false; }
            S.AddRes("steel",-d.CostSteel); S.AddRes("gold",-d.CostGold);
            var u=new GroundUnit{TypeId=typeId,Name=d.Name,Side="ours",X=x,Z=z,Level=1,
                MaxHp=d.Durability,Hp=d.Durability,BaseAttack=d.Attack,Range=d.Range,Speed=d.Speed};
            u.View=BuildView(u,d);
            Ours.Add(u); AssignGroup(u);
            GM.AddEvent("good",d.Icon+" 新"+d.Name+"列装！");
            return true;
        }

        /// <summary>Debug/Web：无消耗造车（探针入口）</summary>
        public int DebugBuildOwn(int n)
        {
            if(_terrain==null) return 0;
            int made=0;
            string[] types={"tank","apc","missile_vehicle"};
            for(int k=0;k<n;k++)
            {
                string t=types[Random.Range(0,types.Length)]; var d=Defs[t];
                if(Ours.Count+Enemies.Count>=MaxGround) break;
                float x=0,z=0; bool land=false;
                for(int i=0;i<32&&!land;i++)
                {
                    float ang=Random.value*Mathf.PI*2f, dist=30f+Random.value*50f;
                    x=Mathf.Cos(ang)*dist; z=Mathf.Sin(ang)*dist;
                    if(!_terrain.IsWater(x,z)) land=true;
                }
                if(!land) continue;
                var u=new GroundUnit{TypeId=t,Name=d.Name,Side="ours",X=x,Z=z,Level=1,
                    MaxHp=d.Durability,Hp=d.Durability,BaseAttack=d.Attack,Range=d.Range,Speed=d.Speed};
                u.View=BuildView(u,d);
                Ours.Add(u); AssignGroup(u); made++;
            }
            if(made>0) GM.AddEvent("good","🛡 Debug 我方地面部队 "+made+" 辆就位");
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
            string[] types={"tank","apc","missile_vehicle","tank","apc"};
            string t=types[Random.Range(0,types.Length)]; var d=Defs[t];
            var fac=LeastRepresentedFaction();
            float x=0,z=0; bool land=false;
            for(int k=0;k<32&&!land;k++)
            { float a=Random.value*Mathf.PI*2, rr=60f+Random.value*40f;
              x=Mathf.Cos(a)*rr; z=Mathf.Sin(a)*rr;
              if(_terrain==null||!_terrain.IsWater(x,z)) land=true; }
            var u=new GroundUnit{TypeId=t,Name=fac.name+"·"+d.Name,Side="enemy",X=x,Z=z,Level=1,
                MaxHp=d.Durability,Hp=d.Durability,BaseAttack=d.Attack,Range=d.Range,Speed=d.Speed,
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
            if(!Active){ if(S.Year>=4949) Active=true; else return; }
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

        /// <summary>巡航：无目标时朝陆地随机航点缓行（半径 40-100），到点换点</summary>
        void Cruise(GroundUnit u,float dt)
        {
            if(_terrain==null) return;
            if(!u.HasRoute)
            {
                u.HasRoute=true;
                for(int k=0;k<24;k++)
                { float a=Random.value*Mathf.PI*2, rr=40f+Random.value*60f;
                  float nx=Mathf.Cos(a)*rr, nz=Mathf.Sin(a)*rr;
                  if(!_terrain.IsWater(nx,nz)){ u.RX=nx; u.RZ=nz; break; } }
                // 编队聚拢：组内成员向组中心（领队位置）靠拢保持阵型
                if(u.Group>=0 && u.Group<GroupCenter.Count)
                { var c=GroupCenter[u.Group]; if(c!=null){ u.RX=c.X; u.RZ=c.Z; } }
            }
            float dx=u.RX-u.X,dz=u.RZ-u.Z;
            if(dx*dx+dz*dz<4f){ u.HasRoute=false; return; }
            MoveToward(u,u.RX,u.RZ,dt,0.85f);
        }
        readonly Dictionary<int,GroundUnit> GroupCenter = new();

        void MoveToward(GroundUnit u,float tx,float tz,float dt,float mul)
        {
            if(_terrain==null) return;
            float dx=tx-u.X,dz=tz-u.Z; float dist=Mathf.Sqrt(dx*dx+dz*dz);
            if(dist<0.5f) return;
            float sp=u.Speed*mul;
            float nx=u.X+dx/dist*sp*30f*dt, nz=u.Z+dz/dist*sp*30f*dt;
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
                        float sx=u.X+rx*sp*30f*dt, sz=u.Z+rz*sp*30f*dt;
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

        void Hit(GroundUnit t,float atk)
        {
            if(t==null) return;
            t.Hp-=atk;
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
            foreach(var u in Ours)
            {
                if(u==null||u.View==null||u.Hp<=0f) continue;
                c.Add(CombatSystem.K_GROUND,u,u.X,u.Z,u.Hp,u.BaseAttack,u.Range*GameConstants.Tile,CombatSystem.PlayerKey);
            }
            foreach(var e in Enemies)
            {
                if(e==null||e.View==null||e.Hp<=0f) continue;
                c.Add(CombatSystem.K_GROUND,e,e.X,e.Z,e.Hp,e.BaseAttack,e.Range*GameConstants.Tile,e.FactionId);
            }
        }

        public void DamageGround(GroundUnit u,float dmg)
        {
            if(u==null||u.Hp<=0f||dmg<=0f) return;
            u.Hp-=dmg;
        }

        /// <summary>我方军车开火：同类型军车走 Hit+DestroyUnit；跨类型（舰/步/骑/建筑）走统一 Damage</summary>
        void GroundFire(GroundUnit u,CombatTarget ct)
        {
            // V9.5.3 统一武器特效：坦克炮口闪光+开火音效 / 命中爆炸+爆炸音效（桶池化）
            WeaponFxSystem.Muzzle(new Vector3(u.X,1.4f,u.Z),1.5f);
            WeaponFxSystem.Sfx("cannon_fire",new Vector3(u.X,1.4f,u.Z),0.65f);
            if(ct.Ref is GroundUnit tgt)
            {
                var hp=new Vector3(ct.X,1f,ct.Z);
                WeaponFxSystem.Explosion(hp); WeaponFxSystem.Sfx("cannon_explode",hp,0.8f);
                Hit(tgt,u.BaseAttack);
                if(tgt.Hp<=0f){ DestroyUnit(tgt,Enemies); GM.AddEvent("good","💥 击毁一辆敌"+tgt.Name+"！"); }
                return;
            }
            WeaponFxSystem.Explosion(new Vector3(ct.X,1f,ct.Z)); WeaponFxSystem.Sfx("cannon_explode",new Vector3(ct.X,1f,ct.Z),0.8f);
            GM.Combat.Damage(ct,u.BaseAttack);
        }

        /// <summary>敌方军车开火：目标为我方/他派军车走 Hit+DestroyUnit（敌方各派混战）；跨类型走统一 Damage</summary>
        void EnemyGroundFire(GroundUnit e,CombatTarget ct)
        {
            // V9.5.3 统一武器特效（敌方同样生效）
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

        void AssignGroup(GroundUnit u)
        {
            int g=0;
            // 找未满 7 辆的组（3-7 辆一队）
            var cnt=new Dictionary<int,int>();
            foreach(var o in Ours){ if(o.Group>=0) cnt[o.Group]=cnt.TryGetValue(o.Group,out var c)?c+1:1; }
            foreach(var e in Enemies){ if(e.Group>=0) cnt[e.Group]=cnt.TryGetValue(e.Group,out var c)?c+1:1; }
            for(int k=0;k<64;k++){ if(!cnt.TryGetValue(k,out var c)||c<3){ g=k; break; } }
            u.Group=g;
            GroupCenter[g]=u;   // 组中心=最后加入者（领队）
        }
        void RebuildGroups()
        {
            GroupCenter.Clear();
            foreach(var o in Ours) GroupCenter[o.Group]=o;
            foreach(var e in Enemies) GroupCenter[e.Group]=e;
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
            Material body = d.Id=="tank"?_matTank : d.Id=="apc"?_matApc : _matMissile;
            ColorUtility.TryParseHtmlString(colorHex,out var fc);
            if(factionColor!=null) body=ShaderHelper.Pbr(fc,0.25f,0.5f,9700+(int)fc.GetHashCode()%200,0.5f);
            if(d.Id=="tank")
            {
                var bodyGo=Prim(body,new Vector3(2.2f,0.8f,1.4f));
                var turret=Prim(body,new Vector3(1.4f,0.6f,1.0f)); turret.transform.localPosition=new Vector3(0,0.6f,0);
                var barrel=Prim(_matDark,new Vector3(0.25f,0.25f,1.8f)); barrel.transform.localPosition=new Vector3(0,0.7f,1.2f);
                TrackPair(go);
            }
            else if(d.Id=="apc")
            {
                var bodyGo=Prim(body,new Vector3(2.4f,1.0f,1.6f));
                var top=Prim(body,new Vector3(1.6f,0.5f,1.0f)); top.transform.localPosition=new Vector3(0,0.6f,0);
                TrackPair(go);
            }
            else
            {
                var bodyGo=Prim(body,new Vector3(2.6f,0.8f,1.5f));
                var rack=Prim(_matDark,new Vector3(0.3f,0.7f,2.4f)); rack.transform.localPosition=new Vector3(0,0.6f,-0.3f);
                rack.transform.localRotation=Quaternion.Euler(0,0,-18f);
                TrackPair(go);
            }
            go.transform.position=new Vector3(u.X, _terrain!=null?_terrain.HeightAt(u.X,u.Z)+0.5f:0.5f, u.Z);
            return go;
        }
        GameObject Prim(Material m,Vector3 size)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(g.GetComponent<Collider>());   // 防点击拾取干扰
            g.transform.SetParent(_root,false);
            g.transform.localScale=size;
            var r=g.GetComponent<Renderer>(); r.material=m; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
        void TrackPair(GameObject parent)
        {
            var l=Prim(_matTrack,new Vector3(0.4f,0.35f,1.4f)); l.transform.localPosition=new Vector3(-1.1f,-0.5f,0);
            var r=Prim(_matTrack,new Vector3(0.4f,0.35f,1.4f)); r.transform.localPosition=new Vector3(1.1f,-0.5f,0);
        }

        /// <summary>V9.4.6 Web 探针</summary>
        public string Probe()
        {
            int f=0; var seen=new HashSet<string>();
            foreach(var e in Enemies) if(seen.Add(e.FactionId)) f++;
            return "groundActive="+(Active?"1":"0")+" ours="+Ours.Count+" enemy="+Enemies.Count+" factions="+f+" max="+MaxGround;
        }
    }
}
