using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
#if MM_CINEMACHINE
using Cinemachine;
#elif MM_CINEMACHINE3
using Unity.Cinemachine;
#endif

namespace MoreMountains.CorgiEngine
{  

	/// <summary>
	/// A class that handles camera follow for Cinemachine powered cameras
	/// </summary>
	public class CinemachineCameraController : CorgiMonoBehaviour, MMEventListener<MMCameraEvent>, MMEventListener<CorgiEngineEvent>
	{
		public enum PerspectiveZoomMethods { FieldOfView, FramingTransposerDistance }

		/// True if the camera should follow the player
		public bool FollowsPlayer { get; set; }

		[Header("Settings")]

        [Header("プレイヤー追従設定")]
        [Tooltip("カメラがプレイヤーを追従するかどうか")]
        public bool FollowsAPlayer = true;

        [Tooltip("カメラがステージ境界内に制限されるかどうか")]
        public bool ConfineCameraToLevelBounds = true;

        [Tooltip("上/下を見たときにカメラがどれだけ上下に動くか")]
        public float ManualUpDownLookDistance = 3f;

        [Header("キャラクター速度（ズーム計算用）")]
        [MMVector("最小速度", "最大速度")]
        [Tooltip("キャラクターの速度に応じてズームを変えるための速度範囲")]
        public Vector2 CharacterSpeed = new Vector2(0f, 16f);

        [MMReadOnly]
        [Tooltip("このカメラが追従するキャラクター")]
        public Character TargetCharacter;

        [MMReadOnly]
        [Tooltip("追従対象キャラクターのコントローラー")]
        public CorgiController TargetController;

        [Space(10)]
        [Header("🔍 オーソグラフィックズーム（2D）")]
        [Tooltip("キャラクターの速度に応じてズームイン/アウトするかどうか")]
        public bool UseOrthographicZoom = false;

        [MMCondition("UseOrthographicZoom", true)]
        [MMVector("最小ズーム", "最大ズーム")]
        [Tooltip("オーソグラフィックカメラのズーム範囲")]
        public Vector2 OrthographicZoom = new Vector2(5f, 9f);

        [MMCondition("UseOrthographicZoom", true)]
        [Tooltip("ゲーム開始時の初期ズーム値")]
        public float InitialOrthographicZoom = 5f;

        [MMCondition("UseOrthographicZoom", true)]
        [Tooltip("ズームのスピード")]
        public float OrthographicZoomSpeed = 0.4f;

        [Space(10)]
        [Space(10)]
        [Header("パースペクティブズーム（3D）")]
        [Tooltip("パースペクティブカメラでズームを使うかどうか")]
        public bool UsePerspectiveZoom = false;

        [MMCondition("UsePerspectiveZoom", true)]
        [Tooltip("ズーム方式（視野角 or FramingTransposer の距離）")]
        public PerspectiveZoomMethods PerspectiveZoomMethod = PerspectiveZoomMethods.FramingTransposerDistance;

        [MMCondition("UsePerspectiveZoom", true)]
        [MMVector("最小ズーム", "最大ズーム")]
        [Tooltip("パースペクティブカメラのズーム範囲")]
        public Vector2 PerspectiveZoom = new Vector2(10f, 15f);

        [MMCondition("UsePerspectiveZoom", true)]
        [Tooltip("ゲーム開始時の初期ズーム値")]
        public float InitialPerspectiveZoom = 5f;

        [MMCondition("UsePerspectiveZoom", true)]
        [Tooltip("ズームのスピード")]
        public float PerspectiveZoomSpeed = 0.4f;

        [Space(10)]
        [Header("リスポーン時の動作")]
        [Tooltip("リスポーン時にカメラを瞬間移動させるかどうか")]
        public bool InstantRepositionCameraOnRespawn = false;

        [Tooltip("追従停止時に VirtualCamera を無効化するかどうか")]
        public bool DisableVirtualCameraOnStopFollow = true;

        [Space(10)]
        [Header("デバッグ")]
        [MMInspectorButton("StartFollowing")]
        public bool StartFollowingBtn;

