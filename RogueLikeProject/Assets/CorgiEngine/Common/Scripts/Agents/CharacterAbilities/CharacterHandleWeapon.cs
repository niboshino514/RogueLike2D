using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{	
	/// <summary>
	/// Add this class to a character so it can use weapons
	/// Note that this component will trigger animations (if their parameter is present in the Animator), based on 
	/// the current weapon's Animations
	/// Animator parameters : defined from the Weapon's inspector
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Handle Weapon")] 
	public class CharacterHandleWeapon : CharacterAbility 
	{
		/// This method is only used to display a helpbox text at the beginning of the ability's inspector
		public override string HelpBoxText() { return "武器の拾得と使用を可能にします。武器の挙動は Weapon クラスで定義され、ここでは武器を「持つ手」の挙動のみを設定します。初期武器、拾得許可、WeaponAttachment（キャラ内の Transform）を設定できます。"; }

		[Header("武器")]

		/// the initial weapon owned by the character
		[Header("初期武器")]
		[Tooltip("武器を拾える")]
		public Weapon InitialWeapon;
		/// if this is set to true, the character can pick up PickableWeapons
		[Header("武器を拾える")]
		[Tooltip("バインド")]
		public bool CanPickupWeapons = true;

		[Header("バインド")]

		/// the position the weapon will be attached to. If left blank, will be this.transform.
		[Header("武器アタッチメント")]
		[Tooltip("装備時スケールリセット")]
		public Transform WeaponAttachment;
		/// if this is true, the weapon's scale will be forced to 1,1,1 when equipped
		[Header("装備時スケールリセット")]
		[Tooltip("装備時回転リセット")]
		public bool ForceWeaponScaleResetOnEquip = false;
		/// if this is true, the weapon's rotation will be forced to Identity when equipped
		[Header("装備時回転リセット")]
		[Tooltip("自動アニメーターバインド")]
		public bool ForceWeaponRotationResetOnEquip = false;
		/// if this is true this animator will be automatically bound to the weapon
		[Header("自動アニメーターバインド")]
		[Tooltip("弾薬表示ID")]
		public bool AutomaticallyBindAnimator = true;
		/// the ID of the AmmoDisplay this ability should update
		[Header("弾薬表示ID")]
		[Tooltip("入力と自動化")]
		public int AmmoDisplayID = 0;

		[Header("入力と自動化")]

		/// if this is true you won't have to release your fire button to auto reload
		[Header("連続押しで自動リロード")]
		[Tooltip("被弾で攻撃中断")]
		public bool ContinuousPress = false;
		/// whether or not this character getting hit should interrupt its attack (will only work if the weapon is marked as interruptable)
		[Header("被弾で攻撃中断")]
		[Tooltip("はしご中に射撃可能")]
		public bool GettingHitInterruptsAttack = false;
		/// whether or not this character is allowed to shoot while on a ladder)
		[Header("はしご中に射撃可能")]
		[Tooltip("武器方向を向く")]
		public bool CanShootFromLadders = false;
		/// if this is set to true, the character will be forced to face the current weapon direction
		[Header("武器方向を向く")]
		[Tooltip("壁つかみ中に水平狙いを反転")]
		public bool FaceWeaponDirection = false;
		/// if this is true, horizontal aim will be inverted when shooting while wallclinging, to shoot away from the wall
		[Header("壁つかみ中に水平狙いを反転")]
		[Tooltip("常時射撃強制")]
		public bool InvertHorizontalAimWhenWallclinging = false;
		/// if this is true, the character will continuously fire its weapon
		[Header("常時射撃強制")]
		[Tooltip("バッファリング")]
		public bool ForceAlwaysShoot = false;

		[Header("バッファリング")]

		/// whether or not attack input should be buffered, letting you prepare an attack while another is being performed, making it easier to chain them
		[Header("入力をバッファリング")]
		[Tooltip("新入力でバッファ延長")]
		public bool BufferInput;
		/// if this is true, every new input will prolong the buffer
		[MMCondition("BufferInput", true)]
		[Header("新入力でバッファ延長")]
		[Tooltip("バッファ最大持続時間")]
		public bool NewInputExtendsBuffer;
		/// the maximum duration for the buffer, in seconds
		[MMCondition("BufferInput", true)]
		[Header("バッファ最大持続時間")]
		[Tooltip("デバッグ")]
		public float MaximumBufferDuration = 0.25f;

		[Header("デバッグ")]
		/// returns the current equipped weapon
		[MMReadOnly]
		[Header("現在の武器")]
		[Tooltip("returns the current equipped weapon")]
		public Weapon CurrentWeapon;

		/// the ID / index of this CharacterHandleWeapon. This will be used to determine what handle weapon ability should equip a weapon.
		/// If you create more Handle Weapon abilities, make sure to override and increment this  
		public virtual int HandleWeaponID { get { return 1; } }

		public Animator CharacterAnimator { get; set; }

		protected float _fireTimer = 0f;
		protected float _secondaryHorizontalMovement;
		protected float _secondaryVerticalMovement;
		protected WeaponAim _aimableWeapon;
		protected ProjectileWeapon _projectileWeapon;
		protected WeaponIK _weaponIK;
		protected Transform _leftHandTarget = null;
		protected Transform _rightHandTarget = null;

		protected float _bufferEndsAt = 0f;
		protected bool _buffering = false;
		protected bool _charHztlMvmtFlipInitialSetting;
		protected bool _charHztlMvmtFlipInitialSettingSet = false;
		protected Vector2 _invertedHorizontalAimMultiplier = new Vector2(-1f, 1f);

		// Initialization
		protected override void Initialization () 
		{
			base.Initialization();
			if (_characterHorizontalMovement != null)
			{
				_charHztlMvmtFlipInitialSetting = _characterHorizontalMovement.FlipCharacterToFaceDirection;
			}
			Setup ();
		}

		/// <summary>
		/// Grabs various components and inits stuff
		/// </summary>
		public virtual void Setup()
		{
			_character = gameObject.GetComponentInParent<Character>();
			CharacterAnimator = _animator;
            
			// filler if the WeaponAttachment has not been set
			if (WeaponAttachment==null)
			{
				WeaponAttachment=transform;
			}		
			if (_animator != null)
			{
				_weaponIK = _animator.GetComponent<WeaponIK> ();
			}	
			// we set the initial weapon
			if (InitialWeapon != null)
			{
				ChangeWeapon(InitialWeapon, null);
			}
		}

		/// <summary>
		/// Every frame we check if it's needed to update the ammo display
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility ();
			UpdateAmmoDisplay (); 
			HandleBuffer();
			HandleFacingDirection();
			HandleWeaponStop();
		}

		/// <summary>
		/// Checks for state changes to trigger stop feedbacks
		/// </summary>
		protected virtual void HandleWeaponStop()
		{
			if (CurrentWeapon == null)
			{
				return;
			}

			if (CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponStop)
			{
				PlayAbilityStopFeedbacks();	
			}
		}

		/// <summary>
		/// If FaceWeaponDirection is true, will force the character to face the weapon direction
		/// </summary>
		protected virtual void HandleFacingDirection()
		{
			if ((_characterHorizontalMovement != null) && FaceWeaponDirection && (_aimableWeapon != null))
			{
				_characterHorizontalMovement.FlipCharacterToFaceDirection = false;
				_charHztlMvmtFlipInitialSettingSet = true;
			}

			if (_charHztlMvmtFlipInitialSettingSet && (_aimableWeapon == null))
			{
				_characterHorizontalMovement.FlipCharacterToFaceDirection = _charHztlMvmtFlipInitialSetting;
			}
            
			
			
			if (InvertHorizontalAimWhenWallclinging && (_movement.CurrentState == CharacterStates.MovementStates.WallClinging))
			{
				if (_aimableWeapon != null)
				{
					_aimableWeapon.CurrentAimMultiplier = _invertedHorizontalAimMultiplier;	
				}

				if (_projectileWeapon != null)
				{
					_projectileWeapon.WallClinging = true;
				}
			}

			// if we're not in FaceWeaponDirection mode, if we don't have a HztalMvmt ability, or a weapon aim, we do nothing and exit
			if (!FaceWeaponDirection || (_characterHorizontalMovement == null) || (_aimableWeapon == null))
			{
				return;
			}

			if ((_aimableWeapon.CurrentAngleRelative < -90f) || (_aimableWeapon.CurrentAngleRelative > 90f))
			{
				_character.Flip();
			}
		}

		/// <summary>
		/// Gets input and triggers methods based on what's been pressed
		/// </summary>
		protected override void HandleInput ()
		{
			bool shootFromLaddersAuthorized = (CanShootFromLadders &&
			                                   (_movement.CurrentState ==
			                                    CharacterStates.MovementStates.LadderClimbing));
			
			if (!AbilityAuthorized
			    || ((_condition.CurrentState != CharacterStates.CharacterConditions.Normal)
			         && !shootFromLaddersAuthorized)
			    || (CurrentWeapon == null))
			{
				return;
			}

			if (ForceAlwaysShoot)
			{
				ShootStart();
			}

			if ((_inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown) || (_inputManager.ShootAxis == MMInput.ButtonStates.ButtonDown))
			{
				ShootStart();
			}

			bool buttonPressed =
				(_inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed) ||
				(_inputManager.ShootAxis == MMInput.ButtonStates.ButtonPressed); 

			if (ContinuousPress && (CurrentWeapon.TriggerMode == Weapon.TriggerModes.Auto) && buttonPressed)
			{
				ShootStart();
			}

			if (_inputManager.ReloadButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
			{
				Reload();
			}

			if ((_inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonUp) || (_inputManager.ShootAxis == MMInput.ButtonStates.ButtonUp))
			{
				ShootStop();
				CurrentWeapon.WeaponInputReleased();
			}

			if (CurrentWeapon != null)
			{
				if ((CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponDelayBetweenUses)
				    && ((_inputManager.ShootAxis == MMInput.ButtonStates.Off) && (_inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.Off)))
				{
					CurrentWeapon.WeaponInputStop();
				}
			}            
		}

		/// <summary>
		/// Triggers an attack if the weapon is idle and an input has been buffered
		/// </summary>
		protected virtual void HandleBuffer()
		{
			if (CurrentWeapon == null)
			{
				return;
			}

			// if we are currently buffering an input and if the weapon is now idle
			if (_buffering && (CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponIdle))
			{
				// and if our buffer is still valid, we trigger an attack
				if (Time.time < _bufferEndsAt)
				{
					ShootStart();
				}
				_buffering = false;
			}
		}
						
		/// <summary>
		/// Causes the character to start shooting
		/// </summary>
		public virtual void ShootStart()
		{
			// if the Shoot action is enabled in the permissions, we continue, if not we do nothing.  If the player is dead we do nothing.
			if ( !AbilityAuthorized
			     || (CurrentWeapon == null)
			     || ((_condition.CurrentState != CharacterStates.CharacterConditions.Normal) && (_condition.CurrentState != CharacterStates.CharacterConditions.ControlledMovement)))
			{
				return;
			}

			if (!CanShootFromLadders && (_movement.CurrentState == CharacterStates.MovementStates.LadderClimbing))
			{
				return;
			}

			//  if we've decided to buffer input, and if the weapon is in use right now
			if (BufferInput && (CurrentWeapon.WeaponState.CurrentState != Weapon.WeaponStates.WeaponIdle))
			{
				// if we're not already buffering, or if each new input extends the buffer, we turn our buffering state to true
				if (!_buffering || NewInputExtendsBuffer)
				{
					_buffering = true;
					_bufferEndsAt = Time.time + MaximumBufferDuration;
				}
			}

			PlayAbilityStartFeedbacks();
			MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.HandleWeapon, MMCharacterEvent.Moments.Start);
			CurrentWeapon.WeaponInputStart();
		}
		
		/// <summary>
		/// Causes the character to stop shooting
		/// </summary>
		public virtual void ShootStop()
		{
			// if the Shoot action is enabled in the permissions, we continue, if not we do nothing
			if (!AbilityAuthorized
			    || (CurrentWeapon == null)
			    || (_movement == null))
			{
				return;		
			}		

			if (!CanShootFromLadders && _movement.CurrentState == CharacterStates.MovementStates.LadderClimbing && CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponIdle)
			{
				return;
			}

			if ((CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponReload)
			    || (CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponReloadStart)
			    || (CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponReloadStop)
			    || (CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponUse)
			    || (CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponInCooldown))
			{
				return;
			}

			if ((CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponDelayBeforeUse) && (!CurrentWeapon.DelayBeforeUseReleaseInterruption))
			{
				return;
			}

			if ((CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponDelayBetweenUses) && (!CurrentWeapon.TimeBetweenUsesReleaseInterruption))
			{
				return;
			}

			StopStartFeedbacks();
			MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.HandleWeapon, MMCharacterEvent.Moments.End);
			CurrentWeapon.TurnWeaponOff();
		}
		
		/// <summary>
		/// A method used (usually by AIs) to force the weapon to stop
		/// </summary>
		public virtual void ForceStop()
		{
			StopStartFeedbacks();
			MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.HandleWeapon, MMCharacterEvent.Moments.End);
			CurrentWeapon?.TurnWeaponOff();
		}

		/// <summary>
		/// Reloads the weapon
		/// </summary>
		public virtual void Reload()
		{
			if (CurrentWeapon != null)
			{
				CurrentWeapon.InitiateReloadWeapon ();
			}
		}
		
		/// <summary>
		/// Changes the character's current weapon to the one passed as a parameter
		/// </summary>
		/// <param name="newWeapon">The new weapon.</param>
		public virtual void ChangeWeapon(Weapon newWeapon, string weaponID, bool combo = false)
		{
			// if the character already has a weapon, we make it stop shooting
			if (CurrentWeapon != null)
			{
				CurrentWeapon.ResetComboAnimatorParameter();

				if (!combo)
				{
					ShootStop();
					if (_character._animator != null)
					{
						AnimatorControllerParameter[] parameters = _character._animator.parameters;
						foreach(AnimatorControllerParameter parameter in parameters)
						{
							if (parameter.name == CurrentWeapon.EquippedAnimationParameter)
							{
								MMAnimatorExtensions.UpdateAnimatorBool(_animator, CurrentWeapon.EquippedAnimationParameter, false);
							}
						}
					}

					Destroy(CurrentWeapon.gameObject);
				}
			}
            
			if (newWeapon != null)
			{			
				if (!combo)
				{
					CurrentWeapon = (Weapon)Instantiate(newWeapon, WeaponAttachment.transform.position + newWeapon.WeaponAttachmentOffset, Quaternion.identity, WeaponAttachment.transform); 
				}				
				if (ForceWeaponScaleResetOnEquip)
				{
					CurrentWeapon.transform.localScale = Vector3.one;
				}
				if (ForceWeaponRotationResetOnEquip)
				{
					CurrentWeapon.transform.localRotation = Quaternion.identity;    
				}
                
				CurrentWeapon.SetOwner (_character, this);
				CurrentWeapon.WeaponID = weaponID;
				_aimableWeapon = CurrentWeapon.GetComponent<WeaponAim>();
				_projectileWeapon = CurrentWeapon.GetComponent<ProjectileWeapon>();
				// we handle (optional) inverse kinematics (IK) 
				if (_weaponIK != null)
				{
					_weaponIK.SetHandles(CurrentWeapon.LeftHandHandle, CurrentWeapon.RightHandHandle);
				}
				// we turn off the gun's emitters.
				CurrentWeapon.Initialization();
				CurrentWeapon.InitializeComboWeapons();
				CurrentWeapon.InitializeAnimatorParameters();
				InitializeAnimatorParameters();
				if ((_character != null) && !combo)
				{
					if (!_character.IsFacingRight)
					{
						if (CurrentWeapon != null)
						{
							CurrentWeapon.FlipWeapon();
							CurrentWeapon.FlipWeaponModel();
						}
					}
				}				
			}
			else
			{
				CurrentWeapon = null;
			}
		}	

		/// <summary>
		/// Flips the current weapon if needed
		/// </summary>
		public override void Flip()
		{
			if (CurrentWeapon != null)
			{
				CurrentWeapon.FlipWeapon();
				if (CurrentWeapon.FlipWeaponOnCharacterFlip)
				{
					CurrentWeapon.FlipWeaponModel();
				}
			}
		}

		/// <summary>
		/// Updates the ammo display bar and text.
		/// </summary>
		public virtual void UpdateAmmoDisplay()
		{
			if ( (GUIManager.HasInstance) && (_character.CharacterType == Character.CharacterTypes.Player) )
			{
				if (CurrentWeapon == null)
				{
					GUIManager.Instance.SetAmmoDisplays (false, _character.PlayerID, AmmoDisplayID);
					return;
				}

				if (!CurrentWeapon.MagazineBased && (CurrentWeapon.WeaponAmmo == null))
				{
					GUIManager.Instance.SetAmmoDisplays (false, _character.PlayerID, AmmoDisplayID);
					return;
				}

				if (CurrentWeapon.WeaponAmmo == null)
				{					
					GUIManager.Instance.SetAmmoDisplays (true, _character.PlayerID, AmmoDisplayID);
					GUIManager.Instance.UpdateAmmoDisplays(CurrentWeapon.MagazineBased, 0, 0, CurrentWeapon.CurrentAmmoLoaded, CurrentWeapon.MagazineSize, _character.PlayerID, AmmoDisplayID, false);	
					return;
				}
				else
				{
					GUIManager.Instance.SetAmmoDisplays (true, _character.PlayerID, AmmoDisplayID);
					GUIManager.Instance.UpdateAmmoDisplays(CurrentWeapon.MagazineBased, CurrentWeapon.WeaponAmmo.CurrentAmmoAvailable + CurrentWeapon.CurrentAmmoLoaded, CurrentWeapon.WeaponAmmo.MaxAmmo, CurrentWeapon.CurrentAmmoLoaded, CurrentWeapon.MagazineSize, _character.PlayerID, AmmoDisplayID, true);
					return;
				}
			}
		}
		
		/// <summary>
		/// On respawn we setup our weapon again
		/// </summary>
		protected override void OnRespawn()
		{
			base.OnRespawn();
			Setup();
		}
        
		/// <summary>
		/// On hit we interrupt our weapon if needed
		/// </summary>
		protected override void OnHit()
		{
			base.OnHit();
			if (GettingHitInterruptsAttack && (CurrentWeapon != null))
			{
				CurrentWeapon.Interrupt();
			}
		}

		/// <summary>
		/// On death we stop shooting if needed
		/// </summary>
		protected override void OnDeath()
		{
			base.OnDeath();
			ShootStop();
		}

		/// <summary>
		/// On reset ability, we cancel all the changes made
		/// </summary>
		public override void ResetAbility()
		{
			base.ResetAbility();
			if (_condition.CurrentState == CharacterStates.CharacterConditions.Normal)
			{
				ShootStop();
			}
		}
	}
}