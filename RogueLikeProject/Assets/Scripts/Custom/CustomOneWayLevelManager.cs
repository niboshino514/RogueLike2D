using UnityEngine;
using MoreMountains.CorgiEngine;

public class CustomOneWayLevelManager : OneWayLevelManager
{
    
    private Bounds _originalBounds;
    private bool _initialized = false;

    protected override void Initialization()
    {
        base.Initialization();

        if (!_initialized)
        {
            _originalBounds = LevelManager.Instance.LevelBounds;
            _initialized = true;
        }
    }

    // ★ StageManager が Bounds を設定した後に呼ぶ
    public void ApplyPreventGoingBack(Vector2 playerPos)
    {
        base.Initialization(); // 念のため初期化

        HandlePreventGoingBack_Manual(playerPos);
    }

    // ★ OneWayLevelManager の HandlePreventGoingBack を外部から呼べるように再構築
    private void HandlePreventGoingBack_Manual(Vector2 playerPos)
    {
        MinBounds = LevelManager.Instance.LevelBounds.min;
        MaxBounds = LevelManager.Instance.LevelBounds.max;

        switch (OneWayLevelDirection)
        {
        case OneWayLevelDirections.Right:
            MinBounds.x = Mathf.Max(playerPos.x - ThresholdDistance, _minBoundsLastFrame.x);
            break;

        case OneWayLevelDirections.Left:
            MaxBounds.x = Mathf.Min(playerPos.x + ThresholdDistance, _maxBoundsLastFrame.x);
            break;

        case OneWayLevelDirections.Up:
            MinBounds.y = Mathf.Max(playerPos.y - ThresholdDistance, _minBoundsLastFrame.y);
            break;

        case OneWayLevelDirections.Down:
            MaxBounds.y = Mathf.Min(playerPos.y + ThresholdDistance, _maxBoundsLastFrame.y);
            break;
        }

        LevelManager.Instance.SetNewMinLevelBounds(MinBounds);
        LevelManager.Instance.SetNewMaxLevelBounds(MaxBounds);

        _minBoundsLastFrame = LevelManager.Instance.LevelBounds.min;
        _maxBoundsLastFrame = LevelManager.Instance.LevelBounds.max;
    }

    public void SyncInternalBoundsWithLevelManager()
    {
        Bounds b = LevelManager.Instance.LevelBounds;

        MinBounds = b.min;
        MaxBounds = b.max;

        _minBoundsLastFrame = b.min;
        _maxBoundsLastFrame = b.max;
    }


    // ★ Reset（元の Bounds に戻す）
    public void ResetBounds()
    {
        if (!_initialized) return;

        LevelManager.Instance.SetNewLevelBounds(_originalBounds);
    }
}
