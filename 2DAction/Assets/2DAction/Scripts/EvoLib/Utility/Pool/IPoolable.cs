using UnityEngine;

namespace EvoLib.Utility.Pool
{
    /// <summary>
    /// プールされるオブジェクトが実装すべきライフサイクルイベント
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 初期生成時処理
        /// </summary>
        /// <param name="prefab"></param>
        abstract void OnCreate(GameObject prefab);
        /// <summary>
        /// スポーン(再利用されたとき)処理
        /// </summary>
        abstract void OnSpawn();
        /// <summary>
        /// Poolに返却する際の処理
        /// </summary>
        abstract void OnDespawn();
    }
}