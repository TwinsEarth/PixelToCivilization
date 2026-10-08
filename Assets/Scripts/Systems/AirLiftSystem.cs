using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.6.1 远程投送系统：紧急集结令（绿旗=地面部队 / 红旗=军人）下达时，
    /// 距军旗超过 100 格的己方地面作战单位 & 人员，自动由 运输机（公元1900 起）& 直升机（公元1949 起）
    /// 远程投送：机飞往单位上空 → 装载上机 → 飞往军旗 → 空投列阵。
    /// 运行时实体（不进存档）：投送中单位 Lifted=true（原系统跳过 AI，视图挂机），落地后恢复。
    /// 机队自动提供（运输机 2 架 / 直升机 2 架，代表国家空运力量，不占玩家操作）。
    /// </summary>
    public class AirLiftSystem : GameSystemBase
    {
        public const float AirLiftRange = 100f;   // 格：距旗超此距离才触发空运（与集结半径一致）
        const float CruiseAlt = 26f;              // 巡航高度（世界单位）
        const float HoverAlt = 5f;                // 装卸悬停高度
        const float PlaneSpeed = 24f;             // 世界单位/秒（真实时间，玩家可见投送动画）
        const float LoadGap = 0.8f;               // 每个单位装载/空投间隔（真实秒）
        const int MaxPerTask = 6;                 // 每架机一次运载上限
        const int CargoFleet = 2, HeliFleet = 2;  // 运输机/直升机机队规模

        /// <summary>运输机实体（运行时态，不进存档）</summary>
        class LiftPlane
        {
            public GameObject View, Root, Rotor;
            public int Kind;             // 0运输机 1直升机
            public int Phase;            // 0空闲 1飞往装载 2装载 3飞往投送 4空投 5回收
            public float X, Z, Alt, TX, TZ, TAlt, Timer;
            public readonly List<LiftRequest> Cargo = new();
            public int CargoIdx;
            public float FlagX, FlagZ;
        }

        /// <summary>投送请求：单位 + 目标军旗坐标</summary>
        class LiftRequest
        {
            public object Unit;
            public float FlagX, FlagZ;
        }

        readonly List<LiftPlane> _planes = new();
        readonly Queue<LiftRequest> _groundQueue = new();   // 待投送地面部队
        readonly Queue<LiftRequest> _infQueue = new();      // 待投送军人
        bool _cargoBuilt, _heliBuilt;
        int _recoveredStuck;                                 // V9.6.2 投送兜底复位计数（探针证据：任何异常不再永久卡死）
        float _hintCd;                                       // V9.6.3f 未解锁投送提示节流（真实时间 10s 一次，防每 tick 刷事件卡死）

        public override void Init(GameManager gm) { base.Init(gm); _terrain = Object.FindObjectOfType<WorldGenerator>(); }
        WorldGenerator _terrain;   // V9.6.2 空投落地贴地（地形高度）

        public bool UnlockedCargo() => GM.State.Year >= 4930;   // 公元1900 运输机
        public bool UnlockedHeli()  => GM.State.Year >= 4949;   // 公元1949 直升机

        /// <summary>绿旗地面部队集结请求（>100格）</summary>
        public bool RequestLift(GroundWarfareSystem.GroundUnit u, float fx, float fz, float _)
        {
            if (u == null || !CanLift(u.X, u.Z, fx, fz)) return false;
            u.Lifted = true;
            _groundQueue.Enqueue(new LiftRequest { Unit = u, FlagX = fx, FlagZ = fz });
            return true;
        }

        /// <summary>红旗军人集结请求（>100格）</summary>
        public bool RequestLift(FriendlyUnit u, float fx, float fz, float _)
        {
            if (u == null || !CanLift(u.X, u.Z, fx, fz)) return false;
            u.Lifted = true;
            _infQueue.Enqueue(new LiftRequest { Unit = u, FlagX = fx, FlagZ = fz });
            return true;
        }

        bool CanLift(float x, float z, float fx, float fz)
        {
            if (!UnlockedCargo() && !UnlockedHeli()) return false;
            float dx = fx - x, dz = fz - z;
            return Mathf.Sqrt(dx * dx + dz * dz) > AirLiftRange * GameConstants.Tile;
        }

        public override void Tick(float dt)
        {
            EnsureFleet();
            DispatchTasks();
            for (int i = _planes.Count - 1; i >= 0; i--) StepPlane(_planes[i]);
            RecoverStuck();   // V9.6.2 兜底：任何异常泄漏的 Lifted 单位强制复位，杜绝永久卡死/消失
        }

        void EnsureFleet()
        {
            if (!_cargoBuilt && UnlockedCargo())
            {
                _cargoBuilt = true;
                for (int i = 0; i < CargoFleet; i++) _planes.Add(BuildPlane(0));
            }
            if (!_heliBuilt && UnlockedHeli())
            {
                _heliBuilt = true;
                for (int i = 0; i < HeliFleet; i++) _planes.Add(BuildPlane(1));
            }
        }

        /// <summary>空闲机领取任务：攒够一批（≤MaxPerTask）即出发；机种优先匹配（地面→运输机/人员→直升机，缺则互用）</summary>
        void DispatchTasks()
        {
            if (_planes.Count == 0) return;
            if (_groundQueue.Count > 0) DispatchFrom(_groundQueue, 0);
            if (_infQueue.Count > 0) DispatchFrom(_infQueue, 1);
        }

        void DispatchFrom(Queue<LiftRequest> q, int prefKind)
        {
            // V9.6.2 丢弃已失效请求（单位已被移除/击杀/读档重置），防机上空转
            while (q.Count > 0 && !RequestAlive(q.Peek().Unit)) q.Dequeue();
            if (q.Count == 0) return;
            LiftPlane idle = null;
            foreach (var p in _planes) if (p.Phase == 0 && p.Kind == prefKind) { idle = p; break; }
            if (idle == null) foreach (var p in _planes) if (p.Phase == 0) { idle = p; break; }
            if (idle == null) return;
            int n = 0;
            while (n < MaxPerTask && q.Count > 0)
            {
                var r = q.Peek();
                if (RequestAlive(r.Unit)) { idle.Cargo.Add(q.Dequeue()); n++; }
                else q.Dequeue();
            }
            if (idle.Cargo.Count == 0) return;
            var first = idle.Cargo[0];
            idle.FlagX = first.FlagX; idle.FlagZ = first.FlagZ;
            idle.CargoIdx = 0;
            idle.Phase = 1;
            var fp = PosOf(first.Unit);
            SetPlaneTarget(idle, fp.x, fp.y, CruiseAlt);
            if (idle.View != null) idle.View.SetActive(true);
            if (idle.Root != null) idle.Root.SetActive(true);
        }

        /// <summary>V9.6.2 请求单位是否仍有效（未被移除/击杀/读档重置）</summary>
        bool RequestAlive(object unit)
        {
            if (unit is GroundWarfareSystem.GroundUnit g) return GM.Ground!=null && GM.Ground.Ours.Contains(g);
            if (unit is FriendlyUnit fu) return GM.State!=null && GM.State.FriendlyUnits.Contains(fu);
            return false;
        }

        void StepPlane(LiftPlane p)
        {
            if (p.Phase == 0) { if (p.Root != null && p.Root.activeSelf) p.Root.SetActive(false); return; }
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.05f) dt = 0.05f;   // 帧间钳制，防掉帧瞬移

            // 旋翼/螺旋桨动画
            if (p.Rotor != null) p.Rotor.transform.Rotate(0, dt * 900f, 0, Space.Self);

            // 水平移动
            float dx = p.TX - p.X, dz = p.TZ - p.Z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist > 0.01f)
            {
                float step = PlaneSpeed * dt;
                float mv = Mathf.Min(dist, step);
                p.X += dx / dist * mv; p.Z += dz / dist * mv;
            }
            // 高度趋近
            p.Alt = Mathf.MoveTowards(p.Alt, p.TAlt, PlaneSpeed * 0.7f * dt);

            // 装载中的单位坐标跟随飞机
            for (int i = 0; i < p.Cargo.Count; i++)
            {
                var c = p.Cargo[i].Unit;
                if (c is GroundWarfareSystem.GroundUnit g && g.Lifted) { g.X = p.X; g.Z = p.Z; }
                else if (c is FriendlyUnit fu && fu.Lifted) { fu.X = p.X; fu.Z = p.Z; }
            }

            if (p.Root != null) p.Root.transform.position = new Vector3(p.X, p.Alt, p.Z);

            switch (p.Phase)
            {
                case 1:   // 飞往装载点
                    if (dist < 3f) { p.Phase = 2; p.Timer = LoadGap; SetPlaneTarget(p, p.X, p.Z, HoverAlt); }
                    break;
                case 2:   // 装载（悬停逐个上机）
                    p.Timer -= dt;
                    if (p.Timer <= 0f && p.CargoIdx < p.Cargo.Count)
                    {
                        Board(p.Cargo[p.CargoIdx].Unit, p);
                        p.CargoIdx++; p.Timer = LoadGap;
                    }
                    if (p.CargoIdx >= p.Cargo.Count)
                    {
                        p.Phase = 3; SetPlaneTarget(p, p.FlagX, p.FlagZ, CruiseAlt);
                    }
                    break;
                case 3:   // 飞往军旗
                    if (dist < 3f) { p.Phase = 4; p.Timer = LoadGap; SetPlaneTarget(p, p.X, p.Z, HoverAlt); }
                    break;
                case 4:   // 空投（逐个落地）
                    p.Timer -= dt;
                    if (p.Timer <= 0f && p.CargoIdx > 0)
                    {
                        Drop(p.Cargo[p.CargoIdx - 1].Unit, p);
                        p.CargoIdx--; p.Timer = LoadGap;
                    }
                    if (p.CargoIdx <= 0) Retire(p);
                    break;
            }
        }

        void Board(object unit, LiftPlane p)
        {
            GameObject view = ViewOf(unit);
            if (view != null && p.Root != null)
            {
                view.transform.SetParent(p.Root.transform, false);
                view.transform.localPosition = Vector3.zero;
                view.transform.localRotation = Quaternion.identity;
            }
        }

        void Drop(object unit, LiftPlane p)
        {
            if (unit is GroundWarfareSystem.GroundUnit g)
            {
                g.Lifted = false;
                DropAt(g, p);
            }
            else if (unit is FriendlyUnit fu)
            {
                fu.Lifted = false;
                DropAt(fu, p);
            }
        }

        void DropAt(object unit, LiftPlane p)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float r = 3f + Random.value * 6f;   // 旗旁 FormRange(10) 内环
            float nx = p.FlagX + Mathf.Cos(ang) * r;
            float nz = p.FlagZ + Mathf.Sin(ang) * r;
            if (unit is GroundWarfareSystem.GroundUnit g) { g.X = nx; g.Z = nz; }
            else if (unit is FriendlyUnit fu) { fu.X = nx; fu.Z = nz; }
            GameObject view = ViewOf(unit);
            if (view != null)
            {
                view.transform.SetParent(null, true);   // V9.6.2 保世界坐标解挂（机上 Root 回巢复用，不再销毁）
                float h = 0.5f;
                if (_terrain == null) _terrain = Object.FindObjectOfType<WorldGenerator>();
                if (_terrain != null) h = _terrain.HeightAt(nx, nz) + 0.5f;
                view.transform.position = new Vector3(nx, h, nz);   // V9.6.2 空投落地贴地，杜绝悬空/错位
            }
        }

        /// <summary>V9.6.2 空投完成回巢待命复用：不销毁飞机、不移除机队（旧实现 Recycle 销毁 Root 连带销毁机上单位视图，
        /// 且 _planes.Remove + _cargoBuilt 保持 true → 机队永久丢失、后续投送队列无人处理 → 单位永久卡死"消失"）。</summary>
        void Retire(LiftPlane p)
        {
            p.Cargo.Clear();
            p.CargoIdx = 0;
            p.Phase = 0;   // StepPlane Phase==0 分支自动隐藏 Root
        }

        /// <summary>V9.6.2 兜底复位：Lifted 但不在任何队列/机载中的单位强制恢复（异常泄漏防永久卡死），计数供探针</summary>
        void RecoverStuck()
        {
            int n = 0;
            if (GM.Ground != null)
            {
                foreach (var g in GM.Ground.Ours)
                {
                    if (g == null || !g.Lifted) continue;
                    if (!UnitInFlight(g) && !InQueue(_groundQueue, g)) { g.Lifted = false; n++; }
                }
            }
            if (GM.State != null)
            {
                foreach (var fu in GM.State.FriendlyUnits)
                {
                    if (fu == null || !fu.Lifted) continue;
                    if (!UnitInFlight(fu) && !InQueue(_infQueue, fu)) { fu.Lifted = false; n++; }
                }
            }
            if (n > 0) _recoveredStuck += n;
        }
        bool UnitInFlight(object unit)
        {
            foreach (var p in _planes)
                foreach (var c in p.Cargo) if (c.Unit == unit) return true;
            return false;
        }
        static bool InQueue(Queue<LiftRequest> q, object unit)
        {
            foreach (var r in q) if (r.Unit == unit) return true;
            return false;
        }

        static GameObject ViewOf(object unit)
        {
            if (unit is GroundWarfareSystem.GroundUnit g) return g.View;
            if (unit is FriendlyUnit fu) return fu.View;
            return null;
        }

        static Vector2 PosOf(object unit)
        {
            if (unit is GroundWarfareSystem.GroundUnit g) return new Vector2(g.X, g.Z);
            if (unit is FriendlyUnit fu) return new Vector2(fu.X, fu.Z);
            return Vector2.zero;
        }

        void SetPlaneTarget(LiftPlane p, float x, float z, float alt)
        {
            p.TX = x; p.TZ = z; p.TAlt = alt;
        }

        // ================= V9.6.2 投送端强化 =================
        /// <summary>V9.6.3f 未解锁投送提示节流：真实时间 10s 仅提示一次，防全场超距单位每 tick 刷事件 → 编年史/滚动条每帧重建卡死</summary>
        bool HintGate()
        {
            if (Time.unscaledTime < _hintCd) return false;
            _hintCd = Time.unscaledTime + 10f;
            return true;
        }
        /// <summary>地面部队集结（超100格）：投送不可用（机种未解锁）时广播提示并返回 false，由调用方落巡航</summary>
        public bool RequestLiftAuto(GroundWarfareSystem.GroundUnit u, float fx, float fz)
        {
            if (u == null) return false;
            if (!UnlockedCargo() && !UnlockedHeli())
            {
                if (HintGate())
                    GM.AddEvent("bad", "✈ 运输机需公元1900/直升机公元1949解锁，当前无法远程投送，" + u.Name + "改为就近巡航");
                return false;
            }
            return RequestLift(u, fx, fz, 1f);
        }
        /// <summary>军人集结（超100格）：投送不可用广播提示并返回 false，由调用方回城</summary>
        public bool RequestLiftAuto(FriendlyUnit u, float fx, float fz)
        {
            if (u == null) return false;
            if (!UnlockedCargo() && !UnlockedHeli())
            {
                if (HintGate())
                    GM.AddEvent("bad", "✈ 运输机需公元1900/直升机公元1949解锁，当前无法远程投送，军人回城待命");
                return false;
            }
            return RequestLift(u, fx, fz, 0f);
        }

        // ================= 程序化低多边形模型（运输机/直升机）=================
        LiftPlane BuildPlane(int kind)
        {
            var p = new LiftPlane { Kind = kind, Phase = 0 };
            var root = new GameObject(kind == 0 ? "AirLift_Cargo" : "AirLift_Heli");
            root.transform.SetParent(GM.transform, false);
            p.Root = root;
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);

            if (kind == 0) BuildCargoPlane(model.transform, out p.View);
            else BuildHelicopter(model.transform, out p.View);
            if (p.View == null) p.View = model;

            foreach (var col in root.GetComponentsInChildren<Collider>()) Object.Destroy(col);
            root.SetActive(false);
            _planes.Add(p);
            return p;
        }

        void BuildCargoPlane(Transform parent, out GameObject body)
        {
            var bodyMat = ShaderHelper.Pbr(new Color(0.32f, 0.42f, 0.22f), 0.2f, 0.45f, 9631, 0.5f);   // 军绿
            var wingMat  = ShaderHelper.Pbr(new Color(0.42f, 0.52f, 0.30f), 0.2f, 0.4f, 9632, 0.5f);  // 翼面灰绿
            var darkMat  = ShaderHelper.Pbr(new Color(0.12f, 0.12f, 0.14f), 0.2f, 0.5f, 9633, 0.4f);

            var bodyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyGo.name = "Fuselage"; bodyGo.transform.SetParent(parent, false);
            bodyGo.transform.localScale = new Vector3(0.9f, 0.9f, 3.4f);
            bodyGo.GetComponent<Renderer>().material = bodyMat;

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose"; nose.transform.SetParent(parent, false);
            nose.transform.localPosition = new Vector3(0, 0.05f, 1.9f);
            nose.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);
            nose.GetComponent<Renderer>().material = bodyMat;

            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = "Wing"; wing.transform.SetParent(parent, false);
            wing.transform.localPosition = new Vector3(0, 0.1f, 0.2f);
            wing.transform.localScale = new Vector3(5.4f, 0.14f, 1.3f);
            wing.GetComponent<Renderer>().material = wingMat;

            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "TailPlane"; tail.transform.SetParent(parent, false);
            tail.transform.localPosition = new Vector3(0, 0.35f, -1.55f);
            tail.transform.localScale = new Vector3(2.2f, 0.1f, 0.7f);
            tail.GetComponent<Renderer>().material = wingMat;

            var fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fin.name = "Fin"; fin.transform.SetParent(parent, false);
            fin.transform.localPosition = new Vector3(0, 0.9f, -1.6f);
            fin.transform.localScale = new Vector3(0.18f, 0.9f, 0.7f);
            fin.GetComponent<Renderer>().material = darkMat;

            // 螺旋桨（旋翼动画对象：双桨叶）
            var rotor = new GameObject("Prop");
            rotor.transform.SetParent(parent, false);
            rotor.transform.localPosition = new Vector3(0, 0.6f, 1.55f);
            var b1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b1.name = "BladeA"; b1.transform.SetParent(rotor.transform, false);
            b1.transform.localPosition = new Vector3(1.4f, 0, 0); b1.transform.localScale = new Vector3(2.8f, 0.08f, 0.35f);
            b1.GetComponent<Renderer>().material = darkMat;
            var b2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b2.name = "BladeB"; b2.transform.SetParent(rotor.transform, false);
            b2.transform.localPosition = new Vector3(-1.4f, 0, 0); b2.transform.localScale = new Vector3(2.8f, 0.08f, 0.35f);
            b2.GetComponent<Renderer>().material = darkMat;
            body = bodyGo;
        }

        void BuildHelicopter(Transform parent, out GameObject body)
        {
            var bodyMat = ShaderHelper.Pbr(new Color(0.22f, 0.36f, 0.48f), 0.25f, 0.45f, 9641, 0.5f);  // 蓝灰机身
            var darkMat = ShaderHelper.Pbr(new Color(0.10f, 0.10f, 0.12f), 0.1f, 0.4f, 9642, 0.4f);
            var glass   = ShaderHelper.Pbr(new Color(0.45f, 0.70f, 0.88f), 0.2f, 0.9f, 9643, 0.3f);

            var bodyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyGo.name = "Hull"; bodyGo.transform.SetParent(parent, false);
            bodyGo.transform.localScale = new Vector3(0.95f, 0.85f, 1.6f);
            bodyGo.GetComponent<Renderer>().material = bodyMat;

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy"; canopy.transform.SetParent(parent, false);
            canopy.transform.localPosition = new Vector3(0, 0.12f, 0.85f);
            canopy.transform.localScale = new Vector3(0.8f, 0.55f, 0.7f);
            canopy.GetComponent<Renderer>().material = glass;

            var boom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boom.name = "Boom"; boom.transform.SetParent(parent, false);
            boom.transform.localPosition = new Vector3(0, 0.15f, -1.05f);
            boom.transform.localScale = new Vector3(0.22f, 0.22f, 1.2f);
            boom.GetComponent<Renderer>().material = bodyMat;

            // 主旋翼（旋翼动画对象）
            var rotor = new GameObject("Rotor");
            rotor.transform.SetParent(parent, false);
            rotor.transform.localPosition = new Vector3(0, 0.65f, 0.05f);
            for (int i = 0; i < 2; i++)
            {
                var bl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bl.name = "Blade" + i; bl.transform.SetParent(rotor.transform, false);
                bl.transform.localPosition = new Vector3(i == 0 ? 1.7f : -1.7f, 0, 0);
                bl.transform.localScale = new Vector3(3.4f, 0.06f, 0.3f);
                bl.GetComponent<Renderer>().material = darkMat;
            }

            // 尾旋翼
            var tailRotor = new GameObject("TailRotor");
            tailRotor.transform.SetParent(parent, false);
            tailRotor.transform.localPosition = new Vector3(0, 0.55f, -1.75f);
            var tb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tb.name = "TBlade"; tb.transform.SetParent(tailRotor.transform, false);
            tb.transform.localScale = new Vector3(0.12f, 0.9f, 0.5f);
            tb.GetComponent<Renderer>().material = darkMat;

            // 起落架
            for (int s = -1; s <= 1; s += 2)
            {
                var skid = GameObject.CreatePrimitive(PrimitiveType.Cube);
                skid.name = "Skid"; skid.transform.SetParent(parent, false);
                skid.transform.localPosition = new Vector3(s * 0.5f, -0.55f, 0.3f);
                skid.transform.localScale = new Vector3(0.14f, 0.14f, 1.6f);
                skid.GetComponent<Renderer>().material = darkMat;
            }
            body = bodyGo;
        }

        /// <summary>Web 探针：air:q队列数,p忙机数|机型;lifted:地面/军人投送中;stuck:兜底复位累计</summary>
        public string Probe()
        {
            int busy = 0, cargo = 0, heli = 0;
            foreach (var p in _planes)
            {
                if (p.Phase > 0) busy++;
                if (p.Kind == 0) cargo++; else heli++;
            }
            int gl = 0, il = 0;
            if (GM.Ground != null) foreach (var g in GM.Ground.Ours) if (g.Lifted) gl++;
            if (GM.State != null) foreach (var fu in GM.State.FriendlyUnits) if (fu.Lifted) il++;
            return "air:q" + (_groundQueue.Count + _infQueue.Count) + ",p" + busy
                 + ",f" + cargo + "c" + heli + "h;lifted:" + gl + "/" + il + ";stuck:" + _recoveredStuck;
        }

        /// <summary>新局清理（StartNewGame 调用）：销毁飞机视图与队列</summary>
        public void ResetRuntime()
        {
            for (int i = _planes.Count - 1; i >= 0; i--)
            {
                var p = _planes[i];
                if (p.View != null) Object.Destroy(p.View);
                if (p.Root != null) Object.Destroy(p.Root);
            }
            _planes.Clear();
            _groundQueue.Clear(); _infQueue.Clear();
            _cargoBuilt = false; _heliBuilt = false; _recoveredStuck = 0; _hintCd = 0f;   // V9.6.3f 提示节流一并重置
        }
    }
}
