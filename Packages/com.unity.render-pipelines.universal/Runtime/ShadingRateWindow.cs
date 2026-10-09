#if UNITY_EDITOR
using UnityEditor.Callbacks;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    class DebugShadingRateWindow : EditorWindow
    {
        static Styles s_Styles;

        void DrawShadingRateRow(GUIContent label, Color color)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(label, GUILayout.Width(100));
                Rect colorRect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(65));
                EditorGUI.DrawRect(colorRect, color);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        void OnGUI()
        {
            if (s_Styles == null)
            {
                s_Styles = new Styles();
            }

            var wrect = position;
            wrect.x = 0;
            wrect.y = 0;
            var oldColor = GUI.color;
            GUI.color = s_Styles.skinBackgroundColor;
            GUI.DrawTexture(wrect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;
            EditorGUILayout.Space();

            DrawShadingRateRow(s_Styles.size1x1, s_Styles.size1x1Color);
            DrawShadingRateRow(s_Styles.size1x2, s_Styles.size1x2Color);
            DrawShadingRateRow(s_Styles.size2x1, s_Styles.size2x1Color);
            DrawShadingRateRow(s_Styles.size2x2, s_Styles.size2x2Color);
            DrawShadingRateRow(s_Styles.size1x4, s_Styles.size1x4Color);
            DrawShadingRateRow(s_Styles.size4x1, s_Styles.size4x1Color);
            DrawShadingRateRow(s_Styles.size2x4, s_Styles.size2x4Color);
            DrawShadingRateRow(s_Styles.size4x2, s_Styles.size4x2Color);
            DrawShadingRateRow(s_Styles.size4x4, s_Styles.size4x4Color);
        }

        public class Styles
        {
            public GUIContent size1x1 = EditorGUIUtility.TrTextContent("Size 1x1");
            public GUIContent size1x2 = EditorGUIUtility.TrTextContent("Size 1x2");
            public GUIContent size2x1 = EditorGUIUtility.TrTextContent("Size 2x1");
            public GUIContent size2x2 = EditorGUIUtility.TrTextContent("Size 2x2");
            public GUIContent size1x4 = EditorGUIUtility.TrTextContent("Size 1x4");
            public GUIContent size4x1 = EditorGUIUtility.TrTextContent("Size 4x1");
            public GUIContent size2x4 = EditorGUIUtility.TrTextContent("Size 2x4");
            public GUIContent size4x2 = EditorGUIUtility.TrTextContent("Size 4x2");
            public GUIContent size4x4 = EditorGUIUtility.TrTextContent("Size 4x4");

            public readonly Color skinBackgroundColor;
            public Color size1x1Color = new Color32(247, 247, 247, 255);
            public Color size1x2Color = new Color32(176, 224, 230, 255);
            public Color size2x1Color = new Color32(135, 206, 250, 255);
            public Color size2x2Color = new Color32(102, 179, 204, 255);
            public Color size1x4Color = new Color32(228, 178, 186, 255);
            public Color size4x1Color = new Color32(224, 125, 122, 255);
            public Color size2x4Color = new Color32(209, 88, 67, 255);
            public Color size4x2Color = new Color32(160, 64, 61, 255);
            public Color size4x4Color = new Color32(106, 76, 156, 255);
            public Color defaultColor = new Color(0, 0, 0, 230);

            public Styles()
            {
                Color backgroundColorDarkSkin = new Color32(71, 71, 71, 128);
                Color backgroundColorLightSkin = new Color32(38, 38, 38, 128);
                skinBackgroundColor = backgroundColorDarkSkin;
            }
        }
    }

    class ShadingRateWindowManager
    {
        private static DebugShadingRateWindow windowInstance;
        public static GUIContent windowTitle { get; } = EditorGUIUtility.TrTextContent("Shading Rate Map");

        public static void ToggleWindow(bool isEnable)
        {
            if (isEnable && windowInstance == null)
            {
                windowInstance = DebugShadingRateWindow.GetWindow<DebugShadingRateWindow>();
                windowInstance.titleContent = windowTitle;
                windowInstance.minSize = new Vector2(185, 230);
                windowInstance.maxSize = new Vector2(185, 230);
                windowInstance.hideFlags = HideFlags.DontSave;
                windowInstance.Show();
            }
            else if (!isEnable && windowInstance != null)
            {
                windowInstance.Close();
                windowInstance = null;
            }
        }
    }
}
#endif
