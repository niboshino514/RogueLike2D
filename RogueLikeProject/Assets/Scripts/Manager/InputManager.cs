using Utility.Core;
using Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manager
{
    /// <summary>
    /// 入力を管理するマネージャークラス
    /// </summary>
    public class InputManager : SingletonMonoBehaviour<InputManager>
    {
        /// <summary>
        /// ボタン種別
        /// </summary>
        public enum BtnType
        {
            Up,
            Down,
            Left,
            Right,
        }

        /// <summary>
        /// 現在のデバイスタイプ
        /// </summary>
        public enum InputDeviceType
        {
            KeyboardMouse,
            Gamepad
        }

        /// <summary>
        /// 入力情報
        /// </summary>
        public struct InputInfo
        {
            public int trig;// トリガー
            public int press;// 押しっぱなし
            public int release;// 離したとき
            public int repeat;// リピート
            public Vector2 leftStickVec;// 左Stick
            public Vector2 rightStickVec;// 右Stick
        }

        /// <summary>
        /// 最初のリピート時間
        /// </summary>
        [Header("最初のリピート時間")]
        [SerializeField] private float _repeatFirstSec = 0.5f;
        /// <summary>
        /// 以降のリピート時間
        /// </summary>
        [Header("以降のリピート時間")]
        [SerializeField] private float _repeatAfterSec = 0.25f;

        /// <summary>
        /// 現在のインプット情報
        /// </summary>
        private InputInfo _inputInfo;
        /// <summary>
        /// リピート時間カウンター
        /// </summary>
        private float _repeatSec = 0f;
        /// <summary>
        /// インプットシステム
        /// </summary>
        private InputSystem_Actions _input;
        /// <summary>
        /// アクションマップ辞書
        /// </summary>
        private Dictionary<BtnType, InputAction> _actionMapTable;
        /// <summary>
        /// 移動のアクションマップ
        /// </summary>
        private InputAction _moveActionMap;
        /// <summary>
        /// 右スティックのアクションマップ
        /// </summary>
        private InputAction _rightStickActionMap;

        /// <summary>
        /// 現在のデバイスタイプ
        /// </summary>
        public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.KeyboardMouse;
        /// <summary>
        /// 入力が有効かどうか
        /// </summary>
        public bool IsInputEnabled { get; set; } = true;

        protected override void Awake()
        {
            base.Awake();

            // インスタンス生成
            _input = new();
            _inputInfo = new();

            // アクションマップ登録※入力が増える場合、ここに登録する
            _actionMapTable = new()
            {
                { BtnType.Up,    _input.Player.Move },
                { BtnType.Down,  _input.Player.Move },
                { BtnType.Left,  _input.Player.Move },
                { BtnType.Right, _input.Player.Move },
            };

            // アクションマップ代入
            _moveActionMap = _input.Player.Move;
            _rightStickActionMap = _input.Player.Look;
        }

        private void OnEnable()
        {
            // インプットシステム有効化
            _input.Enable();
        }

        private void OnDisable()
        {
            // インプットシステム無効化
            _input.Disable();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            // インプットシステム破棄
            _input.Dispose();
        }

        private void Update()
        {
            // 入力更新
            InputUpdate();
        }

        /// <summary>
        /// 入力更新
        /// </summary>
        private void InputUpdate()
        {
            // 入力が無効の場合、ここで処理を終える
            if (!IsInputEnabled)
            {
                // _inputInfoにデフォルト値を代入
                _inputInfo = new InputInfo();
                return;
            }

            // スティック更新
            UpdateStickInput();

            // ボタン判定
            foreach (var kv in _actionMapTable)
            {
                BtnType btn = kv.Key;
                bool isPressed = CheckVector(btn);
                // 入力bit初期化
                _inputInfo.trig = BitUtil.Remove(_inputInfo.trig, (int)btn);
                _inputInfo.press = BitUtil.Remove(_inputInfo.press, (int)btn);
                _inputInfo.release = BitUtil.Remove(_inputInfo.release, (int)btn);

                var action = _actionMapTable[btn];

                // 離した瞬間かどうか
                if (action.WasReleasedThisFrame())
                {
                    // 離した瞬間のBitを立てる
                    _inputInfo.release = BitUtil.Add(_inputInfo.release, (int)btn);
                }

                // 何も押されていない場合、コンティニュー
                if (!isPressed)
                {
                    continue;
                }
                // トリガー入力かどうか
                if (action.WasPressedThisFrame())
                {
                    // トリガーのBitを立てる
                    _inputInfo.trig = BitUtil.Add(_inputInfo.trig, (int)btn);
                    // リピートカウンターに最初のリピート時間を代入
                    _repeatSec = _repeatFirstSec;
                }
                // 押しっぱなし入力かどうか
                if (action.IsPressed())
                {
                    // 押しっぱなしのBitを立てる
                    _inputInfo.press = BitUtil.Add(_inputInfo.press, (int)btn);
                }
            }

            // リピート処理
            UpdateRepeat();
            // 現在のデバイスが何かを調べる
            DetectDevice();
        }

        /// <summary>
        /// スティック入力更新
        /// </summary>
        private void UpdateStickInput()
        {
            _inputInfo.leftStickVec = _moveActionMap.ReadValue<Vector2>();
            _inputInfo.rightStickVec = _rightStickActionMap.ReadValue<Vector2>();
        }
        /// <summary>
        /// リピート
        /// </summary>
        void UpdateRepeat()
        {
            if (_repeatSec <= 0f)
            {
                return;
            }
            _repeatSec -= Time.fixedDeltaTime;

            if (_repeatSec <= 0f)
            {
                _inputInfo.repeat = _inputInfo.press;
                _repeatSec += _repeatAfterSec;
            }
            else
            {
                _inputInfo.repeat = _inputInfo.trig;
            }
        }
        /// <summary>
        /// 現在のデバイスが何かを調べる
        /// </summary>
        void DetectDevice()
        {
            // マウス
            if (Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                if (mouseDelta.sqrMagnitude > 0.01f)
                {
                    CurrentDevice = InputDeviceType.KeyboardMouse;
                    return;
                }
            }

            // Move
            if (_moveActionMap.activeControl != null)
            {
                var dev = _moveActionMap.activeControl.device;
                CurrentDevice = dev is Gamepad ? InputDeviceType.Gamepad : InputDeviceType.KeyboardMouse;
                return;
            }

            // Right Stick
            if (_rightStickActionMap.activeControl != null)
            {
                var dev = _rightStickActionMap.activeControl.device;
                CurrentDevice = dev is Gamepad ? InputDeviceType.Gamepad : InputDeviceType.KeyboardMouse;
                return;
            }

            // Buttons
            foreach (var kv in _actionMapTable)
            {
                var action = kv.Value;
                if (action.activeControl != null)
                {
                    var dev = action.activeControl.device;
                    CurrentDevice = dev is Gamepad ? InputDeviceType.Gamepad : InputDeviceType.KeyboardMouse;
                    return;
                }
            }
        }

        /// <summary>
        /// 上下左右ボタン判定
        /// </summary>
        /// <param name="btnType"></param>
        /// <returns></returns>
        private bool CheckVector(BtnType btnType)
        {
            if (btnType == BtnType.Up ||
                btnType == BtnType.Down ||
                btnType == BtnType.Left ||
                btnType == BtnType.Right)
            {
                Vector2 vec = _inputInfo.leftStickVec;

                if (btnType == BtnType.Up) return vec.y > 0.5f;
                if (btnType == BtnType.Down) return vec.y < -0.5f;
                if (btnType == BtnType.Left) return vec.x < -0.5f;
                if (btnType == BtnType.Right) return vec.x > 0.5f;

                return false;
            }

            return _actionMapTable[btnType].ReadValue<float>() > 0.1f;
        }
        
        public bool IsTrig(BtnType btn) => BitUtil.IsOn(_inputInfo.trig, (int)btn);
        public bool IsPress(BtnType btn) => BitUtil.IsOn(_inputInfo.press, (int)btn);
        public bool IsRelease(BtnType btn) => BitUtil.IsOn(_inputInfo.release, (int)btn);
        public bool IsRepeat(BtnType btn) => BitUtil.IsOn(_inputInfo.repeat, (int)btn);
        public Vector2 GetLeftStickVec() => _inputInfo.leftStickVec;
        public Vector2 GetRightStickVec() => _inputInfo.rightStickVec;
    }
}