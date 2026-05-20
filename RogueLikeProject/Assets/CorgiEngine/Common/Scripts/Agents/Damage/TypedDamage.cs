using System;
using MoreMountains.Tools;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// A class used to store and define typed damage impact : damage caused, condition or movement speed changes, etc
	/// </summary>
	[Serializable]
	public class TypedDamage 
	{
		/// the type of damage associated to this definition
		[Tooltip("関連ダメージタイプ")]
		[Header("関連ダメージタイプ")]
		public DamageType AssociatedDamageType;
		/// The min amount of health to remove from the player's health
		[Tooltip("最小ダメージ量")]
		[Header("最小ダメージ量")]
		public float MinDamageCaused = 10f;
		/// The max amount of health to remove from the player's health
		[Tooltip("最大ダメージ量")]
		[Header("最大ダメージ量")]
		public float MaxDamageCaused = 10f;

		/// whether or not this damage, when applied, should force the character into a specified condition
		[Tooltip("キャラクター状態を強制")]
		[Header("キャラクター状態を強制")]
		public bool ForceCharacterCondition = false;
		/// when in forced character condition mode, the condition to which to swap
		[Tooltip("強制状態")]
		[MMCondition("ForceCharacterCondition", true)]
		[Header("強制状態")]
		public CharacterStates.CharacterConditions ForcedCondition;
		/// when in forced character condition mode, whether or not to disable gravity
		[Tooltip("重力を無効化")]
		[MMCondition("ForceCharacterCondition", true)]
		[Header("重力を無効化")]
		public bool DisableGravity = false;
		/// when in forced character condition mode, whether or not to reset controller forces
		[Tooltip("コントローラー力をリセット")]
		[MMCondition("ForceCharacterCondition", true)]
		[Header("コントローラー力をリセット")]
		public bool ResetControllerForces = false;
		/// when in forced character condition mode, the duration of the effect, after which condition will be reverted
		[Tooltip("強制状態持続時間")]
		[MMCondition("ForceCharacterCondition", true)]
		[Header("強制状態持続時間")]
		public float ForcedConditionDuration = 3f;

		/// whether or not to apply a movement multiplier to the damaged character
		[Tooltip("移動乗数を適用")]
		[Header("移動乗数を適用")]
		public bool ApplyMovementMultiplier = false;
		/// the movement multiplier to apply when ApplyMovementMultiplier is true
		[Tooltip("移動乗数")]
		[MMCondition("ApplyMovementMultiplier", true)]
		[Header("移動乗数")]
		public float MovementMultiplier = 0.5f;
		/// the duration of the movement multiplier, if ApplyMovementMultiplier is true
		[Tooltip("移動乗数持続時間")]
		[MMCondition("ApplyMovementMultiplier", true)]
		[Header("移動乗数持続時間")]
		public float MovementMultiplierDuration = 2f;
		
		

		protected int _lastRandomFrame = -1000;
		protected float _lastRandomValue = 0f;

		public virtual float DamageCaused
		{
			get
			{
				if (Time.frameCount != _lastRandomFrame)
				{
					_lastRandomValue = Random.Range(MinDamageCaused, MaxDamageCaused);
					_lastRandomFrame = Time.frameCount;
				}
				return _lastRandomValue;
			}
		} 
	}	
}
