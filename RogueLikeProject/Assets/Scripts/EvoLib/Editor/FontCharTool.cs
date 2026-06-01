using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace EvoLib.EditorTools
{
    /// <summary>
    /// フォントを作る際の文字セットを生成する
    /// </summary>
    public class FontCharTool : EditorWindow
    {
        private TextAsset baseFile;
        private TextAsset addFile;
        private TextAsset saveFile; // ← 保存先を指定する TextAsset
        private string result = "";

        [MenuItem("Tools/Font Char Tool")]
        public static void Open()
        {
            GetWindow<FontCharTool>("Font Char Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("フォント文字セット生成ツール", EditorStyles.boldLabel);

            baseFile = (TextAsset)EditorGUILayout.ObjectField("ベースファイル", baseFile, typeof(TextAsset), false);
            addFile = (TextAsset)EditorGUILayout.ObjectField("追加ファイル", addFile, typeof(TextAsset), false);
            saveFile = (TextAsset)EditorGUILayout.ObjectField("保存先ファイル", saveFile, typeof(TextAsset), false);

            if (GUILayout.Button("生成"))
            {
                // 保存先が未指定ならエラー
                if (saveFile == null)
                {
                    EditorUtility.DisplayDialog("エラー", "保存先の TextAsset が指定されていません。", "OK");
                    return;
                }

                string baseChars = baseFile != null ? baseFile.text : "";
                string addChars = addFile != null ? addFile.text : "";

                result = MergeChars(baseChars, addChars);

                // 自動コピー
                EditorGUIUtility.systemCopyBuffer = result;

                // 保存先の実ファイルパスを取得
                string path = AssetDatabase.GetAssetPath(saveFile);

                if (string.IsNullOrEmpty(path))
                {
                    EditorUtility.DisplayDialog("エラー", "保存先ファイルのパスが取得できません。", "OK");
                    return;
                }

                // 上書き保存
                File.WriteAllText(path, result);
                AssetDatabase.Refresh();

                Debug.Log("生成 & コピー & 保存完了: " + path);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("結果", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(result, GUILayout.Height(80));
        }

        private static string MergeChars(string baseStr, string addStr)
        {
            HashSet<char> set = new HashSet<char>();
            List<char> list = new List<char>();

            foreach (char c in baseStr)
            {
                if (set.Add(c))
                {
                    list.Add(c);
                }
            }

            foreach (char c in addStr)
            {
                if (set.Add(c))
                {
                    list.Add(c);
                }
            }
            return new string(list.ToArray());
        }
    }
}