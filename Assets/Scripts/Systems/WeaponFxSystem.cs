using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Audio;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.5.3 统一武器特效系统：炮口闪光 / 命中火花 / 爆炸火光 / 弹道拖尾 + 即时音效。
    /// 全部经 GameObjectPool 分桶 Rent/Return 复用（统一粒子&内存回收机制），
    /// 避免每发开火 Instantiate/Destroy 造成的 GC 尖峰。
    /// - 特效根挂 GameManager 下，世界重置走既有 GameObjectPool.ClearAll；
    /// - 音效走 AudioManager.PlaySfx（WebGL 手势解锁 / 静音持久化 / 缺失资源自动静默）；
    /// - 相机 85 世界单位外不生成视觉（省算力），音效由 AudioManager 距离衰减管理。
    /// 海军 CannonFx（V9.2.3）保留——已有成熟声光与共享材质缓存修复，不迁移。
    /// </summary>
    public static class WeaponFxSystem
    {
        const float VISIBLE_RANGE = 85f;
        static Transform _root;

        static Transform Root
        {
            get
            {
                if (_root == null)
                {
                    var gm = GameManager.Instance;
                    var host = gm != null ? gm.transform : null;
                    var go = new GameObject("WeaponFx");
                    if (host != null) go.transform.SetParent(host, false);
                    _root = go.transform;
                }
                return _root;
            }
        }

        static bool Visible(Vector3 p)
        {
            var cam = Camera.main;
            return cam != null && Vector3.Distance(cam.transform.position, p) < VISIBLE_RANGE;
        }

        /// <summary>炮口/枪口闪光：自发光 Quad + 点光源，0.22s 消失（回收）。</summary>
        public static void Muzzle(Vector3 p, float scale = 1.3f)
        {
            if (!Visible(p)) return;
            var go = GameObjectPool.Rent("wfx_muzzle", Root, CreateMuzzle);
            go.transform.position = p;
            var lf = go.GetComponent<FxLifetime>();
            if (lf == null) lf = go.AddComponent<FxLifetime>();
            lf.Begin("wfx_muzzle", 0.22f);
            var light = go.transform.Find("light");
            if (light != null)
            {
                var l = light.GetComponent<Light>();
                if (l != null) { l.intensity = 8f; l.range = 15f; }
            }
            var flash = go.transform.Find("flash");
            if (flash != null)
            {
                flash.localScale = Vector3.one * scale;
                if (Camera.main != null) flash.LookAt(Camera.main.transform);
            }
        }

        /// <summary>命中火花：发光线球 + 2 团烟雾，0.5s 消失。</summary>
        public static void Hit(Vector3 p)
        {
            if (!Visible(p)) return;
            var go = GameObjectPool.Rent("wfx_hit", Root, CreateHit);
            go.transform.position = p;
            var lf = go.GetComponent<FxLifetime>();
            if (lf == null) lf = go.AddComponent<FxLifetime>();
            lf.Begin("wfx_hit", 0.5f);
        }

        /// <summary>V9.6.3f 爆炸火光：蘑菇云（爆心火球+蘑菇盖+盖缘+烟柱），整体黑泡缩小 1/3，每次爆炸随机大小 0.7–1.3 倍；0.8s 消失。</summary>
        public static void Explosion(Vector3 p)
        {
            if (!Visible(p)) return;
            var go = GameObjectPool.Rent("wfx_explosion", Root, CreateExplosion);
            go.transform.position = p;
            // V9.6.3f：黑泡缩小到原 1/3，并随机蘑菇云大小（0.7–1.3 倍）——不同爆炸大小不一
            float scl = Random.Range(0.7f, 1.3f) * 0.333f;
            go.transform.localScale = Vector3.one * scl;
            var lf = go.GetComponent<FxLifetime>();
            if (lf == null) lf = go.AddComponent<FxLifetime>();
            lf.Begin("wfx_explosion", 0.8f);
            var fire = go.transform.Find("fire");
            if (fire != null && Camera.main != null) fire.LookAt(Camera.main.transform);
        }

        /// <summary>弹道拖尾：a→b 发光线，0.25s 消失。</summary>
        public static void Tracer(Vector3 a, Vector3 b)
        {
            if (!Visible(a)) return;
            var go = GameObjectPool.Rent("wfx_tracer", Root, CreateTracer);
            go.transform.position = (a + b) * 0.5f;
            float len = Vector3.Distance(a, b);
            go.transform.localScale = new Vector3(0.06f, 0.06f, Mathf.Max(0.2f, len));
            go.transform.rotation = Quaternion.LookRotation(b - a);
            var lf = go.GetComponent<FxLifetime>();
            if (lf == null) lf = go.AddComponent<FxLifetime>();
            lf.Begin("wfx_tracer", 0.25f);
        }

        /// <summary>V9.6.4 火龙喷射（火塔开火）：沿 dir 排布 3 团递增火焰 + 根部强闪光，0.4s 消失（一烧一片的喷射感）。</summary>
        public static void FlameJet(Vector3 p, Vector3 dir, float scale = 1f)
        {
            if (!Visible(p)) return;
            var go = GameObjectPool.Rent("wfx_flamejet", Root, CreateFlameJet);
            go.transform.position = p;
            go.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
            go.transform.localScale = Vector3.one * scale;
            var lf = go.GetComponent<FxLifetime>();
            if (lf == null) lf = go.AddComponent<FxLifetime>();
            lf.Begin("wfx_flamejet", 0.4f);
        }

        /// <summary>V9.6.4 地面持续燃烧（火塔命中后 1s）：中心火球 + 3 团环绕火焰 + 浓烟上升，1s 消失。</summary>
        public static void Burn(Vector3 p, float scale = 1f)
        {
            if (!Visible(p)) return;
            var go = GameObjectPool.Rent("wfx_burn", Root, CreateBurn);
            go.transform.position = p;
            go.transform.localScale = Vector3.one * scale;
            var lf = go.GetComponent<FxLifetime>();
            if (lf == null) lf = go.AddComponent<FxLifetime>();
            lf.Begin("wfx_burn", 1f);
        }

        /// <summary>V9.6.4 烽火烟柱（烽火台常驻传警）：竖直烟柱 + 顶部火团，2.5s 消失后循环重放。</summary>
        public static void Smoke(Vector3 p, float scale = 1f)
        {
            if (!Visible(p)) return;
            var go = GameObjectPool.Rent("wfx_smoke", Root, CreateSmoke);
            go.transform.position = p;
            go.transform.localScale = Vector3.one * scale;
            var lf = go.GetComponent<FxLifetime>();
            if (lf == null) lf = go.AddComponent<FxLifetime>();
            lf.Begin("wfx_smoke", 2.5f);
        }

        /// <summary>即时音效：走 AudioManager（手势解锁/静音/资源缺失自动安全跳过）。</summary>
        public static void Sfx(string res, Vector3 p, float volume = 0.8f)
        {
            AudioManager.I?.PlaySfx(res, p, volume);
        }

        // ================= 桶创建器（仅首租时执行一次） =================
        static GameObject CreateMuzzle()
        {
            var go = new GameObject("muzzleFx");
            var flash = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(flash.GetComponent<Collider>());
            flash.name = "flash";
            flash.transform.SetParent(go.transform, false);
            flash.GetComponent<MeshRenderer>().sharedMaterial =
                ShaderHelper.Emissive(new Color(1f, 0.85f, 0.35f), new Color(3f, 2f, 0.6f));
            var lg = new GameObject("light");
            lg.transform.SetParent(go.transform, false);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.8f, 0.45f); l.intensity = 7f; l.range = 14f;
            return go;
        }

        static GameObject CreateHit()
        {
            var go = new GameObject("hitFx");
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(core.GetComponent<Collider>());
            core.name = "core";
            core.transform.SetParent(go.transform, false);
            core.GetComponent<MeshRenderer>().sharedMaterial =
                ShaderHelper.Emissive(new Color(1f, 0.75f, 0.3f), new Color(2.5f, 1.5f, 0.4f));
            AddSmokeChild(go, "smoke0", new Vector3(0, 0.15f, 0), 0.5f);
            AddSmokeChild(go, "smoke1", new Vector3(0, -0.1f, 0), 0.65f);
            return go;
        }

        /// <summary>V9.6.3f 蘑菇云爆炸：爆心火球（缩小）+ 顶部蘑菇盖（扁圆盘）+ 两侧盖缘 + 竖直烟柱 + 底部烟团；
        /// 子烟团整体缩放由 Explosion 的随机 localScale 控制（黑泡 1/3 + 大小 0.7–1.3 随机）。</summary>
        static GameObject CreateExplosion()
        {
            var go = new GameObject("explosionFx");
            var fire = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(fire.GetComponent<Collider>());
            fire.name = "fire";
            fire.transform.SetParent(go.transform, false);
            fire.transform.localScale = Vector3.one * 0.5f;   // V9.6.3f 爆心火球缩小
            fire.GetComponent<MeshRenderer>().sharedMaterial =
                ShaderHelper.Emissive(new Color(1f, 0.5f, 0.12f), new Color(4f, 2f, 0.4f));
            var flash = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(flash.GetComponent<Collider>());
            flash.name = "flash";
            flash.transform.SetParent(go.transform, false);
            flash.transform.localScale = Vector3.one * 0.8f;  // V9.6.3f 闪光随整体缩放（1/3 基数）
            flash.GetComponent<MeshRenderer>().sharedMaterial =
                ShaderHelper.Emissive(new Color(1f, 0.9f, 0.6f), new Color(3f, 2.5f, 1f));
            // 蘑菇云：顶部扁盖 + 两侧盖缘 + 竖直烟柱 + 底部烟团
            AddSmokeChild(go, "cap",    new Vector3(0f,  0.55f, 0f), 1.0f, 0.35f);   // 蘑菇盖（扁圆盘）
            AddSmokeChild(go, "capL",   new Vector3(0.4f, 0.5f, 0f), 0.6f, 0.5f);     // 左侧盖缘
            AddSmokeChild(go, "capR",   new Vector3(-0.4f, 0.5f, 0f), 0.6f, 0.5f);    // 右侧盖缘
            AddSmokeChild(go, "column", new Vector3(0f, -0.15f, 0f), 0.55f, 2.4f);    // 竖直烟柱
            AddSmokeChild(go, "baseL",  new Vector3(-0.3f, 0.05f, 0f), 0.45f, 1f);    // 底部烟团
            AddSmokeChild(go, "baseR",  new Vector3(0.3f, 0.05f, 0f), 0.45f, 1f);     // 底部烟团
            return go;
        }

        static GameObject CreateTracer()
        {
            var go = new GameObject("tracerFx");
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(beam.GetComponent<Collider>());
            beam.name = "beam";
            beam.transform.SetParent(go.transform, false);
            beam.GetComponent<MeshRenderer>().sharedMaterial =
                ShaderHelper.Emissive(new Color(1f, 0.95f, 0.5f), new Color(1.5f, 1.2f, 0.3f));
            return go;
        }

        /// <summary>V9.6.4 火龙喷射组：根部强火球 + 沿 +Z 依次放大 3 团火焰（go 朝向=LookRotation 朝向 dir）</summary>
        static GameObject CreateFlameJet()
        {
            var go = new GameObject("flamejetFx");
            AddFlameChild(go, "root",  new Vector3(0f,   0.3f, 0f),   0.5f);   // 根部火球
            AddFlameChild(go, "jet1",  new Vector3(0f,   0.35f, 0.8f), 0.72f);  // 火龙前端1
            AddFlameChild(go, "jet2",  new Vector3(0f,   0.4f,  1.7f), 0.95f);  // 火龙前端2（更大）
            AddFlameChild(go, "jet3",  new Vector3(0f,   0.45f, 2.6f), 1.2f);   // 火龙前端3（最大，一烧一片）
            return go;
        }

        /// <summary>V9.6.4 地面持续燃烧组：中心大火球 + 环绕三团小火 + 顶部烟</summary>
        static GameObject CreateBurn()
        {
            var go = new GameObject("burnFx");
            AddFlameChild(go, "core",  new Vector3(0f,      0.45f, 0f),   1.0f);  // 中心火球
            AddFlameChild(go, "f0",    new Vector3(0.55f,   0.25f, 0.15f), 0.55f);
            AddFlameChild(go, "f1",    new Vector3(-0.4f,   0.2f,  0.5f),  0.5f);
            AddFlameChild(go, "f2",    new Vector3(0.1f,    0.2f,  -0.6f), 0.45f);
            AddSmokeChild(go, "smoke", new Vector3(0f,      0.7f,  0f),     0.9f);  // 浓烟上升
            return go;
        }

        /// <summary>V9.6.4 烽火烟柱组：顶部火团 + 竖直烟柱</summary>
        static GameObject CreateSmoke()
        {
            var go = new GameObject("smokeFx");
            AddFlameChild(go, "beacon", new Vector3(0f, 0.2f, 0f), 0.7f);      // 顶部火团
            AddSmokeChild(go, "pillar", new Vector3(0f, 0.8f, 0f), 0.55f, 2.2f); // 竖直烟柱
            return go;
        }

        /// <summary>V9.6.4 火焰团子物体（自发光火色球，随整体生命周期隐藏，无独立动画）</summary>
        static void AddFlameChild(GameObject parent, string name, Vector3 localPos, float size)
        {
            var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(f.GetComponent<Collider>());
            f.name = name;
            f.transform.SetParent(parent.transform, false);
            f.transform.localPosition = localPos;
            f.transform.localScale = Vector3.one * size;
            f.GetComponent<MeshRenderer>().sharedMaterial =
                ShaderHelper.Emissive(new Color(1f, 0.45f, 0.08f), new Color(3.5f, 1.6f, 0.3f));
        }

        /// <summary>V9.6.3f 支持非均匀 Y 缩放（sy=1 为球团；0.35=扁蘑菇盖；2.4=竖直烟柱）</summary>
        static void AddSmokeChild(GameObject parent, string name, Vector3 localPos, float size, float sy = 1f)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(s.GetComponent<Collider>());
            s.name = name;
            s.transform.SetParent(parent.transform, false);
            s.transform.localPosition = localPos;
            var fade = s.AddComponent<SmokeFade>();
            fade.Size = size;
            fade.ScaleY = sy;
        }
    }

    /// <summary>特效根生命周期：到期后整组回收（Return 池）。烟雾子物体停用复用，不销毁。</summary>
    public class FxLifetime : MonoBehaviour
    {
        string _key;
        float _life, _t;
        bool _running;

        public void Begin(string key, float life)
        {
            _key = key; _life = life; _t = 0f; _running = true;
            foreach (Transform ch in transform)
            {
                var sf = ch.GetComponent<SmokeFade>();
                if (sf != null) { sf.ResetFade(); continue; }
                var r = ch.GetComponent<Renderer>();
                if (r != null) r.enabled = true;
                ch.gameObject.SetActive(true);
            }
        }

        void Update()
        {
            if (!_running) return;
            _t += Time.deltaTime;
            if (_t >= _life)
            {
                _running = false;
                GameObjectPool.Return(_key, gameObject); // 根物体停用缓存；烟雾子物体已 Expired 停用
            }
        }
    }

    /// <summary>烟雾团：私有材质淡出 + 上升扩大，到期停用（保留材质实例，池内复用）。
    /// V9.6.3f 新增 ScaleY 非均匀 Y 缩放（蘑菇盖扁 / 烟柱高），扩大动画同步保持比例。</summary>
    public class SmokeFade : MonoBehaviour
    {
        public float Size = 0.6f;
        public float ScaleY = 1f;   // V9.6.3f Y 轴倍率（1=球团；<1 扁；>1 高柱）
        public bool Expired { get; private set; }
        Material _mat;
        float _age, _life;
        bool _active;
        Vector3 _initLocalPos;   // V9.7.1 初始局部位置：池化复用时恢复，防上升位移逐次漂移

        void Awake()
        {
            _mat = new Material(ShaderHelper.Trans(new Color(0.28f, 0.28f, 0.28f, 0.6f), 0.6f));
            var r = GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = _mat;
            _initLocalPos = transform.localPosition;   // AddSmokeChild 在 AddComponent 前已设置 localPosition
        }

        public void ResetFade()
        {
            _age = 0f; _life = Random.Range(0.9f, 1.4f); _active = true; Expired = false;
            gameObject.SetActive(true);
            transform.localPosition = _initLocalPos;   // V9.7.1 复位上升漂移（旧实现只重置 scale，烟雾位置逐次漂移）
            transform.localScale = new Vector3(Size, Size * ScaleY, Size);
            if (_mat != null) { var c = _mat.color; c.a = 0.6f; _mat.color = c; }
            var r = GetComponent<Renderer>();
            if (r != null) r.enabled = true;
        }

        void Update()
        {
            if (!_active) return;
            _age += Time.deltaTime;
            float t01 = Mathf.Clamp01(_age / _life);
            transform.position += Vector3.up * Time.deltaTime * 1.6f;
            float s = Size + t01 * 1.8f;
            transform.localScale = new Vector3(s, s * ScaleY, s);   // V9.6.3f 保持蘑菇云比例扩大
            if (_mat != null) { var c = _mat.color; c.a = (1f - t01) * 0.6f; _mat.color = c; }
            if (t01 >= 1f)
            {
                _active = false; Expired = true;
                var r = GetComponent<Renderer>();
                if (r != null) r.enabled = false;
                gameObject.SetActive(false); // 保留材质实例，池内下一轮 ResetFade 复用
            }
        }
    }
}