        [MMInspectorButton("StopFollowing")]
        public bool StopFollowingBtn;

#if MM_CINEMACHINE
		protected CinemachineVirtualCamera _virtualCamera;
		protected CinemachineConfiner _confiner;
		protected CinemachineFramingTransposer _framingTransposer;
#elif MM_CINEMACHINE3
        protected CinemachineCamera _virtualCamera;
		protected CinemachineConfiner3D _confiner3D;
		protected CinemachineConfiner2D _confiner2D;
		#endif
		protected CinemachineBrain _brain;
		protected static CinemachineBlendDefinition _savedBlend;
		protected static bool _savedBlendSet = false;

		protected float _currentZoom;
		protected bool _initialized = false;

		/// <summary>
		/// On Awake we grab our components
		/// </summary>
		protected virtual void Awake()
		{
			Initialization();
		}

		protected virtual void Initialization()
		{
			if (_initialized)
			{
				return;
			}
			#if MM_CINEMACHINE
				_virtualCamera = GetComponent<CinemachineVirtualCamera>();
				_brain = CinemachineCore.Instance.FindPotentialTargetBrain(_virtualCamera);
				_confiner = GetComponent<CinemachineConfiner>();
				_currentZoom = _virtualCamera.m_Lens.Orthographic ? InitialOrthographicZoom : InitialPerspectiveZoom;
				_framingTransposer = _virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
			#elif MM_CINEMACHINE3
				_virtualCamera = GetComponent<CinemachineCamera>();
				_brain = CinemachineCore.FindPotentialTargetBrain(_virtualCamera);
				_confiner2D = GetComponent<CinemachineConfiner2D>();
				_confiner3D = GetComponent<CinemachineConfiner3D>();
				_currentZoom = _virtualCamera.Lens.Orthographic ? InitialOrthographicZoom : InitialPerspectiveZoom;
			#endif
			
			_initialized = true;
		}

		/// <summary>
		/// On Start we assign our bounding volume
		/// </summary>
		protected virtual void Start()
		{
			InitializeConfiner();
            
			if (UseOrthographicZoom)
			{
				#if MM_CINEMACHINE
					_virtualCamera.m_Lens.OrthographicSize = InitialOrthographicZoom;
				#elif MM_CINEMACHINE3
					_virtualCamera.Lens.OrthographicSize = InitialOrthographicZoom;
					_confiner2D?.InvalidateLensCache();
				#endif
			}
            
			if (UsePerspectiveZoom)
			{
				SetPerspectiveZoom(InitialPerspectiveZoom);
			}
		}

		protected virtual void InitializeConfiner()
		{
			#if MM_CINEMACHINE
			if ((_confiner != null) && ConfineCameraToLevelBounds)
			{
				if (_confiner.m_ConfineMode == CinemachineConfiner.Mode.Confine2D)
				{
					_confiner.m_BoundingShape2D = LevelManager.Instance.BoundsCollider2D;    
				}
				else
				{
					_confiner.m_BoundingVolume = LevelManager.Instance.BoundsCollider;    
				}
			}
			#elif MM_CINEMACHINE3
			if (ConfineCameraToLevelBounds && LevelManager.HasInstance)
			{
				if (_confiner3D != null)
				{
					_confiner3D.BoundingVolume = LevelManager.Instance.BoundsCollider;	
				}
				if (_confiner2D != null)
				{
					_confiner2D.BoundingShape2D = LevelManager.Instance.BoundsCollider2D;	
				}
			}
			#endif
		}

		/// <summary>
		/// Sets a new target for this camera to track
		/// </summary>
		/// <param name="character"></param>
		public virtual void SetTarget(Character character)
		{
			TargetCharacter = character;
			TargetController = character.gameObject.MMGetComponentNoAlloc<CorgiController>();
		}

		/// <summary>
		/// Starts following the LevelManager's main player
		/// </summary>
		public virtual void StartFollowing()
		{
			Initialization();
			if (!FollowsAPlayer) { return; }
			FollowsPlayer = true;
			_virtualCamera.Follow = TargetCharacter.CameraTarget.transform;
			_virtualCamera.enabled = true;
		}

