using Manager;
using Microsoft.Unity.VisualStudio.Editor;
using MoreMountains.CorgiEngine;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;
using static MoreMountains.CorgiEngine.OneWayLevelManager;
namespace Custom
{
    public class CustomLevelManager : LevelManager
    {
        [Header("CinemachineのCinemachineConfiner2D"), SerializeField]
        private CinemachineConfiner2D _confiner;

        /// <summary>
        /// 一方通行設定
        /// </summary>
        private CustomOneWayLevelManager _customOneWay;
        /// <summary>
        /// PlayerのSpriteRenderer
        /// </summary>
        private SpriteRenderer _playerSpriteRenderer;
        /// <summary>
        /// BoxCollider2D
        /// </summary>
        private BoxCollider2D _boxCollider;

        /// <summary>
        /// ステージの境界
        /// </summary>
        private Bounds _stageBounds;

        public override void Start()
        {
            // 既存処理
            base.Start();

            // コンポーネント取得
            _playerSpriteRenderer = Players[0].GetComponentInChildren<SpriteRenderer>(true);
            _boxCollider = this.GetComponent<BoxCollider2D>();
            _customOneWay = CustomInstanceManager.Instance.GetCustomOneWayLevelManager();
        }

        /// <summary>
        /// Playerが死亡した瞬間の処理
        /// </summary>
        /// <param name="player"></param>
        public override void PlayerDead(Character player)
        {
            // キャラクターを非表示にする
            IsDrawCharacter(false);
            // プレイヤーを死亡判定にする
            _customOneWay.IsPlayerDead = true;
        }

        /// <summary>
        /// キャラクター復活
        /// </summary>
        public override void RespawnCharacter()
        {
            if (CurrentCheckPoint != null)
            {
                CurrentCheckPoint.SpawnPlayer(Players[0]);
            }
            // プレイヤーを生きている判定にする
            _customOneWay.IsPlayerDead = false;

            SetupScroll();
        }

        /// <summary>
        /// キャラクター画像を表示するかどうか
        /// </summary>
        /// <param name="isDraw"></param>
        public override void IsDrawCharacter(bool isDraw)
        {
            // Spriterendererが無ければ取得する
            if (_playerSpriteRenderer == null)
            {
                _playerSpriteRenderer = Players[0].GetComponentInChildren<SpriteRenderer>(true);
            }

            // オブジェクトをisDrawフラグに応じて、表示非表示を行う
            _playerSpriteRenderer.gameObject.SetActive(isDraw);
        }

        /// <summary>
        /// 境界線をセットする
        /// </summary>
        /// <param name="bounds"></param>
        public void SetStageBounds(Bounds bounds)
        {
            // ステージの境界を保存
            _stageBounds = bounds;

            // 境界線セット
            SetNewLevelBounds(bounds);

            // 境界サイズ計算
            _boxCollider.size = bounds.size;
            _boxCollider.offset = bounds.center;

            // 範囲変更後にカメラの現在位置を再計算させる
            _confiner.InvalidateBoundingShapeCache();
        }

        public void SetupScroll()
        {
            // スクロールの方向
            OneWayLevelDirections direction = _customOneWay.OneWayLevelDirection;

            // スクロールの方向がNoneの場合、ここで処理を終了する
            if (direction == OneWayLevelDirections.None)
            {
                return;
            }

            Vector3 center = LevelBounds.center;
            if (direction == OneWayLevelDirections.Left ||
                direction == OneWayLevelDirections.Right)
            {
                center.x = Players[0].transform.position.x;
                float cameraHalfSizeX = GetCameraWorldSize().x * 0.5f;

                if (direction == OneWayLevelDirections.Right)
                {
                    center.x += (_stageBounds.extents.x - cameraHalfSizeX);

                    if (center.x >= _stageBounds.center.x)
                    {
                        LevelBounds.center = center;
                    }
                    else
                    {
                        LevelBounds.center = _stageBounds.center;
                    }
                }
                else
                {
                    center.x -= (_stageBounds.extents.x - cameraHalfSizeX);

                    if (center.x <= _stageBounds.center.x)
                    {
                        LevelBounds.center = center;
                    }
                    else
                    {
                        LevelBounds.center = _stageBounds.center;
                    }
                }
            }



           

            // カメラのワールドサイズ計算
            Vector2 GetCameraWorldSize()
            {
                Camera cam = Camera.main;

                float height = cam.orthographicSize * 2f;
                float width = height * cam.aspect;

                return new Vector2(width, height);
            }
        }
    }
}