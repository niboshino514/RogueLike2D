using UnityEditor;
using UnityEngine;

namespace EvoLib.EditorTools
{
    /// <summary>
    /// ゲームオブジェクトのActive状態をHierarchyで設定出来るEditor拡張クラス<br/>
    /// Hierarchyに映っている✅を切り替える事でActive状態の変更が可能
    /// 参考記事：https://baba-s.hatenablog.com/entry/2015/04/28/121747
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyActiveToggleDrawer
    {
        private const int WIDTH = 25;

        static HierarchyActiveToggleDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnGUI;
        }

        private static void OnGUI(int instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null) return;

            var position = selectionRect;

            position.x = position.xMax - WIDTH;
            position.width = WIDTH;

            var newActive = GUI.Toggle(position, gameObject.activeSelf, string.Empty);

            if (newActive == gameObject.activeSelf) return;

            gameObject.SetActive(newActive);
        }
    }
}