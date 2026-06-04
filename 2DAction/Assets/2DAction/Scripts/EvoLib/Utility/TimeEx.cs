using UnityEngine;

namespace EvoLib.Utility
{
    public static class TimeEx
    {
        private static float _gameTimeScale = 1f;
        private static bool _isPaused = false;

        /// <summary>
        /// ゲーム内の時間（ポーズ中は0）
        /// </summary>
        public static float GameDeltaTime =>
            _isPaused ? 0f : Time.deltaTime * _gameTimeScale;

        /// <summary>
        /// UI やポーズメニュー用（timeScale の影響を受けない）
        /// </summary>
        public static float UnscaledDeltaTime => Time.unscaledDeltaTime;

        /// <summary>
        /// ゲームをポーズ
        /// </summary>
        public static void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// ポーズ解除
        /// </summary>
        public static void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// ゲーム速度変更（スロー・加速）
        /// </summary>
        public static void SetGameSpeed(float scale)
        {
            _gameTimeScale = Mathf.Clamp(scale, 0f, 5f);
        }
    }
}
