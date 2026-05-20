using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// This ability will apply damage or cause the death of the character when crushed.
	/// Getting crushed happens when your character is colliding on one direction, and being crushed by anything on the CrusherLayers
	/// from the opposite direction. So a character that is standing on a platform and gets a falling platform on its head is getting crushed.
	/// </summary>
	[MMHiddenProperties("AbilityStopFeedbacks")]
	[RequireComponent(typeof(Health))]
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Crush Detection")]
	public class CharacterCrushDetection : CharacterAbility
	{
		public override string HelpBoxText() { return "押しつぶされるとダメージまたは即死します。一方の方向に接触し、CrusherLayers から反対方向に押されると発生します。例：足場の上に立っているときに上から落ちるプラットフォームで押しつぶされます。"; }

		/// the possible ways to detect crushes
		public enum DetectionDirections { Vertical, Horizontal, Both }

		[Header("押しつぶし検出")]

		/// the layermask to look for crushing objects on
		[Tooltip("押しつぶしオブジェクトのレイヤーマスク")]
		[Header("押しつぶしオブジェクトのレイヤーマスク")]
		public LayerMask CrusherLayers = LayerManager.MovingObjectsLayerMask;
		/// the direction to look for crushing objects
		[Tooltip("検出方向")]
		[Header("検出方向")]
		public DetectionDirections DetectionDirection = DetectionDirections.Vertical;
		/// raycasts will be cast from the center of your character, towards the edges. This skin width is usually a small value, that will
		/// shorten the rays to avoid them detecting simply colliding walls/ceilings/grounds. If you're not sure, leave it at 0.02f
		[Tooltip("raycasts will be cast from the center of your character, towards the edges. This skin width is usually a small value, that will" +
		         "shorten the rays to avoid them detecting simply colliding walls/ceilings/grounds. If you're not sure, leave it at 0.02f")]
		[Header("押しつぶし検出スキン幅")]
		public float CrushDetectionSkinWidth = 0.02f;

		[Header("押しつぶし時")]

		/// whether the character should insta die when crushed or not
		[Tooltip("押しつぶし時に即死")]
		[Header("押しつぶし時に即死")]
		public bool DieWhenCrushed = true;
		/// whether damage should be applied to the character when crushed or not
		[Tooltip("押しつぶし時にダメージを与える")]
		[Header("押しつぶし時にダメージを与える")]
		public bool ApplyDamageWhenCrushed = false;
		/// the amount of damage to take per crush
		[MMCondition("ApplyDamageWhenCrushed", true)]
		[Tooltip("押しつぶし時のダメージ量")]
		[Header("押しつぶし時のダメージ量")]
		public float DamageTakenWhenCrushed = 10;
		/// how long the character should flicker when crushed
		[MMCondition("ApplyDamageWhenCrushed", true)]
		[Tooltip("ダメージ時の点滅持続時間")]
		[Header("ダメージ時の点滅持続時間")]
		public float DamageTakenFlickerDuration = 0.2f;
		/// how long (in seconds) the character should remain invincible after a crush
		[MMCondition("ApplyDamageWhenCrushed", true)]
		[Tooltip("ダメージ後の無敵持続時間")]
		[Header("ダメージ後の無敵持続時間")]
		public float DamageTakenInvincibilityDuration = 0.6f;
		/// an optional list of damage types to apply when crush damage kicks in
		[Tooltip("押しつぶしダメージタイプ一覧")]
		[Header("押しつぶしダメージタイプ一覧")]
		public List<TypedDamage> CrushDamageTypes;

		protected bool _crushedThisFrame = false;
		protected RaycastHit2D _hit;

		// animation parameters
		protected const string _crushedAnimationParameterName = "Crushed";
		protected int _crushedAnimationParameter;

		/// <summary>
		/// On initialization we grab our health comp
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			_health = this.gameObject.GetComponentInParent<Health>();
		}

		/// <summary>
		/// On Update, we check if we're taking flight, and if we should take damage
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();

			if (!AbilityAuthorized)
			{
				return;
			}

			DetectCrush();
			ApplyCrush();
		}

		/// <summary>
		/// Casts rays on the required sides, and updates the _crushedThisFrame bool
		/// </summary>
		protected virtual void DetectCrush()
		{
			_crushedThisFrame = false;
			if ((DetectionDirection == DetectionDirections.Both) || (DetectionDirection == DetectionDirections.Horizontal))
			{
				float length = _controller.Bounds.x / 2f - CrushDetectionSkinWidth;

				if (_controller.State.IsCollidingRight && DetectionRay(_controller.BoundsCenter, Vector3.left, length))
				{
					_crushedThisFrame = true;
				}
				if (_controller.State.IsCollidingLeft && DetectionRay(_controller.BoundsCenter, Vector3.right, length))
				{
					_crushedThisFrame = true;
				}                    
			}
			if ((DetectionDirection == DetectionDirections.Both) || (DetectionDirection == DetectionDirections.Vertical))
			{
				float length = _controller.Bounds.y / 2f - CrushDetectionSkinWidth;

				if (_controller.State.IsCollidingBelow && DetectionRay(_controller.BoundsCenter, Vector3.up, length))
				{
					_crushedThisFrame = true;
				}
				if (_controller.State.IsCollidingAbove && DetectionRay(_controller.BoundsCenter, Vector3.down, length))
				{
					_crushedThisFrame = true;
				}
			}
		}

		/// <summary>
		/// Casts a ray to look for crushing objects
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="direction"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		protected virtual bool DetectionRay(Vector3 origin, Vector3 direction, float length)
		{
			_hit = MMDebug.RayCast(origin, direction, length, CrusherLayers, Color.red, _controller.Parameters.DrawRaycastsGizmos);
			return _hit;
		}

		/// <summary>
		/// Handles a crush if needed
		/// </summary>
		protected virtual void ApplyCrush()
		{
			if ( !_crushedThisFrame
			     || (_character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead) )
			{
				return;
			}
			if (DieWhenCrushed )
			{
				_character.CharacterHealth.Kill();
			}
			if (ApplyDamageWhenCrushed)
			{
				_health.Damage(DamageTakenWhenCrushed, this.gameObject, DamageTakenFlickerDuration, DamageTakenInvincibilityDuration, Vector3.up, CrushDamageTypes);
			}
			if (!_startFeedbackIsPlaying)
			{
				PlayAbilityStartFeedbacks();
				MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Crush);
				if (this.gameObject.activeInHierarchy)
				{
					StartCoroutine(DelayedStopStartFeedbacks(DamageTakenInvincibilityDuration));	
				}
				else
				{
					StopStartFeedbacks();
				}
			}
		}

		protected virtual IEnumerator DelayedStopStartFeedbacks(float delay)
		{
			yield return MMCoroutine.WaitFor(delay);
			StopStartFeedbacks();
		}
        
		/// <summary>
		/// Adds required animator parameters to the animator parameters list if they exist
		/// </summary>
		protected override void InitializeAnimatorParameters()
		{
			RegisterAnimatorParameter(_crushedAnimationParameterName, AnimatorControllerParameterType.Bool, out _crushedAnimationParameter);
		}

		/// <summary>
		/// At the end of each cycle, we send our character's animator the current fall damage status
		/// </summary>
		public override void UpdateAnimator()
		{
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _crushedAnimationParameter, _crushedThisFrame, _character._animatorParameters, _character.PerformAnimatorSanityChecks);            
		}
	}
}