using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneSystem
{
    public class CommonSceneChecker : MonoBehaviour
    {
        /// <summary>
        /// エラーメッセージ
        /// </summary>
        private const string ERROR_MESSAGE = "CommonSceneが複数ロードされています。CommonSceneからゲームを起動している可能性があります。";

        private void Start()
        {
            // CommonScene が 2 つ以上ロードされていたらエラー
            Debug.Assert(CommonSceneCount() == 1,ERROR_MESSAGE);
        }

        /// <summary>
        /// CommonSceneの数を数える
        /// </summary>
        /// <returns></returns>
        private static int CommonSceneCount()
        {
            // CommonSceneの個数
            int countNum = 0;
            // 全てのSceneを参照し、CommonSceneがあれば、countを足す
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == CommonSceneBootstrap.COMMON_SCENE_NAME)
                {
                    countNum++;
                }
            }
            // CommonSceneの数を返す
            return countNum;
        }
    }
}