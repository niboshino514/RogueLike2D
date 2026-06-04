using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using EvoLib.Utility.Core;

namespace EvoLib.Utility.Pool
{
    /// <summary>
    /// プールの生成・管理・取得・返却を一括で行うマネージャー
    /// </summary>
    public class ObjectPoolManager : SingletonMonoBehaviour<ObjectPoolManager>
    {
        /// <summary>
        /// Null時のエラーメッセージ
        /// </summary>
        public readonly static string NULL_ERROR_MESSAGE = "Prefabの中身がNullです。";

        // プレハブごとに ObjectPool を管理
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

        /// <summary>
        /// プレハブのプールを登録
        /// </summary>
        public void RegisterPool(GameObject prefab, int defaultCapacity = 10, int maxSize = 50, Transform parent = null)
        {
            // prefabがNullかどうか
            Assert.IsNotNull(prefab, NULL_ERROR_MESSAGE);

            if (_pools.ContainsKey(prefab))
            {
                return;
            }

            GameObject child = new($"{prefab.name}Parnet");

            Transform createTransform = parent == null ? this.transform : parent.transform;
            child.transform.SetParent(createTransform);
            parent = child.transform;

            var pool = new ObjectPool<GameObject>(
                () => CreatePoolObject(prefab, parent),
                OnGetFromPool,
                OnReleaseToPool,
                OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            _pools.Add(prefab, pool);
        }

        /// <summary>
        /// プレハブから新規生成
        /// </summary>
        private static GameObject CreatePoolObject(GameObject prefab, Transform parent)
        {
            var obj = Instantiate(prefab, parent);

            // IPoolable があれば OnSpawn を呼ぶ
            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnCreate(prefab);
            }

            return obj;
        }

        private static void OnGetFromPool(GameObject obj)
        {
            obj.SetActive(true);

            // IPoolable があれば OnSpawn を呼ぶ
            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnSpawn();
            }
        }

        private static void OnReleaseToPool(GameObject obj)
        {
            // IPoolable があれば OnDespawn を呼ぶ
            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnDespawn();
            }

            obj.SetActive(false);
        }

        private static void OnDestroyPoolObject(GameObject obj)
        {
#if UNITY_EDITOR
            // エディタモード or PlayMode終了中
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
#endif
            Destroy(obj);
        }

        /// <summary>
        /// プールから取得
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position)
        {
            // prefabがNullかどうか
            Assert.IsNotNull(prefab, NULL_ERROR_MESSAGE);

            if (!_pools.ContainsKey(prefab))
            {
                Debug.LogWarning($"Prefab {prefab.name} のプールが未登録のため自動登録します。");
                RegisterPool(prefab);
            }

            GameObject obj = _pools[prefab].Get();
            obj.transform.position = position;
            return obj;
        }

        /// <summary>
        /// プールに返却
        /// </summary>
        public void Release(GameObject prefab, GameObject obj)
        {
            if (!_pools.ContainsKey(prefab))
            {
                Debug.LogError($"Prefab {prefab.name} のプールが存在しません。Release できません。");
                Destroy(obj);
                return;
            }
            _pools[prefab].Release(obj);
        }
    }
}