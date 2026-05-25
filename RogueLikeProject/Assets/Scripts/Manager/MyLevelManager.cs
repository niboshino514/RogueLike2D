using MoreMountains.CorgiEngine;

namespace Manager
{
    public class MyLevelManager : LevelManager
    {
        public override void PlayerDead(Character player)
        {
            // 残機処理だけやる
            if (GameManager.Instance.MaximumLives > 0)
            {
                GameManager.Instance.LoseLife();
            }
        }
    }
}