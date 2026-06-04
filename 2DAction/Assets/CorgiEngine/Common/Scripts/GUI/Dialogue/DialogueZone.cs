using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// Add this class to an empty component. It will automatically add a boxcollider2d, set it to "is trigger". Then customize the dialogue zone
	/// through the inspector.
	/// </summary>
	[RequireComponent(typeof(Collider2D))]

	public class DialogueZone : ButtonActivated
	{
		[MMInspectorGroup("Dialogue Look", true, 22)]
		/// the prefab to use for the dialogue box
		[Tooltip("ダイアログボックスPrefab")]
		[Header("ダイアログボックスPrefab")]
		public GameObject DialogueBoxPrefab;
		/// the color of the text background.
		[Tooltip("テキスト背景色")]
		[Header("テキスト背景色")]
		public Color TextBackgroundColor=Color.black;
		/// the color of the text
		[Tooltip("テキスト色")]
		[Header("テキスト色")]
		public Color TextColor=Color.white;
		/// if true, the dialogue box will have a small, downward pointing arrow
		[Tooltip("矢印表示")]
		[Header("矢印表示")]
		public bool ArrowVisible=true;
		/// the font that should be used to display the text
		[Tooltip("テキストフォント")]
		[Header("テキストフォント")]
		public Font TextFont;
		/// the size of the font
		[Tooltip("テキストサイズ")]
		[Header("テキストサイズ")]
		public int TextSize = 20;
		/// the text alignment in the box used to display the text
		[Tooltip("テキスト整列")]
		[Header("テキスト整列")]
		public TextAnchor Alignment = TextAnchor.MiddleCenter;

		[MMInspectorGroup("Dialogue Speed (in seconds)", true, 23)]

		/// the duration of the in and out fades
		[Tooltip("フェード持続時間")]
		[Header("フェード持続時間")]
		public float FadeDuration=0.2f;
		/// the time between two dialogues
		[Tooltip("遷移時間")]
		[Header("遷移時間")]
		public float TransitionTime=0.2f;

		[MMInspectorGroup("Dialogue Position", true, 24)]

		/// the distance from the top of the box collider the dialogue box should appear at
		[Tooltip("上端からの距離")]
		[Header("上端からの距離")]
		public float DistanceFromTop=0;
		/// if this is true, the dialogue boxes will follow the zone's position
		[Tooltip("ゾーンに追従")]
		[Header("ゾーンに追従")]
		public bool BoxesFollowZone = false;

		[MMInspectorGroup("Player Movement", true, 25)]

		/// if this is set to true, the character will be able to move while dialogue is in progress
		[Tooltip("会話中移動可")]
		[Header("会話中移動可")]
		public bool CanMoveWhileTalking = true;

		[MMInspectorGroup("Button Handling", true, 26)]

		/// whether this dialogue zone is operated via the CharacterButtonActivation ability or not
		[Tooltip("ボタン操作")]
		[Header("ボタン操作")]
		public bool ButtonHandled=true;
		/// duration of the message. only considered if the box is not button handled
		[Range (1, 100)]
		[Tooltip("メッセージ持続時間")]
		[Header("メッセージ持続時間")]
		public float MessageDuration=3f;

		[MMInspectorGroup("Activations", true, 28)]

		/// true if can be activated more than once
		[Tooltip("複数回起動可")]
		[Header("複数回起動可")]
		public bool ActivableMoreThanOnce=true;
		/// if the zone is activable more than once, how long should it remain inactive between up times ?
		[Range (1, 100)]
		[Tooltip("非活性時間")]
		[Header("非活性時間")]
		public float InactiveTime=2f;

		[MMInspectorGroup("Dialogue Lines", true, 29)]

		/// the dialogue lines
		[Multiline]
		[Tooltip("ダイアログ行")]
		[Header("ダイアログ行")]
		public string[] Dialogue;

		/// private variables
		protected DialogueBox _dialogueBox;
		protected bool _activated=false;
		protected bool _playing=false;
		protected int _currentIndex;
		protected bool _activable=true;
		protected WaitForSeconds _transitionTimeWFS;
		protected WaitForSeconds _messageDurationWFS;
		protected WaitForSeconds _inactiveTimeWFS;

		/// <summary>
		/// Initializes the dialogue zone
		/// </summary>
		protected override void OnEnable ()
		{
			base.OnEnable();
			_currentIndex=0;
			_transitionTimeWFS = new WaitForSeconds (TransitionTime);
			_messageDurationWFS = new WaitForSeconds (MessageDuration);
			_inactiveTimeWFS = new WaitForSeconds (InactiveTime);
		}

		/// <summary>
		/// When the button is pressed we start the dialogue
		/// </summary>
		public override void TriggerButtonAction(GameObject instigator)
		{
			if (!CheckNumberOfUses())
			{
				return;
			}
			if (_playing && !ButtonHandled)
			{
				return;
			}
			base.TriggerButtonAction (instigator);
			StartDialogue();
			ActivateZone ();
		}

		protected override void DisableAfterActivation()
		{

		}

		/// <summary>
		/// When triggered, either by button press or simply entering the zone, starts the dialogue
		/// </summary>
		public virtual void StartDialogue()
		{
			// if the dialogue zone has no box collider, we do nothing and exit
			if (_buttonActivatedZoneCollider==null)
			{
				return;
			}

			// if the zone has already been activated and can't be activated more than once.
			if (_activated && !ActivableMoreThanOnce)
			{
				return;
			}

			// if the zone is not activable, we do nothing and exit
			if (!_activable)
			{
				return;
			}

			// if the player can't move while talking, we notify the game manager
			if (!CanMoveWhileTalking)
			{
				LevelManager.Instance.FreezeCharacters();
				if (ShouldUpdateState)
				{
					_characterButtonActivation.GetComponentInParent<Character>().MovementState.ChangeState(CharacterStates.MovementStates.Idle);
				}
			}

			// if it's not already playing, we'll initialize the dialogue box
			if (!_playing)
			{
				// we instantiate the dialogue box
				GameObject dialogueObject = (GameObject)Instantiate(DialogueBoxPrefab);
				_dialogueBox = dialogueObject.GetComponent<DialogueBox>();
				// we set its position
				_dialogueBox.transform.position=new Vector2(_buttonActivatedZoneCollider.bounds.center.x,_buttonActivatedZoneCollider.bounds.max.y+DistanceFromTop);
				// we set the color's and background's colors
				_dialogueBox.ChangeColor(TextBackgroundColor,TextColor);
				// if it's a button handled dialogue, we turn the A prompt on
				_dialogueBox.ButtonActive(ButtonHandled);
				// if font settings have been specified, we set them

				if (BoxesFollowZone)
				{
					_dialogueBox.transform.SetParent (this.gameObject.transform);
				}

				if (TextFont != null)
				{
					_dialogueBox.DialogueText.font = TextFont;
				}
				if (TextSize != 0)
				{
					_dialogueBox.DialogueText.fontSize = TextSize;
				}
				_dialogueBox.DialogueText.alignment = Alignment;

				// if we don't want to show the arrow, we tell that to the dialogue box
				if (!ArrowVisible)
				{
					_dialogueBox.HideArrow();
				}

				// the dialogue is now playing
				_playing=true;
			}
			// we start the next dialogue
			StartCoroutine(PlayNextDialogue());
		}

		/// <summary>
		/// Plays the next dialogue in the queue
		/// </summary>
		protected virtual IEnumerator PlayNextDialogue()
		{
			// we check that the dialogue box still exists
			if (_dialogueBox == null)
			{
				yield break;
			}
			// if this is not the first message
			if (_currentIndex != 0)
			{
				// we turn the message off
				_dialogueBox.FadeOut(FadeDuration);
				// we wait for the specified transition time before playing the next dialogue
				yield return _transitionTimeWFS;
			}

			// if we've reached the last dialogue line, we exit
			if (_currentIndex >= Dialogue.Length)
			{
				_currentIndex = 0;
				Destroy(_dialogueBox.gameObject);
				_buttonActivatedZoneCollider.enabled = false;
				// we set activated to true as the dialogue zone has now been turned on
				_activated = true;
				// we let the player move again
				if (!CanMoveWhileTalking)
				{
					LevelManager.Instance.UnFreezeCharacters();
				}
				if ((_characterButtonActivation!=null))
				{
					_characterButtonActivation.InButtonActivatedZone=false;
					_characterButtonActivation.ButtonActivatedZone=null;
				}
				// we turn the zone inactive for a while
				if (ActivableMoreThanOnce)
				{
					_activable=false;
					_playing=false;
					StartCoroutine(Reactivate());
				}
				else
				{
					gameObject.SetActive(false);
				}

				if (DisableAfterUse && (_numberOfActivationsLeft <= 0))
				{
					DisableZone();
				}

				yield break;
			}

			// we check that the dialogue box still exists
			if (_dialogueBox.DialogueText!=null)
			{
				// every dialogue box starts with it fading in
				_dialogueBox.FadeIn(FadeDuration);
				// then we set the box's text with the current dialogue
				_dialogueBox.DialogueText.text = Dialogue[_currentIndex];
			}

			_currentIndex++;

			// if the zone is not button handled, we start a coroutine to autoplay the next dialogue
			if (!ButtonHandled)
			{
				StartCoroutine(AutoNextDialogue());
			}
		}

		/// <summary>
		/// Automatically goes to the next dialogue line
		/// </summary>
		/// <returns>The next dialogue.</returns>
		protected virtual IEnumerator AutoNextDialogue()
		{
			// we wait for the duration of the message
			yield return _messageDurationWFS;
			StartCoroutine(PlayNextDialogue());
		}

		/// <summary>
		/// Reactivate the dialogue zone
		/// </summary>
		protected virtual IEnumerator Reactivate()
		{
			yield return _inactiveTimeWFS;
			_buttonActivatedZoneCollider.enabled=true;
			_activable=true;
			_playing=false;
			_currentIndex=0;
			_promptHiddenForever = false;

			if (AlwaysShowPrompt)
			{
				ShowPrompt();
			}

		}
	}
}