		/// <summary>
		/// Stops following any target
		/// </summary>
		public virtual void StopFollowing()
		{
			Initialization();
			if (!FollowsAPlayer) { return; }
			FollowsPlayer = false;
			
			if (DisableVirtualCameraOnStopFollow)
			{
				_virtualCamera.enabled = false;	
			}
			else
			{
				_virtualCamera.Follow = null;
			}
		}

		/// <summary>
		/// On late update, we handle our zoom level
		/// </summary>
		protected virtual void LateUpdate()
		{
			HandleZoom();
		}

		/// <summary>
		/// Makes the camera zoom in or out based on the current target speed
		/// </summary>
		protected virtual void HandleZoom()
		{
			bool lensIsOrthographic = true;
			#if MM_CINEMACHINE
			lensIsOrthographic = _virtualCamera.m_Lens.Orthographic;
			#elif MM_CINEMACHINE3
			lensIsOrthographic = _virtualCamera.Lens.Orthographic;
			#endif
			
			if (lensIsOrthographic)
			{
				PerformOrthographicZoom();
			}
			else
			{
				PerformPerspectiveZoom();
			}
		}

		/// <summary>
		/// Modifies the orthographic zoom
		/// </summary>
		protected virtual void PerformOrthographicZoom()
		{
			if (!UseOrthographicZoom || (TargetController == null))
			{
				return;
			}

			float characterSpeed = Mathf.Abs(TargetController.Speed.x);
			float currentVelocity = Mathf.Max(characterSpeed, CharacterSpeed.x);
			float targetZoom = MMMaths.Remap(currentVelocity, CharacterSpeed.x, CharacterSpeed.y, OrthographicZoom.x, OrthographicZoom.y);
			_currentZoom = Mathf.Lerp(_currentZoom, targetZoom, Time.deltaTime * OrthographicZoomSpeed);
			#if MM_CINEMACHINE
			_virtualCamera.m_Lens.OrthographicSize = _currentZoom;
			#elif MM_CINEMACHINE3
			_virtualCamera.Lens.OrthographicSize = _currentZoom;
			_confiner2D?.InvalidateLensCache();
			#endif
		}

		/// <summary>
		/// Modifies the zoom if the camera is in perspective mode
		/// </summary>
		protected virtual void PerformPerspectiveZoom()
		{
			if (!UsePerspectiveZoom || (TargetController == null))
			{
				return;
			}

			float characterSpeed = Mathf.Abs(TargetController.Speed.x);
			float currentVelocity = Mathf.Max(characterSpeed, CharacterSpeed.x);
			float targetZoom = MMMaths.Remap(currentVelocity, CharacterSpeed.x, CharacterSpeed.y, PerspectiveZoom.x, PerspectiveZoom.y);
			_currentZoom = Mathf.Lerp(_currentZoom, targetZoom, Time.deltaTime * PerspectiveZoomSpeed);
			SetPerspectiveZoom(_currentZoom);
		}

		protected virtual void SetPerspectiveZoom(float newZoom)
		{
			switch (PerspectiveZoomMethod)
			{
				case PerspectiveZoomMethods.FieldOfView:
					#if MM_CINEMACHINE
					_virtualCamera.m_Lens.FieldOfView = newZoom;
					#elif MM_CINEMACHINE3
					_virtualCamera.Lens.FieldOfView = newZoom;
					#endif
					break;

				case PerspectiveZoomMethods.FramingTransposerDistance:
					#if MM_CINEMACHINE
					if (_framingTransposer != null)
					{
						_framingTransposer.m_CameraDistance = newZoom;
					}
					#endif
					break;
			}
		}

