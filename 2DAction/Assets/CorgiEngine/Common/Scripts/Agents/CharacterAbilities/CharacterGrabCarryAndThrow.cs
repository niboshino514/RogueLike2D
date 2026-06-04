using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// This class lets you grab, carry and throw objects with a GrabCarryAndThrowObject component.
	/// 
	/// Animation parameters :
	/// - Grabbing, boolean, triggered when an object is grabbed
	/// - Carrying : boolean, true if an object is being carried, false otherwise
	/// - CarryingID : int, set to whatever value is set on the carried object 
	/// - Throwing, boolean, triggered when an object gets thrown
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Grab, Carry and Throw")]
	public class CharacterGrabCarryAndThrow : CharacterAbility
	{
		public override string HelpBoxText() { return "GrabCarryAndThrowObject 付きオブジェクトを掴み、運び、投げられます。掴みセクションで検出レイキャスト、運搬セクションで子 Transform への付け先、投げセクションで投げの強さと反動を設定できます。"; }

		[Header("掴む")]

		/// the direction the raycast used to detect grabbable objects will be cast in (if the Character is facing right). Use Vector3.down for Mario2-like grabs from the top, or Vector3.right
		/// for side grabs for example.
		[Tooltip("レイキャスト方向")]
		[Header("レイキャスト方向")]
		public Vector3 RaycastDirection = Vector3.down;
		/// the distance the grab raycast should cover (you'll want it bigger than half your Character's dimensions
		[Tooltip("レイキャスト距離")]
		[Header("レイキャスト距離")]
		public float RaycastDistance = 1f;
		/// the layer this grab raycast should look for objects on. This should match the layer you put your GrabCarryAndThrowObjects on
		[Tooltip("検出レイヤーマスク")]
		[Header("検出レイヤーマスク")]
		public LayerMask DetectionLayerMask = LayerManager.PlatformsLayerMask | LayerManager.EnemiesLayerMask;
		/// whether or not this Character is grabbing something right now
		[MMReadOnly]
		[Tooltip("掴んでいる")]
		[Header("掴んでいる")]
		public bool Grabbing = false;

		[Header("運ぶ")]

		/// a Transform used to attach carried objects to
		[Tooltip("運搬親トランスフォーム")]
		[Header("運搬親トランスフォーム")]
		public Transform CarryParent;
		/// whether or not this Character is carrying an object this frame
		[MMReadOnly]
		[Tooltip("運んでいる")]
		[Header("運んでいる")]
		public bool Carrying = false;
		/// the ID of the object being carried
		[MMReadOnly]
		[Tooltip("運搬オブジェクトID")]
		[Header("運搬オブジェクトID")]
		public int CarryingID = -1;
		/// a reference to the object being carried
		[MMReadOnly]
		[Tooltip("運搬オブジェクト")]
		[Header("運搬オブジェクト")]
		public GrabCarryAndThrowObject CarriedObject = null;

		[Header("投げる")]

		/// the force to apply when throwing
		[Tooltip("投げる力")]
		[Header("投げる力")]
		public float ThrowForce = 1f;
		/// a modifier to apply to the recoil set on the object
		[Tooltip("反動修正値")]
		[Header("反動修正値")]
		public float RecoilModifier = 1f;
		/// whether or not this Character is throwing something this frame
		[MMReadOnly]
		[Tooltip("投げている")]
		[Header("投げている")]
		public bool Throwing = false;
		/// whether or not to allow the character to throw if next to a grabbable object
		[Tooltip("掴み時の投げを防ぐ")]
		[Header("掴み時の投げを防ぐ")]
		public bool PreventThrowIfCarryingOnGrab = false;

		protected Vector2 _raycastOrigin;
		protected Vector2 _recoilVector;

		// animation parameters
		protected const string _grabbingAnimationParameterName = "Grabbing";
		protected int _grabbingAnimationParameter;
		protected const string _carryingAnimationParameterName = "Carrying";
		protected int _carryingAnimationParameter;
		protected const string _carryingIDAnimationParameterName = "CarryingID";
		protected int _carryingIDAnimationParameter;
		protected const string _throwingAnimationParameterName = "Throwing";
		protected int _throwingAnimationParameter;
		protected Vector3 _actualRaycastDirection;
		
		/// <summary>
		/// On init we set our CarryParent to the character transform if null
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			if (CarryParent == null)
			{
				CarryParent = this.transform;
			}
		}
        
		/// <summary>
		/// Looks for throw and grab inputs
		/// </summary>
		protected override void HandleInput()
		{
			if (_inputManager.ThrowButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
			{
				if (Carrying)
				{
					if (PreventThrowIfCarryingOnGrab && (GetGrababbleObject() != null))
					{
						return;
					}
					Throw();
				}
			}
			if (_inputManager.GrabButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
			{
				if (!Carrying)
				{
					GrabAttempt();
				}                
			}
		}

		/// <summary>
		/// Tries to grab by casting a raycast
		/// </summary>
		protected virtual void GrabAttempt()
		{
			if (!AbilityAuthorized
			    || ((_condition.CurrentState != CharacterStates.CharacterConditions.Normal) && (_condition.CurrentState != CharacterStates.CharacterConditions.ControlledMovement)))
			{
				return;
			}
            
			CarriedObject = GetGrababbleObject();    
			if (CarriedObject != null)
			{
				Grab();
			}
		}

		protected virtual GrabCarryAndThrowObject GetGrababbleObject()
		{
			_raycastOrigin = this.transform.position;
			_actualRaycastDirection = RaycastDirection;
			if (!_character.IsFacingRight)
			{
				_actualRaycastDirection = _actualRaycastDirection.MMSetX(-RaycastDirection.x);
			}
			RaycastHit2D hit = MMDebug.RayCast(_raycastOrigin, _actualRaycastDirection, RaycastDistance, DetectionLayerMask, Color.blue, _controller.Parameters.DrawRaycastsGizmos);
			if (hit)
			{
				// we make sure we have an object that can be carried
				return hit.collider.gameObject.MMGetComponentNoAlloc<GrabCarryAndThrowObject>();                
			}

			return null;
		}

		/// <summary>
		/// Sets the ability in carrying mode
		/// </summary>
		protected virtual void Grab()
		{
			if (!AbilityAuthorized)
			{
				return;
			}
            
			Carrying = true;
			CarryingID = CarriedObject.CarryingAnimationID;
			CarriedObject.Grab(CarryParent);
			Grabbing = true;
			PlayAbilityStartFeedbacks();
			MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Grab, MMCharacterEvent.Moments.Start);
		}

		/// <summary>
		/// Throws the carried object
		/// </summary>
		protected virtual void Throw()
		{
			if (!AbilityAuthorized)
			{
				return;
			}
            
			if (CarriedObject == null)
			{
				return;
			}

			int direction = _character.IsFacingRight ? 1 : -1;
			CarriedObject.Throw(direction, ThrowForce);

			// apply recoil
			if (RecoilModifier != 0f)
			{
				_recoilVector = (direction == 1) ? Vector2.left : Vector2.right;
				_recoilVector *= RecoilModifier * CarriedObject.Recoil;
				_controller.AddForce(_recoilVector);
			}

			StopFeedbacks();
			CarriedObject = null;
			CarryingID = -1;
			Carrying = false;
			Throwing = true;
		}

		/// <summary>
		/// Stops all feedbacks
		/// </summary>
		protected virtual void StopFeedbacks()
		{
			if (_startFeedbackIsPlaying)
			{
				StopStartFeedbacks();
				PlayAbilityStopFeedbacks();
				MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Grab, MMCharacterEvent.Moments.End);
			}
		}
        
		/// <summary>
		/// On late update we reset our states
		/// </summary>
		protected virtual void LateUpdate()
		{
			Grabbing = false;
			Throwing = false;
		}

		/// <summary>
		/// Adds required animator parameters to the animator parameters list if they exist
		/// </summary>
		protected override void InitializeAnimatorParameters()
		{
			RegisterAnimatorParameter(_grabbingAnimationParameterName, AnimatorControllerParameterType.Bool, out _grabbingAnimationParameter);
			RegisterAnimatorParameter(_carryingAnimationParameterName, AnimatorControllerParameterType.Bool, out _carryingAnimationParameter);
			RegisterAnimatorParameter(_carryingIDAnimationParameterName, AnimatorControllerParameterType.Int, out _carryingIDAnimationParameter);
			RegisterAnimatorParameter(_throwingAnimationParameterName, AnimatorControllerParameterType.Bool, out _throwingAnimationParameter);
		}

		/// <summary>
		/// At the end of each cycle, we update our animator parameters with our current state
		/// </summary>
		public override void UpdateAnimator()
		{
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _grabbingAnimationParameter, Grabbing, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _throwingAnimationParameter, Throwing, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _carryingAnimationParameter, Carrying, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorInteger(_animator, _carryingIDAnimationParameter, CarryingID, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}

		/// <summary>
		/// On reset ability, we cancel all the changes made
		/// </summary>
		public override void ResetAbility()
		{
			base.ResetAbility();
			Throw();
			if (_animator != null)
			{
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _grabbingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _throwingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _carryingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			}
		}
	}
}