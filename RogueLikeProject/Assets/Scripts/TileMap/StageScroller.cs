using Custom;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using Utility.Core;
using static MoreMountains.CorgiEngine.OneWayLevelManager;

public class StageScroller : SingletonMonoBehaviour<StageScroller>
{
    /// <summary>
    /// ステージスクロール情報
    /// </summary>
    [Serializable]
    public class StageScrollInfo
    {
        /// <summary>
        /// ステージの進行方向
        /// </summary>
        [LabelText("ステージの進行方向")]
        [LabelWidth(200)]
        [Tooltip("スクロールを行わないのなら[未設定]\n右スクロール(右に進むステージ)なら [右]に設定\netc...")]
        public OneWayLevelDirections oneWayLevelDirection = OneWayLevelDirections.None;

        /// <summary>
        /// 進行方向と逆方向への移動無効化
        /// </summary>
        [HideIf(nameof(oneWayLevelDirection), OneWayLevelDirections.None)]
        [LabelText("進行方向と逆方向への移動無効化")]
        [LabelWidth(200)]
        [Tooltip("✓を入れると、\nプレイヤーが進行方向と逆方向に戻れなくなります")]
        public bool preventGoingBack = true;

        /// <summary>
        /// 戻り禁止の境界距離
        /// </summary>
        [ShowIf(nameof(IsDrawThresholdDistance))]
        [LabelText("戻り禁止の境界距離")]
        [LabelWidth(200)]
        [Tooltip("戻り禁止の境界がプレイヤーからどれだけ離れるかの距離")]
        [Range(0.5f, 10.0f)]
        public float thresholdDistance = 5f;

        /// <summary>
        /// thresholdDistanceをInspector上で表示するための条件
        /// </summary>
        /// <returns></returns>
        private bool IsDrawThresholdDistance()
        {
            if (oneWayLevelDirection != OneWayLevelDirections.None &&
                preventGoingBack)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// スクロール情報
    /// </summary>
    [LabelText("スクロール設定"),SerializeField]
    private StageScrollInfo _stageScrollInfo;

 

    [Header("オートスクロール設定")]

    /// if this is true, the level bounds will be modified so that the level auto scrolls towards the OneWayLevelDirection
    [Header("true にすると、ステージが自動でスクロールします")]
    public bool OneWayLevelAutoScrolling = false;
    /// the main camera to use to compute the size of the modified constrained bounds
    [MMCondition("OneWayLevelAutoScrolling", true)]
    [Header("オートスクロール時に使用するメインカメラ")]
    public Camera MainCamera;
    /// the speed at which the level should auto scroll
    [MMCondition("OneWayLevelAutoScrolling", true)]
    [Header("オートスクロールの速度")]
    public float OneWayLevelAutoScrollingSpeed = 1f;
    

    public CustomLevelManager _customLevelManager; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
