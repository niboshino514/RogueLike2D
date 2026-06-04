using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// This component will apply damage to the character if it falls from a height higher than the specified MinimumDamageFallHeight
	/// How much damage that is will be remapped between the specified min and max damage values.
	/// Animation parameter : FallDamage, bool, true the frame the character takes fall damage
	/// </summary>
	[MMHiddenProperties("AbilityStopFeedbacks")]
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Fall Damage")]
	public class CharacterFallDamage : CharacterAbility
	{
		public override string HelpBoxText() { return "指定の MinimumDamageFallHeight より高い場所から落ちるとダメージを与えます。最小／最大落下高でリマップ規則を定義し、最大ダメージでクランプするか比例のみかを選べます。"; }

		[Header("落下高さ")]
		/// the minimum height at which a character has to fall for damage to be applied
		[Header("最小ダメージ落下高さ")]
		[Tooltip("最大ダメージ落下高さ")]
		public float MinimumDamageFallHeight = 5f;
		/// the height at which you'd have to fall to apply the highest damage
		[Header("最大ダメージ落下高さ")]
		[Tooltip("落下ダメージ")]
		public float MaximumDamageFallHeight = 10f;

		[Header("落下ダメージ")]
		/// the damage to apply when falling from the min height
		[Header("最小ダメージ")]
		[Tooltip("最大ダメージ")]
		public float MinimumDamage = 10f;
		/// the damage to apply when falling from the max height
		[Header("最大ダメージ")]
		[Tooltip("ダメージ上限あり")]
		public float MaximumDamage = 50f;
		/// whether or not to clamp the damage to MaximumDamage. If not clamped, falling from an even higher height will apply even more damage.
		[Header("ダメージ上限あり")]
		[Tooltip("ダメージ種別")]
		public bool ClampedDamage = true;

		[Header("ダメージ種別")]
		/// an optional list of damage types to apply when fall damage kicks in
		[Header("落下ダメージ種別リスト")]
		[Tooltip("速度")]
		public List<TypedDamage> FallDamageTypes;

		[Header("速度")]
		/// the minimum (absolute) velocity the character has to hit the ground at for damage to apply
		[Header("ダメージ速度しきい値")]
		[Tooltip("the minimum (absolute) velocity the character has to hit the ground at for damage to apply")]
		public float DamageVelocityThreshold = 5f;
		
		protected bool _airborneLastFrame = false;
		protected bool _damageThisFrame = false;
		protected float _highestAltitudeY = 0f;
		protected float _verticalVelocityLastFrame = 0f;
		protected float _altitudeDelta;
		
		// animation parameters
		protected const string _fallDamageAnimationParameterName = "FallDamage";
		protected int _fallDamageAnimationParameter;

		/// <summary>
		/// On init we initialize our altitude
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			StoreCurrentAltitudeAsHighest();
		}
		
		/// <summary>
		/// On Update, we check our altitude
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();
			_damageThisFrame = false;
			ProcessAirborne();
			ResetTakeOffAltitude();
			// if we were airborne and are not anymore, we just touched the ground
			if (CanTakeDamage())
			{
				ApplyDamage(_altitudeDelta);
			}
			_verticalVelocityLastFrame = _controller.Speed.y;
			_airborneLastFrame = _character.Airborne;
		}

		/// <summary>
		/// Goes through various conditions that could prevent damage
		/// </summary>
		/// <returns></returns>
		public virtual bool CanTakeDamage()
		{
			if (!InDamageableState())
			{
				return false;
			}
			if (_character.Airborne || !_airborneLastFrame)
			{
				return false;
			}
			
			if (!HighEnoughToGetDamaged())
			{
				return false;
			}
			
			if (!FastEnoughToGetDamaged())
			{
				return false;
			}
			
			if (!OtherConditions())
			{
				return false;
			}
			
			return true;
		}

		/// <summary>
		/// Returns true if the character is falling from high enough to take damage
		/// </summary>
		/// <returns></returns>
		public virtual bool HighEnoughToGetDamaged()
		{
			 _altitudeDelta = _highestAltitudeY - this.transform.position.y;
			 return (_altitudeDelta > MinimumDamageFallHeight);
		}

		/// <summary>
		/// Returns true if the character is hitting the ground fast enough to take damage, false otherwise
		/// </summary>
		/// <returns></returns>
		public virtual bool FastEnoughToGetDamaged()
		{
			return (Mathf.Abs(_verticalVelocityLastFrame) > DamageVelocityThreshold);
		}

		/// <summary>
		/// Override this to implement other conditions that could prevent damage from being applied
		/// </summary>
		/// <returns></returns>
		protected virtual bool OtherConditions()
		{
			return true;
		}

		/// <summary>
		/// Call this method to force an altitude reset
		/// </summary>
		public virtual void StoreCurrentAltitudeAsHighest()
		{
			_highestAltitudeY = this.transform.position.y;	
		}

		/// <summary>
		/// Every frame, we check if we're in a state that should reset the altitude (fall, glide to the ground, touch the ground shouldn't trigger damage, for example)
		/// </summary>
		public virtual void ResetTakeOffAltitude()
		{
			if (!InDamageableState())
			{
				StoreCurrentAltitudeAsHighest();
			}
		}

		/// <summary>
		/// Processes airborne state and stores altitude if needed
		/// </summary>
		protected virtual void ProcessAirborne()
		{
			// if we were not airborne last frame and are now, we're taking off, we log that altitude
			if (_character.Airborne)
			{
				if (this.transform.position.y > _highestAltitudeY)
				{
					StoreCurrentAltitudeAsHighest();
				}
			}
			else
			{
				if (!_airborneLastFrame)
				{
					StoreCurrentAltitudeAsHighest();
				}
			}
		}

		/// <summary>
		/// This method returns true if the character is in a state that can take damage.
		/// Don't hesitate to extend and override this method to specify your own rules
		/// </summary>
		/// <returns></returns>
		protected virtual bool InDamageableState()
		{
			return (_character.MovementState.CurrentState != CharacterStates.MovementStates.LadderClimbing
			        && _character.MovementState.CurrentState != CharacterStates.MovementStates.SwimmingIdle
			        && _character.MovementState.CurrentState != CharacterStates.MovementStates.Diving
			        && _character.MovementState.CurrentState != CharacterStates.MovementStates.Flying
			        && _character.MovementState.CurrentState != CharacterStates.MovementStates.Gliding
			        && _character.MovementState.CurrentState != CharacterStates.MovementStates.WallClinging
			        && _character.MovementState.CurrentState != CharacterStates.MovementStates.Jetpacking
			        && _character.ConditionState.CurrentState != CharacterStates.CharacterConditions.Frozen
			        && _character.ConditionState.CurrentState != CharacterStates.CharacterConditions.Dead
			        && _character.ConditionState.CurrentState != CharacterStates.CharacterConditions.ControlledMovement);
		}

		/// <summary>
		/// Applies fall damage
		/// </summary>
		/// <param name="distance"></param>
		protected virtual void ApplyDamage(float distance)
		{
			int damageToApply = (int)Mathf.Round(MMMaths.Remap(distance, MinimumDamageFallHeight, MaximumDamageFallHeight,
				MinimumDamage, MaximumDamage));
			if (ClampedDamage)
			{
				damageToApply = (int)Mathf.Clamp(damageToApply, MinimumDamage, MaximumDamage);
			}

			
			PlayAbilityStartFeedbacks();
			MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.FallDamage);
			
			_health.Damage(damageToApply, this.gameObject, 0.2f, 0.2f, Vector3.up, FallDamageTypes);
			_damageThisFrame = true;
		}

		/// <summary>
		/// On respawn we reset our take off altitude
		/// </summary>
		protected override void OnRespawn()
		{
			base.OnDeath();
			StoreCurrentAltitudeAsHighest();
		}
        
		/// <summary>
		/// Adds required animator parameters to the animator parameters list if they exist
		/// </summary>
		protected override void InitializeAnimatorParameters()
		{
			RegisterAnimatorParameter(_fallDamageAnimationParameterName, AnimatorControllerParameterType.Bool, out _fallDamageAnimationParameter);
		}

		/// <summary>
		/// At the end of each cycle, we send our character's animator the current fall damage status
		/// </summary>
		public override void UpdateAnimator()
		{
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _fallDamageAnimationParameter, _damageThisFrame, _character._animatorParameters, _character.PerformAnimatorSanityChecks);            
		}
	}
}