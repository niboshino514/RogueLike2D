using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Utility;

namespace TileMap
{
    public class TileMapDuplicateChecker : MonoBehaviour
    {
#if UNITY_EDITOR

        private void OnValidate()
        {
            // 再生中、または再生開始直前は実行しない
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // OnValidate 内で直接処理すると Unity が警告を出すため、遅延実行
                EditorApplication.delayCall += CheckDuplicates;
            }
        }

        private void CheckDuplicates()
        {
            // オブジェクト削除直後の null エラー回避
            if (this == null) return;

            Transform parent = transform.parent;
            if (parent == null) return;

            string myName = this.gameObject.name;
            int count = 0;

            // 親の子オブジェクトの中から、自分と同じ名前の数を数える
            foreach (Transform child in parent)
            {
                if (child.name == myName)
                {
                    count++;
                }
            }

            // 自分と同名のオブジェクトが2つ以上ある場合は重複とみなす
            if (count > 1)
            {
                // エラーメッセージ表示
                MsgDialogBox.MesBoxInfo mesBoxInfo = new();
                mesBoxInfo.titleText = "マップチップエラー";
                mesBoxInfo.msgText = $"[{myName}] はステージに一つしか配置できません。";
                MsgDialogBox.Open(mesBoxInfo);

                // 自身を削除
                DestroyImmediate(this.gameObject);
            }
        }
#endif
    }
}
