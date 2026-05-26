using Manager;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace TileMap
{
    public class EndChip : CorgiMonoBehaviour
    {
        /// <summary>
        /// ステージマネージャー
        /// </summary>
        private StageManager _stageManager;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // インスタンス取得
            _stageManager = StageManager.Instance;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Character character = other.GetComponent<Character>();

            if (character == null) { return; }
            if (character.CharacterType != Character.CharacterTypes.Player) { return; }

            _stageManager.NextStage(character.transform);
        }
    }
}