using System;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// Used by the DamageResistanceProcessor, this class defines the resistance versus a certain type of damage. 
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/Health/Damage Resistance")]
	public class DamageResistance : CorgiMonoBehaviour
	{
		public enum DamageModifierModes { Multiplier, Flat }
		public enum KnockbackModifierModes { Multiplier, Flat }

		[Header("全般")]
		/// The priority of this damage resistance. This will be used to determine in what order damage resistances should be evaluated. Lowest priority means evaluated first.
		[Tooltip("優先度")]
		[Header("優先度")]
		public float Priority = 0;
		/// The label of this damage resistance. Used for organization, and to activate/disactivate a resistance by its label.
		[Tooltip("ラベル")]
		[Header("ラベル")]
		public string Label = "";
		
		[Header("ダメージ耐性設定")]
		/// Whether this resistance impacts base damage or typed damage
		[Tooltip("ダメージタイプモード")]
		[Header("ダメージタイプモード")]
		public DamageTypeModes DamageTypeMode = DamageTypeModes.BaseDamage;
		/// In TypedDamage mode, the type of damage this resistance will interact with
		[Tooltip("タイプ耐性")]
		[MMEnumCondition("DamageTypeMode", (int)DamageTypeModes.TypedDamage)]
		[Header("タイプ耐性")]
		public DamageType TypeResistance;
		/// the way to reduce (or increase) received damage. Multiplier will multiply incoming damage by a multiplier, flat will subtract a constant value from incoming damage.
		[Tooltip("ダメージ修正モード")]
		[Header("ダメージ修正モード")]
		public DamageModifierModes DamageModifierMode = DamageModifierModes.Multiplier;

		[Header("ダメージ修正値")]
		/// In multiplier mode, the multiplier to apply to incoming damage. 0.5 will reduce it in half, while a value of 2 will create a weakness to the specified damage type, and damages will double.
		[Tooltip("ダメージ乗数")]
		[MMEnumCondition("DamageModifierMode", (int)DamageModifierModes.Multiplier)]
		[Header("ダメージ乗数")]
		public float DamageMultiplier = 0.25f;
		/// In flat mode, the amount of damage to subtract every time that type of damage is received
		[Tooltip("固定ダメージ軽減量")]
		[MMEnumCondition("DamageModifierMode", (int)DamageModifierModes.Flat)]
		[Header("固定ダメージ軽減量")]
		public float FlatDamageReduction = 10f;
		/// whether or not incoming damage of the specified type should be clamped between a min and a max
		[Tooltip("ダメージをクランプ")]
		[Header("ダメージをクランプ")]
		public bool ClampDamage = false;
		/// the values between which to clamp incoming damage
		[Tooltip("ダメージクランプ範囲")]
		[MMVector("Min","Max")]
		[Header("ダメージクランプ範囲")]
		public Vector2 DamageModifierClamps = new Vector2(0f,10f);

		[Header("状態変化")]
		/// whether or not condition change for that type of damage is allowed or not
		[Tooltip("キャラクター状態変化を防ぐ")]
		[Header("キャラクター状態変化を防ぐ")]
		public bool PreventCharacterConditionChange = false;
		/// whether or not movement modifiers are allowed for that type of damage or not
		[Tooltip("移動修正を防ぐ")]
		[Header("移動修正を防ぐ")]
		public bool PreventMovementModifier = false;

		[Header("ノックバック")]
		/// if this is true, knockback force will be ignored and not applied
		[Tooltip("ノックバック免疫")]
		[Header("ノックバック免疫")]
		public bool ImmuneToKnockback = false;
		/// the way to reduce (or increase) received knockback. Multiplier will multiply incoming knockback intensity by a multiplier, flat will subtract a constant value from incoming knockback intensity.
		[Tooltip("ノックバック修正モード")]
		[Header("ノックバック修正モード")]
		public KnockbackModifierModes KnockbackModifierMode = KnockbackModifierModes.Multiplier;
		/// In multiplier mode, the multiplier to apply to incoming knockback. 0.5 will reduce it in half, while a value of 2 will create a weakness to the specified damage type, and knockback intensity will double.
		[Tooltip("ノックバック乗数")]
		[MMEnumCondition("KnockbackModifierMode", (int)DamageModifierModes.Multiplier)]
		[Header("ノックバック乗数")]
		public float KnockbackMultiplier = 1f;
		/// In flat mode, the amount of knockback to subtract every time that type of damage is received
		[Tooltip("固定ノックバック軽減量")]
		[MMEnumCondition("KnockbackModifierMode", (int)DamageModifierModes.Flat)]
		[Header("固定ノックバック軽減量")]
		public float FlatKnockbackMagnitudeReduction = 10f;
		/// whether or not incoming knockback of the specified type should be clamped between a min and a max
		[Tooltip("ノックバックをクランプ")]
		[Header("ノックバックをクランプ")]
		public bool ClampKnockback = false;
		/// the values between which to clamp incoming knockback magnitude
		[Tooltip("ノックバック最大強度")]
		[MMCondition("ClampKnockback", true)]
		[Header("ノックバック最大強度")]
		public float KnockbackMaxMagnitude = 10f;
		
		[Header("フィードバック")]
		/// This feedback will only be triggered if damage of the matching type is received
		[Tooltip("ダメージ受信時フィードバック")]
		[Header("ダメージ受信時フィードバック")]
		public MMFeedbacks OnDamageReceived;
		/// whether or not this feedback can be interrupted (stopped) when that type of damage is interrupted
		[Tooltip("フィードバックを中断可能")]
		[Header("フィードバックを中断可能")]
		public bool InterruptibleFeedback = false;
		/// if this is true, the feedback will always be preventively stopped before playing
		[Tooltip("再生前に常に中断")]
		[Header("再生前に常に中断")]
		public bool AlwaysInterruptFeedbackBeforePlay = false;
		/// whether this feedback should play if damage received is zero
		[Tooltip("ダメージゼロでもフィードバック再生")]
		[Header("ダメージゼロでもフィードバック再生")]
		public bool TriggerFeedbackIfDamageIsZero = false;

		/// <summary>
		/// On awake we initialize our feedback
		/// </summary>
		protected virtual void Awake()
		{
			OnDamageReceived?.Initialization(this.gameObject);
		}
		
		/// <summary>
		/// When getting damage, goes through damage reduction and outputs the resulting damage
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="type"></param>
		/// <param name="damageApplied"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public virtual float ProcessDamage(float damage, DamageType type, bool damageApplied)
		{
			if (!this.gameObject.activeInHierarchy)
			{
				return damage;
			}
			
			if ((type == null) && (DamageTypeMode != DamageTypeModes.BaseDamage))
			{
				return damage;
			}

			if ((type != null) && (DamageTypeMode == DamageTypeModes.BaseDamage))
			{
				return damage;
			}

			if ((type != null) && (type != TypeResistance))
			{
				return damage;
			}
			
			// applies damage modifier or reduction
			switch (DamageModifierMode)
			{
				case DamageModifierModes.Multiplier:
					damage = damage * DamageMultiplier;
					break;
				case DamageModifierModes.Flat:
					damage = damage - FlatDamageReduction;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			// clamps damage
			damage = ClampDamage ? Mathf.Clamp(damage, DamageModifierClamps.x, DamageModifierClamps.y) : damage;

			if (damageApplied)
			{
				if (!TriggerFeedbackIfDamageIsZero && (damage == 0))
				{
					// do nothing
				}
				else
				{
					if (AlwaysInterruptFeedbackBeforePlay)
					{
						OnDamageReceived?.StopFeedbacks();
					}
					OnDamageReceived?.PlayFeedbacks(this.transform.position);	
				}
			}

			return damage;
		}
		
		
		/// <summary>
		/// Processes the knockback input value and returns it potentially modified by damage resistances
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="type"></param>
		/// <param name="damageApplied"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public virtual Vector2 ProcessKnockback(Vector2 knockback, DamageType type)
		{
			if (!this.gameObject.activeInHierarchy)
			{
				return knockback;
			}
			
			if ((type == null) && (DamageTypeMode != DamageTypeModes.BaseDamage))
			{
				return knockback;
			}

			if ((type != null) && (DamageTypeMode == DamageTypeModes.BaseDamage))
			{
				return knockback;
			}

			if ((type != null) && (type != TypeResistance))
			{
				return knockback;
			}
			
			// applies damage modifier or reduction
			switch (KnockbackModifierMode)
			{
				case KnockbackModifierModes.Multiplier:
					knockback = knockback * KnockbackMultiplier;
					break;
				case KnockbackModifierModes.Flat:
					float magnitudeReduction = Mathf.Clamp(Mathf.Abs(knockback.magnitude) - FlatKnockbackMagnitudeReduction, 0f, Single.MaxValue);
					knockback = knockback.normalized * magnitudeReduction * Mathf.Sign(knockback.magnitude);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			// clamps damage
			knockback = ClampKnockback ? Vector2.ClampMagnitude(knockback, KnockbackMaxMagnitude) : knockback;

			return knockback;
		}
	}
}