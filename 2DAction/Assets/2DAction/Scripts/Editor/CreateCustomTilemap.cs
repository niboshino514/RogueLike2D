using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using EvoLib.Utility;

namespace EditorTools
{
    /// <summary>
    /// Unity エディタ上で、選択中の Grid オブジェクトの子として<br/>
    /// Tilemap を自動生成するエディタ拡張ツール。<br/>
    /// <br/>
    /// メニュー「GameObject/タイルマップ作成」から実行でき、<br/>
    /// Tilemap・TilemapRenderer・TilemapCollider2D・StageScroller を<br/>
    /// 自動で追加した Tilemap オブジェクトを生成する。<br/>
    /// <br/>
    /// Grid が選択されていない場合は、Windows 標準のメッセージボックスを用いて<br/>
    /// エラー内容をユーザーに通知し、誤操作を防止する。<br/>
    /// <br/>
    /// 主な機能：<br/>
    /// ・Grid を選択した状態で Tilemap を自動生成<br/>
    /// ・兄弟オブジェクトと重複しないユニークな名前を自動付与<br/>
    /// ・必要な Tilemap 系コンポーネントを自動追加<br/>
    /// ・生成後に Tilemap を選択状態にすることで編集をスムーズに開始可能<br/>
    /// <br/>
    /// ステージ制作や Tilemap ベースのゲーム開発において、<br/>
    /// Tilemap 作成の手間を軽減するためのエディタ補助ツール。<br/>
    /// </summary>

    static public class CustomTilemapCreator
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
}