using UnityEditor;
using UnityEngine;
using System.IO;


public class TagLayerPostprocessor : AssetPostprocessor
{
    private const string TagManagerPath = "ProjectSettings/TagManager.asset";

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

    private static void GenerateTagClass()
    {
        var tags = UnityEditorInternal.InternalEditorUtility.tags;
        string outputPath = "Assets/Scripts/Generated/TagName.cs";

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        using (var writer = new StreamWriter(outputPath))
        {
            writer.WriteLine("public static class TagName");
            writer.WriteLine("{");

            foreach (var tag in tags)
            {
                var safe = tag.Replace(" ", "_");
                writer.WriteLine($"    public const string {safe} = \"{tag}\";");
            }

            writer.WriteLine("}");
        }

        AssetDatabase.Refresh();
        Debug.Log("TagName.cs を自動生成しました");
    }

    private static void GenerateLayerClass()
    {
        var layers = UnityEditorInternal.InternalEditorUtility.layers;
        string outputPath = "Assets/Scripts/Generated/LayerName.cs";

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        using (var writer = new StreamWriter(outputPath))
        {
            writer.WriteLine("public static class LayerName");
            writer.WriteLine("{");

            foreach (var layer in layers)
            {
                if (string.IsNullOrEmpty(layer)) continue;

                var safe = layer.Replace(" ", "_");
                writer.WriteLine($"    public const string {safe} = \"{layer}\";");
            }

            writer.WriteLine("}");
        }

        AssetDatabase.Refresh();
        Debug.Log("LayerName.cs を自動生成しました");
    }
}