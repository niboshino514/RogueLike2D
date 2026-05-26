using Sirenix.OdinInspector;
using UnityEngine;
using System.Xml.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utility
{
    public class PrefabIdentifier : MonoBehaviour
    {
        /// <summary>
        /// IDの設定
        /// </summary>
        [Header("IDの設定"),SerializeField]
        private string _prefabID;
        /// <summary>
        /// IDを返す
        /// </summary>
        public string PrefabID => _prefabID;

#if UNITY_EDITOR
    [Button("GUID を自動設定")]
    private void SetupAutoID()
        {
            // プレハブの元アセットを取得
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab == null)
            {
                // エラーメッセージ表示
                MsgDialogBox.MesBoxInfo mesBoxInfo = new();
                mesBoxInfo.titleText = "ID設定エラー";
                mesBoxInfo.msgText = $"[{name}] はPrefabになっていないようです。";
                MsgDialogBox.Open(mesBoxInfo);

                return;
            }

            // GUID を取得
            string path = AssetDatabase.GetAssetPath(prefab);
            string guid = AssetDatabase.AssetPathToGUID(path);

            // 保存
            _prefabID = guid;

            // 変更を保存
            EditorUtility.SetDirty(this);
            Debug.Log($"{name} の GUID を設定しました: {guid}");
        }
#endif
    }
}