using Custom;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using Utility.Core;
using static MoreMountains.CorgiEngine.OneWayLevelManager;

[AddComponentMenu("ステージのスクロール設定")]
public class StageScroller : SingletonMonoBehaviour<StageScroller>
{
    /// <summary>
    /// スクロールタイプ
    /// </summary>
    public enum ScrollType
    {
        [InspectorName("一方通行")]
        OneWay,
        [InspectorName("自動")]
        Auto
    }

    /// <summary>
    /// ステージの進行方向
    /// </summary>
    [LabelText("ステージの進行方向"),SerializeField]
    [LabelWidth(200)]
    [Tooltip("強制スクロールを行わないのなら[未設定]\n右スクロール(右に進むステージ)なら[右]\netc...")]
    private OneWayLevelDirections _oneWayLevelDirection = OneWayLevelDirections.None;

    /// <summary>
    /// スクロールタイプ
    /// </summary>
    [HideIf(nameof(_oneWayLevelDirection), OneWayLevelDirections.None)]
    [LabelText("スクロールを行う際のタイプ"), SerializeField]
    [LabelWidth(200), Indent(1)]
    [Tooltip("[一方通行]：進行方向と反対に戻れなくなる(初代マリオ等)\n[自動]：進行方向に向かって自動スクロール")]
    private ScrollType _scrollType;

    /// <summary>
    /// 自動スクロールの速度
    /// </summary>
    [ShowIf(nameof(IsDrawAutoScrollSpeed))]
    [LabelText("自動スクロールの速度")]
    [LabelWidth(200), Indent(1)]
    [MinValue(0)]
    [Tooltip("速度の最小値は0")]
    public float _autoScrollSpeed = 1f;

    /// <summary>
    /// 戻り禁止の境界距離
    /// </summary>
    [ShowIf(nameof(IsDrawThresholdDistance))]
    [LabelText("戻り禁止の境界距離")]
    [LabelWidth(200), Indent(1)]
    [Range(0.5f, 10.0f)]
    [Tooltip("戻り禁止の境界がプレイヤーからどれだけ離れるかの距離")]
    public float _thresholdDistance = 5f;

    /// <summary>
    /// _autoScrollSpeedをInspector上で表示するための条件
    /// </summary>
    /// <returns></returns>
    private bool IsDrawAutoScrollSpeed()
    {
        return _oneWayLevelDirection != OneWayLevelDirections.None && _scrollType == ScrollType.Auto;
    }

    /// <summary>
    /// _thresholdDistanceをInspector上で表示するための条件
    /// </summary>
    /// <returns></returns>
    private bool IsDrawThresholdDistance()
    {
        return _oneWayLevelDirection != OneWayLevelDirections.None && _scrollType == ScrollType.OneWay;
    }

    public CustomLevelManager _customLevelManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        Vector3 test = _customLevelManager.LevelBounds.center;
        test.x += (_autoScrollSpeed * Time.deltaTime);
        Bounds bounds = _customLevelManager.LevelBounds;
        bounds.center = test;

        _customLevelManager.LevelBounds = bounds;
    }
}
