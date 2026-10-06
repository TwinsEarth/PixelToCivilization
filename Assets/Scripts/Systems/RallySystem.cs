using UnityEngine;
using PixelToCivilization.Core;
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
        public void SetRally(string kind, float x, float z)
        {
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
            // V9.6.1 战时广播：传令兵传达集结指令（语音+横幅+编年史）
            if (GM.War != null)
                GM.War.Command("传令——" + nm + "！" + RallyRange + " 格内各军听令，火速向军旗列阵；超出 " + RallyRange + " 格者，由运输机、直升机远程投送！");
        }

        /// <summary>撤销集结令（清旗）</summary>
        public void ClearRally()
        {
            GM.State.RallyKind = ""; GM.State.RallyX = -9999f; GM.State.RallyZ = -9999f;
            if (_flagRoot != null) { Object.Destroy(_flagRoot); _flagRoot = null; _cloth = null; _clothB = null; }
            // V9.6.1 战时广播：撤销指令
            if (GM.War != null) GM.War.Command("传令——紧急集结令撤销，各军回防待命！");
        }

        /// <summary>读档恢复：若存档带旗则重建旗帜模型</summary>
        public void RestoreFlag()
        {
            var s = GM.State;
            if (!string.IsNullOrEmpty(s.RallyKind) && s.RallyX > -9000f) BuildFlag(s.RallyKind, s.RallyX, s.RallyZ);
            else if (_flagRoot != null) { Object.Destroy(_flagRoot); _flagRoot = null; _cloth = null; _clothB = null; }
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
            if (_cloth != null) _cloth.sharedMaterial = ShaderHelper.Mat(c);
            if (_clothB != null) _clothB.sharedMaterial = ShaderHelper.Mat(c);
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
