using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;

namespace MoreMountains.CorgiEngine
{	
	/// <summary>
	/// Add this script to an object and it will automatically be reactivated and revived when the player respawns.
	/// </summary>
	[AddComponentMenu("Corgi Engine/Spawn/Auto Respawn")]
	public class AutoRespawn : CorgiMonoBehaviour, Respawnable 
	{
		[Header("プレイヤーリスポーン時の設定")]

		/// if this is true, this object will respawn at its last position when the player revives
		[Tooltip("プレイヤーリスポーン時に復活")]
		[Header("プレイヤーリスポーン時に復活")]
		public bool RespawnOnPlayerRespawn = true;
		/// if this is true, this object will be repositioned at its initial position when the player revives
		[Tooltip("初期位置に戻す")]
		[Header("初期位置に戻す")]
		public bool RepositionToInitOnPlayerRespawn = false;
		/// whether or not this auto respawn should disable its game object when Kill is called
		[Tooltip("死亡時に無効化")]
		[Header("死亡時に無効化")]
		public bool DisableOnKill = false;
		/// whether or not this auto respawn should disable its character model when Kill is called
		[Tooltip("死亡時にモデルを非表示")]
		[Header("死亡時にモデルを非表示")]
		public bool DisableModelOnKill = true;

		[Header("X秒後に自動リスポーン")]

		/// if this has a value superior to 0, this object will respawn at its last position X seconds after its death
		[Tooltip("自動リスポーン持続時間")]
		[Header("自動リスポーン持続時間")]
		public float AutoRespawnDuration = 0f;
		/// the amount of times this object can auto respawn
		[Tooltip("自動リスポーン回数")]
		[Header("自動リスポーン回数")]
		public int AutoRespawnAmount = 3;
		/// the remaining amounts of respawns (readonly, controlled by the class at runtime)
		[Tooltip("残りリスポーン回数")]
		[MMReadOnly]
		[Header("残りリスポーン回数")]
		public int AutoRespawnRemainingAmount = 3;

		[Header("チェックポイント")]

		/// if this is true, the object will always respawn, whether or not it's associated to a checkpoint
		[Tooltip("チェックポイント無視・常に復活")]
		[Header("チェックポイント無視・常に復活")]
		public bool IgnoreCheckpointsAlwaysRespawn = true;
		/// if the player respawns at these checkpoints, the object will be respawned
		[Tooltip("関連チェックポイント")]
		[Header("関連チェックポイント")]
		public List<CheckPoint> AssociatedCheckpoints;

		[Header("フィードバック")]

		/// the MMFeedbacks to play when the player respawns
		[Tooltip("リスポーンフィードバック")]
		[Header("リスポーンフィードバック")]
		public MMFeedbacks RespawnFeedback;

		// respawn
		public delegate void OnReviveDelegate();
		public OnReviveDelegate OnRevive;

		protected MonoBehaviour[] _otherComponents;
		protected Collider2D _collider2D;
		protected Renderer _renderer;
		protected Health _health;
		protected Character _character;
		protected bool _reviving = false;
		protected float _timeOfDeath = 0f;
		protected bool _firstRespawn = true;
		protected Vector3 _initialPosition;
		protected AIBrain _aiBrain;

		/// <summary>
		/// On Start we grab our various components
		/// </summary>
		protected virtual void Start()
		{
			AutoRespawnRemainingAmount = AutoRespawnAmount;
			_otherComponents = this.gameObject.GetComponents<MonoBehaviour>() ;
			_collider2D = this.gameObject.GetComponent<Collider2D> ();
			_renderer = this.gameObject.GetComponent<Renderer> ();
			_health = this.gameObject.GetComponent<Health>();
			_character = this.gameObject.GetComponent<Character>();
			_aiBrain = this.gameObject.GetComponent<AIBrain>();
			if ((_aiBrain == null) && (_character != null))
			{
				_aiBrain = _character.CharacterBrain;
			}
			_initialPosition = this.transform.position;
		}

		/// <summary>
		/// When the player respawns, we reinstate this agent.
		/// </summary>
		/// <param name="checkpoint">Checkpoint.</param>
		/// <param name="player">Player.</param>
		public virtual void OnPlayerRespawn (CheckPoint checkpoint, Character player)
		{
			if (RepositionToInitOnPlayerRespawn)
			{
				this.transform.position = _initialPosition;				
			}

			if (RespawnOnPlayerRespawn)
			{
				Revive ();
			}
			
			AutoRespawnRemainingAmount = AutoRespawnAmount;
		}

		/// <summary>
		/// On Update we check whether we should be reviving this agent
		/// </summary>
		protected virtual void Update()
		{
			if (_reviving)
			{
				if (_timeOfDeath + AutoRespawnDuration < Time.time)
				{
					if (AutoRespawnAmount == 0)
					{
						return;
					}
					if (AutoRespawnAmount > 0)
					{
						if (AutoRespawnRemainingAmount <= 0)
						{
							return;
						}
						AutoRespawnRemainingAmount -= 1;
					}
					
					Revive ();
					_reviving = false;
				}
			}
		}

		/// <summary>
		/// Kills this object, turning its parts off based on the settings set in the inspector
		/// </summary>
		public virtual void Kill()
		{
			if (AutoRespawnDuration <= 0f)
			{
				// object is turned inactive to be able to reinstate it at respawn
				if (DisableOnKill)
				{
					gameObject.SetActive(false);	
				}
			}
			else
			{
				foreach (MonoBehaviour component in _otherComponents)
				{
					if (component != this)
					{
						component.enabled = false;	
					}
				}
				if (_collider2D != null) { _collider2D.enabled = false;	}
				if (_renderer != null)	{ _renderer.enabled = false; }
				_reviving = true;
				_timeOfDeath = Time.time;
			}

			if (DisableModelOnKill && (_character != null))
			{
				if (_character.CharacterModel != null)
				{
					_character.CharacterModel?.SetActive(false); 
				}
			}
		}

		/// <summary>
		/// Revives this object, turning its parts back on again
		/// </summary>
		public virtual void Revive()
		{
			if (_health != null)
			{
				_health.Revive();
			}

			if (AutoRespawnDuration <= 0f)
			{
				gameObject.SetActive(true);
			}
			else
			{
				foreach (MonoBehaviour component in _otherComponents)
				{
					component.enabled = true;
				}
				if (_collider2D != null) { _collider2D.enabled = true;	}
				if (_renderer != null)	{ _renderer.enabled = true; }
			}
			RespawnFeedback?.PlayFeedbacks();

			if (DisableModelOnKill && (_character != null))
			{
				if (_character.CharacterModel != null)
				{
					_character.CharacterModel.SetActive(true);	
				}
			}

			if (_aiBrain != null)
			{
				_aiBrain.ResetBrain();
			}
			if (OnRevive != null)
			{
				OnRevive.Invoke();
			}
		}
	}
}