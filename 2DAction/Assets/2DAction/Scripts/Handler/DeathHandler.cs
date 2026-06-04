using Custom;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hander
{
    public class DeathHandler : MonoBehaviour, MMEventListener<CorgiEngineEvent>
    {
        /// <summary>
        /// リスポーン待ちかどうか
        /// </summary>
        private bool _waitingForRespawn = false;

        /// <summary>
        /// 死亡画面のUI
        /// </summary>
        [Header("死亡画面のUI"),SerializeField]
        private GameObject _deathScreenUI;

        private void OnEnable()
        {
            this.MMEventStartListening<CorgiEngineEvent>();
        }

        private void OnDisable()
        {
            this.MMEventStopListening<CorgiEngineEvent>();
        }

        public void OnMMEvent(CorgiEngineEvent e)
        {
            if (e.EventType == CorgiEngineEventTypes.PlayerDeath)
            {
                ShowDeathScreen();
            }
        }

        private void ShowDeathScreen()
        {
            _waitingForRespawn = true;

            // 死亡画面を表示
            if (_deathScreenUI != null)
                _deathScreenUI.SetActive(true);

            // プレイヤー操作を止める
            LevelManager.Instance.FreezeCharacters();
        }

        private void Update()
        {
            if (!_waitingForRespawn) return;

            // ボタンを押したら復活
            if (Manager.InputManager.Instance.IsTrig(Manager.InputManager.BtnType.Respawn))
            {
                RespawnPlayer();
            }
        }

        /// <summary>
        /// プレイヤー復活
        /// </summary>
        private void RespawnPlayer()
        {
            _waitingForRespawn = false;

            if (_deathScreenUI != null)
                _deathScreenUI.SetActive(false);

            // プレイヤー復活
            LevelManager.Instance.UnFreezeCharacters();
            CustomLevelManager.Instance.RespawnCharacter();
            CustomLevelManager.Instance.IsDrawCharacter(true);
        }
    }
}