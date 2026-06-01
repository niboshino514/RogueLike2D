using UnityEngine;
using Utility.Core;

public class CustomInstanceManager : SingletonMonoBehaviour<CustomInstanceManager>
{
    /// <summary>
    /// カスタム一方通行設定
    /// </summary>
    [Header("カスタム一方通行設定"), SerializeField]
    private CustomOneWayLevelManager _customOneWay;

    /// <summary>
    /// カスタム一方通行設定を返す
    /// </summary>
    /// <returns></returns>
    public CustomOneWayLevelManager GetCustomOneWayLevelManager() => _customOneWay;
}
