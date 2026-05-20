using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// Add this ability to a Character with a CharacterGravity ability, and it will automatically compute the current slope's angle and change the gravity's direction to match the slope normal
	/// This is an experimental ability, results may vary depending on speed and layout, it will likely be bouncy
	/// 
	/// Animator parameters : none
	/// </summary>
	[RequireComponent(typeof(CharacterGravity))]
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Ground Normal Gravity")]
	public class CharacterGroundNormalGravity : CharacterAbility
	{
		public override string HelpBoxText() { return "現在の坂の角度を自動計算し、重力アビリティの方向を坂の法線に合わせます。"; }

		/// the length of the raycast used to detect slope angle
		[Tooltip("下方レイキャスト長")]
		[Header("下方レイキャスト長")]
		public float DownwardsRaycastLength = 5f;
		/// if this is true, slope angle will only be detected if grounded
		[Tooltip("地面接触時のみ検出")]
		[Header("地面接触時のみ検出")]
		public bool OnlyWhenGrounded = false;
        
		protected RaycastHit2D _raycastCenter;
		protected RaycastHit2D _raycastLeft;
		protected RaycastHit2D _raycastRight;
    
		/// <summary>
		/// On ProcessAbility, we cast a ray downwards, compute its angle, and apply it to the gravity ability
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();

			if (!AbilityAuthorized)
			{
				return;
			}

			if (OnlyWhenGrounded && !_controller.State.IsGrounded)
			{
				return;
			}

			_raycastCenter = MMDebug.RayCast (_controller.BoundsCenter,-_controller.transform.up, DownwardsRaycastLength, _controller.PlatformMask, Color.blue, _controller.Parameters.DrawRaycastsGizmos);
			_raycastLeft = MMDebug.RayCast (_controller.BoundsBottomLeftCorner,-_controller.transform.up, DownwardsRaycastLength, _controller.PlatformMask, Color.blue, _controller.Parameters.DrawRaycastsGizmos);
			_raycastRight = MMDebug.RayCast (_controller.BoundsBottomRightCorner,-_controller.transform.up, DownwardsRaycastLength, _controller.PlatformMask, Color.blue, _controller.Parameters.DrawRaycastsGizmos);

			if (_raycastCenter)
			{
				float angleCenter = MMMaths.AngleBetween(_raycastCenter.normal, Vector2.up);
				float angleLeft = MMMaths.AngleBetween(_raycastLeft.normal, Vector2.up);
				float angleRight = MMMaths.AngleBetween(_raycastRight.normal, Vector2.up);

				if (angleLeft == angleRight)
				{
					_characterGravity.SetGravityAngle(angleCenter);    
				}
			}
		}
	}
}