using UnityEditor;
using UnityEngine;
using System.IO;

namespace EvoLib.EditorTools
{
    /// <summary>
    /// Unityの機能にある、TagとLayerをstringとして扱う為のクラスを生成するEditor拡張<br/>
    /// TagやLayerを新たに追加したり、削除したりするたびにTagとLayerをstringとして<br/>
    /// 扱う為のクラスを生成(上書き)する
    /// </summary>
    public class TagLayerPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// タグマネージャーのファイルパス
        /// </summary>
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";
        /// <summary>
        /// 自動生成されるスクリプトの生成パス
        /// </summary>
        private const string ScriptGenerationPath = "Assets/2DAction/Scripts/EvoLib/Utility/Generated/";
        /// <summary>
        /// タグ名をまとめたクラス名
        /// </summary>
        private const string TagNameCrassName = "TagName";
        /// <summary>
        /// レイヤー名をまとめたクラス名
        /// </summary>
        private const string LayerNameCrassName = "LayerName";
        /// <summary>
        /// 生成したクラスのnamespace
        /// </summary>
        private const string GeneratedNameSpace = "EvoLib.Utility.Generated";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                if (asset == TagManagerPath)
                {
                    GenerateTagClass();
                    GenerateLayerClass();
                    break;
                }
            }
        }

        /// <summary>
        /// タグクラス生成
        /// </summary>
        private static void GenerateTagClass()
        {
            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            string outputPath = $"{ScriptGenerationPath}{TagNameCrassName}.cs";

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine($"namespace {GeneratedNameSpace}");
                writer.WriteLine("{");
                writer.WriteLine("\tpublic static class TagName");
                writer.WriteLine("\t{");

                foreach (var tag in tags)
                {
                    var safe = tag.Replace(" ", "_");
                    writer.WriteLine($"\t\tpublic const string {safe} = \"{tag}\";");
                }

                writer.WriteLine("\t}");
                writer.WriteLine("}");
            }

            AssetDatabase.Refresh();
            Debug.Log($"{TagNameCrassName}.cs を自動生成しました");
        }

        /// <summary>
        /// レイヤークラス生成
        /// </summary>
        private static void GenerateLayerClass()
        {
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            string outputPath = $"{ScriptGenerationPath}{LayerNameCrassName}.cs";

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine($"namespace {GeneratedNameSpace}");
                writer.WriteLine("{");
                writer.WriteLine("\tpublic static class LayerName");
                writer.WriteLine("\t{");

                foreach (var layer in layers)
                {
                    if (string.IsNullOrEmpty(layer)) continue;

                    var safe = layer.Replace(" ", "_");
                    writer.WriteLine($"\t\tpublic const string {safe} = \"{layer}\";");
                }

                writer.WriteLine("\t}");
                writer.WriteLine("}");
            }

            AssetDatabase.Refresh();
            Debug.Log($"{LayerNameCrassName}.cs を自動生成しました");
        }
    }
}