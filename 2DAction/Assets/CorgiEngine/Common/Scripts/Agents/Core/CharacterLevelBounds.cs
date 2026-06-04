using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// キャラクターがステージ境界（LevelBounds）に触れたときの挙動を管理するクラス
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/Core/キャラクター境界処理")]
    public class CharacterLevelBounds : CorgiMonoBehaviour
    {
        /// <summary>
        /// 境界に触れたときの動作
        /// </summary>
        public enum BoundsBehavior
        {
            Nothing,    // 何もしない
            Constrain,  // 境界内に押し戻す
            Kill,       // 即死させる
            Loop        // 反対側にワープさせる
        }

        //────────────────────────────────────────────
        [Header("📘 説明")]
        [MMInformation(
            "キャラクターがステージの上下左右の境界に触れたときの挙動を設定します。\n" +
            "境界（LevelBounds）は LevelManager によって定義されます。",
            MoreMountains.Tools.MMInformationAttribute.InformationType.Info, false)]
        //────────────────────────────────────────────


        //────────────────────────────────────────────
        [Header("📏 境界に触れたときの挙動設定")]
        //────────────────────────────────────────────

        [Tooltip("キャラクターがステージ上端に到達したときの挙動")]
        public BoundsBehavior Top = BoundsBehavior.Constrain;

        [Tooltip("キャラクターがステージ下端に到達したときの挙動")]
        public BoundsBehavior Bottom = BoundsBehavior.Kill;

        [Tooltip("キャラクターがステージ左端に到達したときの挙動")]
        public BoundsBehavior Left = BoundsBehavior.Constrain;

        [Tooltip("キャラクターがステージ右端に到達したときの挙動")]
        public BoundsBehavior Right = BoundsBehavior.Constrain;

        [Tooltip("Constrain（押し戻し）時に、該当軸の移動力をリセットするかどうか")]
        public bool ResetForcesOnConstrain = true;


        //────────────────────────────────────────────
        [Header("🔁 ループ（Loop）時のワープ位置オフセット")]
        //────────────────────────────────────────────

        [Tooltip("上下方向のループ時に適用されるオフセット量")]
        public float LoopHorizontalOffset = 1f;

        [Tooltip("左右方向のループ時に適用されるオフセット量")]
        public float LoopVerticalOffset = 1f;


        //────────────────────────────────────────────
        // 内部変数（Inspector 非表示）
        //────────────────────────────────────────────
        protected Bounds _bounds;
        protected CorgiController _controller;
        protected Character _character;
        protected Vector2 _constrainedPosition;
        protected OneWayLevelManager _oneWayLevelManager;
        protected Vector2 _loopPosition;

        /// <summary>
        /// 初期化
        /// </summary>
        public virtual void Start()
        {
            _character = this.gameObject.GetComponentInParent<Character>();
            _controller = this.gameObject.GetComponentInParent<CorgiController>();

            if (LevelManager.HasInstance)
            {
                _bounds = LevelManager.Instance.LevelBounds;
                _oneWayLevelManager = LevelManager.Instance.gameObject.GetComponent<OneWayLevelManager>();
            }
        }

        /// <summary>
        /// 毎フレーム、境界に触れているかチェック
        /// </summary>
        public virtual void LateUpdate()
        {
            _controller.State.TouchingLevelBounds = false;

            // 死亡中は処理しない
            if ((_character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead)
                || (!LevelManager.HasInstance))
            {
                return;
            }

            Physics2D.SyncTransforms();
            _bounds = LevelManager.Instance.LevelBounds;

            if (_bounds.size != Vector3.zero)
            {
                // 上端
                if ((Top != BoundsBehavior.Nothing) && (_controller.ColliderTopPosition.y > _bounds.max.y))
                {
                    _constrainedPosition.x = transform.position.x;
                    _constrainedPosition.y = _bounds.max.y - _controller.ColliderSize.y / 2 - _controller.ColliderOffset.y;
                    _loopPosition.x = transform.position.x;
                    _loopPosition.y = _bounds.min.y + LoopVerticalOffset;
                    if (ResetForcesOnConstrain) { _controller.SetVerticalForce(0f); }
                    ApplyBoundsBehavior(Top, _constrainedPosition, _loopPosition);
                }

                // 下端
                if ((Bottom != BoundsBehavior.Nothing) && (_controller.ColliderBottomPosition.y < _bounds.min.y))
                {
                    _constrainedPosition.x = transform.position.x;
                    _constrainedPosition.y = _bounds.min.y + _controller.ColliderSize.y / 2 - _controller.ColliderOffset.y;
                    _loopPosition.x = transform.position.x;
                    _loopPosition.y = _bounds.max.y - LoopVerticalOffset;
                    if (ResetForcesOnConstrain) { _controller.SetVerticalForce(0f); }
                    ApplyBoundsBehavior(Bottom, _constrainedPosition, _loopPosition);
                }

                // 右端
                if ((Right != BoundsBehavior.Nothing) && (_controller.ColliderRightPosition.x > _bounds.max.x))
                {
                    _constrainedPosition.x = _bounds.max.x - _controller.ColliderSize.x / 2 - _controller.ColliderOffset.x;
                    _constrainedPosition.y = transform.position.y;
                    _loopPosition.x = _bounds.min.x + LoopHorizontalOffset;
                    _loopPosition.y = transform.position.y;
                    if (ResetForcesOnConstrain) { _controller.SetHorizontalForce(0f); }
                    ApplyBoundsBehavior(Right, _constrainedPosition, _loopPosition);
                }

                // 左端
                if ((Left != BoundsBehavior.Nothing) && (_controller.ColliderLeftPosition.x < _bounds.min.x))
                {
                    _constrainedPosition.x = _bounds.min.x + _controller.ColliderSize.x / 2 + _controller.ColliderOffset.x;
                    _constrainedPosition.y = transform.position.y;
                    _loopPosition.x = _bounds.max.x - LoopHorizontalOffset;
                    _loopPosition.y = transform.position.y;
                    if (ResetForcesOnConstrain) { _controller.SetHorizontalForce(0f); }
                    ApplyBoundsBehavior(Left, _constrainedPosition, _loopPosition);
                }
            }

            // 強制スクロール中の圧死判定
            if ((_oneWayLevelManager != null) && _oneWayLevelManager.OneWayLevelAutoScrolling)
            {
                bool colliding = false;
                switch (_oneWayLevelManager.OneWayLevelDirection)
                {
                case OneWayLevelManager.OneWayLevelDirections.Right:
                    colliding = _controller.State.IsCollidingRight;
                    break;
                case OneWayLevelManager.OneWayLevelDirections.Left:
                    colliding = _controller.State.IsCollidingLeft;
                    break;
                case OneWayLevelManager.OneWayLevelDirections.Up:
                    colliding = _controller.State.IsCollidingAbove;
                    break;
                case OneWayLevelManager.OneWayLevelDirections.Down:
                    colliding = _controller.State.IsCollidingBelow;
                    break;
                }
                if (colliding && _controller.State.TouchingLevelBounds)
                {
                    _character.CharacterHealth.Kill();
                    _oneWayLevelManager.SetOneWayLevelAutoScrolling(false);
                }
            }
        }

        /// <summary>
        /// 境界に触れたときの挙動を適用
        /// </summary>
        protected virtual void ApplyBoundsBehavior(BoundsBehavior behavior, Vector2 constrainedPosition, Vector2 loopPosition)
        {
            _controller.State.TouchingLevelBounds = true;

            if ((_character == null) || (!LevelManager.HasInstance))
            {
                return;
            }

            switch (behavior)
            {
            case BoundsBehavior.Kill:
                if (_character.CharacterType == Character.CharacterTypes.Player)
                {
                    _character.CharacterHealth.Kill();
                }
                else
                {
                    Health health = _character.gameObject.MMGetComponentNoAlloc<Health>();
                    if (health != null)
                    {
                        health.Kill();
                    }
                }
                break;

            case BoundsBehavior.Constrain:
                transform.position = constrainedPosition;
                Physics2D.SyncTransforms();
                break;

            case BoundsBehavior.Loop:
                transform.position = loopPosition;
                Physics2D.SyncTransforms();
                break;
            }
        }
    }
}
