using DunGen.Editor.Windows;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DunGen.Editor.Inspectors
{
	[CustomEditor(typeof(DunGenSettings))]
	public sealed class DunGenSettingsInspector : UnityEditor.Editor
	{
		private static class Label
		{
			public static readonly GUIContent IgnoreSpriteBounds = new GUIContent("Ignore Sprite Bounds", "If true, sprite components will be ignored when calculating bounding volumes for tiles");
			public static readonly GUIContent RecalculateTileBoundsOnSave = new GUIContent("Recalculate On Save", "If true, tile bounds will automatically be recalculated every time the tile is saved. Otherwise, bounds must be recalculated manually in the Tile component inspector");
			public static readonly GUIContent BroadphaseSettings = new GUIContent("Collision Broadphase", "The settings to use for broadphase collision detection");
			public static readonly GUIContent TilePooling = new GUIContent("Enable Tile Pooling", "If true, tile instances will be re-used instead of being destroyed, improving generation performance at the cost of increased memory consumption");
			public static readonly GUIContent DisplayFailureReportWindow = new GUIContent("Display Failure Report Window", "If true, a window will be displayed when a generation failure occurs, allowing you to inspect the failure report");
			public static readonly GUIContent CheckForRemovedFiles = new GUIContent("Check for Unused Files", "DunGen will check if any old files from previous versions are still present in the DunGen directory and will present a list of files to potentially delete");
			public static readonly GUIContent CleanDunGenDirectory = new GUIContent("Remove Unused Files", "Removes any DunGen files left over from previous versions that are no longer in use");
		}

		private SerializedProperty ignoreSpriteBounds;
		private SerializedProperty recalculateTileBoundsOnSave;
		private SerializedProperty broadphaseSettings;
		private SerializedProperty enableTilePooling;
		private SerializedProperty displayFailureReportWindow;
		private SerializedProperty checkForUnusedFiles;


		private void OnEnable()
		{
			ignoreSpriteBounds = serializedObject.FindProperty(nameof(DunGenSettings.BoundsCalculationsIgnoreSprites));
			recalculateTileBoundsOnSave = serializedObject.FindProperty(nameof(DunGenSettings.RecalculateTileBoundsOnSave));
			broadphaseSettings = serializedObject.FindProperty(nameof(DunGenSettings.BroadphaseSettings));
			enableTilePooling = serializedObject.FindProperty(nameof(DunGenSettings.EnableTilePooling));
			displayFailureReportWindow = serializedObject.FindProperty(nameof(DunGenSettings.DisplayFailureReportWindow));
			checkForUnusedFiles = serializedObject.FindProperty(nameof(DunGenSettings.CheckForUnusedFiles));
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawGeneralProperties();
			EditorGUILayout.Space();
			DrawOptimizationProperties();
			EditorGUILayout.Space();
			DrawBoundsProperties();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawGeneralProperties()
		{
			var obj = target as DunGenSettings;

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			{
				EditorGUILayout.LabelField("General", EditorStyles.boldLabel);

				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Default Socket", obj.DefaultSocket, typeof(DoorwaySocket), false);
				EditorGUI.EndDisabledGroup();

				EditorGUILayout.PropertyField(displayFailureReportWindow, Label.DisplayFailureReportWindow);
				EditorGUILayout.PropertyField(checkForUnusedFiles, Label.CheckForRemovedFiles);

				if (GUILayout.Button(Label.CleanDunGenDirectory))
					DunGenFolderCleaningWindow.CleanDunGenDirectory();
			}
			EditorGUILayout.EndVertical();
		}

		private void DrawOptimizationProperties()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			{
				EditorGUILayout.LabelField("Optimization", EditorStyles.boldLabel);

				// Warn the user about the limitations of the quadtree implementation
				if (broadphaseSettings.managedReferenceFullTypename.ToLower().Contains("quadtree"))
					EditorGUILayout.HelpBox("Quadtree broadphase is experimental and is currently not recommended. Only supports dungeons using the default up vector (Positive Y)", MessageType.Warning);

				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(broadphaseSettings, Label.BroadphaseSettings);
				EditorGUI.indentLevel--;

				if (enableTilePooling.boolValue)
					EditorGUILayout.HelpBox("Tile pooling requires special attention to how your tiles are structured. Please consult the documentation for more information", MessageType.Warning);

				EditorGUILayout.PropertyField(enableTilePooling, Label.TilePooling);

			}
			EditorGUILayout.EndVertical();
		}

		private void DrawBoundsProperties()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			{
				EditorGUILayout.LabelField("Tile Bounds", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(ignoreSpriteBounds, Label.IgnoreSpriteBounds);
				EditorGUILayout.PropertyField(recalculateTileBoundsOnSave, Label.RecalculateTileBoundsOnSave);

				EditorGUILayout.Space();

				var rect = EditorGUILayout.GetControlRect();
				var buttonRect = rect;
				var dropdownRect = new Rect(buttonRect.xMax - 20, buttonRect.y, 20, buttonRect.height);
				buttonRect.width -= 20;

				if (GUI.Button(buttonRect, "Calculate Missing Tile Bounds"))
					AskUserToCacheBounds(false);

				if (EditorGUI.DropdownButton(dropdownRect, GUIContent.none, FocusType.Passive))
				{
					var menu = new GenericMenu();
					menu.AddItem(new GUIContent("Clear and Recalculate All Tile Bounds"), false, () => AskUserToCacheBounds(true));
					menu.DropDown(dropdownRect);
				}
			}
			EditorGUILayout.EndVertical();
		}

		private static void AskUserToCacheBounds(bool clearExisting)
		{
			string message = clearExisting
				? "This will clear and recalculate bounds for all Tile prefabs in your project that belong to a TileSet. This may take some time. Continue?"
				: "This will calculate missing bounds for Tile prefabs in your project that belong to a TileSet. This may take some time. Continue?";

			if (!EditorUtility.DisplayDialog("Calculate Tile Bounds", message, "Yes", "No"))
				return;

			IEnumerable<Tile> tiles = GetAllTilePrefabs();

			if (!clearExisting)
				tiles = tiles.Where(t => !t.HasValidBounds);

			if (tiles.Count() == 0)
				EditorUtility.DisplayDialog("Calculate Tile Bounds", "No tile bounds were missing", "OK");
			else
			{
				int cachedBoundsCount = CacheTileBounds(tiles, clearExisting);
				EditorUtility.DisplayDialog("Calculate Tile Bounds", $"Recalculated bounds for {cachedBoundsCount} tiles", "OK");
			}
		}

		private static HashSet<Tile> GetAllTilePrefabs()
		{
			var tiles = new HashSet<Tile>();

			// Find all the TileSets in the project
			// This is much faster than loading all prefabs in the project and checking if they have a Tile component
			var tileSetGuids = AssetDatabase.FindAssets("t:TileSet");

			foreach (var guid in tileSetGuids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var tileSet = AssetDatabase.LoadAssetAtPath<TileSet>(path);

				if(tileSet != null)
					tiles.UnionWith(tileSet.TileWeights.Weights.Select(w => w.Value.GetComponent<Tile>()));
			}

			return tiles;
		}

		private static int CacheTileBounds(IEnumerable<Tile> tiles, bool force)
		{
			int updatedBoundsCount = 0;

			foreach (var tile in tiles)
			{
				bool boundsUpdated = tile.RecalculateBounds();

				if (boundsUpdated)
				{
					updatedBoundsCount++;
					EditorUtility.SetDirty(tile);
				}
			}

			return updatedBoundsCount;
		}
	}
}
