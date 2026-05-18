using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Test
{
    /// <summary>
    /// シーン変更用のテスター
    /// </summary>
    public class SceneChangeTester : MonoBehaviour
    {
        /// <summary>
        /// シーンID
        /// </summary>
        [Header("ここで設定したシーンIDが次のシーンとなる")]
        [SerializeField]
        private SceneTransitionManager.SceneID _sceneID;

        // Update is called once per frame
        void Update()
        {
            if (InputManager.Instance.IsTrig(InputManager.BtnType.Up))
            {
                SceneChange().Forget();
            }
        }

        async UniTaskVoid SceneChange()
        {
            // Scene切り替え
            await SceneTransitionManager.Instance.SceneTransition(
                    _sceneID,
                    LoadSceneMode.Additive,
                    this.gameObject.scene);
        }
    }
}
