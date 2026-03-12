using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace DunGen.Editor
{
	[CustomEditor(typeof(RuntimeDungeon))]
	public sealed class RuntimeDungeonInspector : UnityEditor.Editor
	{
		private SerializedProperty generateOnStartProp;
		private SerializedProperty rootProp;

		private BoxBoundsHandle placementBoundsHandle;


		private void OnEnable()
		{
			generateOnStartProp = serializedObject.FindProperty(nameof(RuntimeDungeon.GenerateOnStart));
			rootProp = serializedObject.FindProperty(nameof(RuntimeDungeon.Root));

			placementBoundsHandle = new BoxBoundsHandle();
			placementBoundsHandle.SetColor(Color.magenta);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(generateOnStartProp);
			EditorGUILayout.PropertyField(rootProp, new GUIContent("Root", "An optional root object for the dungeon to be parented to. If blank, a new root GameObject will be created named \"" + Constants.DefaultDungeonRootName + "\""), true);

			EditorGUILayout.BeginVertical("box");
			DungeonGeneratorDrawUtil.DrawDungeonGenerator(serializedObject.FindProperty(nameof(RuntimeDungeon.Generator)), true);
			EditorGUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();
		}

		private void OnSceneGUI()
		{
			var dungeon = (RuntimeDungeon)target;

			if (!dungeon.Generator.RestrictDungeonToBounds)
				return;

			placementBoundsHandle.center = dungeon.Generator.TilePlacementBounds.center;
			placementBoundsHandle.size = dungeon.Generator.TilePlacementBounds.size;

			EditorGUI.BeginChangeCheck();

			using (new Handles.DrawingScope(dungeon.transform.localToWorldMatrix))
			{
				placementBoundsHandle.DrawHandle();
			}

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(dungeon, "Inspector");
				dungeon.Generator.TilePlacementBounds = new Bounds(placementBoundsHandle.center, placementBoundsHandle.size);
				Undo.FlushUndoRecordObjects();
			}
		}
	}
}
