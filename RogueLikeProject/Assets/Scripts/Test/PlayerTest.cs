using Manager;
using UnityEngine;

namespace Test
{
    public class PlayerTest : MonoBehaviour
    {
        /// <summary>
        /// 移動速度
        /// </summary>
        [Header("移動速度"), SerializeField]
        private float _moveSpeed = 5.0f;
        /// <summary>
        /// 入力マネージャー
        /// </summary>
        InputManager _input;
        /// <summary>
        /// 入力量
        /// </summary>
        Vector2 _inputVec;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _input = InputManager.Instance;
        }

        // Update is called once per frame
        void Update()
        {
            // 入力量取得
            _inputVec = _input.GetLeftStickVec();
        }

        void FixedUpdate()
        {
            // 移動ベクトル計算
            Vector3 moveVec = (_inputVec * _moveSpeed) * Time.deltaTime;
            // 移動ベクトルを加算
            this.transform.position += moveVec;
        }
    }
}