#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Linq;
using UnityEngine;
using DunGen.Tags;
using DunGen.Collision;

namespace DunGen
{
	public sealed class DunGenSettings : ScriptableObject
	{
		#region Singleton

		private static DunGenSettings instance;
		public static DunGenSettings Instance
		{
			get
			{
				if (instance != null)
					return instance;
				else
				{
					instance = FindOrCreateInstanceAsset();
					return instance;
				}
			}
		}


		public static DunGenSettings FindOrCreateInstanceAsset()
		{
			// Try to find an existing instance in a resource folder
			instance = Resources.Load<DunGenSettings>("DunGen Settings");

			// Create a new instance if one is not found
			if (instance == null)
			{
#if UNITY_EDITOR
				instance = CreateInstance<DunGenSettings>();

				if (!Directory.Exists(Application.dataPath + "/Resources"))
					AssetDatabase.CreateFolder("Assets", "Resources");

				AssetDatabase.CreateAsset(instance, "Assets/Resources/DunGen Settings.asset");
				instance.defaultSocket = instance.GetOrAddSocketByName("Default");
#else
				throw new System.Exception("No instance of DunGen settings was found.");
#endif
			}

			return instance;
		}

		#endregion

		public DoorwaySocket DefaultSocket { get { return defaultSocket; } }
		public TagManager TagManager { get { return tagManager; } }

		/// <summary>
		/// Optional broadphase settings for speeding up collision tests
		/// </summary>
		[SubclassSelector]
		[SerializeReference]
		public BroadphaseSettings BroadphaseSettings = new SpatialHashBroadphaseSettings();

		/// <summary>
		/// If true, sprite components will be ignored when automatically calculating bounding volumes around tiles
		/// </summary>
		public bool BoundsCalculationsIgnoreSprites = false;
		/// <summary>
		/// If true, tile bounds will be automatically recalculated whenever a tile is saved. Otherwise, bounds must be recalculated manually using the button in the Tile inspector
		/// </summary>
		public bool RecalculateTileBoundsOnSave = true;
		/// <summary>
		/// If true, tile instances will be re-used instead of destroyed and re-created, improving generation performance at the cost of increased memory consumption
		/// </summary>
		public bool EnableTilePooling = false;
		/// <summary>
		/// If true, a window will be displayed when a generation failure occurs, allowing you to inspect the failure report
		/// </summary>
		public bool DisplayFailureReportWindow = true;
		/// <summary>
		/// Should the DunGen folder be checked for files that are no longer in use? If true, when loading DunGen will check if any old files are still present in the DunGen directory from previous DunGen version and will present the user with a list of files to potentially delete
		/// </summary>
		public bool CheckForUnusedFiles = true;

		[SerializeField]
		private DoorwaySocket defaultSocket = null;

		[SerializeField]
		private TagManager tagManager = new TagManager();


#if UNITY_EDITOR

		private void OnValidate()
		{
			if (defaultSocket == null)
				defaultSocket = GetOrAddSocketByName("Default");
		}

		private DoorwaySocket GetOrAddSocketByName(string name)
		{
			string path = AssetDatabase.GetAssetPath(this);

			var socket = AssetDatabase.LoadAllAssetsAtPath(path)
				.OfType<DoorwaySocket>()
				.FirstOrDefault(x => x.name == name);

			if (socket != null)
				return socket;

			socket = CreateInstance<DoorwaySocket>();
			socket.name = name;

			AssetDatabase.AddObjectToAsset(socket, this);

#if UNITY_2021_1_OR_NEWER
			AssetDatabase.SaveAssetIfDirty(socket);
#else
			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(socket));
#endif

			return socket;
		}
#endif
	}
}
