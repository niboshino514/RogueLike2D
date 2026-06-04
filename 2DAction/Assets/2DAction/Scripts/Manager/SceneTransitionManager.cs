using Cysharp.Threading.Tasks;
using DG.Tweening.Core.Easing;
using EvoLib.Utility.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneSystem;

namespace Manager
{
    public class SceneTransitionManager : SingletonMonoBehaviour<SceneTransitionManager>
    {
        /// <summary>
        /// シーン種別
        /// </summary>
        public enum SceneID
        {
            Title,
            Main,
            Result
        }

        /// <summary>
        /// シーン名テーブル
        /// </summary>
        private static readonly Dictionary<SceneID, string> SceneNameTable = new()
        {
            { SceneID.Title,"TitleScene"},
            { SceneID.Main,"MainScene"},
            { SceneID.Result,"ResultScene"},
        };

        /// <summary>
        /// シーン変更フェード
        /// </summary>
        [SerializeField]
        [Header("シーン変更フェード")]
        private SceneChangeFade _sceneChangeFade;

        /// <summary>
        /// シーン切り替え処理
        /// </summary>
        /// <param name="sceneID">シーン種別</param>
        /// <param name="sceneMode">切り替え時のモード</param>
        /// <returns></returns>
        public async UniTask SceneTransition(SceneID sceneID, LoadSceneMode sceneMode, Scene currentScene)
        {
            // フェードイン
            await _sceneChangeFade.FadeIn();

            if (sceneMode == LoadSceneMode.Additive)
            {
                // シーンアンロード
                await SceneManager.UnloadSceneAsync(currentScene);
            }
            // シーンロード
            await SceneManager.LoadSceneAsync(SceneNameTable[sceneID], sceneMode);
            // フェードアウト
            await _sceneChangeFade.FadeOut();
        }
    }
}