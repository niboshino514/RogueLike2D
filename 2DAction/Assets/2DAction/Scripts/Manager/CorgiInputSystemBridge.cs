#if MM_INPUTSYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using MoreMountains.Tools;
using MoreMountains.CorgiEngine;

namespace Manager
{
    /// <summary>
    /// Corgi Engine の入力を InputSystem_Actions（プロジェクト共通の InputAction アセット）へ接続するブリッジ。
    /// シーン上の Corgi Engine InputManager コンポーネントをこのクラスに差し替えて使用する。
    ///
    /// アクションマッピング:
    ///   Player/Move    → PrimaryMovement（左スティック / WASD）
    ///   Player/Look    → SecondaryMovement（右スティック / マウスデルタ）
    ///   Player/Jump    → JumpButton（スペース / Gamepad A）
    ///   Player/Sprint  → RunButton（Shift / Gamepad L3）
    ///   Player/Attack  → ShootButton（マウス左 / Gamepad X）
    ///   Player/Interact→ InteractButton（E / Gamepad Y）
    ///   Player/Crouch  → DashButton（C / Gamepad B）
    /// </summary>
    [AddComponentMenu("RogueLike/Managers/Corgi Input System Bridge")]
    public class CorgiInputSystemBridge : InputSystemManager
    {
        private InputSystem_Actions _gameActions;

        protected override void Initialization()
        {
            // Corgi 基底の初期化（ControlsModeDetection, InitializeButtons, InitializeAxis）
            ControlsModeDetection();
            InitializeButtons();
            InitializeAxis();

            _inputActionsEnabled = true;
            _gameActions = new InputSystem_Actions();

            // 移動ベクトル
            _gameActions.Player.Move.performed += ctx => _primaryMovement = ctx.ReadValue<Vector2>();
            _gameActions.Player.Move.canceled  += ctx => _primaryMovement = Vector2.zero;

            // 照準ベクトル（右スティック / マウスデルタ）
            _gameActions.Player.Look.performed += ctx => _secondaryMovement = ctx.ReadValue<Vector2>();
            _gameActions.Player.Look.canceled  += ctx => _secondaryMovement = Vector2.zero;

            // ボタン（started/canceled でHold等のインタラクションを無視して素直に押下・解放を扱う）
            BindStartedCanceled(_gameActions.Player.Jump,     JumpButton);
            BindStartedCanceled(_gameActions.Player.Sprint,   RunButton);
            BindStartedCanceled(_gameActions.Player.Attack,   ShootButton);
            BindStartedCanceled(_gameActions.Player.Interact, InteractButton);
            BindStartedCanceled(_gameActions.Player.Crouch,   DashButton);

            _initialized = true;
        }

        // 押した瞬間 → ButtonDown、離した瞬間 → ButtonUp を直接設定する
        private void BindStartedCanceled(InputAction action, MMInput.IMButton button)
        {
            action.started  += _ => button?.TriggerButtonDown();
            action.canceled += _ => button?.TriggerButtonUp();
        }

        protected override void Update()
        {
            if (IsMobile && _inputActionsEnabled)
            {
                _inputActionsEnabled = false;
                _gameActions.Disable();
                return;
            }

            if (!IsMobile && InputDetectionActive != _inputActionsEnabled)
            {
                if (InputDetectionActive)
                {
                    _inputActionsEnabled = true;
                    _gameActions.Enable();
                    // 有効化直後に現在値を同期
                    _primaryMovement   = _gameActions.Player.Move.ReadValue<Vector2>();
                    _secondaryMovement = _gameActions.Player.Look.ReadValue<Vector2>();
                }
                else
                {
                    _inputActionsEnabled = false;
                    _gameActions.Disable();
                }
            }
        }

        protected override void OnEnable()
        {
            if (!_initialized) Initialization();
            _gameActions.Enable();
        }

        protected override void OnDisable()
        {
            _gameActions?.Disable();
        }

        private void OnDestroy()
        {
            _gameActions?.Dispose();
        }
    }
}
#endif
