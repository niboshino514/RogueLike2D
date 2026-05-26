using UnityEditor.U2D.Aseprite;
using UnityEngine;
using Utility;
using Utility.Core;

namespace Manager
{
    public class StageManager : SingletonMonoBehaviour<StageManager>
    {
        
        /// <summary>
        /// タイルマップオブジェクト
        /// </summary>
        [Header("タイルマップオブジェクト")]
        private GameObject[] _TileMapObjArray;

        /// <summary>
        /// 現在のステージ番号
        /// </summary>
        public int CurrentStageNumber { get; private set; }

        private void Awake()
        {
            // 特定のコンポーネントがあるか確認
            //CheckPrefabIdentifier();

            // ステージ番号代入
            CurrentStageNumber = 0;

            foreach (var tilemap in _TileMapObjArray)
            {
                // ステージを非表示
                tilemap.SetActive(false);
            }

            // ステージを表示
            _TileMapObjArray[CurrentStageNumber].SetActive(true);
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        }

        public void NextStage(Transform playerTransform)
        {
            // 現在のステージを非表示にする
            _TileMapObjArray[CurrentStageNumber].SetActive(false);
            // ステージ番号を増やす
            CurrentStageNumber++;
            // 次のステージを表示する
            _TileMapObjArray[CurrentStageNumber].SetActive(true);

            //Transform levelStartTransform = FindActiveChildWithTag(_TileMapObjArray[CurrentStageNumber],);
        }

        public GameObject FindActiveChildWithTag(Transform parent, string tag)
        {
            // 子オブジェクトをすべて取得（非アクティブも含む）
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                // 親自身は除外
                if (child == parent) continue;

                // タグ一致 & アクティブ状態
                if (child.CompareTag(tag) && child.gameObject.activeInHierarchy)
                {
                    return child.gameObject;
                }
            }

            return null; // 見つからなかった
        }


        /// <summary>
        /// _TileMapObjArrayの要素全てにPrefabIdentifierが入っているかを確認する
        /// </summary>
        private void CheckPrefabIdentifier()
        {
#if UNITY_EDITOR
            foreach (var tilemap in _TileMapObjArray)
            {
                if (tilemap.GetComponent<PrefabIdentifier>() == null)
                {
                    // エラーメッセージ表示
                    MsgDialogBox.MesBoxInfo mesBoxInfo = new();
                    mesBoxInfo.titleText = "コンポーネントエラー";
                    mesBoxInfo.msgText = $"[{tilemap.name}] に、[PrefabIdentifier]コンポーネントが入っていないようです。";
                    MsgDialogBox.Open(mesBoxInfo);

                    // ゲームを落とす
                    UnityEditor.EditorApplication.isPlaying = false;
                }
            }
#endif
        }
    }
}