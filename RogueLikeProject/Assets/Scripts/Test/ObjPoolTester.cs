using ObjectPool;
using UnityEngine;
using Manager;

namespace Test
{
    /// <summary>
    /// オブジェクトプールのテスト用コード
    /// </summary>
    public class ObjPoolTester : MonoBehaviour
    {
        /// <summary>
        /// プールを行うオブジェクト
        /// </summary>
        [Header("プールを行うオブジェクト")]
        [SerializeField]
        private GameObject _poolObj;

        /// <summary>
        /// 生成時に向かせるターゲット
        /// </summary>
        [Header("生成時に向かせるターゲット")]
        [SerializeField]
        private Transform _target;

        /// <summary>
        /// オブジェクトプール
        /// </summary>
        private ObjectPoolManager _objectPoolManager;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // インスタンス作成
            _objectPoolManager = ObjectPoolManager.Instance;

            // オブジェクトプールに登録
            _objectPoolManager.RegisterPool(_poolObj);
        }

        // Update is called once per frame
        void Update()
        {
            if (InputManager.Instance.IsTrig(InputManager.BtnType.Up))
            {
                // オブジェクトプールから、オブジェクトを取得
                var obj = _objectPoolManager.Get(_poolObj, this.transform.position);
            }
        }
    }
}