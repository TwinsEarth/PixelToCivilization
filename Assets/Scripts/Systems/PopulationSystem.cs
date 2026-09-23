using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// 人口系统 —— 对齐 v5.9.9：住房供给、出生/死亡、年龄结构(儿童/青年/中年/老年)、社会阶层、个体游走。
    /// </summary>
    public class PopulationSystem : GameSystemBase
    {
        public float HousingCapacity;
        private float _birthNotifyCd;
        private bool _visualDirty;   // V7.0.2 年龄/时代变化后待重建外观
        private WorldGenerator _terrain;
        const float WalkSpeed=1.2f;   // 平民陆地游走速度（世界单位/秒）

        // ===== V9.2.2 人口周期律 =====
        private int _lastDynastyIdx = -1;   // 上一次所见朝代（运行态，读档后对齐）
        public int Cycle => S.CyclePhase;    // 0恢复/1繁荣/2过剩/3崩溃
        public int LastCapacity;             // 最近一次计算的土地承载力（供人口神决策）

        /// <summary>读档后调用：把朝代时钟对齐到存档朝代，避免首年误判为新朝重置周期</summary>
        public void BindAfterLoad(){ _lastDynastyIdx = S.DynastyIdx; }

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _terrain=Object.FindObjectOfType<WorldGenerator>();
        }

        public override void Tick(float dt)
        {
            // 住房 = 所有居住建筑 housing 之和 * 等级系数 + 我方船只提供的船舱居住（V6.1.1 船只居住属性）
            float h = 0;
            foreach (var b in S.Buildings) h += b.Def != null ? b.Def.GetFunc("housing") * b.LevelMult : 0;
            foreach (var ship in S.Ships) h += ship != null ? ship.EffectiveHousing : 0;
            foreach (var fleet in S.OceanFleets) h += fleet != null ? fleet.EffectiveHousing : 0;
            if (GM.Wonder != null) h += GM.Wonder.HousingAdd;   // V6.8.0 紫禁城等奇观住房
            HousingCapacity = h; S.Housing = h;
            S.MaxPop = Mathf.Max(100, Mathf.RoundToInt(h));

            // 个体游走
            UpdateAgents(dt);
            // V7.0.2 外观按需重建（实时帧内每体最多一次；跳年/快进只改数据，不在补算中刷视图）
            if(_visualDirty)
            {
                _visualDirty=false;
                var ag=S.Agents;
                for(int i=0;i<ag.Count;i++) GM.Env.SyncAgentView(ag[i]);
            }
        }

        public override void OnYear(int year)
        {
            // ===== V9.2.2 人口周期：先算土地承载力与压力比，决定是否压生育 =====
            float eraMult = S.Era>=6?8f : S.Era>=5?3.5f : S.Era>=4?1.6f : 1f;
            LastCapacity = Mathf.RoundToInt(GameConstants.StartPop*(0.9f+0.012f*S.Buildings.Count)*S.LandIntegrity*eraMult);
            float mRatio = S.Pop/Mathf.Max(1f, LastCapacity);
            // 过剩/崩溃期停止自然增殖；现代(工业后)若逼近上限转入低生育率陷阱，同样压生育
            bool suppressBirth = S.CyclePhase>=2 || (S.Era>=5 && mRatio>0.9f);

            // ===== 出生 =====
            if (!suppressBirth && S.GetRes("food") > 50 && S.Pop < S.Housing && S.Pop < GameConstants.MaxPop)
            {
                int oldPop = S.Pop;
                int birth = 3 + Mathf.FloorToInt(UnityEngine.Random.value*4);
                S.Pop = Mathf.Min(GameConstants.MaxPop, Mathf.FloorToInt(S.Housing), S.Pop+birth);
                int real = S.Pop - oldPop;
                S.Children += real;
                if (real > 0)
                {
                    int oldM = Mathf.FloorToInt(oldPop/50f), newM = Mathf.FloorToInt(S.Pop/50f);
                    if (newM > oldM && newM > 0) GM.AddEvent("good","👥 人口达到"+(newM*50)+"人");
                }
            }
            // ===== 年龄增长（每年约2%）=====
            int ageUp = Mathf.FloorToInt(S.Pop*0.02f);
            float t;
            t = Mathf.Min(S.Children, ageUp); S.Children-=t; S.Young+=t;
            t = Mathf.Min(S.Young, Mathf.FloorToInt(ageUp*0.5f)); S.Young-=t; S.Middle+=t;
            t = Mathf.Min(S.Middle, Mathf.FloorToInt(ageUp*0.3f)); S.Middle-=t; S.Old+=t;
            // 老年死亡
            if (S.Old > 0 && UnityEngine.Random.value < 0.3f)
            {
                int deaths = Mathf.Min(Mathf.RoundToInt(S.Old), 1+Mathf.FloorToInt(UnityEngine.Random.value*3));
                S.Old -= deaths; S.Pop = Mathf.Max(10, S.Pop-deaths);
            }
            NormalizeAge();
            AgeAndGrow();
            RunMalthus(year);
        }

        /// <summary>
        /// V9.2.2 人口周期律：人口指数增长 vs 土地线性产出的剪刀差。
        /// 恢复期(人少)→繁荣期→过剩期(土地兼并/流民/税基萎缩)→崩溃期(战乱饥荒疫病消灭30-70%)，
        /// 新朝识别后土地系数回升、周期重启；工业时代后转入低生育率/老龄化（不爆战乱）。
        /// </summary>
        void RunMalthus(int year)
        {
            // 新朝识别：换朝则地广人稀、土地肥力重置，周期自恢复期重启
            if (_lastDynastyIdx < 0) _lastDynastyIdx = S.DynastyIdx;
            else if (_lastDynastyIdx != S.DynastyIdx)
            {
                _lastDynastyIdx = S.DynastyIdx;
                S.LandIntegrity = 1.2f;
                S.PeakPop = S.Pop;
                GM.AddEvent("good","🌱 新朝肇建·地广人稀，人口周期自【恢复期】重启");
            }
            S.DynastyAge++;
            if (S.Pop > S.PeakPop) S.PeakPop = S.Pop;
            // 土地退化：每年 0.0018，约 330 年从 1.2 衰减到 0.6，下限 0.55（对应"承载力衰减到六至七成"）
            S.LandIntegrity = Mathf.Max(0.55f, S.LandIntegrity - 0.0018f);

            float eraMult = S.Era>=6?8f : S.Era>=5?3.5f : S.Era>=4?1.6f : 1f;
            int cap = Mathf.RoundToInt(GameConstants.StartPop*(0.9f+0.012f*S.Buildings.Count)*S.LandIntegrity*eraMult);
            float r = S.Pop/Mathf.Max(1f, cap);
            int prev = S.CyclePhase;
            S.CyclePhase = r<0.55f?0 : r<0.85f?1 : r<1.05f?2 : 3;
            bool modern = S.Era>=5;

            switch (S.CyclePhase)
            {
                case 0: // 恢复：轻徭薄赋，民心缓升、腐败缓降
                    S.Happiness = Mathf.Min(100, S.Happiness+0.05f);
                    S.Corruption = Mathf.Max(0, S.Corruption-0.02f);
                    break;
                case 1: // 繁荣：盛世，中性
                    break;
                case 2: // 过剩：土地兼并→民心/天命缓降、腐败升、税基(金)萎缩、概率流民
                    S.Happiness = Mathf.Max(0, S.Happiness - 0.12f*r);
                    S.Corruption = Mathf.Min(100, S.Corruption + 0.08f*r);
                    S.AddRes("gold", -0.05f*r);
                    if (UnityEngine.Random.value < (r-0.85f)*0.5f)
                    {
                        int lost = Mathf.Max(1, Mathf.RoundToInt(S.Pop*0.01f));
                        S.Pop = Mathf.Max(10, S.Pop-lost);
                        S.Happiness = Mathf.Max(0, S.Happiness-3f);
                        GM.AddEvent("bad","🌾 人地矛盾激化·土地兼并，流民四起（-"+lost+"人）");
                    }
                    break;
                case 3: // 崩溃：饥荒/瘟疫/战乱消灭过剩人口（现代低生育率则不爆战乱，仅停滞）
                    if (modern) { S.Happiness = Mathf.Max(0, S.Happiness-0.05f); break; }
                    float p = Mathf.Clamp01((r-1f)*0.6f);
                    if (UnityEngine.Random.value < Mathf.Max(0.08f, p))
                    {
                        float frac = UnityEngine.Random.Range(0.10f,0.22f);
                        int lost = Mathf.RoundToInt(S.Pop*frac);
                        S.Pop = Mathf.Max(10, S.Pop-lost);
                        S.DynastyMorale = Mathf.Max(0, S.DynastyMorale-6f);
                        S.Happiness = Mathf.Max(0, S.Happiness-6f);
                        GM.AddEvent("bad","☠️ 人口崩溃·战乱饥荒疫病横生，人口骤减约 "+Mathf.RoundToInt(frac*100)+"%");
                    }
                    break;
            }
            if (S.CyclePhase != prev)
            {
                string[] names = {"恢复期","繁荣期","过剩期","崩溃期"};
                GM.AddEvent("info","🔄 人口周期进入【"+names[S.CyclePhase]+"】（人口 "+S.Pop+" / 土地承载力 "+cap+"，比率 "+r.ToString("F2")+"）");
            }
        }

        /// <summary>V7.0.2 个体逐年成长（只更新数据）：幼→壮→老，寿尽轮回为同户新生孩童；视觉置脏，由 Tick 按需重建</summary>
        void AgeAndGrow()
        {
            var agents=S.Agents;
            for(int i=0;i<agents.Count;i++)
            {
                var a=agents[i];
                a.Age++;
                int st=PixelToCivilization.Actors.HumanoidFactory.StageOf(a.Age);
                if(st!=a.LifeStage) a.LifeStage=st;
                if(a.Age>a.LifeSpan)
                {   // 寿尽：以同户新生孩童闭环（继承家门职业/阶层/家园，换新颜色个体）
                    a.Age=UnityEngine.Random.Range(0,3); a.LifeStage=0;
                    a.ColorSeed=UnityEngine.Random.Range(1,999999); a.LifeSpan=UnityEngine.Random.Range(60,89);
                }
            }
            _visualDirty=true;
        }

        /// <summary>V7.0.2 跨时代全员换装（置脏，Tick 内每体按签名最多重建一次，避免跳时代连环重建卡顿）</summary>
        public override void OnEra(int newEra,int oldEra)
        {
            if(newEra!=oldEra) _visualDirty=true;
        }

        /// <summary>建造居住建筑后提升社会阶层（对齐 commoner+2 / rich+1）</summary>
        public void OnResidenceBuilt(string type)
        {
            if (type == "rich_house") { ShiftClass("slave","commoner",2); ShiftClass("commoner","rich",1); }
            else if (type == "noble_palace") { ShiftClass("commoner","rich",2); ShiftClass("rich","noble",1); }
        }
        private void ShiftClass(string from, string to, float amt)
        {
            float v = Mathf.Min(S.SocialClasses[from], amt);
            S.SocialClasses[from]-=v; S.SocialClasses[to]+=v;
        }
        /// <summary>
        /// 年龄结构统一为「人数」口径并归一到当前总人口：出生计入儿童、衰老在四桶间流转、老年死亡扣减，
        /// 每游戏年末把四项之和缩放对齐 S.Pop，避免“初始百分比(和=100) + 人数增减”混用导致结构长期漂移。
        /// </summary>
        public void NormalizeAge()
        {
            float sum = S.Children+S.Young+S.Middle+S.Old;
            if (sum <= 0 || S.Pop <= 0)
            {
                S.Children=S.Pop*0.25f; S.Young=S.Pop*0.35f; S.Middle=S.Pop*0.30f; S.Old=S.Pop*0.10f;
                return;
            }
            float k = S.Pop/sum;
            S.Children*=k; S.Young*=k; S.Middle*=k; S.Old*=k;
        }

        // V9.1.1 职业通勤（运行期状态，不存档；读档后按职业与建筑就近重派）
        class WorkInfo { public bool Has; public float WX,WZ; public bool AtWork=true; public float Phase; public float Replan; public string Job=""; }
        readonly Dictionary<AgentEntity, WorkInfo> _work = new();
        const float WorkSeconds=42f, HomeSeconds=18f, WorkRadius=90f;

        static bool WorkingAge(AgentEntity a) => a!=null && a.Age>=15 && a.Age<=64;

        WorkInfo GetWork(AgentEntity a)
        {
            if (!_work.TryGetValue(a, out var wi)) { wi=new WorkInfo(); _work[a]=wi; }
            if (wi.Has && wi.Job==a.Job) return wi;
            wi.Job=a.Job??""; wi.Has=false;
            float bd=WorkRadius*WorkRadius;
            foreach (var b in S.Buildings)
            {
                if (b==null||b.Def==null) continue;
                if (!JobMatches(wi.Job,b)) continue;
                float dx=b.X-a.HomeX, dz=b.Z-a.HomeZ, d2=dx*dx+dz*dz;
                if (d2<bd)
                {   // 稳定散布：按个体哈希在工作建筑周围取固定工位，避免全挤一个点
                    int h=(a.GetHashCode()&0xffff); float ang=(h%360)*Mathf.Deg2Rad; float rr=0.8f+(h%9)/10f;
                    bd=d2; wi.Has=true;
                    wi.WX=b.X+Mathf.Cos(ang)*rr; wi.WZ=b.Z+Mathf.Sin(ang)*rr;
                }
            }
            wi.AtWork=true; wi.Phase=10f+UnityEngine.Random.value*30f; wi.Replan=40f+UnityEngine.Random.value*40f;
            return wi;
        }

        // V9.1.1 职业 → 工作建筑（按类型/分类关键字）；未匹配返回 false（该个体回退为家周边活动）
        static bool JobMatches(string job, BuildingEntity b)
        {
            string j=(job??"").ToLowerInvariant(), t=(b.Type??"").ToLowerInvariant(), c=b.Def.Cat??"";
            bool T(string s)=>t.Contains(s);
            if (j.Contains("farm")||j.Contains("农")||j.Contains("种植")||j.Contains("牧"))
                return c=="食物"||T("farm")||T("field")||T("pasture")||T("ranch")||T("orchard");
            if (j.Contains("wood")||j.Contains("伐木")||j.Contains("林"))
                return T("lumber")||T("log")||T("wood")||T("forest")||T("timber");
            if (j.Contains("quarry")||j.Contains("采石"))
                return T("quarry")||T("stone");
            if (j.Contains("mine")||j.Contains("矿"))
                return T("mine")||T("quarry");
            if (j.Contains("merchant")||j.Contains("trade")||j.Contains("商")||j.Contains("市场")||j.Contains("买卖"))
                return c=="经济"||T("market")||T("shop")||T("mall")||T("supermarket")||T("trade")||T("bank");
            if (j.Contains("soldier")||j.Contains("军")||j.Contains("兵"))
                return MilitarySystem.IsMilitaryBuilding(b);
            if (j.Contains("official")||j.Contains("官"))
                return T("palace")||T("hall")||T("government")||T("admin")||T("townhall")||T("capital")||T("office");
            if (j.Contains("police")||j.Contains("警")) return T("police");
            if (j.Contains("doctor")||j.Contains("医")) return T("hospital")||T("clinic");
            if (j.Contains("fire")||j.Contains("消防")) return T("fire_station");
            if (j.Contains("teacher")||j.Contains("教育")||j.Contains("教")) return T("school")||T("university")||T("edu");
            if (j.Contains("driver")||j.Contains("车")) return T("road")||T("highway")||T("garage")||T("bus")||T("station")||T("interchange");
            if (j.Contains("sailor")||j.Contains("船")||j.Contains("渔")) return T("port")||T("harbor")||T("dock")||T("shipyard")||T("wharf")||T("fishery")||c=="海洋";
            if (j.Contains("research")||j.Contains("科研")||j.Contains("技术")) return c=="科技"||T("lab")||T("research");
            if (j.Contains("worker")||j.Contains("工")||j.Contains("维修"))
                return c=="工业"||T("factory")||T("workshop")||T("plant")||T("construction")||T("apartment")||T("hut")||T("house");
            return false;
        }

        private void SteerTo(AgentEntity a, float gx, float gz)
        {
            float dx=gx-a.X, dz=gz-a.Z, dl=Mathf.Sqrt(dx*dx+dz*dz);
            if (dl>0.05f){ a.Vx=dx/dl*WalkSpeed; a.Vz=dz/dl*WalkSpeed; }
        }

        private void ChooseHomeWander(AgentEntity a, float distHome)
        {
            float tx=a.HomeX, tz=a.HomeZ; bool have=false;
            if (distHome <= 20f)
            {
                // 绕家园选一个距当前位置 >1.4 的可行走点，最多 10 次，保证真的会走起来
                for(int t=0;t<10;t++)
                {
                    float ang=UnityEngine.Random.value*Mathf.PI*2f, rr=2.5f+UnityEngine.Random.value*10.5f;
                    float cx=a.HomeX+Mathf.Cos(ang)*rr, cz=a.HomeZ+Mathf.Sin(ang)*rr;
                    if(Walkable(cx,cz) && (cx-a.X)*(cx-a.X)+(cz-a.Z)*(cz-a.Z)>1.96f){tx=cx;tz=cz;have=true;break;}
                }
                // 家园周边多水：改在当前位置附近找落点，贴着岸移动
                if(!have) for(int t=0;t<8;t++)
                {
                    float ang=UnityEngine.Random.value*Mathf.PI*2f, rr=1.2f+UnityEngine.Random.value*2.5f;
                    float cx=a.X+Mathf.Cos(ang)*rr, cz=a.Z+Mathf.Sin(ang)*rr;
                    if(Walkable(cx,cz)){tx=cx;tz=cz;have=true;break;}
                }
            }
            SteerTo(a,tx,tz);
            a.WanderTimer = 2.0f + UnityEngine.Random.value*2.5f;
        }

        private void UpdateAgents(float dt)
        {
            var agents = S.Agents;
            for (int i = agents.Count-1; i >= 0; i--)
            {
                var a = agents[i];
                if (a.Boarded) continue; // V6.3.7 已登乘车船者随载具移动，不再陆地游走
                a.WanderTimer -= dt;
                float distHome=Mathf.Sqrt((a.X-a.HomeX)*(a.X-a.HomeX)+(a.Z-a.HomeZ)*(a.Z-a.HomeZ));

                // ===== V9.1.1 职业通勤：有工作建筑的劳动年龄人口按“上岗⇄回家”周期往返，工作时间驻守工位 =====
                bool workIdle=false; bool worker=false;
                var wi=GetWork(a);
                wi.Replan-=dt;
                if (wi.Replan<=0f){ wi.Has=false; wi=GetWork(a); }
                if (wi.Has && WorkingAge(a))
                {
                    worker=true;
                    wi.Phase-=dt;
                    if(wi.Phase<=0f){ wi.AtWork=!wi.AtWork; wi.Phase=wi.AtWork?WorkSeconds:HomeSeconds; }
                    if(wi.AtWork)
                    {
                        float dw=Mathf.Sqrt((a.X-wi.WX)*(a.X-wi.WX)+(a.Z-wi.WZ)*(a.Z-wi.WZ));
                        if(dw<2.6f){ workIdle=true; a.Vx=0f; a.Vz=0f; }   // 到岗：驻守作业（不再聚在村中心）
                        else if(a.WanderTimer<=0f || (a.Vx==0f&&a.Vz==0f)){ SteerTo(a,wi.WX,wi.WZ); a.WanderTimer=1.2f+UnityEngine.Random.value; }
                    }
                    else
                    {
                        // 回家时段：远离家则回家，到家附近则正常休憩游走
                        if(distHome>20f){ if(a.WanderTimer<=0f||(a.Vx==0f&&a.Vz==0f)){SteerTo(a,a.HomeX,a.HomeZ);a.WanderTimer=1.5f;} }
                        else if(a.WanderTimer<=0f||(a.Vx==0f&&a.Vz==0f)) ChooseHomeWander(a,distHome);
                    }
                }
                // 定期在村落周边选游走点；走出活动半径则回家；速度为 0（到站/被水挡住）也立即重选，避免原地呆立
                else if (a.WanderTimer <= 0f || distHome > 20f || (a.Vx==0f && a.Vz==0f))
                {
                    if(distHome>20f){ SteerTo(a,a.HomeX,a.HomeZ); a.WanderTimer=1.5f; }
                    else ChooseHomeWander(a,distHome);
                }

                bool moved=false;
                if(!workIdle)
                {
                float nx=a.X+a.Vx*dt, nz=a.Z+a.Vz*dt;
                if (Walkable(nx,nz)){ a.X=nx; a.Z=nz; moved=true; }
                else
                {
                    // 贴岸滑行：保持前进方向，依次向左右偏转找可行走格，而不是原地停下
                    float baseAng=Mathf.Atan2(a.Vx,a.Vz);
                    float[] turns={30f,-30f,60f,-60f,90f,-90f,120f,-120f,150f,-150f,180f};
                    foreach(var deg in turns)
                    {
                        float ang=baseAng+deg*Mathf.Deg2Rad;
                        float vx=Mathf.Sin(ang)*WalkSpeed, vz=Mathf.Cos(ang)*WalkSpeed;
                        float cx=a.X+vx*dt, cz=a.Z+vz*dt;
                        if(Walkable(cx,cz)){ a.Vx=vx;a.Vz=vz;a.X=cx;a.Z=cz;moved=true;break; }
                    }
                    if(!moved)
                    {
                        // V7.0.5 被困（涨潮淹了落脚点/被水围住）：螺旋搜索最近可行走陆地并走过去，
                        // 而不是原地反复重选目标、永久呆立；确实无陆地才停下等待。
                        if(NearestWalkable(a.X,a.Z,18f,out float lx,out float lz))
                        {
                            float ddx=lx-a.X,ddz=lz-a.Z,ddl=Mathf.Sqrt(ddx*ddx+ddz*ddz);
                            // 仅设定朝陆地的行进方向；真正迈出（相邻格可行走）由上面的贴岸滑行结算，
                            // 隔着水面则不过水、不原地踏步，等退潮或下轮重选。
                            if(ddl>0.05f){ a.Vx=ddx/ddl*WalkSpeed; a.Vz=ddz/ddl*WalkSpeed; a.WanderTimer=0.5f; }
                        }
                        else { a.Vx=0f;a.Vz=0f;a.WanderTimer=Mathf.Min(a.WanderTimer,0.25f); }
                    }
                }
                } // end if(!workIdle)
                if (a.View != null)
                {
                    float y=_terrain!=null?_terrain.HeightAt(a.X,a.Z):0f;
                    a.View.transform.position = new Vector3(a.X,y,a.Z);
                    // 朝向移动方向
                    if (moved)
                        a.View.transform.rotation=Quaternion.Slerp(a.View.transform.rotation,
                            Quaternion.Euler(0,Mathf.Atan2(a.Vx,a.Vz)*Mathf.Rad2Deg,0),0.2f);
                    // V6.8.1 迈腿动画：把世界速度喂给人形动画器（懒加载，读档视图重建后自动重取）
                    if(a.Anim==null) a.Anim=a.View.GetComponentInChildren<PixelToCivilization.Actors.HumanoidAnimator>();
                    if(a.Anim!=null) a.Anim.Velocity = moved ? new Vector3(a.Vx,0f,a.Vz) : Vector3.zero;
                }
            }
        }

        /// <summary>V6.5.6 人员可行走判定：非水面，或站在桥梁上；无地形数据时放行（兼容）</summary>
        bool Walkable(float x,float z)
        {
            if(_terrain==null)return true;
            if(!_terrain.IsWater(x,z))return true;
            return GM.Bridge!=null && GM.Bridge.IsBridgeAt(x,z);
        }

        /// <summary>V7.0.5 由近及远螺旋搜索最近的可行走陆地（被困水中/涨潮时脱困用），找到最近一圈即返回。</summary>
        bool NearestWalkable(float x,float z,float maxR,out float ox,out float oz)
        {
            ox=x; oz=z; bool found=false; float best=maxR*maxR;
            for(float r=2f; r<=maxR; r+=2f)
            {
                int n=Mathf.Max(8,Mathf.RoundToInt(2f*Mathf.PI*r/2f));
                for(int k=0;k<n;k++)
                {
                    float ang=k*(Mathf.PI*2f/n);
                    float cx=x+Mathf.Cos(ang)*r, cz=z+Mathf.Sin(ang)*r;
                    if(Walkable(cx,cz)){ float d=(cx-x)*(cx-x)+(cz-z)*(cz-z); if(d<best){best=d;ox=cx;oz=cz;found=true;} }
                }
                if(found) return true;
            }
            return false;
        }
    }
}
