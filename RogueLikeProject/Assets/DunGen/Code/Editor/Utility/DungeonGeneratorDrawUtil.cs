using UnityEditor;
using UnityEngine;

namespace DunGen.Editor
{
	public static class DungeonGeneratorDrawUtil
	{
		private static class Labels
		{
			public static readonly GUIContent RandomizeSeed = new GUIContent("Randomize Seed", "If checked, a new random seed will be created every time a dungeon is generated. If unchecked, a specific seed will be used each time");
			public static readonly GUIContent Seed = new GUIContent("Seed", "The seed used to generate a dungeon layout. Generating a dungeon multiple times with the same seed will produce the exact same results each time");
			public static readonly GUIContent MaxFailedAttempts = new GUIContent("Max Failed Attempts", "The maximum number of times DunGen is allowed to fail at generating a dungeon layout before giving up. This only applies in-editor; in a packaged build, DunGen will keep trying indefinitely");
			public static readonly GUIContent LengthMultiplier = new GUIContent("Length Multiplier", "Used to alter the length of the dungeon without modifying the Dungeon Flow asset. 1 = normal-length, 2 = double-length, 0.5 = half-length, etc.");
			public static readonly GUIContent UpDirection = new GUIContent("Up Direction", "The up direction of the dungeon. This won't actually rotate your dungeon, but it must match the expected up-vector for your dungeon layout - usually +Y for 3D and side-on 2D, -Z for top-down 2D");
			public static readonly GUIContent TriggerPlacement = new GUIContent("Trigger Placement", "Places trigger colliders around Tiles which can be used in conjunction with the DungenCharacter component to receive events when changing rooms");
			public static readonly GUIContent TriggerLayer = new GUIContent("Trigger Layer", "The layer to place the tile root objects on if \"Place Tile Triggers\" is checked");
			public static readonly GUIContent GenerateAsynchronously = new GUIContent("Generate Asynchronously", "If checked, DunGen will generate the layout without blocking Unity's main thread, allowing for things like animated loading screens to be shown");
			public static readonly GUIContent MaxFrameTime = new GUIContent("Max Frame Time", "How many milliseconds the dungeon generation is allowed to take per-frame");
			public static readonly GUIContent PauseBetweenRooms = new GUIContent("Pause Between Rooms", "If greater than zero, the dungeon generation will pause for the set time (in seconds) after placing a room; useful for visualising the generation process");
			public static readonly GUIContent OverlapThreshold = new GUIContent("Overlap Threshold", "Maximum distance two connected tiles are allowed to overlap without being discarded. If doorways aren't exactly on the tile's axis-aligned bounding box, two tiles can overlap slighty when connected. This property can help to fix this issue");
			public static readonly GUIContent CollideAllDungeons = new GUIContent("Collide All Dungeons", "When placing a new tile, DunGen will test for collisions against all tiles in every dungeon in the scene, instead of just this one");
			public static readonly GUIContent DisallowOverhangs = new GUIContent("Disallow Overhangs", "If checked, two tiles cannot overlap along the Up-Vector (a room cannot spawn above another room)");
			public static readonly GUIContent Padding = new GUIContent("Padding", "A minimum buffer distance between two unconnected tiles");
			public static readonly GUIContent RestrictToBounds = new GUIContent("Restrict to Bounds?", "If checked, tiles will only be placed within the specified bounds below. May increase generation times");
			public static readonly GUIContent PlacementBounds = new GUIContent("Placement Bounds", "Tiles are not allowed to be placed outside of these bounds");
			public static readonly GUIContent RepeatMode = new GUIContent("Repeat Mode");

			public static readonly GUIContent[] UpDirectionDisplayOptions = new GUIContent[]
			{
				new GUIContent("+X"),
				new GUIContent("-X"),
				new GUIContent("+Y"),
				new GUIContent("-Y"),
				new GUIContent("+Z"),
				new GUIContent("-Z")
			};
		}

