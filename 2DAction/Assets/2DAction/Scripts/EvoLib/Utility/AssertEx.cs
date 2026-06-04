using UnityEngine;

namespace EvoLib.Utility
{
    /// <summary>
    /// Unity の Assert をラップし、<br/>
    /// ・ジェネリック対応<br/>
    /// ・エディタ/ビルドで挙動切り替え<br/>
    /// ・例外スローの有無を選択<br/>
    /// ・ログフォーマット統一<br/>
    /// を実現したユーティリティクラス。
    /// </summary>
    public static class AssertEx
    {
        /// <summary>
        /// 値が null でないことを確認する（ジェネリック対応）<br/>
        /// null の場合はログ出力、または例外をスローする。
        /// </summary>
        public static void IsNotNull<T>(T obj, string message = "Null reference detected.", bool throwException = false) where T : class
        {
#if UNITY_EDITOR
            UnityEngine.Assertions.Assert.IsNotNull(obj, message);
#else
            if (obj == null)
            {
                Debug.LogError($"[ASSERT] {message}");

                if (throwException)
                {
                    throw new System.NullReferenceException(message);
                }
            }
#endif
        }

        /// <summary>
        /// 条件が true であることを確認する。<br/>
        /// false の場合はログ出力、または例外をスローする。
        /// </summary>
        public static void IsTrue(bool condition, string message = "Assertion failed.", bool throwException = false)
        {
#if UNITY_EDITOR
            UnityEngine.Assertions.Assert.IsTrue(condition, message);
#else
            if (!condition)
            {
                Debug.LogError($"[ASSERT] {message}");

                if (throwException)
                {
                    throw new System.Exception(message);
                }
            }
#endif
        }

        /// <summary>
        /// 条件が false であることを確認する。<br/>
        /// true の場合はログ出力、または例外をスローする。
        /// </summary>
        public static void IsFalse(bool condition, string message = "Assertion failed.", bool throwException = false)
        {
#if UNITY_EDITOR
            UnityEngine.Assertions.Assert.IsFalse(condition, message);
#else
            if (condition)
            {
                Debug.LogError($"[ASSERT] {message}");

                if (throwException)
                {
                    throw new System.Exception(message);
                }
            }
#endif
        }
    }
}
