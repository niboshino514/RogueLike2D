using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;


#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
#endif

namespace Utility.Core
{
    public interface ISingletonInitTag { }

    /// ==========================================================================================
    /// <summary>
    /// Monobehaviourを継承したSingletonクラス
    /// </summary>
    /// ==========================================================================================
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour, ISingletonInitTag where T : MonoBehaviour
    {
        public static T Instance { get; private set; } = null;

        /// ----------------------------------------------------------------------
        /// <summary>
        /// MonoBehaviourのAwake処理
        /// </summary>
        /// <remarks>
        /// 継承先でAwakeを実装する際は、Instance生成が正しく行われるように
        /// 継承元のAwakeを呼び出すように実装する必要がある。
        /// 具体的には以下。
        /// protected override void Awake() {
        ///     base.Awake();
        ///     // Awake後、継承先固有処理を記載
        /// }
        /// </remarks>
        /// ----------------------------------------------------------------------
        virtual protected void Awake()
        {
            Assert.IsNull(Instance, $"多重生成は禁止: {typeof(T)}");
            Instance = this as T;
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// MonoBehaviourのOnDestroy処理
        /// </summary>
        /// <remarks>
        /// 継承先でOnDestroyを実装する際は、破棄処理が正しく行われるように
        /// 継承元のOnDestroyを呼び出すように実装する必要がある。
        /// 具体的には以下。
        /// protected override void OnDestroy() {
        ///     // 継承先固有処理を記載。
        ///     // その後、OnDestroyの呼び出し
        ///     base.OnDestroy();
        /// }
        /// </remarks>
        /// ----------------------------------------------------------------------
        virtual protected void OnDestroy()
        {
            DeleteInstance();
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// インスタンスを明示的に生成
        /// </summary>
        /// <remarks>
        /// スクリプトからインスタンス生成する場合に使用。
        /// 予めコンポーネントとして用意されている場合は使用不可（アサートされる）
        /// </remarks>
        /// <returns>作成されたGameObject</returns>
        /// ----------------------------------------------------------------------
        public static GameObject CreateInstance()
        {
            // GameObjectを作成し、コンポーネントとして追加。
            // 内部でAwakeが呼ばれるので、Instanceは設定される。
            GameObject obj = new();
            obj.AddComponent<T>();
            return obj;
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// インスタンスを削除
        /// </summary>
        /// ----------------------------------------------------------------------
        public static void DeleteInstance()
        {
            if (Instance != null)
            {
                Destroy(Instance.gameObject);
                Instance = null;
            }
        }
    }

    /// ==========================================================================================
    /// <summary>
    /// Singletonクラス
    /// </summary>
    /// ==========================================================================================
    public abstract class Singleton<T> : ISingletonInitTag where T : class, new()
    {
        public static T Instance { get; private set; } = null;

        /// ----------------------------------------------------------------------
        /// <summary>
        /// Singletonインスタンスを生成
        /// </summary>
        /// ----------------------------------------------------------------------
        public static void CreateInstance()
        {
            Assert.IsNull(Instance, $"多重生成は禁止: {typeof(T)}");
            Instance = new T();
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// Singletonインスタンスを破棄
        /// </summary>
        /// ----------------------------------------------------------------------
        public static void DeleteInstance()
        {
            Assert.IsNotNull(Instance, $"未生成のものを削除: {typeof(T)}");
            Instance = null;
        }
    }

#if UNITY_EDITOR
    /// ==========================================================================================
    /// <summary>
    /// SingletonのInstnanceを初期化するための仕組み
    /// </summary>
    /// ==========================================================================================
    public static class SingletonInitializer
    {
        /// <summary>
        /// 対象とするasmdefのホワイトリスト
        /// </summary>
        /// <remarks>
        /// このパッケージを導入しているという事は、GMLib.Runtime.asmdefが存在し、
        /// asmdef が存在するという事は"Assembly-CSharp"は生成されないので
        /// リストには記載していない。
        /// </remarks>
        private static readonly HashSet<string> TargetAssemblies = new()
        {
            "GMLib.Runtime",
            "Assembly-CSharp-firstpass"
        };

        /// ----------------------------------------------------------------------
        /// <summary>
        /// static なメンバ初期化
        /// </summary>
        /// <remarks>
        /// RuntimeInitializeOnLoadMethod にて、実行時に呼ばれる処理。
        /// ISingletonInitTag を継承しているクラスの "Instance"メンバをnull で初期化。
        /// ※初期化を明示的に行っている理由はファイル先頭のコメント参照
        /// Editor上で実行されれば十分なので、UNITY_EDITOR時のみ有効にしている。
        /// </remarks>
        /// ----------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticInitializer()
        {
            // ISingletonInitTag を継承しているも全てを対象とする。
            var checkType = typeof(ISingletonInitTag);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // ホワイトリストに登録されていないasmは無視する。
                if (!TargetAssemblies.Contains(asm.GetName().Name))
                {
                    continue;
                }

                // 対象asmの全ての型を取得
                // GetTypesで失敗していた場合は、成功しているもののみ取り出す。
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                // ISingletonを継承していて、インスタンス可能な型に対し、
                // "Instnace"プロパティの初期化を行う。
                foreach (var type in types)
                {
                    if (checkType.IsAssignableFrom(type) &&
                        !type.IsAbstract &&
                        !type.IsGenericTypeDefinition)
                    {
                        MemberInit(type, "Instance");
                    }
                }
            }
        }

        // 指定メンバをnullで初期化
        private static void MemberInit(System.Type type, string memberName)
        {
            var prop = type.BaseType.GetProperty(
                memberName, BindingFlags.Static | BindingFlags.Public);
            prop?.SetValue(null, null);
        }
    }
#endif
}