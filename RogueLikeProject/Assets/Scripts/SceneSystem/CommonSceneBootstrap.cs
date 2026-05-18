using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneSystem
{
    /// <summary>
    /// どのSceneから起動したとしても、必ずCommonSceneを起動するためのクラス<br/>
    /// ※CommonSceneから起動しようとするとエラーをはきます
    /// </summary>
    public static class CommonSceneBootstrap
    {
        /// <summary>
        /// コモンシーン名
        /// </summary>
        public const string COMMON_SCENE_NAME = "CommonScene";

        /// <summary>
        /// ゲーム起動時にコモンシーンを加算ロードする
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadCommonScene()
        {
            // コモンシーン加算ロード
            SceneManager.LoadSceneAsync(COMMON_SCENE_NAME, LoadSceneMode.Additive);
        }
    }
}