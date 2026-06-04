using Manager;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// InputManagerがうまく動作するかどうかの確認用コード
    /// </summary>
    public class InputTest : MonoBehaviour
    {
        /// <summary>
        /// 入力マネージャー
        /// </summary>
        private InputManager _input;

        /// <summary>
        /// 入力を確かめるボタンタイプ
        /// </summary>
        [Header("入力を確かめるボタンタイプ"),SerializeField]
        private InputManager.BtnType _btnType;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // インスタンス取得
            _input = InputManager.Instance;
        }
        // Update is called once per frame
        void Update()
        {
            // トリガーボタンが押された場合、Logを出す
            if (_input.IsTrig(_btnType))
            {
                Debug.Log($"トリガー {_btnType}");
            }
            // ボタンが押しっぱなしの場合、Logを出す
            if (_input.IsPress(_btnType))
            {
                Debug.Log($"プレス {_btnType}");
            }
            // ボタンが離された場合、Logを出す
            if (_input.IsRelease(_btnType))
            {
                Debug.Log($"リリース {_btnType}");
            }
        }
    }

}
