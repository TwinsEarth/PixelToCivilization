using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.8.5 紧急集结令 · 重新设计（推倒重写，非修补）。
    /// 铁律：插旗/按钮链路上【零 UI 构建】——不 AddEvent、不 Toast、不刷新编年史/Ticker/日志、不广播语音。
    /// 旗帜在 Init 预建一次，落旗仅改位置+材质引用；反馈走静态 HUD 状态槽（预建 Text，只改 text）；
    /// 响应统计延迟 0.6s 执行一次（节流 3s），结果写 HUD 槽，不再产生任何事件广播。
    /// 旗点与种类仍存 GameState（随存档持久化），读档用预建旗帜重建视图（零创建）。
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
        WorldGenerator _terrain;          // V9.8.5 Init 缓存，落旗零 FindObjectOfType

        // V9.8.5 节流/延迟统计
        float _lastRallyAt = -999f;
        float _lastRespAt  = -999f;
        float _respPending = -1f;         // >0 表示等待延迟统计；到点执行一次
        const float RallyInterval = 0.5f; // 插旗最小间隔
        const float RespDelay = 0.6f;     // 落旗后延迟统计
        const float RespGap   = 3f;       // 统计节流

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _terrain = FindObjectOfType<WorldGenerator>();   // 仅一次
            BuildFlag("navy", 0f, 0f);                       // 预建旗帜（初始隐藏）
            if (_flagRoot != null) _flagRoot.SetActive(false);
        }

        /// <summary>每帧：延迟响应统计（只写 HUD 槽，不广播）</summary>
        public override void Tick(float dt)
        {
            if (_respPending > 0f)
            {
                _respPending -= dt;
                if (_respPending <= 0f)
                {
                    _respPending = -1f;
                    if (Time.unscaledTime >= _lastRespAt + RespGap)
                    {
                        _lastRespAt = Time.unscaledTime;
                        TryReportResponders();
                    }
                }
            }
        }

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

        /// <summary>玩家落旗：kind = navy / ground / inf，x z 为世界坐标。全程零 UI 构建。</summary>
        public void SetRally(string kind, float x, float z)
        {
            try { SetRallyCore(kind, x, z); }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Rally] SetRally guard: " + e.GetType().Name + " " + e.Message);
                Hint("⚠ 集结令执行异常，请重试");
            }
        }
        void SetRallyCore(string kind, float x, float z)
        {
            // 0.5s 插旗节流：连点不重复触发
            if (Time.unscaledTime < _lastRallyAt + RallyInterval) return;
            _lastRallyAt = Time.unscaledTime;
            // NaN/Inf 防御
            if (float.IsNaN(x) || float.IsNaN(z) || float.IsInfinity(x) || float.IsInfinity(z))
            { Hint("⚠ 旗点无效，请重新落旗"); return; }
            // 陆地防御（用缓存引用）：地面/军人旗须在陆地，海军旗允许海上
            if (kind != "navy" && _terrain != null && _terrain.IsWater(x, z))
            { Hint("⚠ 地面/军人旗须插在陆地"); return; }
            var s = GM.State;
            s.RallyKind = kind; s.RallyX = x; s.RallyZ = z;
            // 移动预建旗帜 + 换旗色（零创建）
            if (_flagRoot != null)
            {
                _flagRoot.SetActive(true);
                _flagRoot.transform.position = new Vector3(x, 0, z);
                Color c = kind == "navy" ? NavyColor : kind == "ground" ? GroundColor : InfColor;
                if (_cloth != null)  _cloth.sharedMaterial  = FlagClothMat(c, kind);
                if (_clothB != null) _clothB.sharedMaterial = FlagClothMat(c, kind);
            }
            string nm = kind == "navy" ? "蓝旗·海军集结" : kind == "ground" ? "绿旗·地面部队集结" : "红旗·军人集结";
            Hint("🚩 紧急集结令：" + nm + "（" + RallyRange + " 格内单位向军旗列阵）");
            // 延迟响应统计（0.6s 后一次，节流 3s；只写 HUD 槽）
            _respPending = RespDelay;
        }

        /// <summary>延迟统计并写 HUD 槽（异常全隔离，不影响主循环）</summary>
        void TryReportResponders()
        {
            try
            {
                var s = GM.State;
                if (string.IsNullOrEmpty(s.RallyKind) || s.RallyX <= -9000f) return;
                int[] resp = CountResponders(s.RallyKind, s.RallyX, s.RallyZ);
                string txt = "";
                if (resp[0] > 0) txt = resp[0] + " 地面部队";
                if (resp[1] > 0) txt += (txt.Length > 0 ? "，" : "") + resp[1] + " 艘军舰";
                if (resp[2] > 0) txt += (txt.Length > 0 ? "，" : "") + resp[2] + " 名军人";
                if (txt.Length > 0) Hint("📣 " + RallyRange + " 格内响应：" + txt + " 听令向军旗列阵");
            }
            catch (System.Exception e) { Debug.LogWarning("[Rally] resp guard: " + e.GetType().Name + " " + e.Message); }
        }

        /// <summary>统计 RallyRange 格内响应集结的单位数（0=地面部队 / 1=军舰 / 2=军人）。只读，不广播。</summary>
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

        /// <summary>撤销集结令（清旗）：隐藏预建旗帜 + 清 HUD 槽，零创建零广播。</summary>
        public void ClearRally()
        {
            GM.State.RallyKind = ""; GM.State.RallyX = -9999f; GM.State.RallyZ = -9999f;
            if (_flagRoot != null) _flagRoot.SetActive(false);
            _respPending = -1f;
            Hint("");
        }

        /// <summary>读档恢复：若存档带旗则显示预建旗帜并定位（零创建），否则隐藏</summary>
        public void RestoreFlag()
        {
            var s = GM.State;
            if (_flagRoot == null) return;
            if (!string.IsNullOrEmpty(s.RallyKind) && s.RallyX > -9000f)
            {
                _flagRoot.SetActive(true);
                _flagRoot.transform.position = new Vector3(s.RallyX, 0, s.RallyZ);
                Color c = s.RallyKind == "navy" ? NavyColor : s.RallyKind == "ground" ? GroundColor : InfColor;
                if (_cloth != null)  _cloth.sharedMaterial  = FlagClothMat(c, s.RallyKind);
                if (_clothB != null) _clothB.sharedMaterial = FlagClothMat(c, s.RallyKind);
            }
            else _flagRoot.SetActive(false);
        }

        /// <summary>静态 HUD 状态槽（预建 Text，只改 text；空串隐藏）。零对象创建。</summary>
        void Hint(string msg)
        {
            try
            {
                var ui = PixelToCivilization.UI.UIManager.Instance;
                if (ui != null) ui.SetRallyHint(msg);
            }
            catch (System.Exception e) { Debug.LogWarning("[Rally] hint guard: " + e.GetType().Name + " " + e.Message); }
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

        /// <summary>预建旗帜体（Init 调用一次；子物体恒 5：底座/旗杆/旗面/旗面背/清碰撞由创建时处理）</summary>
        void BuildFlag(string kind, float x, float z)
        {
            _flagRoot = new GameObject("RallyFlag");
            _flagRoot.transform.SetParent(GM.transform, false);
            CreateFlagBody();
            _flagRoot.transform.position = new Vector3(x, 0, z);
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
            // 清除碰撞（不挡点击/不参与物理）——仅预建时执行一次
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
