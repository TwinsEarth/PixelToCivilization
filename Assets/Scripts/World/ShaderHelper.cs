// V6.7.1 统一材质工厂：所有运行时实体一律走 URP/Lit 基础变体，
// 绝不运行时 enable _NORMALMAP/_METALLICSPECGLOSSMAP 关键字（WebGL 会剥离变体导致发黑）。
// V8.0.1 乐高积木像素风：LegoPlastic 模式下统一为高光滑塑料色（去噪点、提饱和、开实例化）。
using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.World
{
    /// <summary>
    /// 运行时材质中枢：建筑/人物/载具/植被/天气等所有 MeshRenderer 共用，
    /// 保证 WebGL 只用内置 shader、避免变体剥离发黑。
    /// </summary>
    public static class ShaderHelper
    {
        /// <summary>V8.0.1 乐高塑料模式开关。V9.0.1 现代风默认关闭，恢复 V7.1.1 干净光滑的写实质感（现代玻璃/沥青/混凝土另走显式材质）。</summary>
        public static bool LegoPlastic = false;

        static Shader Pick(params string[] names)
        {
            foreach (var n in names) { var s = Shader.Find(n); if (s != null) return s; }
            return Shader.Find("Standard"); // 兜底（URP 工程一般不会走到）
        }

        static Shader _lit;
        public static Shader Lit => _lit ??= Pick("Universal Render Pipeline/Lit", "Standard");

        static Shader _simple;
        public static Shader SimpleUnlit => _simple ??= Pick("Universal Render Pipeline/Unlit", "Unlit/Color", "Sprites/Default");

        static Shader _transparent;
        public static Shader Transparent => _transparent ??= Pick("Universal Render Pipeline/2D/Sprite-Lit-Default", "Universal Render Pipeline/Unlit", "Sprites/Default", "UI/Default");

        static Shader _water;
        public static Shader Water => _water ??= Pick("PxC/WaterURP", "Universal Render Pipeline/Lit", "Sprites/Default");

        static Shader _emissive;
        public static Shader EmissiveShader => _emissive ??= Pick("Universal Render Pipeline/Lit", "Standard");

        // 按颜色+参数缓存，避免同色实体反复建材质导致 SRP Batcher 断批
        static readonly Dictionary<int, Material> _cache = new Dictionary<int, Material>();

        /// <summary>标准塑料/实体材质（最常用）。</summary>
        public static Material Mat(Color c)
        {
            if (LegoPlastic) c = LegoKit.Tint(c);
            return Pbr(c, 0f, LegoPlastic ? 0.60f : 0.35f, StableSeed(c), 0f, false);
        }

        /// <summary>
        /// URP Lit PBR 材质。
        /// </summary>
        /// <param name="baseColor">基色</param><param name="metallic">金属度 0..1</param>
        /// <param name="smoothness">光滑度 0..1（越大越亮）</param><param name="seed">稳定随机种子</param>
        /// <param name="normalStrength">法线强度（仅 useDetail 时生效）</param>
        /// <param name="useDetail">是否贴程序化 Albedo/法线细节（哑光表面用）</param>
        public static Material Pbr(Color baseColor, float metallic, float smoothness, int seed,
                                   float normalStrength = 1f, bool useDetail = true)
        {
            // V8.0.1 乐高塑料：先统一参数再算缓存键，保证同色实体命中同一材质
            if (LegoPlastic)
            {
                baseColor = LegoKit.Tint(baseColor);
                if (metallic <= 0.02f) { smoothness = Mathf.Max(smoothness, 0.58f); normalStrength = 0f; useDetail = false; }
                else { smoothness = Mathf.Max(smoothness, 0.68f); normalStrength = 0f; useDetail = false; } // 电镀金属/金
            }
            int key = (baseColor.GetHashCode() * 397)
                      ^ (Mathf.RoundToInt(metallic * 8) << 4)
                      ^ (Mathf.RoundToInt(smoothness * 16) << 9)
                      ^ (useDetail ? 0x5bd1 : 0)
                      ^ (Mathf.RoundToInt(normalStrength * 4) << 20)
                      ^ seed;
            if (_cache.TryGetValue(key, out var m)) return m;

            m = new Material(Lit);
            m.name = "Pbr_" + baseColor.ToString();
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_Color", baseColor);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Surface", 0);
            m.SetFloat("_AlphaClip", 0);
            SetRenderMode(m, false);
            if (useDetail)
            {
                // V9.0.1fix 关键根因：实参顺序曾写成 Albedo(baseColor,128,seed)/Normal(128,seed,..)，
                // 把"种子"误传给"分辨率 res"形参。植被传入的 seed 是地形种子(约 2 千万)，导致 res≈2 千万，
                // 内部 new Color32[res*res] 要分配数 TB，WebGL 堆瞬间越界（启动 90% 裸 memory access out of bounds）。
                // 正确：第二参=种子 seed，第三参=分辨率 128。
                var a = PixelToCivilization.Art.ProceduralTextures.Albedo(baseColor, seed, 128);
                var n = PixelToCivilization.Art.ProceduralTextures.Normal(seed, 128, normalStrength);
                m.SetTexture("_BaseMap", a); m.SetTexture("_MainTex", a);
                m.SetTexture("_BumpMap", n); m.SetTexture("_NormalMap", n);
                m.SetFloat("_BumpScale", normalStrength);
            }
            m.enableInstancing = true; // V8.0.1 凸点/积木高复用，开 GPU 实例化
            _cache[key] = m;
            return m;
        }

        /// <summary>自发光材质（灯火/能量/激光/未来玻璃）。</summary>
        // V9.0.8 夜景注册表：记录基准发光色，EnvironmentDirector 按昼夜相位统一提亮/压暗
        static readonly List<(Material mat, Color baseEmit)> NightRegistry = new();
        public static Material Emissive(Color color, Color? emit = null)
        {
            Color e = emit ?? color;
            int key = (color.GetHashCode() * 31) ^ e.GetHashCode();
            if (_cache.TryGetValue(key, out var m)) return m;
            m = new Material(EmissiveShader);
            m.name = "Emit_" + color;
            m.SetColor("_BaseColor", color); m.SetColor("_Color", color);
            Color baseEmit = e * 1.6f;
            m.SetColor("_EmissionColor", baseEmit);
            m.EnableKeyword("_EMISSION");
            m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", 0.5f);
            m.enableInstancing = true;
            _cache[key] = m;
            NightRegistry.Add((m, baseEmit));
            return m;
        }
        /// <summary>V9.0.8 昼夜调度：dayFactor=1 正午（发光 0.9 倍，火/能量白天仍可见）；0 深夜（2.4 倍，亮窗/路灯/M 牌醒目）。</summary>
        public static void ApplyNight(float dayFactor)
        {
            float mult = Mathf.Lerp(2.4f, 0.9f, Mathf.Clamp01(dayFactor));
            for (int i = 0; i < NightRegistry.Count; i++)
            {
                var rec = NightRegistry[i];
                if (rec.mat != null) rec.mat.SetColor("_EmissionColor", rec.baseEmit * mult);
            }
        }

        /// <summary>半透明材质（玻璃/水/气泡/罩）。乐高模式下更光滑透亮，呈积木水面/塑料玻璃。</summary>
        public static Material Trans(Color c, float alpha = 0.5f)
        {
            if (LegoPlastic)
            {
                c = LegoKit.Tint(c);
                // 保留调用方在颜色里给定的 alpha（罩子0.18/烟0.5/漏斗0.7）；仅当颜色不透明时才用默认 alpha
                if (!(c.a > 0f && c.a < 1f)) c.a = alpha;
            }
            int key = 0x2233 ^ c.GetHashCode() ^ Mathf.RoundToInt(c.a * 16);
            if (_cache.TryGetValue(key, out var m)) return m;
            m = new Material(Lit);
            m.name = "Trans_" + c;
            m.SetColor("_BaseColor", c); m.SetColor("_Color", c);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_AlphaClip", 0);
            SetRenderMode(m, true);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", LegoPlastic ? 0.90f : 0.75f);
            m.enableInstancing = true;
            _cache[key] = m;
            return m;
        }

        /// <summary>UI/世界空间纯 Sprite 用（头像等）。</summary>
        public static Material SpriteUnlit()
        {
            int key = 0x77;
            if (_cache.TryGetValue(key, out var m)) return m;
            m = new Material(Transparent); m.name = "PxCSprite";
            m.enableInstancing = true;
            _cache[key] = m;
            return m;
        }

        public static Material Unlit(Color c)
        {
            int key = 0x99 ^ c.GetHashCode();
            if (_cache.TryGetValue(key, out var m)) return m;
            m = new Material(SimpleUnlit);
            m.name = "Unlit_" + c;
            m.SetColor("_BaseColor", c); m.SetColor("_Color", c);
            m.enableInstancing = true;
            _cache[key] = m;
            return m;
        }

        /// <summary>把任意材质（含 PxC/VertexColor）设为不透明渲染（WorldGenerator 顶点色地形用）。</summary>
        public static void SetSurfaceOpaque(Material m)
        {
            if (m == null) return;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0);
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 0);
            SetRenderMode(m, false);
        }

        static void SetRenderMode(Material m, bool transparent)
        {
            // URP Lit：Surface=0 不透明 / 1 透明；ZWrite 透明时关
            if (transparent)
            {
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
                m.SetInt("_ZWrite", 0);
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3);
            }
            else
            {
                m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = 2000;
                m.SetInt("_ZWrite", 1);
            }
        }

        static int StableSeed(Color c)
        {
            return Mathf.RoundToInt(c.r * 31 + c.g * 57 + c.b * 91) & 0x7fffffff;
        }
    }
}
