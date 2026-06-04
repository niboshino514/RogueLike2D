using Cysharp.Threading.Tasks;
using DG.Tweening;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace SceneSystem
{
    /// <summary>
    /// シーンの変更を行う為のフェード処理
    /// </summary>
    public class SceneChangeFade : MonoBehaviour
    {
        /// <summary>
        /// フェード時の画像
        /// </summary>
        [Header("フェード時の画像")]
        [SerializeField]
        private Image _image;

        /// <summary>
        /// フェードにかかる時間
        /// </summary>
        [Header("フェードにかかる時間")]
        [SerializeField]
        private float _fadeTime;

        /// <summary>
        /// フェードイン
        /// </summary>
        public async UniTask FadeIn()
        {
            // コントローラー無効化
            InputManager.Instance.IsInputEnabled = false;
            // フェード前に有効化
            _image.enabled = true;
            // Color調整
            var color = _image.color;
            color.a = 0.0f;
            _image.color = color;
            // 指定時間をかけて、フェードイン
            await _image.DOFade(1f, _fadeTime);
        }

        /// <summary>
        /// フェードアウト
        /// </summary>
        public async UniTask FadeOut()
        {
            // Color調整
            var color = _image.color;
            color.a = 1.0f;
            _image.color = color;
            // 指定時間をかけて、フェードアウト
            await _image.DOFade(0f, _fadeTime);
            // フェード後に無効化
            _image.enabled = false;
            // コントローラー有効化
            InputManager.Instance.IsInputEnabled = true;
        }
    }

}