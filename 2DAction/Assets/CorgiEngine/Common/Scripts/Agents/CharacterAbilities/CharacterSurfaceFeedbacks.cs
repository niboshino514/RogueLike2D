using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace MoreMountains.CorgiEngine
{
	
	/// <summary>
	/// Add this component to a character and it'll let you define a number of surfaces and associate walk and run feedbacks to them
	/// It will also let you trigger events when entering or exiting these surfaces
	/// Important : Surfaces are evaluated from top to bottom. The first surface definition that matches the current detected
	/// ground will be considered the current surface. So make sure your order them accordingly.
	/// </summary>
	[MMHiddenProperties("AbilityStopFeedbacks", "AbilityStartFeedbacks")]
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Surface Feedbacks")] 
	public class CharacterSurfaceFeedbacks : CharacterAbility
	{	
		[Serializable]
		public class CharacterSurfaceFeedbacksItems
		{
			/// an ID to identify this surface in the list. Not used by anything but makes the list more readable
			[Tooltip("レイヤー")]
			[Header("ID")]
			public string ID;
			/// the list of layers that identify this surface
			[Tooltip("レイヤー")]
			[Header("レイヤー")]
			public LayerMask Layers;
			/// whether or not to use a tag to identify this surface or just rely only on the layer
			[Tooltip("タグを使用")]
			[Header("タグを使用")]
			public bool UseTag;
			/// if using tags, the Tag that should be on this surface to identify it (on top of the layer)
			[Tooltip("タグ")]
			[MMCondition("UseTag", true)]
			[Header("タグ")]
			public string Tag;
			/// the feedback to bind to the Movement ability's AbilityStartFeedbacks slot
			[Tooltip("歩行開始フィードバック")]
			[Header("歩行開始フィードバック")]
			public MMFeedbacks WalkStartFeedback;
			/// the feedback to bind to the Movement ability's AbilityStopFeedbacks slot
			[Tooltip("歩行停止フィードバック")]
			[Header("歩行停止フィードバック")]
			public MMFeedbacks WalkStopFeedback;
			/// the feedback to bind to the Run ability's AbilityStartFeedbacks slot
			[Tooltip("走行開始フィードバック")]
			[Header("走行開始フィードバック")]
			public MMFeedbacks RunStartFeedback;
			/// the feedback to bind to the Run ability's AbilityStopFeedbacks slot
			[Tooltip("走行停止フィードバック")]
			[Header("走行停止フィードバック")]
			public MMFeedbacks RunStopFeedback;
			/// a UnityEvent that will trigger when entering this surface
			[Tooltip("表面進入時イベント")]
			[Header("表面進入時イベント")]
			public UnityEvent OnEnterSurface;
			/// a UnityEvent that will trigger when exiting this surface
			[Tooltip("表面退出時イベント")]
			[Header("表面退出時イベント")]
			public UnityEvent OnExitSurface;
		}
		
		/// whether detection should rely on periodical controller checks or be driven by an external script (via the SetCurrentSurfaceIndex(int index) method)
		public enum SurfaceDetectionModes { Controller, Script }
		/// This method is only used to display a helpbox text at the beginning of the ability's inspector
		public override string HelpBoxText() { return "複数の表面を定義し、歩行／走行フィードバックを関連付けられます。表面の出入り時にイベントも発火できます。重要：表面は上から順に評価され、最初に一致した定義が現在の表面になります。順序に注意してください。"; }

		[Header("表面リスト")]
		/// a list of surface definitions, defined by a layer, an optional tag, and a walk and run sound. These will be evaluated from top to bottom, first match found becomes the current surface.
		[Tooltip("表面一覧")]
		[Header("表面一覧")]
		public List<CharacterSurfaceFeedbacksItems> Surfaces;

		[Header("検出")]
		/// whether detection should rely on periodical controller checks or be driven by an external script (via the SetCurrentSurfaceIndex(int index) method)
		[Tooltip("表面検出モード")]
		[Header("表面検出モード")]
		public SurfaceDetectionModes SurfaceDetectionMode = SurfaceDetectionModes.Controller;
		/// the frequency (in seconds) at which to cast the raycast to detect surfaces, usually you'll want to space them a bit to save on performance
		[Tooltip("コントローラーチェック頻度")]
		[MMEnumCondition("SurfaceDetectionMode", (int)SurfaceDetectionModes.Controller)]
		[Header("コントローラーチェック頻度")]
		public float ControllerCheckFrequency = 0.3f;

		[Header("デバッグ")]
		/// The current index of the surface we're on in the Surfaces list
		[Tooltip("現在の表面インデックス")]
		[MMReadOnly]
		[Header("現在の表面インデックス")]
		public int CurrentSurfaceIndex = -1;
		
		protected float _timeSinceLastCheck = -float.PositiveInfinity;
		protected int _surfaceIndexLastFrame;
		protected CharacterRun _characterRun;
		
		/// <summary>
		/// A method you can use to force the surface index, when in ScriptDriven mode
		/// </summary>
		/// <param name="index"></param>
		public virtual void SetCurrentSurfaceIndex(int index)
		{
			CurrentSurfaceIndex = index;
		}

		/// <summary>
		/// On init we grab our run ability and init our index
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			_characterRun = _character.FindAbility<CharacterRun>();
			_surfaceIndexLastFrame = -1;
		}
		
		/// <summary>
		/// Every frame we detect surfaces if needed, and handle a potential surface change
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();
			DetectSurface();
			HandleSurfaceChange();
		}

		/// <summary>
		/// If we're on a new surface, we swap feedbacks and invoke our events
		/// </summary>
		protected virtual void HandleSurfaceChange()
		{
			if (_surfaceIndexLastFrame != CurrentSurfaceIndex)
			{
				if (_surfaceIndexLastFrame >= 0 && _surfaceIndexLastFrame < Surfaces.Count)
				{
					Surfaces[_surfaceIndexLastFrame].OnExitSurface?.Invoke();
				}
				Surfaces[CurrentSurfaceIndex].OnEnterSurface?.Invoke();
				_characterHorizontalMovement.StopStartFeedbacks();
				_characterRun.StopStartFeedbacks();
				_characterHorizontalMovement.AbilityStartFeedbacks = Surfaces[CurrentSurfaceIndex].WalkStartFeedback;
				_characterHorizontalMovement.AbilityStopFeedbacks = Surfaces[CurrentSurfaceIndex].WalkStopFeedback;
				_characterRun.AbilityStartFeedbacks = Surfaces[CurrentSurfaceIndex].RunStartFeedback;
				_characterRun.AbilityStopFeedbacks = Surfaces[CurrentSurfaceIndex].RunStopFeedback;
				if (_movement.CurrentState == CharacterStates.MovementStates.Walking)
				{
					_characterHorizontalMovement.PlayAbilityStartFeedbacks();
				}
				if (_movement.CurrentState == CharacterStates.MovementStates.Running)
				{
					_characterRun.PlayAbilityStartFeedbacks();
				}
			}
			_surfaceIndexLastFrame = CurrentSurfaceIndex;
		}

		/// <summary>
		/// Returns true if the tags match or if we're not using tags
		/// </summary>
		/// <param name="useTag"></param>
		/// <param name="contactTag"></param>
		/// <param name="surfaceTag"></param>
		/// <returns></returns>
		protected virtual bool TagsMatch(bool useTag, string contactTag, string surfaceTag)
		{
			if (!useTag)
			{
				return true;
			}
			return contactTag == surfaceTag;
		}

		/// <summary>
		/// Checks if a surface detection is needed and performs it
		/// </summary>
		protected virtual void DetectSurface()
		{
			if (SurfaceDetectionMode == SurfaceDetectionModes.Script)
			{
				return;
			}
			
			if (Time.time - _timeSinceLastCheck < ControllerCheckFrequency)
			{
				return;
			}
			_timeSinceLastCheck = Time.time;

			if (!_controller.State.IsGrounded)
			{
				return;
			}

			if (_controller.StandingOn == null)
			{
				return;
			}
			
			foreach (CharacterSurfaceFeedbacksItems item in Surfaces)
			{
				if (item.Layers.MMContains(_controller.StandingOn.layer) && TagsMatch(item.UseTag, item.Tag, _controller.StandingOn.tag))
				{
					CurrentSurfaceIndex = Surfaces.IndexOf(item);
					return;
				}
			}
		}
	}
}