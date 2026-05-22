using UnityEditor;
using UnityEngine;

public class CustomErrorWindow : EditorWindow
{
    private string errorMessage = "エラーが発生しました！";

    // メニューバーからウィンドウを開くための設定
    [MenuItem("Tools/カスタムエラーウィンドウ")]
    public static void ShowWindow()
    {
        // ウィンドウの生成（サイズ固定）
        CustomErrorWindow window = GetWindow<CustomErrorWindow>("カスタムエラー");
        window.minSize = new Vector2(350, 150);
        window.maxSize = new Vector2(350, 150);
    }

    private void OnGUI()
    {
        GUILayout.Space(20);

        // アイコンの表示（Info, Warning, Error の3種類から選択）
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(EditorGUIUtility.IconContent("console.erroricon.sml"), GUILayout.Width(40), GUILayout.Height(40));

        // エラーメッセージの表示（折り返し有効）
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.wordWrap = true;
        style.fontSize = 14;
        GUILayout.Label(errorMessage, style);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(30);

        // 閉じるボタン
        if (GUILayout.Button("閉じる", GUILayout.Height(30)))
        {
            this.Close();
        }
    }

    // 外部からエラーメッセージを動的に変更して開くメソッド
    public static void OpenWithError(string message)
    {
        CustomErrorWindow window = GetWindow<CustomErrorWindow>("カスタムエラー");
        window.errorMessage = message;
        window.Show();
    }
}