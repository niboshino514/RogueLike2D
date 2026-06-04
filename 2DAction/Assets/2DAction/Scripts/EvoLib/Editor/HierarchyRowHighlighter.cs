using UnityEditor;
using UnityEngine;

namespace EvoLib.EditorTools
{
    /// <summary>
    /// Hierarchyを一行おきに色を変更するEditor拡張クラス<br/>
    /// [COLOR]変数で色の指定可能<br/>
    /// 参考記事：https://baba-s.hatenablog.com/entry/2015/05/09/122713
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyRowHighlighter
    {
        private const int ROW_HEIGHT = 16;
        private const int OFFSET_Y = -4;

        /// <summary>
        /// Inspectorの色設定
        /// </summary>
        private static readonly Color COLOR = new Color(0.5f, 0.5f, 0.5f, 0.1f);

        static HierarchyRowHighlighter()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnGUI;
        }

        private static void OnGUI(int instanceID, Rect rect)
        {
            var index = (int)(rect.y + OFFSET_Y) / ROW_HEIGHT;

            if (index % 2 == 0) return;

            var xMax = rect.xMax;

            rect.x = 32;
            rect.xMax = xMax + 16;

            EditorGUI.DrawRect(rect, COLOR);
        }
    }
}