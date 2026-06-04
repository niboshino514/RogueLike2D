using UnityEditor;
using UnityEngine;

namespace EvoLib.EditorTools
{
    /// <summary>
    /// Projectビューを一行おきに色を変更するEditor拡張クラス<br/>
    /// [COLOR]変数で色の指定可能<br/>
    /// 参考記事：https://baba-s.hatenablog.com/entry/2015/05/16/115549
    /// </summary>
    public static class ProjectRowHighlighter
    {
        /// <summary>
        /// Projectファイルの色設定
        /// </summary>
        private static readonly Color COLOR = new Color(0, 0, 1, 0.3f);

        [InitializeOnLoadMethod]
        private static void Example()
        {
            EditorApplication.projectWindowItemOnGUI += OnGUI;
        }

        private static void OnGUI(string guid, Rect selectionRect)
        {
            var index = (int)(selectionRect.y - 4) / 16;

            if (index % 2 == 0)
            {
                return;
            }

            var pos = selectionRect;
            pos.x = 0;
            pos.xMax = selectionRect.xMax;

            var color = GUI.color;
            GUI.color = COLOR;
            GUI.Box(pos, string.Empty);
            GUI.color = color;
        }
    }
}