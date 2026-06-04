using UnityEngine;
using EvoLib.Utility.Core;
using Custom;

public class CustomInstanceManager : SingletonMonoBehaviour<CustomInstanceManager>
{
    /// <summary>
    /// カスタム一方通行設定
    /// </summary>
    [Header("カスタム一方通行設定"), SerializeField]
    private CustomOneWayLevelManager _customOneWay;

    /// <summary>
    /// カスタムレベル
    /// </summary>
    [Header("カスタムレベル"), SerializeField]
    private CustomLevelManager _customLevel;

    /// <summary>
    /// カスタム一方通行設定を返す
    /// </summary>
    /// <returns></returns>
    public CustomOneWayLevelManager GetCustomOneWayLevelManager() => _customOneWay;
    /// <summary>
    /// カスタムレベルを返す
    /// </summary>
    /// <returns></returns>
    public CustomLevelManager GetCustomLevelManager() => _customLevel;
}
