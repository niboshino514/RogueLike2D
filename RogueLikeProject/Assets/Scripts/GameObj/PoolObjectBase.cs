using UnityEngine;
using UnityEngine.Assertions;
using Utility.Pool;

namespace GameObj
{

    /// <summary>
    /// オブジェクトPoolを扱う際の、ゲームオブジェクトに着けるベースクラス。<br/>
    /// オブジェクトを破棄する際は、<br/>
    /// _objectPoolManager?.Release(_prefab, this.gameObject);
    /// </summary>
    public abstract class PoolObjectBase : MonoBehaviour, IPoolable
    {
        /// <summary>
        /// Null時のエラーメッセージ
        /// </summary>
        protected const string NULL_ERROR_MESSAGE = "変数の中身がNullです。";

        /// <summary>
        /// オブジェクトプールマネージャー
        /// </summary>
        protected ObjectPoolManager _objectPoolManager;

        /// <summary>
        /// 代入用のゲームオブジェクト変数
        /// </summary>
        protected GameObject _prefab;

        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected virtual void Start()
        {
            // コンポーネント取得
            _objectPoolManager = ObjectPoolManager.Instance;

            // Nullチェック
            Assert.IsNotNull(_objectPoolManager, NULL_ERROR_MESSAGE);
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// 生成時処理
        /// </summary>
        /// <param name="prefab"></param>
        public virtual void OnCreate(GameObject prefab)
        {
            _prefab = prefab;
        }

        /// <summary>
        /// スポーン時処理
        /// </summary>
        public virtual void OnSpawn()
        {
            Initialize();
        }

        /// <summary>
        /// デスポーン時処理
        /// </summary>
        public virtual void OnDespawn() { }
    }
}