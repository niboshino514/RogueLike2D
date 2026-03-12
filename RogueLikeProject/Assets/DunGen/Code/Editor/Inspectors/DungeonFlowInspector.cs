using DunGen.Editor.Validation;
using DunGen.Graph;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DunGen.Editor
{
	[CustomEditor(typeof(DungeonFlow))]
	public sealed class DungeonFlowInspector : UnityEditor.Editor
	{
		#region Helpers

		private sealed class Properties
		{
			public SerializedProperty Length;
			public SerializedProperty BranchMode;
			public SerializedProperty BranchCount;
			public SerializedProperty KeyManager;
			public SerializedProperty DoorwayConnectionChance;
			public SerializedProperty TileInjectionRules;
			public SerializedProperty RestrictConnectionToSameSection;
			public SerializedProperty TileTagConnectionMode;
			public SerializedProperty TileConnectionTags;
			public SerializedProperty BranchTagPruneMode;
			public SerializedProperty BranchPruneTags;
			public SerializedProperty StraighteningSettings;

			public ReorderableList GlobalProps;
			public ReorderableList TileConnectionTagsList;
			public ReorderableList BranchPruneTagsList;
			public ReorderableList TileInjectionRulesList;
		}

		private static class Labels
		{
			public static readonly GUIContent Validate = new GUIContent("Validate Dungeon", "Runs a set of automated checks on the integrity of the dungeon, reporting any errors that are found");
			public static readonly GUIContent Length = new GUIContent("Length", "Min and max length of the main path. This will determine how long the dungeon is");
			public static readonly GUIContent BranchMode = new GUIContent("Branch Mode", "Determines how the number of branches is computed");
			public static readonly GUIContent BranchCount = new GUIContent("Branch Count", "The total number of branches to appear accross the entire dungeon. Only used when Branch Mode is set to Global");
			public static readonly GUIContent GlobalProps = new GUIContent("Global Props");
			public static readonly GUIContent KeyManager = new GUIContent("Key Manager", "Defines which keys are available to be placed throughout the dungeon. This can be left blank if you don't want to make use of the lock & key system");
			public static readonly GUIContent DoorwayConnectionHeader = new GUIContent("Doorway Connection");
			public static readonly GUIContent DoorwayConnectionChance = new GUIContent("Connection Chance", "The percentage chance that an unconnected but overlapping set of doorways will be connected. This can be overriden on a per-tile basis");
			public static readonly GUIContent RestrictConnectionToSameSection = new GUIContent("Restrict to Same Section", "If checked, doorways will only be connected if they lie on the same line segment in the dungeon flow graph");
			public static readonly GUIContent TileInjection = new GUIContent("Special Tile Injection", "Used to inject specific tiles into the dungeon layout based on a set of rules");
			public static readonly GUIContent OpenFlowEditor = new GUIContent("Open Flow Editor", "The node graph lets you design how the dungeon should be laid out");
			public static readonly GUIContent GlobalPropGroupID = new GUIContent("Group ID", "The prop ID. This should match the ID on the GlobalProp component placed inside Tiles");
			public static readonly GUIContent GlobalPropGroupCount = new GUIContent("Count", "The number of times this prop should appear across the entire dungeon");
			public static readonly GUIContent TileConnectionTagMode = new GUIContent("Mode", "How to apply the tag rules below. NOTE: This section is ignored if the tag pair list is empty.\n    Accept: Tiles are only connected if their tags match one of the pairs in the list below.\n    Reject: Tiles will always connect unless their tags match one of the pairs in the list below.");
			public static readonly GUIContent TileConnectionTags = new GUIContent("Tag Pairs");
			public static readonly GUIContent TileConnectionRules = new GUIContent("Tile Connection Rules", "Allows us to accept or reject a connection between two tiles based on the tags each of them have.");
			public static readonly GUIContent BranchPruneMode = new GUIContent("Branch Prune Mode", "The method by which tiles at the end of a branch are pruned based on the tags below.");
			public static readonly GUIContent BranchPruneTags = new GUIContent("Branch Prune Tags", "Tiles on the end of branches will be deleted depending on which tags they have. Based on the branch prune mode");
			public static readonly GUIContent PathStraighteningHeader = new GUIContent("Path Straightening", "Determines if and how the path should be straightened. These settings can be overridden in the Archetype asset and on Nodes in the flow graph");
			public static readonly GUIContent BranchingHeader = new GUIContent("Branching");

			public static readonly string LocalBranchMode = "In Local mode, the number of branches is calculated per-tile using the Archetype's 'Branch Count' property";
			public static readonly string GlobalBranchMode = "In Global mode, the number of branches is calculated across the entire dungeon. NOTE: The number of branches might be less than the specified minimum value, but will never be more than the maximum";
			public static readonly string SectionBranchMode = "In Section mode, the number of branches is calculated for each section using the 'Branch Count' property in that section's Archetype settings";
		}

		#endregion

		private Properties properties;


		private void OnEnable()
		{
			properties = new Properties()
			{
				Length = serializedObject.FindProperty(nameof(DungeonFlow.Length)),
				BranchMode = serializedObject.FindProperty(nameof(DungeonFlow.BranchMode)),
				BranchCount = serializedObject.FindProperty(nameof(DungeonFlow.BranchCount)),
				KeyManager = serializedObject.FindProperty(nameof(DungeonFlow.KeyManager)),
				DoorwayConnectionChance = serializedObject.FindProperty(nameof(DungeonFlow.DoorwayConnectionChance)),
				RestrictConnectionToSameSection = serializedObject.FindProperty(nameof(DungeonFlow.RestrictConnectionToSameSection)),
				TileInjectionRules = serializedObject.FindProperty(nameof(DungeonFlow.TileInjectionRules)),
				TileTagConnectionMode = serializedObject.FindProperty(nameof(DungeonFlow.TileTagConnectionMode)),
				TileConnectionTags = serializedObject.FindProperty(nameof(DungeonFlow.TileConnectionTags)),
				BranchTagPruneMode = serializedObject.FindProperty(nameof(DungeonFlow.BranchTagPruneMode)),
				BranchPruneTags = serializedObject.FindProperty(nameof(DungeonFlow.BranchPruneTags)),
				StraighteningSettings = serializedObject.FindProperty(nameof(DungeonFlow.GlobalStraighteningSettings)),

				GlobalProps = new ReorderableList(serializedObject, serializedObject.FindProperty("GlobalProps"), true, false, true, true)
				{
					drawElementCallback = (rect, index, isActive, isFocused) => DrawGlobalProp(rect, index),
					elementHeightCallback = GetGlobalPropHeight,
				},
			};

			properties.TileConnectionTagsList = new ReorderableList(serializedObject, properties.TileConnectionTags)
			{
				drawHeaderCallback = DrawTileConnectionTagsHeader,
				drawElementCallback = DrawTileConnectionTagsElement,
			};

			properties.BranchPruneTagsList = new ReorderableList(serializedObject, properties.BranchPruneTags)
			{
				drawHeaderCallback = (Rect rect) =>
				{
					EditorGUI.LabelField(rect, Labels.BranchPruneTags);
				},
				drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
				{
					EditorGUI.PropertyField(rect, properties.BranchPruneTags.GetArrayElementAtIndex(index), GUIContent.none);
				},
			};

			properties.TileInjectionRulesList = new ReorderableList(serializedObject, properties.TileInjectionRules, true, true, true, true)
			{
				drawHeaderCallback = rect =>
				{
					EditorGUI.LabelField(rect, Labels.TileInjection);
				},
				drawElementCallback = DrawTileInjectionRule,
				elementHeightCallback = index =>
				{
					var element = properties.TileInjectionRules.GetArrayElementAtIndex(index);

					// If collapsed, just one line
					if (!element.isExpanded)
						return EditorGUIUtility.singleLineHeight + 6;

					// If expanded, enough lines for all fields
					int lines = 8;
					return (lines + 1) * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + 4;
				}
			};

			var flow = target as DungeonFlow;

			if (flow != null)
			{
				foreach (var line in flow.Lines)
					line.Graph = flow;
				foreach (var node in flow.Nodes)
					node.Graph = flow;
			}
		}

		private void DrawTileConnectionTagsHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, Labels.TileConnectionTags);
		}

		private void DrawTileConnectionTagsElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			EditorGUI.PropertyField(rect, properties.TileConnectionTags.GetArrayElementAtIndex(index), GUIContent.none);
		}

		private void DrawTileInjectionRule(Rect rect, int index, bool isActive, bool isFocused)
		{
			var element = properties.TileInjectionRules.GetArrayElementAtIndex(index);
			rect.y += 2;
			float lineHeight = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;

			var tileSetProp = element.FindPropertyRelative(nameof(TileInjectionRule.TileSet));
			string tileSetName = "None";

			if (tileSetProp != null && tileSetProp.objectReferenceValue != null)
				tileSetName = tileSetProp.objectReferenceValue.name;

			// Foldout
			element.isExpanded = EditorGUI.Foldout(
				new Rect(rect.x + 10, rect.y, rect.width, lineHeight),
				element.isExpanded,
				new GUIContent(tileSetName),
				true);

			if (!element.isExpanded)
				return;

			float y = rect.y + lineHeight + spacing;

			// TileSet
			EditorGUI.PropertyField(
				new Rect(rect.x, y, rect.width, lineHeight),
				tileSetProp, new GUIContent("Tile Set"));
			y += lineHeight + spacing;

			// IsRequired
			var isRequiredProp = element.FindPropertyRelative("IsRequired");
			EditorGUI.PropertyField(
				new Rect(rect.x, y, rect.width, lineHeight),
				isRequiredProp, new GUIContent("Is Required?"));
			y += lineHeight + spacing;

			// CanAppearOnMainPath
			var canAppearOnMainPathProp = element.FindPropertyRelative("CanAppearOnMainPath");
			EditorGUI.PropertyField(
				new Rect(rect.x, y, rect.width, lineHeight),
				canAppearOnMainPathProp, new GUIContent("Can appear on Main Path?"));
			y += lineHeight + spacing;

			// CanAppearOnBranchPath
			var canAppearOnBranchPathProp = element.FindPropertyRelative("CanAppearOnBranchPath");
			EditorGUI.PropertyField(
				new Rect(rect.x, y, rect.width, lineHeight),
				canAppearOnBranchPathProp, new GUIContent("Can appear on Branch Path?"));
			y += lineHeight + spacing;

			// IsLocked
			var isLockedProp = element.FindPropertyRelative("IsLocked");
			EditorGUI.PropertyField(
				new Rect(rect.x, y, rect.width, lineHeight),
				isLockedProp, new GUIContent("Locked"));
			y += lineHeight + spacing;

			// LockID
			EditorGUI.BeginDisabledGroup(isLockedProp == null || !isLockedProp.boolValue);
			{
				var lockIDProp = element.FindPropertyRelative("LockID");
				var dungeonFlow = target as DungeonFlow;

				int keyID = lockIDProp.intValue;

				EditorGUI.BeginChangeCheck();
				EditorUtil.DrawKey(
					new Rect(rect.x, y, rect.width, lineHeight),
					new GUIContent("Lock Type"), dungeonFlow.KeyManager, ref keyID);

				if (EditorGUI.EndChangeCheck())
					lockIDProp.intValue = keyID;
			}
			EditorGUI.EndDisabledGroup();
			y += lineHeight + spacing;

			// NormalizedPathDepth
			var pathDepthProp = element.FindPropertyRelative("NormalizedPathDepth");
			EditorGUI.PropertyField(
				new Rect(rect.x, y, rect.width, lineHeight),
				pathDepthProp, new GUIContent("Path Depth"));
			y += lineHeight + spacing;

			// NormalizedBranchDepth
			var branchDepthProp = element.FindPropertyRelative("NormalizedBranchDepth");
			EditorGUI.PropertyField(
				new Rect(rect.x, y, rect.width, lineHeight),
				branchDepthProp, new GUIContent("Branch Depth"));
		}

		private string GetCurrentBranchModeLabel()
		{
			var dungeonFlow = target as DungeonFlow;

			switch (dungeonFlow.BranchMode)
			{
				case BranchMode.Local:
					return Labels.LocalBranchMode;
				case BranchMode.Global:
					return Labels.GlobalBranchMode;
				case BranchMode.Section:
					return Labels.SectionBranchMode;

				default:
					throw new NotImplementedException(string.Format("{0}.{1} is not implemented", typeof(BranchMode).Name, dungeonFlow.BranchMode));
			}
		}

		public override void OnInspectorGUI()
		{
			var data = target as DungeonFlow;

			if (data == null)
				return;

			serializedObject.Update();

			if (GUILayout.Button(Labels.Validate))
				DungeonValidator.Instance.Validate(data);

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(properties.KeyManager, Labels.KeyManager);
			EditorGUILayout.PropertyField(properties.Length, Labels.Length);

			// Doorway Connections
			using (new EditorGUILayout.VerticalScope("box"))
			{
				EditorGUILayout.LabelField(Labels.DoorwayConnectionHeader, EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(properties.DoorwayConnectionChance, Labels.DoorwayConnectionChance);
				EditorGUILayout.PropertyField(properties.RestrictConnectionToSameSection, Labels.RestrictConnectionToSameSection);
			}

			// Straightening Section
			using (new EditorGUILayout.VerticalScope("box"))
			{
				EditorGUILayout.LabelField(Labels.PathStraighteningHeader, EditorStyles.boldLabel);
				EditorUtil.DrawStraightenSettings(properties.StraighteningSettings, true);
			}

			// Branches Section
			using (new EditorGUILayout.VerticalScope("box"))
			{
				EditorGUILayout.LabelField(Labels.BranchingHeader, EditorStyles.boldLabel);

				// Branch Mode
				EditorGUILayout.HelpBox(GetCurrentBranchModeLabel(), MessageType.Info);
				EditorGUILayout.PropertyField(properties.BranchMode, Labels.BranchMode);

				EditorGUI.BeginDisabledGroup(data.BranchMode != BranchMode.Global);
				EditorGUILayout.PropertyField(properties.BranchCount, Labels.BranchCount);
				EditorGUI.EndDisabledGroup();

				EditorGUILayout.Space();
				EditorGUILayout.Space();

				// Branch Prune Tags
				EditorGUILayout.PropertyField(properties.BranchTagPruneMode, Labels.BranchPruneMode);
				EditorGUILayout.Space();
				properties.BranchPruneTagsList.DoLayoutList();
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			// Open Flow Editor
			if (GUILayout.Button(Labels.OpenFlowEditor))
				DungeonFlowEditorWindow.Open(data);

			EditorGUILayout.Space();

			// Tile Injection Rules (ReorderableList)
			properties.TileInjectionRulesList.DoLayoutList();

			EditorGUILayout.Space();

			// Global Props
			var globalProps = properties.GlobalProps.serializedProperty;
			globalProps.isExpanded = EditorGUILayout.Foldout(globalProps.isExpanded, Labels.GlobalProps, true);

			if (globalProps.isExpanded)
			{
				EditorGUI.indentLevel++;
				properties.GlobalProps.DoLayoutList();
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			// Tile Connection Rules
			properties.TileConnectionTags.isExpanded = EditorGUILayout.Foldout(properties.TileConnectionTags.isExpanded, Labels.TileConnectionRules);

			if (properties.TileConnectionTags.isExpanded)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(properties.TileTagConnectionMode, Labels.TileConnectionTagMode);
				EditorGUILayout.Space();
				properties.TileConnectionTagsList.DoLayoutList();
				EditorGUI.indentLevel--;
			}


			if (GUI.changed)
				EditorUtility.SetDirty(data);

			serializedObject.ApplyModifiedProperties();
		}

		private float GetGlobalPropHeight(int index)
		{
			return EditorGUI.GetPropertyHeight(properties.GlobalProps.serializedProperty.GetArrayElementAtIndex(index));
		}

		private void DrawGlobalProp(Rect rect, int index)
		{
			var propProperty = properties.GlobalProps.serializedProperty.GetArrayElementAtIndex(index);
			EditorGUI.PropertyField(rect, propProperty);
		}
	}
}
