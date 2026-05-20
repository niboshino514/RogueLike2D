using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
	[MMHiddenProperties("AbilityStartFeedbacks", "AbilityStopFeedbacks")]
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Particles")] 
	/// <summary>
	/// Add this component to a Character and bind ParticleSystems to it to have it emit particles when certain states are active.
	/// You can have a look at the RetroCorgi demo character for examples of how to use it.
	/// </summary>
	public class CharacterParticles : CharacterAbility 
	{
		[Header("キャラクターパーティクル")]

		/// the particle system to use when the character is idle
		[Tooltip("待機パーティクル")]
		[Header("待機パーティクル")]
		public ParticleSystem IdleParticles;
		/// the particle system to use when the character is walking
		[Tooltip("歩行パーティクル")]
		[Header("歩行パーティクル")]
		public ParticleSystem WalkingParticles;
		/// the particle system to use when the character is crouching
		[Tooltip("しゃがみパーティクル")]
		[Header("しゃがみパーティクル")]
		public ParticleSystem CrouchingParticles;
		/// the particle system to use when the character is crawling
		[Tooltip("這いパーティクル")]
		[Header("這いパーティクル")]
		public ParticleSystem CrawlingParticles;
		/// the particle system to use when the character is dangling
		[Tooltip("ぶら下がりパーティクル")]
		[Header("ぶら下がりパーティクル")]
		public ParticleSystem DanglingParticles;
		/// the particle system to use when the character is dashing
		[Tooltip("ダッシュパーティクル")]
		[Header("ダッシュパーティクル")]
		public ParticleSystem DashingParticles;
		/// the particle system to use when the character is diving
		[Tooltip("急降下パーティクル")]
		[Header("急降下パーティクル")]
		public ParticleSystem DivingParticles;
		/// the particle system to use when the character is gripping
		[Tooltip("しがみつきパーティクル")]
		[Header("しがみつきパーティクル")]
		public ParticleSystem GrippingParticles;
		/// the particle system to use when the character is jetpacking
		[Tooltip("ジェットパックパーティクル")]
		[Header("ジェットパックパーティクル")]
		public ParticleSystem JetpackingParticles;
		/// the particle system to use when the character is jumping
		[Tooltip("ジャンプパーティクル")]
		[Header("ジャンプパーティクル")]
		public ParticleSystem JumpingParticles;
		/// the particle system to use when the character is on a ladder
		[Tooltip("梯子パーティクル")]
		[Header("梯子パーティクル")]
		public ParticleSystem LadderParticles;
		/// the particle system to use when the character is looking up
		[Tooltip("見上げパーティクル")]
		[Header("見上げパーティクル")]
		public ParticleSystem LookupParticles;
		/// the particle system to use when the character is pushing
		[Tooltip("押しパーティクル")]
		[Header("押しパーティクル")]
		public ParticleSystem PushParticles;
		/// the particle system to use when the character is running
		[Tooltip("走行パーティクル")]
		[Header("走行パーティクル")]
		public ParticleSystem RunParticles;
		/// the particle system to use when the character is wallclinging
		[Tooltip("壁張り付きパーティクル")]
		[Header("壁張り付きパーティクル")]
		public ParticleSystem WallclingingParticles;
		/// the particle system to use when the character is walljumping
		[Tooltip("壁ジャンプパーティクル")]
		[Header("壁ジャンプパーティクル")]
		public ParticleSystem WalljumpParticles;

		protected ParticleSystem.EmissionModule _emissionModule;
		protected CharacterStates.MovementStates _stateLastFrame;

		/// <summary>
		/// On update we go through all our particle systems and activate them if needed
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility ();
			HandleParticleSystem (IdleParticles, CharacterStates.MovementStates.Idle);
			HandleParticleSystem (WalkingParticles, CharacterStates.MovementStates.Walking);
			HandleParticleSystem (CrouchingParticles, CharacterStates.MovementStates.Crouching);
			HandleParticleSystem (CrawlingParticles, CharacterStates.MovementStates.Crawling);
			HandleParticleSystem (DanglingParticles, CharacterStates.MovementStates.Dangling);
			HandleParticleSystem (DashingParticles, CharacterStates.MovementStates.Dashing);
			HandleParticleSystem (DivingParticles, CharacterStates.MovementStates.Diving);
			HandleParticleSystem (GrippingParticles, CharacterStates.MovementStates.Gripping);
			HandleParticleSystem (JetpackingParticles, CharacterStates.MovementStates.Jetpacking);
			HandleParticleSystem (JumpingParticles, CharacterStates.MovementStates.Jumping);
			HandleParticleSystem (LadderParticles, CharacterStates.MovementStates.LadderClimbing);
			HandleParticleSystem (LookupParticles, CharacterStates.MovementStates.LookingUp);
			HandleParticleSystem (PushParticles, CharacterStates.MovementStates.Pushing);
			HandleParticleSystem (RunParticles, CharacterStates.MovementStates.Running);
			HandleParticleSystem (WallclingingParticles, CharacterStates.MovementStates.WallClinging);
			HandleParticleSystem (WalljumpParticles, CharacterStates.MovementStates.WallJumping);
			_stateLastFrame = _movement.CurrentState;
		}

		/// <summary>
		/// Checks if the specified state is active, and if yes, triggers the particle system's emission
		/// </summary>
		/// <param name="system">System.</param>
		/// <param name="state">State.</param>
		protected virtual void HandleParticleSystem(ParticleSystem system, CharacterStates.MovementStates state)
		{
			if (system == null)
			{
				return;
			}
			if (_movement.CurrentState == state)
			{
				if (!system.main.loop && _stateLastFrame != state)
				{
					system.Clear ();
					system.Play ();
				}
				_emissionModule = system.emission;
				_emissionModule.enabled = true;
			} 
			else
			{
				_emissionModule = system.emission;
				_emissionModule.enabled = false;
			}
		}
	}
}