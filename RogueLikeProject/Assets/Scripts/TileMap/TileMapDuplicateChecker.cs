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
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += CheckDuplicates;
            }
        }

        private void CheckDuplicates()
        {
            if (this == null) return; // オブジェクト削除時のエラー回避

            Transform parent = transform.parent;
            if (parent == null) return;

            Dictionary<string, int> nameCount = new Dictionary<string, int>();

            foreach (Transform child in parent)
            {
                string n = child.name;

                if (!nameCount.ContainsKey(n))
                    nameCount[n] = 0;

                nameCount[n]++;
            }

            foreach (var pair in nameCount)
            {
                if (pair.Value > 1)
                {
                    // エラーメッセージWindow表示
                    MsgDialogBox.MesBoxInfo mesBoxInfo = new();
                    mesBoxInfo.titleText = "マップチップエラー";
                    mesBoxInfo.msgText = $"[{this.gameObject.name}]は、ステージに一つしか配置出来ません。";
                    MsgDialogBox.Open(mesBoxInfo);

                    // オブジェクトを削除
                    DestroyImmediate(this.gameObject);
                }
            }
        }
#endif
    }
}