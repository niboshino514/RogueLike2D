using UnityEngine;
using UnityEngine.Tilemaps;
using EvoLib.Utility;
using EvoLib.Utility.Core;
using EvoLib.Utility.Generated;

namespace Manager
{
    public class StageManager : SingletonMonoBehaviour<StageManager>
    {
        /// <summary>
        /// タイルマップオブジェクト
        /// </summary>
        [Header("タイルマップオブジェクト"),SerializeField]
        private GameObject[] _TileMapObjArray;

        /// <summary>
        /// 一方通行マネージャー
        /// </summary>
        private CustomOneWayLevelManager _oneWay;

        /// <summary>
        /// タイルマップ境界
        /// </summary>
        private TilemapBoundsManager _TileMapBoundsManager;

        /// <summary>
        /// 現在のステージ番号
        /// </summary>
        public int CurrentStageNumber { get; private set; }

        protected override void Awake()
        {
            // 既存の処理実行
            base.Awake();
            // ステージ番号代入
            CurrentStageNumber = 0;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // インスタンス取得
            _TileMapBoundsManager = TilemapBoundsManager.Instance;
            _oneWay = CustomInstanceManager.Instance.GetCustomOneWayLevelManager();

            // 全てのステージを非表示
            foreach (var tilemap in _TileMapObjArray)
            {
                // ステージを非表示
                tilemap.SetActive(false);
            }
            // 指定された番号のステージを表示
            _TileMapObjArray[CurrentStageNumber].SetActive(true);

            // ステージのセットアップ
            SetupStage();
        }

        /// <summary>
        /// 次のステージへ進む
        /// </summary>
        /// <param name="playerTransform"></param>
        public void NextStage(Transform playerTransform)
        {
            // 現在のステージを非表示にする
            _TileMapObjArray[CurrentStageNumber].SetActive(false);
            // ステージ番号を増やす
            CurrentStageNumber++;

            // ステージがあるかどうかを確認
            CheckTileMapObj();
            // 次のステージを表示する
            _TileMapObjArray[CurrentStageNumber].SetActive(true);
            // ステージのセットアップ
            SetupStage();
        }

        /// <summary>
        /// ステージのセットアップ
        /// </summary>
        private void SetupStage()
        {
            // ステージの境界を設定
            SetupStageBounds();
            // 一方通行設定
            SetupOneWayLevel();
            // プレイヤーの開始位置を設定
            SetupPlayerStartPos();
        }

        /// <summary>
        /// ステージの境界を設定
        /// </summary>
        private void SetupStageBounds()
        {
            // Tilemapコンポーネント取得
            Tilemap tilemap = _TileMapObjArray[CurrentStageNumber].GetComponent<Tilemap>();
            // _TileMapBoundsManagerで境界を計算
            _TileMapBoundsManager.CalculateBounds(tilemap);
        }

        /// <summary>
        /// 一方通行設定
        /// </summary>
        private void SetupOneWayLevel()
        {
            // ステージスクローラーコンポーネント取得
            StageScroller stageScroller = _TileMapObjArray[CurrentStageNumber].GetComponent<StageScroller>();
            // ステージ設定
            _oneWay.ScrollConfig(stageScroller);
        }

        /// <summary>
        /// 特定タグのオブジェクトを探す
        /// </summary>
        /// <param name="parent">親オブジェクト</param>
        /// <param name="tag">タグ</param>
        /// <returns></returns>
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


        private void SetupPlayerStartPos()
        {
            // レベルスタートのTransform取得
            Transform levelStartTransform =
                FindActiveChildWithTag(
                    _TileMapObjArray[CurrentStageNumber].transform,
                    TagName.LevelStart).transform;

            // プレイヤーのTransform取得
            Transform playerTransform =
                CustomInstanceManager.Instance.GetCustomLevelManager().Players[0].transform;

            // プレイヤーにレベルスタートの座標を代入
            playerTransform.position = levelStartTransform.position;
        }

        /// <summary>
        /// 指定した番号のタイルマップが存在するかどうかを確認する
        /// </summary>
        private void CheckTileMapObj()
        {
#if UNITY_EDITOR
            if (_TileMapObjArray.Length <= CurrentStageNumber)
            {
                // エラーメッセージ表示
                MsgDialogBox.MesBoxInfo mesBoxInfo = new();
                mesBoxInfo.titleText = "ステージエラー";
                mesBoxInfo.msgText = $"[{CurrentStageNumber}] 番ステージは存在しません。";
                MsgDialogBox.Open(mesBoxInfo);

                // ゲームを落とす
                UnityEditor.EditorApplication.isPlaying = false;
            }
#endif
        }
    }
}