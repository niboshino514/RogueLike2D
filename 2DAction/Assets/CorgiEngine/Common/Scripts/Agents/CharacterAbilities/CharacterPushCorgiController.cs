using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
	/// Add this class to a Character and it'll be able to push and/or pull CorgiController equipped objects around.
	/// Animator parameters : Pushing (bool), Pulling (bool)
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Push Corgi Controller")]
	public class CharacterPushCorgiController : CharacterAbility
	{
		public override string HelpBoxText() { return "ブロックを押せます。必須ではなく、CorgiController の押し設定を上書きし、専用アニメを使えます。"; }

		/// if this is true, the user will have to press the Push button to push or pull, otherwise it's automatic on contact
		[Tooltip("ボタン操作方式")]
		[Header("ボタン操作方式")]
		public bool ButtonBased = false;
		/// If this is set to true, the Character will be able to push blocks
		[Tooltip("押す動作を有効にする")]
		[Header("押す動作を有効にする")]
		public bool CanPush = true;
		/// If this is set to true, the Character will be able to pull blocks. Note that this requires ButtonBased to be true.
		[Tooltip("引く動作を有効にする")]
		[Header("引く動作を有効にする")]
		public bool CanPull = true;
		/// if this is true, the Character will only be able to push objects while grounded
		[Tooltip("接地中のみ押す")]
		[Header("接地中のみ押す")]
		public bool PushWhenGroundedOnly = true;
		/// the length of the raycast used to detect if we're colliding with a pushable object. Increase this if your animation is flickering.
		[Tooltip("検出レイキャスト長")]
		[Header("検出レイキャスト長")]
		public float DetectionRaycastLength = 0.2f;
		/// the minimum horizontal speed below which we don't consider the character pushing anymore
		[Tooltip("最小押し速度")]
		[Header("最小押し速度")]
		public float MinimumPushSpeed = 0.05f;

		protected bool _collidingWithPushable = false;
		protected Vector3 _raycastDirection;
		protected Vector3 _raycastOrigin;
		protected Pushable _pushedObject;
		protected float _movementMultiplierStorage;
		protected bool _pulling = false;
		protected CharacterRun _characterRun;

		// animation parameters
		protected const string _pushingAnimationParameterName = "Pushing";
		protected const string _pullingAnimationParameterName = "Pulling";
		protected int _pushingAnimationParameter;
		protected int _pullingAnimationParameter;

		/// <summary>
		/// On Start(), we initialize our various flags
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			_characterRun = _character?.FindAbility<CharacterRun>();
		}

		/// <summary>
		/// Every frame we override parameters if needed and cast a ray to see if we're actually pushing anything
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();

			if (!CanPush || !AbilityAuthorized)
			{
				return;
			}

			CheckForPushEnd();

			// if we're button based we only proceed if the push button is being pressed
			if (ButtonBased
			    && (_character.CharacterType == Character.CharacterTypes.Player)
			    && (_inputManager.PushButton.State.CurrentState != MMInput.ButtonStates.ButtonPressed))
			{
				return;
			}

			// we set our flag to false
			_collidingWithPushable = false;

			// we cast a ray in front of us to see if we're colliding with a pushable object
			_raycastDirection = _character.IsFacingRight ? transform.right : -transform.right;
			_raycastOrigin = _controller.ColliderCenterPosition + _raycastDirection * (_controller.Width()/2 );

			// we cast our ray to see if we're hitting something
			RaycastHit2D hit = MMDebug.RayCast (_raycastOrigin,_raycastDirection,DetectionRaycastLength,_controller.PlatformMask,Color.green,_controller.Parameters.DrawRaycastsGizmos);
			if (hit)
			{
				if (hit.collider.gameObject.MMGetComponentNoAlloc<Pushable>() != null)
				{
					_collidingWithPushable = true;
				}
			}

			// if we're colliding with a pushable and are in the right conditions, we start pushing
			if (_controller.State.IsGrounded
			    && _collidingWithPushable
			    && Mathf.Abs(_controller.ExternalForce.x) >= MinimumPushSpeed
			    && _movement.CurrentState != CharacterStates.MovementStates.Pushing
			    && _movement.CurrentState != CharacterStates.MovementStates.Jumping)
			{
				if (_movement.CurrentState == CharacterStates.MovementStates.Running)
				{
					if (_characterRun != null)
					{
						_characterRun.RunStop();
					}
				}
				PlayAbilityStartFeedbacks ();
				_movement.ChangeState (CharacterStates.MovementStates.Pushing);
			}

			if (hit && (_movement.CurrentState == CharacterStates.MovementStates.Pushing) && (_pushedObject == null))
			{
				_pushedObject = hit.collider.gameObject.MMGetComponentNoAlloc<Pushable>();
				_pushedObject.Attach(_controller);
				_character.CanFlip = false;
				_movementMultiplierStorage = _characterHorizontalMovement.PushSpeedMultiplier;
				_characterHorizontalMovement.PushSpeedMultiplier = _pushedObject.PushSpeed;
			}

			if (((_controller.Speed.x > MinimumPushSpeed)
			     && (_movement.CurrentState == CharacterStates.MovementStates.Pushing)
			     && (_pushedObject.transform.position.x < this.transform.position.x))
			    ||
			    ((_controller.Speed.x < -MinimumPushSpeed)
			     && (_movement.CurrentState == CharacterStates.MovementStates.Pushing)
			     && (_pushedObject.transform.position.x > this.transform.position.x)))
			{
				if (!CanPull)
				{
					StopPushing();
				}
				else
				{
					_pulling = true;
				}
			}
			else
			{
				_pulling = false;
			}
		}

		/// <summary>
		/// Checks whether we should stop pushing and change state
		/// </summary>
		protected virtual void CheckForPushEnd()
		{
			if ((_pushedObject != null) && (_character.CharacterType == Character.CharacterTypes.Player) && _inputManager.PushButton.State.CurrentState != MMInput.ButtonStates.ButtonPressed && ButtonBased)
			{
				StopPushing();
			}

			if (!_collidingWithPushable && (_movement.CurrentState == CharacterStates.MovementStates.Pushing))
			{
				StopPushing();
			}

			if (((_pushedObject == null) && _movement.CurrentState == CharacterStates.MovementStates.Pushing)
			    || ((_pushedObject != null) && Mathf.Abs(_controller.Speed.x) <= MinimumPushSpeed && _movement.CurrentState == CharacterStates.MovementStates.Pushing))
			{
				// we reset the state
				_movement.ChangeState(CharacterStates.MovementStates.Idle);

				PlayAbilityStopFeedbacks();
				StopStartFeedbacks();
			}

			if ((_movement.CurrentState != CharacterStates.MovementStates.Pushing) && _startFeedbackIsPlaying)
			{
				PlayAbilityStopFeedbacks();
				StopStartFeedbacks();
			}
		}

		/// <summary>
		/// Stops the character from pushing or pulling
		/// </summary>
		protected virtual void StopPushing()
		{
			if (_pushedObject == null)
			{
				return;
			}
			_pushedObject.Detach(_controller);
			_pushedObject = null;
			_character.CanFlip = true;
			_characterHorizontalMovement.PushSpeedMultiplier = _movementMultiplierStorage;
			_pulling = false;
		}

		/// <summary>
		/// Adds required animator parameters to the animator parameters list if they exist
		/// </summary>
		protected override void InitializeAnimatorParameters()
		{
			RegisterAnimatorParameter(_pushingAnimationParameterName, AnimatorControllerParameterType.Bool, out _pushingAnimationParameter);
			RegisterAnimatorParameter(_pullingAnimationParameterName, AnimatorControllerParameterType.Bool, out _pullingAnimationParameter);
		}

		/// <summary>
		/// Sends the current state of the push and pull states to the character's animator
		/// </summary>
		public override void UpdateAnimator()
		{
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _pushingAnimationParameter, (_movement.CurrentState == CharacterStates.MovementStates.Pushing), _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _pullingAnimationParameter, _pulling, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}

		/// <summary>
		/// On reset ability, we cancel all the changes made
		/// </summary>
		public override void ResetAbility()
		{
			base.ResetAbility();
			if (_animator != null)
			{
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _pushingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _pullingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);    
			}
		}
	}
}