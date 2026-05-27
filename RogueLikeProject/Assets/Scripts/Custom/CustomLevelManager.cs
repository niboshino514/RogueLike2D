using Microsoft.Unity.VisualStudio.Editor;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace Custom
{
    public class CustomLevelManager : LevelManager
    {
        /// <summary>
        /// PlayerのSpriteRenderer
        /// </summary>
        private SpriteRenderer _playerSpriteRenderer;

        /// <summary>
        /// BoxCollider2D
        /// </summary>
        private BoxCollider2D _boxCollider;

        public override void Start()
        {
            // 既存処理
            base.Start();

            // コンポーネント取得
            _playerSpriteRenderer = Players[0].GetComponentInChildren<SpriteRenderer>(true);
            _boxCollider = this.GetComponent<BoxCollider2D>();
        }


        /// <summary>
        /// Playerが死亡した瞬間の処理
        /// </summary>
        /// <param name="player"></param>
        public override void PlayerDead(Character player)
        {
            // キャラクターを非表示にする
            IsDrawCharacter(false);
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
            // 境界サイズ計算
            _boxCollider.size.Set(bounds.size.x, bounds.size.y);

            // 境界線セット
            SetNewLevelBounds(bounds);
        }
    }
}