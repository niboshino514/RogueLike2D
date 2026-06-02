using Codice.Client.Common.GameUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utility;

public class CustomTilemapCreator
{
    /// <summary>
    /// タイルマップ名
    /// </summary>
    private const string TILE_MAP_NAME = "Tilemap";

    [MenuItem("GameObject/タイルマップ作成", false, 0)]
    static void CreateCustomTilemap()
    {
        

        GameObject grid = null;


        // 選択中のオブジェクトが Grid ならそれを使う
        if (Selection.activeGameObject != null &&
            Selection.activeGameObject.GetComponent<Grid>() != null)
        {
            grid = Selection.activeGameObject;
        }
        else
        {
#if UNITY_EDITOR
            // エラーメッセージ表示
            MsgDialogBox.MesBoxInfo mesBoxInfo = new();
            mesBoxInfo.titleText = "タイルマップ生成エラー";
            mesBoxInfo.msgText =
                $"タイルマップを生成する際は、\n" +
                $"[Hierarchy(ヒエラルキー)]にある[Grid]オブジェクトをクリックし、\n" +
                $"選択状態にしてから行ってください。\n\n" +
                $"※[Grid]オブジェクトは[Level]オブジェクトの下の階層にあります。";
            MsgDialogBox.Open(mesBoxInfo);

#endif
            return;
        }

        // 重複しない名前を生成
        string uniqueName = GameObjectUtility.GetUniqueNameForSibling(
            grid.transform, TILE_MAP_NAME);

        // Tilemap 作成
        GameObject tilemapObj = new GameObject(uniqueName);
        tilemapObj.transform.SetParent(grid.transform, false);

        // 必須コンポーネント
        tilemapObj.AddComponent<Tilemap>();
        tilemapObj.AddComponent<TilemapRenderer>();
        tilemapObj.AddComponent<TilemapCollider2D>();
        tilemapObj.AddComponent<StageScroller>();
        
        // 作成した Tilemap を選択状態にする
        Selection.activeGameObject = tilemapObj;
    }
}