		public static void DrawDungeonGenerator(SerializedProperty dungeonGeneratorProp, bool isRuntimeDungeon)
		{
			var dungeonFlowProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.DungeonFlow));
			var shouldRandomizeSeedProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.ShouldRandomizeSeed));
			var seedProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.Seed));
			var maxAttemptCountProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.MaxAttemptCount));
			var lengthMultiplierProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.LengthMultiplier));
			var upDirectionProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.UpDirection));
			var debugRenderProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.DebugRender));
			var triggerPlacementProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.TriggerPlacement));
			var tileTriggerLayerProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.TileTriggerLayer));
			var generateAsyncProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.GenerateAsynchronously));
			var maxAsyncFrameMsProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.MaxAsyncFrameMilliseconds));
			var pauseBetweenRoomsProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.PauseBetweenRooms));
			var restrictToBoundsProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.RestrictDungeonToBounds));
			var placementBoundsProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.TilePlacementBounds));
			var overrideRepeatModeProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.OverrideRepeatMode));
			var repeatModeProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.RepeatMode));
			var overrideAllowTileRotationProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.OverrideAllowTileRotation));
			var allowTileRotationProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.AllowTileRotation));
			var collisionSettingsProp = dungeonGeneratorProp.FindPropertyRelative(nameof(DungeonGenerator.CollisionSettings));

			EditorGUILayout.PropertyField(dungeonFlowProp);
			EditorGUILayout.PropertyField(shouldRandomizeSeedProp, Labels.RandomizeSeed);

			if (!shouldRandomizeSeedProp.boolValue)
				EditorGUILayout.PropertyField(seedProp, Labels.Seed);

			EditorGUILayout.PropertyField(lengthMultiplierProp, Labels.LengthMultiplier);

			upDirectionProp.enumValueIndex = EditorGUILayout.Popup(Labels.UpDirection, upDirectionProp.enumValueIndex, Labels.UpDirectionDisplayOptions);

			if (lengthMultiplierProp.floatValue < 0)
				lengthMultiplierProp.floatValue = 0.0f;

			if (isRuntimeDungeon)
			{
				EditorGUILayout.Space();

				EditorGUILayout.BeginVertical("box");
				{
					EditorGUI.indentLevel++;
					generateAsyncProp.isExpanded = EditorGUILayout.Foldout(generateAsyncProp.isExpanded, "Asynchronous Generation", true);

					if (generateAsyncProp.isExpanded)
					{
						EditorGUILayout.PropertyField(generateAsyncProp, Labels.GenerateAsynchronously);

						var unitsLabelSize = EditorStyles.label.CalcSize(new GUIContent("milliseconds"));

						EditorGUI.BeginDisabledGroup(!generateAsyncProp.boolValue);
						EditorGUILayout.BeginHorizontal();
						maxAsyncFrameMsProp.floatValue = EditorGUILayout.Slider(Labels.MaxFrameTime, maxAsyncFrameMsProp.floatValue, 0f, 1000f);
						EditorGUILayout.LabelField("milliseconds", GUILayout.Width(unitsLabelSize.x));
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						pauseBetweenRoomsProp.floatValue = EditorGUILayout.Slider(Labels.PauseBetweenRooms, pauseBetweenRoomsProp.floatValue, 0, 5);
						EditorGUILayout.LabelField("seconds", GUILayout.Width(unitsLabelSize.x));
						EditorGUILayout.EndHorizontal();
						EditorGUI.EndDisabledGroup();
					}
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndVertical();
			}

			// Collision Settings
			if (collisionSettingsProp != null)
			{
				EditorGUILayout.BeginVertical("box");
				{
					EditorGUI.indentLevel++;
					collisionSettingsProp.isExpanded = EditorGUILayout.Foldout(collisionSettingsProp.isExpanded, "Collision", true);

					if (collisionSettingsProp.isExpanded)
					{
						EditorGUILayout.PropertyField(triggerPlacementProp, Labels.TriggerPlacement);

						EditorGUI.BeginDisabledGroup(triggerPlacementProp.enumValueIndex == 0);
						{
							tileTriggerLayerProp.intValue = EditorGUILayout.LayerField(Labels.TriggerLayer, tileTriggerLayerProp.intValue);
						}
						EditorGUI.EndDisabledGroup();

						EditorGUILayout.Space();

						EditorGUILayout.PropertyField(collisionSettingsProp.FindPropertyRelative("OverlapThreshold"), Labels.OverlapThreshold);
						EditorGUILayout.PropertyField(collisionSettingsProp.FindPropertyRelative("AvoidCollisionsWithOtherDungeons"), Labels.CollideAllDungeons);
						EditorGUILayout.PropertyField(collisionSettingsProp.FindPropertyRelative("DisallowOverhangs"), Labels.DisallowOverhangs);

						var paddingProp = collisionSettingsProp.FindPropertyRelative("Padding");
						EditorGUI.BeginChangeCheck();

						float padding = EditorGUILayout.DelayedFloatField(Labels.Padding, paddingProp.floatValue);

						if (EditorGUI.EndChangeCheck())
							paddingProp.floatValue = Mathf.Max(0f, padding);
					}
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndVertical();
			}

			// Constraints
			EditorGUILayout.BeginVertical("box");
			{
				EditorGUI.indentLevel++;
				restrictToBoundsProp.isExpanded = EditorGUILayout.Foldout(restrictToBoundsProp.isExpanded, "Constraints", true);

				if (restrictToBoundsProp.isExpanded)
				{
					EditorGUILayout.HelpBox("Constraints can make dungeon generation more likely to fail. Stricter constraints increase the chance of failure.", MessageType.Info);
					EditorGUILayout.Space();

					EditorGUILayout.PropertyField(restrictToBoundsProp, Labels.RestrictToBounds);

					EditorGUI.BeginDisabledGroup(!restrictToBoundsProp.boolValue);
					EditorGUILayout.PropertyField(placementBoundsProp, Labels.PlacementBounds);
					EditorGUI.EndDisabledGroup();
				}

				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndVertical();

			// Overrides
			EditorGUILayout.BeginVertical("box");
			{
				EditorGUI.indentLevel++;
				overrideRepeatModeProp.isExpanded = EditorGUILayout.Foldout(overrideRepeatModeProp.isExpanded, "Global Overrides", true);

				if (overrideRepeatModeProp.isExpanded)
				{
					EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.PropertyField(overrideRepeatModeProp, GUIContent.none, GUILayout.Width(10));
						EditorGUI.BeginDisabledGroup(!overrideRepeatModeProp.boolValue);
						EditorGUILayout.PropertyField(repeatModeProp, Labels.RepeatMode);
						EditorGUI.EndDisabledGroup();
					}
					EditorGUILayout.EndHorizontal();

					DrawOverride("Allow Tile Rotation", overrideAllowTileRotationProp, allowTileRotationProp);
				}

				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndVertical();

			// Debug Settings
			EditorGUILayout.BeginVertical("box");
			{
				EditorGUI.indentLevel++;
				debugRenderProp.isExpanded = EditorGUILayout.Foldout(debugRenderProp.isExpanded, "Debug", true);

				if (debugRenderProp.isExpanded)
				{
					if (isRuntimeDungeon)
						EditorGUILayout.PropertyField(debugRenderProp);

					EditorGUILayout.PropertyField(maxAttemptCountProp, Labels.MaxFailedAttempts);
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndVertical();
		}

		private static void DrawOverride(string label, SerializedProperty overrideProp, SerializedProperty valueProp)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(overrideProp, GUIContent.none, GUILayout.Width(10));
			EditorGUI.BeginDisabledGroup(!overrideProp.boolValue);
			EditorGUILayout.PropertyField(valueProp, new GUIContent(label));
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		}

	}
}
