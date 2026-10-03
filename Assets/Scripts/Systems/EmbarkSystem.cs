using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V6.3.7 载具登乘系统：船只/车辆升级到 Lv2 起激活载人——自动吸附附近闲人登乘，
    /// 民用船按 EffectiveHousing（居住人数）限载、战船按 Capacity（作战人数，计入 Crew）、
    /// 车辆按 Capacity（不含驾驶员）限载；登乘者停止陆地游走并在甲板/车厢生成乘员小人随载具移动；
    /// 载具损毁/降级时在其旁就近下船、恢复活动。为不抽空街景，车辆登乘上限=总人口的 40%；
    /// 船只自 V9.3.9 起豁免该上限（军/民船都可正常载人；军用低员触发战时征兵令 30 格强制登船）。
    /// </summary>
    public class EmbarkSystem : GameSystemBase
    {
        private const float CatchRadius = 14f;     // 吸附半径（世界单位，车辆/兜底）
        private const float LoadCatch = 400f;      // V9.3.5 载人态船吸附半径=100格（4×100）
        private const float BattleCatch = 120f;    // V9.3.5 战斗态船吸附半径=30格（4×30）
        private const float TickEvery = 2.5f;      // 节流秒
        private const int MaxVisualRiders = 14;    // 单个载具最多画出的乘员小人（再多只计数）
        private const float RideRatio = 0.4f;      // 最多带走四成人口，保留市井活力
        private float _cd;
        private readonly Dictionary<AgentEntity, object> _riding = new();
        private readonly Dictionary<object, Vector3> _lastPos = new();
        private Material _riderMat;
        private Material RiderMat => _riderMat ??= ShaderHelper.Mat(new Color(0.32f,0.36f,0.46f));

        public override void Tick(float dt)
        {
            _cd -= dt;
            if (_cd > 0f) return;
            _cd = TickEvery;
            Step();
        }

        private void Step()
        {
            var alive = new HashSet<object>();
            // 1) 船只
            if (GM.Naval != null)
                foreach (var sh in S.Ships)
                {
                    if (sh==null) continue;
                    _lastPos[sh]=new Vector3(sh.X,0,sh.Z);
                    // V9.3.5 放开门槛：全部我方船(Lv>=1)可载人（原Lv>=2——新船Lv1空员会0速死锁）；
                    // V9.3.9 修复：船只登船不再受全局40%登乘上限阻塞（民用船/军用船都能正常载人，否则人多后budget=0全员不上船）；
                    // V9.3.9 战时征兵令：军用船人员<50%容量时，30格内所有人员强制登船（不限军人），装到50%或范围内无人为止。
                    if (sh.Side=="ours" && sh.Level>=1)
                    {
                        alive.Add(sh);
                        float capF=GM.Naval.Capacity(sh);
                        bool lowCrew = sh.Military && sh.Crew < capF*0.5f;
                        if (lowCrew) BoardShip(sh, BattleCatch, true, Mathf.FloorToInt(capF*0.5f));   // 战时征兵令：30格强制装到50%
                        else BoardShip(sh, GM.Naval.BattlePriority(sh)?BattleCatch:LoadCatch);          // 战斗态30格 / 载人态100格
                        SyncRiders(sh.View, ShipOccupied(sh), 0.9f, 1.1f);
                    }
                    else ReleaseOf(sh);
                }
            // 2) 车辆（保留全局40%登乘上限，防抽空街景）
            foreach (var c in S.Carts)
            {
                if (c==null) continue;
                _lastPos[c]=new Vector3(c.X,0,c.Z);
                if (c.Level>=2) { alive.Add(c); BoardCart(c); SyncRiders(c.View, c.Passengers, 0.7f, 0.8f); }
                else ReleaseOf(c);
            }
            // 3) 释放已失效载具上的乘员
            if (_riding.Count>0)
            {
                var gone=new List<AgentEntity>();
                foreach(var kv in _riding) if(!alive.Contains(kv.Value)) gone.Add(kv.Key);
                foreach(var a in gone) Disembark(a);
            }
        }

        private int ShipOccupied(ShipEntity s) => s.Military ? s.Crew : s.Passengers;
        private int ShipCap(ShipEntity s) => s.Military ? GM.Naval.Capacity(s) : s.EffectiveHousing;

        // 船只登船：不受全局40%上限约束（V9.3.9 修复"附近有人不上船"）。
        // force=true 为战时征兵令：无视剩余名额，在 radius 内循环抓最近未登船人员，直到达到 forceTarget 或范围内无人。
        private void BoardShip(ShipEntity s, float radius, bool force=false, int forceTarget=0)
        {
            int occ=ShipOccupied(s), cap=ShipCap(s);
            if (force)
            {
                if (forceTarget>occ) cap=Mathf.Min(cap,forceTarget);
                else cap=occ;
            }
            while (occ<cap)
            {
                var a=NearestFree(s.X,s.Z,radius); if(a==null) break;
                Embark(a,s);
                if(s.Military) s.Crew++; else s.Passengers++;
                occ++;
            }
        }
        private void BoardCart(CartEntity c)
        {
            int budget=GlobalBudget();
            while (c.Passengers<c.Capacity && budget>0)
            {
                var a=NearestFree(c.X,c.Z,CatchRadius); if(a==null) break;
                Embark(a,c); c.Passengers++; budget--;
            }
        }

        // 全局登乘名额：最多带走四成人口
        private int GlobalBudget()
        {
            int total=S.Agents.Count; if(total==0) return 0;
            int maxRide=Mathf.FloorToInt(total*RideRatio);
            return Mathf.Max(0, maxRide-_riding.Count);
        }

        private AgentEntity NearestFree(float x,float z,float radius)
        {
            AgentEntity best=null; float bd=radius*radius;
            foreach(var a in S.Agents)
            {
                if(a.Boarded) continue;
                float dx=a.X-x,dz=a.Z-z,d=dx*dx+dz*dz;
                if(d<bd){bd=d;best=a;}
            }
            return best;
        }

        private void Embark(AgentEntity a,object vehicle)
        {
            a.Boarded=true;
            _riding[a]=vehicle;
            if(a.View!=null) a.View.SetActive(false);
        }

        private void ReleaseOf(object vehicle)
        {
            if(!_riding.ContainsValue(vehicle)) return;
            var leave=new List<AgentEntity>();
            foreach(var kv in _riding) if(kv.Value==vehicle) leave.Add(kv.Key);
            foreach(var a in leave) Disembark(a);
        }

        private void Disembark(AgentEntity a)
        {
            object v=null; _riding.TryGetValue(a,out v);
            Vector3 p=Vector3.zero; if(v!=null && _lastPos.TryGetValue(v,out var lp)) p=lp;
            if(v is ShipEntity sh){ if(sh.Military) sh.Crew=Mathf.Max(0,sh.Crew-1); else sh.Passengers=Mathf.Max(0,sh.Passengers-1); }
            else if(v is CartEntity cc){ cc.Passengers=Mathf.Max(0,cc.Passengers-1); }
            a.X=p.x+Random.Range(-1.2f,1.2f); a.Z=p.z+Random.Range(-1.2f,1.2f);
            a.Boarded=false; a.WanderTimer=0f;
            if(a.View!=null) a.View.SetActive(true);
            _riding.Remove(a);
        }

        // 在载具视图下生成/对齐乘员小人（数量多于显示上限时只画前 N 个）
        private void SyncRiders(GameObject view,int count,float y,float spread)
        {
            if(view==null) return;
            var root=view.transform.Find("Riders");
            if(root==null){ var go=new GameObject("Riders"); go.transform.SetParent(view.transform,false); root=go.transform; }
            int show=Mathf.Min(count,MaxVisualRiders);
            for(int i=0;i<show;i++)
            {
                Transform r=root.Find("R"+i);
                if(r==null)
                {
                    var g=GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    g.name="R"+i; var c=g.GetComponent<Collider>(); if(c) Object.Destroy(c);
                    g.transform.SetParent(root,false);
                    g.transform.localScale=new Vector3(0.32f,0.4f,0.32f);
                    g.GetComponent<Renderer>().material=RiderMat;
                    r=g.transform;
                }
                int ring=Mathf.FloorToInt(i/6), k=i%6;
                float ang=k*Mathf.PI/3f + ring*0.5f, rad=spread*(0.25f+ring*0.32f);
                r.localPosition=new Vector3(Mathf.Cos(ang)*rad, y, Mathf.Sin(ang)*rad);
            }
            for(int i=root.childCount-1;i>=show;i--) Object.Destroy(root.GetChild(i).gameObject);
        }
    }
}
