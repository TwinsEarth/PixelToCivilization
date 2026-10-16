using UnityEngine;
using UnityEngine.UI;

namespace PxC.UI
{
    /// <summary>
    /// uGUI 元素工厂（v10.8.0）：纯代码创建 Canvas/EventSystem/面板/Text/Button/Image，
    /// 无任何 Prefab 依赖，确保空场景 + Boot 代码启动也能完整构建 UI。
    /// </summary>
    public static class UiFactory
    {
        private static Font _chineseFont;

        /// <summary>加载仓库内中文字体（Assets/Resources/Fonts/SimHei.ttf，WebGL 中文显示必需）。</summary>
        public static Font ChineseFont
        {
            get
            {
                if (_chineseFont == null)
                {
                    _chineseFont = Resources.Load<Font>("Fonts/SimHei");
                    if (_chineseFont == null) _chineseFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
                }
                return _chineseFont;
            }
        }

        // ---------------- 根结构 ----------------

        /// <summary>创建全屏 Canvas（ScreenSpaceOverlay，按 1920×1080 缩放）。</summary>
        public static Canvas CreateCanvas(string name, Component parent)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent.transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>确保 EventSystem 存在（uGUI 按钮交互必需）。</summary>
        public static void EnsureEventSystem(Component root)
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("PxC_EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            es.transform.SetParent(root.transform, false);
        }

        // ---------------- 元素 ----------------

        /// <summary>创建面板（半透明底 + 细边框）。</summary>
        public static RectTransform CreatePanel(string name, Component parent, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            var rt = (RectTransform)go.transform;
            return rt;
        }

        /// <summary>创建文本。</summary>
        public static Text CreateText(string name, Component parent, string content, int size,
            Color color, TextAnchor align = TextAnchor.UpperLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent.transform, false);
            var t = go.GetComponent<Text>();
            t.text = content;
            t.font = ChineseFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        /// <summary>创建按钮（文本 + 底色，可设点击回调）。</summary>
        public static Button CreateButton(string name, Component parent, string label, int fontSize,
            Color bg, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var t = labelGo.GetComponent<Text>();
            t.text = label;
            t.font = ChineseFont;
            t.fontSize = fontSize;
            t.color = UiPalette.TextMain;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            return btn;
        }

        /// <summary>创建色块（地图/图例用）。</summary>
        public static Image CreateBlock(string name, Component parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>锚定一个 RectTransform（anchorMin/Max + 四边偏移）。</summary>
        public static void Anchor(RectTransform rt,
            Vector2 anchorMin, Vector2 anchorMax,
            float left, float top, float right, float bottom)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(left, -top);
            rt.offsetMax = new Vector2(-right, bottom);
            // offsetMin.y 负 = 上边距；offsetMax.y 正 = 下边距（自上而下布局）
        }

        /// <summary>行式布局（用于面板内竖排元素）。</summary>
        public static VerticalLayoutGroup CreateVLayout(string name, Component parent,
            float spacing, RectOffset padding)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent.transform, false);
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset(12, 12, 8, 8);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            return v;
        }
    }
}
