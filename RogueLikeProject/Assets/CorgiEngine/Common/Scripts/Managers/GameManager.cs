using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
using System.Collections.Generic;
using MoreMountains.InventoryEngine;
using MoreMountains.Feedbacks;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Corgi Engine で使われる基本イベントの一覧
    /// 
    /// LevelStart : レベル開始時に LevelManager が発行
    /// LevelComplete : レベルの終点に到達したときに発行される
    /// LevelEnd : 上と同じ意味で使われることもある
    /// Pause : ポーズ開始時に発行
    /// UnPause : ポーズ解除時に発行
    /// PlayerDeath : プレイヤーが死亡したときに発行
    /// Respawn : プレイヤーが復活したときに発行
    /// StarPicked : 星ボーナスを取得したときに発行
    /// GameOver : 全ライフを失ったときに LevelManager が発行
    /// CharacterSwitch : キャラクターが切り替わったときに発行
    /// CharacterSwap : キャラクターが入れ替わったときに発行
    /// TogglePause : ポーズ／解除を要求するときに発行
    /// </summary>
    public enum CorgiEngineEventTypes
	{
		SpawnCharacterStarts,
		LevelStart,
		LevelComplete,
		LevelEnd,
		Pause,
		UnPause,
		PlayerDeath,
		Respawn,
		StarPicked,
		GameOver,
		CharacterSwitch,
		CharacterSwap,
		TogglePause,
		LoadNextScene,
		PauseNoMenu,
		LivesCountChanged
	}

    /// <summary>
    /// レベル開始や終了などを通知するためのイベント
    /// </summary>
    public struct CorgiEngineEvent
	{
		public CorgiEngineEventTypes EventType;
		public Character OriginCharacter;

        /// <summary>
        /// 新しい CorgiEngineEvent を作成します
        /// </summary>
        /// <param name="eventType">イベントの種類</param>
        public CorgiEngineEvent(CorgiEngineEventTypes eventType, Character originCharacter = null)
		{
			EventType = eventType;
			OriginCharacter = originCharacter;
		}
        
		static CorgiEngineEvent e;
		public static void Trigger(CorgiEngineEventTypes eventType, Character originCharacter = null)
		{
			e.EventType = eventType;
			e.OriginCharacter = originCharacter;
			MMEventManager.TriggerEvent(e);
		}
	}

    /// <summary>
    /// スコアの変更方法
    /// Add : 加算
    /// Set : 値を直接セット
    /// </summary>
    public enum PointsMethods
	{
		Add,
		Set
	}

    /// <summary>
    /// 星を取得したときのイベント
    /// </summary>
    public struct CorgiEngineStarEvent
	{
		public string SceneName;
		public int StarID;

		public CorgiEngineStarEvent(string sceneName, int starID)
		{
			SceneName = sceneName;
			StarID = starID;
		}

		static CorgiEngineStarEvent e;
		public static void Trigger(string sceneName, int starID)
		{
			e.SceneName = sceneName;
			e.StarID = starID;
			MMEventManager.TriggerEvent(e);
		}
	}

    /// <summary>
    /// スコアが変更されたときのイベント
    /// </summary>
    public struct CorgiEnginePointsEvent
	{
		public PointsMethods PointsMethod;
		public int Points;
        /// <summary>
        /// 新しい CorgiEnginePointsEvent を作成します
        /// </summary>
        /// <param name="pointsMethod">スコアの変更方法</param>
        /// <param name="points">変更するスコア値</param>
        public CorgiEnginePointsEvent(PointsMethods pointsMethod, int points)
		{
			PointsMethod = pointsMethod;
			Points = points;
		}
        
		static CorgiEnginePointsEvent e;
		public static void Trigger(PointsMethods pointsMethod, int points)
		{
			e.PointsMethod = pointsMethod;
			e.Points = points;
			MMEventManager.TriggerEvent(e);
		}
	}

    /// <summary>
    /// ポーズの種類
    /// PauseMenu : ポーズメニューを表示する通常のポーズ
    /// NoPauseMenu : メニューを出さずに時間だけ止める（インベントリ用など）
    /// </summary>
    public enum PauseMethods
	{
		PauseMenu,
		NoPauseMenu
	}

    /// <summary>
    /// レベルの入口情報を保存するクラス
    /// </summary>
    public class PointsOfEntryStorage
	{
		public string LevelName;
		public int PointOfEntryIndex;
		public Character.FacingDirections FacingDirection;

		public PointsOfEntryStorage(string levelName, int pointOfEntryIndex, Character.FacingDirections facingDirection)
		{
			LevelName = levelName;
			FacingDirection = facingDirection;
			PointOfEntryIndex = pointOfEntryIndex;
		}
	}

    /// <summary>
    /// GameManager はゲーム全体のポイントや時間、ライフなどを管理する永続シングルトン
    /// </summary>
    [AddComponentMenu("Corgi Engine/Managers/Game Manager")]
	public class GameManager : 	MMPersistentSingleton<GameManager>, 
		MMEventListener<MMGameEvent>, 
		MMEventListener<CorgiEngineEvent>, 
		MMEventListener<CorgiEnginePointsEvent>
	{
        [Header("Settings")]
        /// ゲームのターゲットフレームレート
        [Tooltip("ゲームのターゲットフレームレート")]
        public int TargetFrameRate = 300;

        [Header("Lives")]
        /// キャラクターが持てる最大ライフ数
        [Tooltip("キャラクターが持てる最大ライフ数")]
        public int MaximumLives = 0;
        /// 現在のライフ数
        [Tooltip("現在のライフ数")]
        public int CurrentLives = 0;


        [Header("Game Over")]
        /// ゲームオーバー時にライフをリセットするか
        [Tooltip("ゲームオーバー時にライフをリセットするか")]
        public bool ResetLivesOnGameOver = true;
        /// ゲームオーバー時に永続キャラを消すか
        [Tooltip("ゲームオーバー時に永続キャラを消すか")]
        public bool ResetPersistentCharacterOnGameOver = true;
        /// ゲームオーバー時に保存されたキャラを消すか
        [Tooltip("ゲームオーバー時に保存されたキャラを消すか")]
        public bool ResetStoredCharacterOnGameOver = true;
        /// 全ライフを失ったときに遷移するシーン名
        [Tooltip("全ライフを失ったときに遷移するシーン名")]
        public string GameOverScene;
        /// ゲームオーバーシーンに遷移するまでの遅延時間（秒）
        [Tooltip("ゲームオーバーシーンに遷移するまでの遅延時間（秒）")]
        public float DelayBeforeGameOverSceneLoad = 0f;
        /// インベントリを開いたときにゲームをポーズするか
        [Tooltip("インベントリを開いたときにゲームをポーズするか")]
        public bool PauseGameWhenInventoryOpens = true;

        /// the current number of game points
        public int Points { get; private set; }
		/// true if the game is currently paused
		public bool Paused { get; set; } 
		// true if we've stored a map position at least once
		public bool StoredLevelMapPosition{ get; set; }
		/// the current player
		public Vector2 LevelMapPosition { get; set; }
		/// the stored selected character
		public Character StoredCharacter { get; set; }
		/// the stored selected character
		public Character PersistentCharacter { get; set; }
		/// the list of points of entry and exit
		public List<PointsOfEntryStorage> PointsOfEntry { get; set; }

		protected bool _inventoryOpen = false;
		protected bool _pauseMenuOpen = false;
		protected InventoryInputManager _inventoryInputManager;
		protected int _initialMaximumLives;
		protected int _initialCurrentLives;

		protected override void Awake()
		{
			base.Awake ();
			PointsOfEntry = new List<PointsOfEntryStorage> ();
		}

		/// <summary>
		/// On Start(), sets the target framerate to whatever's been specified
		/// </summary>
		protected virtual void Start()
		{
			Application.targetFrameRate = TargetFrameRate;
			_initialCurrentLives = CurrentLives;
			_initialMaximumLives = MaximumLives;            
		}
					
		/// <summary>
		/// this method resets the whole game manager
		/// </summary>
		public virtual void Reset()
		{
			Points = 0;
			MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 1f, 0f, false, 0f, true);
			Paused = false;
			GUIManager.Instance.RefreshPoints ();
			PointsOfEntry?.Clear ();
		}	

		/// <summary>
		/// Use this method to decrease the current number of lives
		/// </summary>
		public virtual void LoseLife()
		{
			CurrentLives--;
			CorgiEngineEvent.Trigger(CorgiEngineEventTypes.LivesCountChanged);
		}

		/// <summary>
		/// Use this method when a life (or more) is gained
		/// </summary>
		/// <param name="lives">Lives.</param>
		public virtual void GainLives(int lives)
		{
			CurrentLives += lives;
			if (CurrentLives > MaximumLives)
			{
				CurrentLives = MaximumLives;
			}
			CorgiEngineEvent.Trigger(CorgiEngineEventTypes.LivesCountChanged);
		}

		/// <summary>
		/// Use this method to increase the max amount of lives, and optionnally the current amount as well
		/// </summary>
		/// <param name="lives">Lives.</param>
		/// <param name="increaseCurrent">If set to <c>true</c> increase current.</param>
		public virtual void AddLives(int lives, bool increaseCurrent)
		{
			MaximumLives += lives;
			if (increaseCurrent) 
			{
				CurrentLives += lives;
			}
			CorgiEngineEvent.Trigger(CorgiEngineEventTypes.LivesCountChanged);
		}

		/// <summary>
		/// Resets the number of lives to their initial values.
		/// </summary>
		public virtual void ResetLives()
		{
			CurrentLives = _initialCurrentLives ;
			MaximumLives = _initialMaximumLives ;
		}
			
		/// <summary>
		/// Adds the points in parameters to the current game points.
		/// </summary>
		/// <param name="pointsToAdd">Points to add.</param>
		public virtual void AddPoints(int pointsToAdd)
		{
			Points += pointsToAdd;
			GUIManager.Instance.RefreshPoints ();
		}
		
		/// <summary>
		/// use this to set the current points to the one you pass as a parameter
		/// </summary>
		/// <param name="points">Points.</param>
		public virtual void SetPoints(int points)
		{
			Points = points;
			GUIManager.Instance.RefreshPoints ();
		}

		protected virtual void SetActiveInventoryInputManager(bool status)
		{
			_inventoryInputManager = GameObject.FindFirstObjectByType<InventoryInputManager> ();
			if (_inventoryInputManager != null)
			{
				_inventoryInputManager.enabled = status;
			}
		}
		
		/// <summary>
		/// Pauses the game or unpauses it depending on the current state
		/// </summary>
		public virtual void Pause(PauseMethods pauseMethod = PauseMethods.PauseMenu)
		{	
			if ((pauseMethod == PauseMethods.PauseMenu) && _inventoryOpen)
			{
				return;
			}

			// if time is not already stopped		
			if (Time.timeScale>0.0f)
			{
				MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0f, 0f, false, 0f, true);
				Instance.Paused=true;
				if ((GUIManager.HasInstance) && (pauseMethod == PauseMethods.PauseMenu))
				{
					GUIManager.Instance.SetPause(true);	
					_pauseMenuOpen = true;
					SetActiveInventoryInputManager (false);
				}
				if (pauseMethod == PauseMethods.NoPauseMenu)
				{
					_inventoryOpen = true;
				}
			}
			else
			{
				UnPause(pauseMethod);
				CorgiEngineEvent.Trigger(CorgiEngineEventTypes.UnPause);
			}		
			LevelManager.Instance.ToggleCharacterPause();
		}

		/// <summary>
		/// Unpauses the game
		/// </summary>
		public virtual void UnPause(PauseMethods pauseMethod = PauseMethods.PauseMenu)
		{
			MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Unfreeze, 1f, 0f, false, 0f, false);
			Instance.Paused = false;
			if ((GUIManager.HasInstance) && (pauseMethod == PauseMethods.PauseMenu))
			{ 
				GUIManager.Instance.SetPause(false);
				_pauseMenuOpen = false;
				SetActiveInventoryInputManager (true);
			}
			if (_inventoryOpen)
			{
				_inventoryOpen = false;
			}
			LevelManager.Instance.ToggleCharacterPause();
		}

		/// <summary>
		/// Deletes all save files
		/// </summary>
		public virtual void ResetAllSaves()
		{
			MMSaveLoadManager.DeleteSaveFolder ("InventoryEngine");
			MMSaveLoadManager.DeleteSaveFolder ("CorgiEngine");
			MMSaveLoadManager.DeleteSaveFolder ("MMAchievements");
			MMSaveLoadManager.DeleteSaveFolder ("MMRetroAdventureProgress");
		}

		/// <summary>
		/// Stores the points of entry for the level whose name you pass as a parameter.
		/// </summary>
		/// <param name="levelName">Level name.</param>
		/// <param name="entryIndex">Entry index.</param>
		/// <param name="exitIndex">Exit index.</param>
		public virtual void StorePointsOfEntry(string levelName, int entryIndex, Character.FacingDirections facingDirection)
		{
			if (PointsOfEntry.Count > 0)
			{
				foreach (PointsOfEntryStorage point in PointsOfEntry)
				{
					if (point.LevelName == levelName)
					{
						point.FacingDirection = facingDirection;
						point.PointOfEntryIndex = entryIndex;
						return;
					}
				}	
			}

			PointsOfEntry.Add (new PointsOfEntryStorage (levelName, entryIndex, facingDirection));
		}

		/// <summary>
		/// Gets point of entry info for the level whose scene name you pass as a parameter
		/// </summary>
		/// <returns>The points of entry.</returns>
		/// <param name="levelName">Level name.</param>
		public virtual PointsOfEntryStorage GetPointsOfEntry(string levelName)
		{
			if (PointsOfEntry.Count > 0)
			{
				foreach (PointsOfEntryStorage point in PointsOfEntry)
				{
					if (point.LevelName == levelName)
					{
						return point;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Clears the stored point of entry infos for the level whose name you pass as a parameter
		/// </summary>
		/// <param name="levelName">Level name.</param>
		public virtual void ClearPointOfEntry(string levelName)
		{
			if (PointsOfEntry.Count > 0)
			{
				foreach (PointsOfEntryStorage point in PointsOfEntry)
				{
					if (point.LevelName == levelName)
					{
						PointsOfEntry.Remove (point);
					}
				}
			}
		}

		/// <summary>
		/// Clears all points of entry.
		/// </summary>
		public virtual void ClearAllPointsOfEntry()
		{
			PointsOfEntry.Clear ();
		}

		/// <summary>
		/// Sets a new persistent character
		/// </summary>
		/// <param name="newCharacter"></param>
		public virtual void SetPersistentCharacter(Character newCharacter)
		{
			PersistentCharacter = newCharacter;
		}

		/// <summary>
		/// Destroys a persistent character if there's one
		/// </summary>
		public virtual void DestroyPersistentCharacter()
		{
			if (PersistentCharacter != null)
			{
				Destroy(PersistentCharacter.gameObject);
				SetPersistentCharacter(null);
			}
			

			if (LevelManager.Instance.Players[0] != null)
			{
				if (LevelManager.Instance.Players[0].gameObject.MMGetComponentNoAlloc<CharacterPersistence>() != null)
				{
					Destroy(LevelManager.Instance.Players[0].gameObject);	
				}
			}
		}

		/// <summary>
		/// Stores the selected character for use in upcoming levels
		/// </summary>
		/// <param name="selectedCharacter">Selected character.</param>
		public virtual void StoreSelectedCharacter(Character selectedCharacter)
		{
			StoredCharacter = selectedCharacter;
		}

		/// <summary>
		/// Clears the selected character.
		/// </summary>
		public virtual void ClearStoredCharacter()
		{
			StoredCharacter = null;
		}

		/// <summary>
		/// Catches inventory events and acts on them, pausing the game if needed
		/// </summary>
		/// <param name="gameEvent">MMGameEvent event.</param>
		public virtual void OnMMEvent(MMGameEvent gameEvent)
		{
			if (!PauseGameWhenInventoryOpens)
			{
				return;
			}
			
			switch (gameEvent.EventName)
			{
				case "inventoryOpens":
					Pause (PauseMethods.NoPauseMenu);
					break;

				case "inventoryCloses":
					Pause (PauseMethods.NoPauseMenu);
					break;
			}
		}

		/// <summary>
		/// Catches CorgiEngineEvents and acts on them, playing the corresponding sounds
		/// </summary>
		/// <param name="engineEvent">CorgiEngineEvent event.</param>
		public virtual void OnMMEvent(CorgiEngineEvent engineEvent)
		{
			switch (engineEvent.EventType)
			{
				case CorgiEngineEventTypes.TogglePause:
					if (Paused)
					{
						CorgiEngineEvent.Trigger(CorgiEngineEventTypes.UnPause);
					}
					else
					{
						CorgiEngineEvent.Trigger(CorgiEngineEventTypes.Pause);
					}
					break;

				case CorgiEngineEventTypes.Pause:
					Pause ();
					break;
				
				case CorgiEngineEventTypes.UnPause:
					UnPause ();
					break;
				
				case CorgiEngineEventTypes.PauseNoMenu:
					Pause(PauseMethods.NoPauseMenu);
					break;
			}
		}

		/// <summary>
		/// Catches CorgiEnginePointsEvents and acts on them, playing the corresponding sounds
		/// </summary>
		/// <param name="pointEvent">CorgiEnginePointsEvent event.</param>
		public virtual void OnMMEvent(CorgiEnginePointsEvent pointEvent)
		{
			switch (pointEvent.PointsMethod)
			{
				case PointsMethods.Set:
					SetPoints (pointEvent.Points);
					break;

				case PointsMethods.Add:
					AddPoints (pointEvent.Points);
					break;
			}
		}

		/// <summary>
		/// OnDisable, we start listening to events.
		/// </summary>
		protected virtual void OnEnable()
		{
			this.MMEventStartListening<MMGameEvent> ();
			this.MMEventStartListening<CorgiEngineEvent> ();
			this.MMEventStartListening<CorgiEnginePointsEvent> ();
			Cursor.visible = true;
		}

		/// <summary>
		/// OnDisable, we stop listening to events.
		/// </summary>
		protected virtual void OnDisable()
		{
			this.MMEventStopListening<MMGameEvent> ();
			this.MMEventStopListening<CorgiEngineEvent> ();
			this.MMEventStopListening<CorgiEnginePointsEvent> ();
		}
	}
}