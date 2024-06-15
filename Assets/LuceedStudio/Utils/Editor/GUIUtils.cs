// GUI Utilties from Luceed Studio - https://luceed.studio

using UnityEditor;
using UnityEngine;

namespace LuceedStudio_Utils
{
    public static class GUIUtils
    {
        public static GUIStyle LabelBold = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        public static GUIStyle LabelCenterBold = new GUIStyle(LabelBold) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        public static GUIStyle LabelRight = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
        public static GUIStyle FoldoutLabel = new GUIStyle(EditorStyles.foldoutHeader) { fontSize = 12 };
        public static GUIStyle HelpBold = new GUIStyle(EditorStyles.helpBox) { fontSize = 12, fontStyle = FontStyle.Bold };
        public static GUIStyle HelpBig = new GUIStyle(EditorStyles.helpBox) { fontSize = 12 };
        public static GUIStyle HelpSmall = new GUIStyle(EditorStyles.helpBox) { fontSize = 9, padding = new RectOffset(4, 4, 1, 1) };

        public static Color RedLight = new Color(1f, 0.6f, 0.6f);
        public static Color GreenLight = new Color(0.6f, 1f, 0.6f);
        public static Color BlueLight = new Color(0.6f, 0.6f, 1f);
        public static Color SubtleBlack = new Color(0f, 0f, 0f, 0.2f);

        public static GUIStyle GetMarginStyle(GUIStyle sourceStyle, Vector4 margin)
        {
            GUIStyle newStyle = new GUIStyle(sourceStyle);
            newStyle.margin = new RectOffset((int)margin.x, (int)margin.y, (int)margin.z, (int)margin.w);

            return newStyle;
        }

        public static GUIStyle GetIndentButtonStyle(int indentLevel)
        {
            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
            buttonStyle.margin = new RectOffset(indentLevel * 17, buttonStyle.margin.right, buttonStyle.margin.top, buttonStyle.margin.bottom);

            return buttonStyle;
        }

        public static void DrawUILine(Color color = default, int thickness = 1, int padding = 20, int margin = -1, int width = 0)
        {
            color = color != default ? color : Color.grey;

            Rect r = new Rect();
            if (width == 0)
            {
                r = EditorGUILayout.GetControlRect(false, GUILayout.Height(padding + thickness));
            }
            else
            {
                r = EditorGUILayout.GetControlRect(false, GUILayout.Height(padding + thickness), GUILayout.Width(width));
            }

            r.height = thickness;
            r.y += padding * 0.5f;

            if (width == 0)
            {
                switch (margin)
                {
                    // expand to maximum width
                    case < 0:
                        r.x = 0;
                        r.width = EditorGUIUtility.currentViewWidth;

                        break;
                    case > 0:
                        // shrink line width
                        r.x += margin;
                        r.width -= margin * 2;

                        break;
                }
            }

            EditorGUI.DrawRect(r, color);
        }
    }
}