		/// <summary>
		/// Acts on MMCameraEvents when caught
		/// </summary>
		/// <param name="cameraEvent"></param>
		public virtual void OnMMEvent(MMCameraEvent cameraEvent)
		{
			switch (cameraEvent.EventType)
			{
				case MMCameraEventTypes.SetTargetCharacter:
					SetTarget(cameraEvent.TargetCharacter);
					break;
				case MMCameraEventTypes.SetConfiner:
					#if MM_CINEMACHINE
					if ((_confiner != null) && (ConfineCameraToLevelBounds))
					{
						if (_confiner.m_ConfineMode == CinemachineConfiner.Mode.Confine2D)
						{
							_confiner.m_BoundingShape2D = cameraEvent.Bounds2D;
						}
						else
						{
							_confiner.m_BoundingVolume = cameraEvent.Bounds;    
						}
					}
					#elif MM_CINEMACHINE3
					if (ConfineCameraToLevelBounds)
					{
						if (_confiner3D != null)
						{
							_confiner3D.BoundingVolume = cameraEvent.Bounds;	
						}
						if (_confiner2D != null)
						{
							_confiner2D.BoundingShape2D = cameraEvent.Bounds2D;	
						}	
					}
					#endif
					break;
				case MMCameraEventTypes.StartFollowing:
					if (cameraEvent.TargetCharacter != null)
					{
						if (cameraEvent.TargetCharacter != TargetCharacter)
						{
							return;
						}
					}
					StartFollowing();
					break;

				case MMCameraEventTypes.StopFollowing:
					if (cameraEvent.TargetCharacter != null)
					{
						if (cameraEvent.TargetCharacter != TargetCharacter)
						{
							return;
						}
					}
					StopFollowing();
					break;

				case MMCameraEventTypes.ResetPriorities:
					_virtualCamera.Priority = 0;
					break;
			}
		}

		/// <summary>
		/// Teleports the camera's transform to the target's position
		/// </summary>
		public virtual void TeleportCameraToTarget()
		{
			this.transform.position = TargetCharacter.transform.position;
		}

		/// <summary>
		/// Sets the virtual camera's priority
		/// </summary>
		/// <param name="priority"></param>
		public virtual void SetPriority(int priority)
		{
			_virtualCamera.Priority = priority;
		}


		/// <summary>
		/// When getting game events, acts on them
		/// </summary>
		/// <param name="corgiEngineEvent"></param>
		public virtual void OnMMEvent(CorgiEngineEvent corgiEngineEvent)
		{
			if (corgiEngineEvent.EventType == CorgiEngineEventTypes.Respawn)
			{
				if (InstantRepositionCameraOnRespawn)
				{
					if (_brain == null)
					{
						_brain = CinemachineCore.FindPotentialTargetBrain(_virtualCamera);
					}

					if (!_savedBlendSet)
					{
						#if MM_CINEMACHINE
						_savedBlend = _brain.m_DefaultBlend;
						#elif MM_CINEMACHINE3
						_savedBlend = _brain.DefaultBlend;
						#endif
						_savedBlendSet = true;
					}
					#if MM_CINEMACHINE
						_brain.m_DefaultBlend = new CinemachineBlendDefinition();
					#elif MM_CINEMACHINE3
						_brain.DefaultBlend = new CinemachineBlendDefinition();
					#endif
					StartCoroutine(RestoreSavedBlend());
						
					_brain.enabled = false;
					TeleportCameraToTarget();
					_brain.enabled = true;
				}
			}

			if (corgiEngineEvent.EventType == CorgiEngineEventTypes.CharacterSwitch)
			{
				SetTarget(LevelManager.Instance.Players[0]);
				StartFollowing();
			}

			if (corgiEngineEvent.EventType == CorgiEngineEventTypes.CharacterSwap)
			{
				SetTarget(LevelManager.Instance.Players[0]);
				StartFollowing();
			}
		}

		protected virtual IEnumerator RestoreSavedBlend()
		{
			yield return new WaitForSeconds(1f);
			if (_savedBlendSet)
			{
				#if MM_CINEMACHINE
					_brain.m_DefaultBlend = _savedBlend;
				#elif MM_CINEMACHINE3
					_brain.DefaultBlend = _savedBlend;
				#endif
				_savedBlendSet = false;	
			}
		}

		/// <summary>
		/// On enable we start listening for events
		/// </summary>
		protected virtual void OnEnable()
		{
			this.MMEventStartListening<MMCameraEvent>();
			this.MMEventStartListening<CorgiEngineEvent>();
		}

		/// <summary>
		/// On disable we stop listening for events
		/// </summary>
		protected virtual void OnDisable()
		{
			this.MMEventStopListening<MMCameraEvent>();
			this.MMEventStopListening<CorgiEngineEvent>();
		}
	}
}