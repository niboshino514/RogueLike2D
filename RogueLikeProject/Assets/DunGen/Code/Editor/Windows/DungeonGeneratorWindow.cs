using UnityEditor;
using UnityEngine;

namespace DunGen.Editor
{
	public sealed class DungeonGeneratorWindow : EditorWindow
	{
		private class SerializedDungeonGeneratorContainer : ScriptableObject
		{
			public DungeonGenerator generator;
		}

		private SerializedDungeonGeneratorContainer container;
		private SerializedObject serializedObject;
		private SerializedProperty generatorProperty;
		private GameObject lastDungeon;
		private bool overwriteExisting = true;

		[MenuItem("Window/DunGen/Generate Dungeon")]
		private static void OpenWindow()
		{
			GetWindow<DungeonGeneratorWindow>(false, "New Dungeon", true);
		}

		private void OnGUI()
		{
			if (serializedObject == null)
				return;

			serializedObject.Update();
			DungeonGeneratorDrawUtil.DrawDungeonGenerator(generatorProperty, false);
			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.Space();

			overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing?", overwriteExisting);

			if (GUILayout.Button("Generate"))
				GenerateDungeon();
		}

		private void OnEnable()
		{
			// Create a container object to hold our generator
			container = ScriptableObject.CreateInstance<SerializedDungeonGeneratorContainer>();
			container.generator = new DungeonGenerator
			{
				AllowTilePooling = false
			};

			// Setup serialization
			serializedObject = new SerializedObject(container);
			generatorProperty = serializedObject.FindProperty("generator");

			container.generator.OnGenerationStatusChanged += HandleGenerationStatusChanged;
		}

		private void OnDisable()
		{
			if (container != null && container.generator != null)
				container.generator.OnGenerationStatusChanged -= HandleGenerationStatusChanged;

			if (container != null)
				DestroyImmediate(container);

			container = null;
			serializedObject = null;
			generatorProperty = null;
		}

		private void GenerateDungeon()
		{
			if (lastDungeon != null)
			{
				if (overwriteExisting)
					UnityUtil.Destroy(lastDungeon);
				else
					container.generator.DetachDungeon();
			}

			lastDungeon = new GameObject("Dungeon Layout");
			container.generator.Root = lastDungeon;

			Undo.RegisterCreatedObjectUndo(lastDungeon, "Create Procedural Dungeon");
			container.generator.GenerateAsynchronously = false;
			container.generator.Generate();
		}

		private void HandleGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
		{
			if (status == GenerationStatus.Failed)
			{
				UnityUtil.Destroy(lastDungeon);
				lastDungeon = container.generator.Root = null;
			}
		}
	}
}