using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace Hander
{
    public class DeathHandler : MonoBehaviour, MMEventListener<CorgiEngineEvent>
    {
        private bool waitingForRespawn = false;

        public GameObject deathScreenUI; // 死亡画面のUI

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
            waitingForRespawn = true;

            // 死亡画面を表示
            if (deathScreenUI != null)
                deathScreenUI.SetActive(true);

            // プレイヤー操作を止める
            LevelManager.Instance.FreezeCharacters();
        }

        private void Update()
        {
            if (!waitingForRespawn) return;

            // Rキーで復活
            if (Input.GetKeyDown(KeyCode.R))
            {
                RespawnPlayer();
            }
        }

        private void RespawnPlayer()
        {
            waitingForRespawn = false;

            if (deathScreenUI != null)
                deathScreenUI.SetActive(false);

            // プレイヤー復活
            LevelManager.Instance.UnFreezeCharacters();
            LevelManager.Instance.RespawnCharacter();
        }
    }
}