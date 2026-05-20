using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;

namespace MoreMountains.CorgiEngine
{	
	/// <summary>
	/// Add this class to a trigger and it will send your player to the next level
	/// </summary>
	[AddComponentMenu("Corgi Engine/Spawn/Finish Level")]
	public class FinishLevel : ButtonActivated 
	{
		[MMInspectorGroup("Finish Level", true, 22)]

		/// the (exact) name of the level to go to
		[Tooltip("レベル名")]
		[Header("レベル名")]
		public string LevelName;
		/// the delay (in seconds) before actually redirecting to a new scene
		[Tooltip("遷移前遅延（秒）")]
		[Header("遷移前遅延（秒）")]
		public float DelayBeforeTransition = 0f;

		[MMInspectorGroup("MMFader トランジション", true, 25)]

		/// if this is true, a fade to black will occur when teleporting
		[Tooltip("フェードを発動")]
		[Header("フェードを発動")]
		public bool TriggerFade = false;
		/// the ID of the fader to target
		[MMCondition("TriggerFade", true)]
		[Tooltip("フェーダーID")]
		[Header("フェーダーID")]
		public int FaderID = 0;
		/// the curve to use to fade to black
		[MMCondition("TriggerFade", true)]
		[Tooltip("フェードトゥイーン")]
		[Header("フェードトゥイーン")]
		public MMTweenType FadeTween = new MMTweenType(MMTween.MMTweenCurve.EaseInCubic);

		[MMInspectorGroup("フリーズ", true, 27)]

		/// whether or not time should be frozen during the transition
		[Tooltip("時間をフリーズ")]
		[Header("時間をフリーズ")]
		public bool FreezeTime = false;
		/// whether or not the character should be frozen (input blocked) for the duration of the transition
		[Tooltip("キャラクターをフリーズ")]
		[Header("キャラクターをフリーズ")]
		public bool FreezeCharacter = true;

		protected WaitForSeconds _delayWaitForSeconds;
		protected Character _character;

		/// <summary>
		/// On initialization, we init our delay
		/// </summary>
		public override void Initialization()
		{
			base.Initialization();
			_delayWaitForSeconds = new WaitForSeconds(DelayBeforeTransition);
		}

		/// <summary>
		/// When the button is pressed we start the dialogue
		/// </summary>
		public override void TriggerButtonAction(GameObject instigator)
		{
			if (instigator.GetComponent<Character>() != null)
			{
				_character = instigator.GetComponent<Character>();
			}

			if (!CheckNumberOfUses())
			{
				return;
			}

			base.TriggerButtonAction (instigator);

			StartCoroutine(GoToNextLevelCoroutine());
			ActivateZone ();
		}	
        
		/// <summary>
		/// A coroutine used to handle the finish level sequence
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator GoToNextLevelCoroutine()
		{
			// we trigger a fade if needed
			if (TriggerFade && (DelayBeforeTransition > 0f))
			{
				MMFadeInEvent.Trigger(DelayBeforeTransition, FadeTween, FaderID, false, LevelManager.Instance.Players[0].transform.position);
			}

			// we freeze time if needed
			if (FreezeTime)
			{
				MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0f, 0f, false, 0f, true);
			}

			// we freeze our character if needed
			if (FreezeCharacter && (_character != null))
			{
				_character.Freeze();
			}

			// we wait for the duration of the specified delay
			yield return _delayWaitForSeconds;

			// finally we move to the next level
			GoToNextLevel();
		}

		/// <summary>
		/// Loads the next level
		/// </summary>
		public virtual void GoToNextLevel()
		{
			if (LevelManager.HasInstance)
			{
				LevelManager.Instance.GotoLevel(LevelName, (DelayBeforeTransition == 0f));
			}
			else
			{
				MMSceneLoadingManager.LoadScene(LevelName);
			}
		}
	}
}