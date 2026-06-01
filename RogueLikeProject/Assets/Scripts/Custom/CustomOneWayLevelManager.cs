using Custom;
using MoreMountains.CorgiEngine;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

public class CustomOneWayLevelManager : OneWayLevelManager
{
    /// <summary>
    /// 自動スクロールフラグ
    /// </summary>
    private bool _isAutoScroll;

    /// <summary>
    /// 自動スクロール速度
    /// </summary>
    private float _autoScrollSpeed;

    /// <summary>
    /// レベルマネージャー
    /// </summary>
    private LevelManager _levelManager;

    /// <summary>
    /// プレイヤーが死亡しているかどうか
    /// </summary>
    public bool IsPlayerDead { get; set; }

    protected override void Awake()
    {
        // 基底処理
        base.Awake();
        // インスタンス取得
        _levelManager = LevelManager.Instance;
        // フラグ初期化
        IsPlayerDead = false;
    }

    public void ScrollConfig(StageScroller stageScroller)
    {
        // 移動方向代入
        OneWayLevelDirection = stageScroller._oneWayLevelDirection;

        // スクロール機能を停止する
        if (stageScroller._oneWayLevelDirection == OneWayLevelDirections.None)
        {
            // プレイヤーが進行方向と逆に進むことができる
            PreventGoingBack = false;
            // 自動スクロールを行わない
            OneWayLevelAutoScrolling = false;
            _isAutoScroll = false;

            return;
        }

        // 一方通行設定
        if (stageScroller._scrollType == StageScroller.ScrollType.OneWay)
        {
            // プレイヤーが進行方向と逆に進むことが出来ない
            PreventGoingBack = true;
            // 戻り禁止の境界距離代入
            ThresholdDistance = stageScroller._thresholdDistance;
            // 自動スクロールを行わない
            OneWayLevelAutoScrolling = false;
            _isAutoScroll = false;
        }
        else if (stageScroller._scrollType == StageScroller.ScrollType.Auto)
        {
            // プレイヤーが進行方向と逆に進むことができる
            PreventGoingBack = false;
            // 自動スクロールを行う
            _isAutoScroll = true;
            // 自動スクロール速度代入
            _autoScrollSpeed = stageScroller._autoScrollSpeed;
        }
    }

    private void FixedUpdate()
    {
        // 自動スクロールがfalseの場合、ここで処理を終了する
        if (!_isAutoScroll)
        {
            return;
        }

        // プレイヤーが死亡していた場合、ここで処理を終了する
        if (IsPlayerDead)
        {
            return;
        }

        // 境界線
        Bounds bounds = _levelManager.LevelBounds;
        // 境界の中心座標
        Vector3 center = bounds.center;

        if (OneWayLevelDirection == OneWayLevelDirections.Right)
        {
            // 右に移動
            center.x += (_autoScrollSpeed * Time.deltaTime);
        }
        else if (OneWayLevelDirection == OneWayLevelDirections.Left)
        {
            // 左に移動
            center.x -= (_autoScrollSpeed * Time.deltaTime);
        }
        else if (OneWayLevelDirection == OneWayLevelDirections.Up)
        {
            // 上に移動
            center.y += (_autoScrollSpeed * Time.deltaTime);
        }
        else if (OneWayLevelDirection == OneWayLevelDirections.Down)
        {
            // 下に移動
            center.y -= (_autoScrollSpeed * Time.deltaTime);
        }

        // 計算した境界の中心座標代入
        bounds.center = center;
        // 境界線代入
        _levelManager.LevelBounds = bounds;
    }

    //// ★ StageManager が Bounds を設定した後に呼ぶ
    //public void ApplyPreventGoingBack(Vector2 playerPos)
    //{
    //    base.Initialization(); // 念のため初期化

    //    HandlePreventGoingBack_Manual(playerPos);
    //}

    //// ★ OneWayLevelManager の HandlePreventGoingBack を外部から呼べるように再構築
    //private void HandlePreventGoingBack_Manual(Vector2 playerPos)
    //{
    //    MinBounds = LevelManager.Instance.LevelBounds.min;
    //    MaxBounds = LevelManager.Instance.LevelBounds.max;

    //    switch (OneWayLevelDirection)
    //    {
    //    case OneWayLevelDirections.Right:
    //        MinBounds.x = Mathf.Max(playerPos.x - ThresholdDistance, _minBoundsLastFrame.x);
    //        break;

    //    case OneWayLevelDirections.Left:
    //        MaxBounds.x = Mathf.Min(playerPos.x + ThresholdDistance, _maxBoundsLastFrame.x);
    //        break;

    //    case OneWayLevelDirections.Up:
    //        MinBounds.y = Mathf.Max(playerPos.y - ThresholdDistance, _minBoundsLastFrame.y);
    //        break;

    //    case OneWayLevelDirections.Down:
    //        MaxBounds.y = Mathf.Min(playerPos.y + ThresholdDistance, _maxBoundsLastFrame.y);
    //        break;
    //    }

    //    LevelManager.Instance.SetNewMinLevelBounds(MinBounds);
    //    LevelManager.Instance.SetNewMaxLevelBounds(MaxBounds);

    //    _minBoundsLastFrame = LevelManager.Instance.LevelBounds.min;
    //    _maxBoundsLastFrame = LevelManager.Instance.LevelBounds.max;
    //}

    //public void SyncInternalBoundsWithLevelManager()
    //{
    //    Bounds b = LevelManager.Instance.LevelBounds;

    //    MinBounds = b.min;
    //    MaxBounds = b.max;

    //    _minBoundsLastFrame = b.min;
    //    _maxBoundsLastFrame = b.max;
    //}


    //// ★ Reset（元の Bounds に戻す）
    //public void ResetBounds()
    //{
    //    if (!_initialized) return;

    //    LevelManager.Instance.SetNewLevelBounds(_originalBounds);
    //}
}
