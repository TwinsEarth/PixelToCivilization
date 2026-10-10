using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.6.0 紧急集结令：底部插旗（蓝旗=海军 / 绿旗=地面作战部队 / 红旗=军人），
    /// 100 格范围内对应我方军事单位自动向军旗坐标集结列阵。
    /// 旗点与种类存于 GameState（随存档持久化），旗帜模型为运行时视图（读档后由 SetRally 重建）。
    /// </summary>
    public class RallySystem : GameSystemBase
    {
        public const float RallyRange = 100f;      // 格（雷达集结半径）
        public const float FormRange = 10f;        // 列阵半径（世界单位，单位进入此圈即待命）
        static readonly Color NavyColor   = new Color(0.12f, 0.39f, 0.96f);
        static readonly Color GroundColor = new Color(0.18f, 0.62f, 0.27f);
        static readonly Color InfColor    = new Color(0.89f, 0.23f, 0.23f);

        GameObject _flagRoot;
        Renderer _cloth, _clothB;

        public override void Init(GameManager gm) { base.Init(gm); }

        /// <summary>指定兵种集结令是否生效</summary>
        public bool Active(string kind)
        {
            var s = GM.State;
            return !string.IsNullOrEmpty(s.RallyKind) && s.RallyKind == kind && s.RallyX > -9000f;
        }

        /// <summary>指定兵种集结目标（世界坐标），无旗返回 null</summary>
        public Vector2? Target(string kind)
        {
            if (!Active(kind)) return null;
            return new Vector2(GM.State.RallyX, GM.State.RallyZ);
        }

        /// <summary>玩家落旗：kind = navy / ground / inf，x z 为世界坐标</summary>
        float _lastRallyAt = -999f;
        float _lastRespAt = -999f;
        const float RallyInterval = 0.5f;   // V9.8.2 插旗最小间隔（防连点打崩）
        const float RespGap = 3f;           // V9.8.2 响应广播节流（防高频插旗刷屏+GC 尖峰）
        public void SetRally(string kind, float x, float z)
        {
            // V9.8.4 整体异常护栏：插旗链路任何意外（列表结构/材质/广播）都不允许打断游戏主循环
            try { SetRallyCore(kind, x, z); }
            catch (System.Exception e) { Debug.LogWarning("[Rally] SetRally guard: " + e.GetType().Name + " " + e.Message); }
        }
        void SetRallyCore(string kind, float x, float z)
        {
            // V9.8.2 插旗节流：0.5s 内重复插旗直接忽略（玩家连点不再反复触发 广播/语音/响应统计 全链）
            if (Time.unscaledTime < _lastRallyAt + RallyInterval) return;
            _lastRallyAt = Time.unscaledTime;
            // V9.6.3f2 防御：NaN/Inf 旗点直接忽略（防世界坐标污染导致集结单位坐标 NaN → 渲染/寻路冻结"卡死"）
            if (float.IsNaN(x) || float.IsNaN(z) || float.IsInfinity(x) || float.IsInfinity(z))
            { GM.AddEvent("bad", "⚠ 集结令旗点无效，请重新落旗"); return; }
            // V9.6.3f2 防御：地面部队/军人旗必须插在陆地（海军旗允许海上）
            var terr = UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            if (kind != "navy" && terr != null && terr.IsWater(x, z))
            { GM.AddEvent("bad", "⚠ 地面部队/军人军旗须插在陆地（海军旗可插海上）"); return; }
            var s = GM.State;
            s.RallyKind = kind; s.RallyX = x; s.RallyZ = z;
            BuildFlag(kind, x, z);
            string nm = kind == "navy" ? "蓝旗·海军集结" : kind == "ground" ? "绿旗·地面部队集结" : "红旗·军人集结";
            GM.AddEvent("good", "🚩 紧急集结令：" + nm + "（" + RallyRange + " 格内单位向军旗列阵）");
            // V9.8.1 集结响应统计：插旗后立刻广播范围内响应单位数，让玩家直观看到集结令已生效
            // V9.8.2 响应广播 3s 节流：高频插旗只报首条，其余静默生效（横幅始终显示）
            if (Time.unscaledTime >= _lastRespAt + RespGap)
            {
                _lastRespAt = Time.unscaledTime;
                int[] resp = CountResponders(kind, x, z);
                string respTxt = resp[0] > 0 ? resp[0] + " 地面部队" : "";
                if (resp[1] > 0) respTxt += (respTxt.Length > 0 ? "，" : "") + resp[1] + " 艘军舰";
                if (resp[2] > 0) respTxt += (respTxt.Length > 0 ? "，" : "") + resp[2] + " 名军人";
                if (respTxt.Length > 0) GM.AddEvent("good", "📣 " + RallyRange + " 格内响应：" + respTxt + " 听令向军旗列阵");
            }
            // V9.6.1 战时广播：传令兵传达集结指令（横幅+编年史）
            // V9.8.3 根治：插旗链路改静默通道（不触发 speechSynthesis 语音）——高频插旗是 Chrome 崩溃源
            if (GM.War != null)
                GM.War.SilentCommand("传令——" + nm + "！" + RallyRange + " 格内各军听令，火速向军旗列阵；超出 " + RallyRange + " 格者，由运输机、直升机远程投送！");
        }

        /// <summary>
        /// V9.8.1 统计 RallyRange 格内响应集结的单位数（0=地面部队 / 1=军舰 / 2=军人）。
        /// 地面部队读 Ground.Ours（世界坐标），军舰读 State.Ships，军人读 State.FriendlyUnits。
        /// </summary>
        int[] CountResponders(string kind, float x, float z)
        {
            int ground = 0, navy = 0, inf = 0;
            float range = RallyRange * GameConstants.Tile;   // 格→世界单位（每格 4 单位）
            if (kind == "ground" && GM.Ground != null)
            {
                foreach (var u in GM.Ground.Ours)
                {
                    if (u == null || u.View == null) continue;
                    float dx = u.X - x, dz = u.Z - z;
                    if (dx * dx + dz * dz <= range * range) ground++;
                }
            }
            else if (kind == "navy" && GM.State != null)
            {
                foreach (var sh in GM.State.Ships)
                {
                    if (sh == null || sh.View == null) continue;
                    float dx = sh.X - x, dz = sh.Z - z;
                    if (dx * dx + dz * dz <= range * range) navy++;
                }
            }
            else if (kind == "inf" && GM.State != null)
            {
                foreach (var fu in GM.State.FriendlyUnits)
                {
                    if (fu == null || fu.View == null) continue;
                    float dx = fu.X - x, dz = fu.Z - z;
                    if (dx * dx + dz * dz <= range * range) inf++;
                }
            }
            return new[] { ground, navy, inf };
        }

        /// <summary>撤销集结令（清旗）</summary>
        public void ClearRally()
        {
            GM.State.RallyKind = ""; GM.State.RallyX = -9999f; GM.State.RallyZ = -9999f;
            if (_flagRoot != null) { Object.Destroy(_flagRoot); _flagRoot = null; _cloth = null; _clothB = null; }
            // V9.6.1 战时广播：撤销指令（V9.8.3 改静默通道，不触发语音）
            if (GM.War != null) GM.War.SilentCommand("传令——紧急集结令撤销，各军回防待命！");
        }

        /// <summary>读档恢复：若存档带旗则重建旗帜模型</summary>
        public void RestoreFlag()
        {
            var s = GM.State;
            if (!string.IsNullOrEmpty(s.RallyKind) && s.RallyX > -9000f) BuildFlag(s.RallyKind, s.RallyX, s.RallyZ);
            else if (_flagRoot != null) { Object.Destroy(_flagRoot); _flagRoot = null; _cloth = null; _clothB = null; }
        }

        // V9.8.4 旗色材质静态复用：3 种旗色只各建一次材质（插旗换色仅切换引用，杜绝高频插旗材质对象堆积）
        static Material _navyMat, _groundMat, _infMat;
        Material FlagClothMat(Color c, string kind)
        {
            if (kind == "navy" && _navyMat != null) return _navyMat;
            if (kind == "ground" && _groundMat != null) return _groundMat;
            if (kind == "inf" && _infMat != null) return _infMat;
            var m = ShaderHelper.Mat(c);
            if (kind == "navy") _navyMat = m; else if (kind == "ground") _groundMat = m; else _infMat = m;
            return m;
        }

        /// <summary>
        /// V9.6.3f2 旗模型只创建一次：后续落旗仅移动位置+换旗面颜色，不再 Destroy/Create 子物体。
        /// 根因：旧实现每次插旗新建 5 个 primitive 并 Object.Destroy 旧子物体（延迟销毁），多次快速插旗
        /// → 子物体堆积 + 每帧 GC 尖峰，WebGL 表现即"插旗多几次就卡死"。
        /// </summary>
        void BuildFlag(string kind, float x, float z)
        {
            if (_flagRoot == null)
            {
                _flagRoot = new GameObject("RallyFlag");
                _flagRoot.transform.SetParent(GM.transform, false);
                CreateFlagBody();
            }
            _flagRoot.transform.position = new Vector3(x, 0, z);
            Color c = kind == "navy" ? NavyColor : kind == "ground" ? GroundColor : InfColor;
            if (_cloth != null) _cloth.sharedMaterial = FlagClothMat(c, kind);
            if (_clothB != null) _clothB.sharedMaterial = FlagClothMat(c, kind);
        }

        void CreateFlagBody()
        {
            // 底座
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "Base"; baseGo.transform.SetParent(_flagRoot.transform, false);
            baseGo.transform.localPosition = new Vector3(0, 0.16f, 0);
            baseGo.transform.localScale = new Vector3(1.5f, 0.32f, 1.5f);
            baseGo.GetComponent<Renderer>().material = ShaderHelper.Mat(new Color(0.38f, 0.32f, 0.26f));
            // 旗杆
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole"; pole.transform.SetParent(_flagRoot.transform, false);
            pole.transform.localPosition = new Vector3(0, 1.75f, 0);
            pole.transform.localScale = new Vector3(0.12f, 1.75f, 0.12f);
            pole.GetComponent<Renderer>().material = ShaderHelper.Mat(new Color(0.28f, 0.24f, 0.2f));
            // 旗面（复用引用：换色即换阵营旗）
            var cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cloth.name = "Cloth"; cloth.transform.SetParent(_flagRoot.transform, false);
            cloth.transform.localPosition = new Vector3(0.68f, 2.45f, 0f);
            cloth.transform.localScale = new Vector3(1.25f, 0.72f, 0.09f);
            _cloth = cloth.GetComponent<Renderer>();
            // 旗面反向补一小块（双面视觉）
            var clothB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clothB.name = "ClothBack"; clothB.transform.SetParent(_flagRoot.transform, false);
            clothB.transform.localPosition = new Vector3(0.06f, 2.45f, 0f);
            clothB.transform.localScale = new Vector3(0.3f, 0.72f, 0.09f);
            _clothB = clothB.GetComponent<Renderer>();
            // 清除碰撞（不挡点击/不参与物理）——仅首次创建执行一次
            foreach (var col in _flagRoot.GetComponentsInChildren<Collider>()) Object.Destroy(col);
        }

        /// <summary>Web 回归：旗根对象（验证子物体复用不堆积）</summary>
        public GameObject GetFlagRootForTest() { return _flagRoot; }

        /// <summary>Web 探针：rally:kind|x,z 或 rally:none</summary>
        public string Probe()
        {
            var s = GM.State;
            if (string.IsNullOrEmpty(s.RallyKind) || s.RallyX <= -9000f) return "rally:none";
            return "rally:" + s.RallyKind + "|" + Mathf.RoundToInt(s.RallyX) + "," + Mathf.RoundToInt(s.RallyZ);
        }
    }
}
