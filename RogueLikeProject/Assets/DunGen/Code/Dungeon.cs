using DunGen.Generation;
using DunGen.Graph;
using DunGen.Tags;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DunGen
{
	public delegate void DungeonTileInstantiatedDelegate(Dungeon dungeon, Tile newTile, int currentTileCount, int totalTileCount);

	public class Dungeon : MonoBehaviour
	{
		public static event DungeonTileInstantiatedDelegate TileInstantiated;

		#region Nestes Types

		public sealed class Branch
		{
			public int Index { get; }
			public ReadOnlyCollection<Tile> Tiles { get; }

			public Branch(int index, List<Tile> tiles)
			{
				Index = index;
				Tiles = new ReadOnlyCollection<Tile>(tiles);
			}
		}

		#endregion

		/// <summary>
		/// World-space bounding box of the entire dungeon
		/// </summary>
		public Bounds Bounds { get; protected set; }

		/// <summary>
		/// The dungeon flow asset used to generate this dungeon
		/// </summary>
		public DungeonFlow DungeonFlow
		{
			get => dungeonFlow;
			set => dungeonFlow = value;
		}

		/// <summary>
		/// Should we render debug information about the dungeon
		/// </summary>
		public bool DebugRender = false;

		public ReadOnlyCollection<Tile> AllTiles { get; }
		public ReadOnlyCollection<Tile> MainPathTiles { get; }
		public ReadOnlyCollection<Tile> BranchPathTiles { get; }
		public ReadOnlyCollection<GameObject> Doors { get; }
		public ReadOnlyCollection<DoorwayConnection> Connections { get; }
		public ReadOnlyCollection<Branch> Branches { get; }
		public DungeonGraph ConnectionGraph { get; private set; }

		public TileInstanceSource TileInstanceSource { get; internal set; }

		[SerializeField]
		private DungeonFlow dungeonFlow;
		[SerializeField]
		private List<Tile> allTiles = new List<Tile>();
		[SerializeField]
		private List<Tile> mainPathTiles = new List<Tile>();
		[SerializeField]
		private List<Tile> branchPathTiles = new List<Tile>();
		[SerializeField]
		private List<GameObject> doors = new List<GameObject>();
		[SerializeField]
		private List<DoorwayConnection> connections = new List<DoorwayConnection>();
		[SerializeField]
		private Tile attachmentTile;
		[SerializeField]
		private List<Branch> branches = new List<Branch>();


		public Dungeon()
		{
			AllTiles = new ReadOnlyCollection<Tile>(allTiles);
			MainPathTiles = new ReadOnlyCollection<Tile>(mainPathTiles);
			BranchPathTiles = new ReadOnlyCollection<Tile>(branchPathTiles);
			Doors = new ReadOnlyCollection<GameObject>(doors);
			Connections = new ReadOnlyCollection<DoorwayConnection>(connections);
			Branches = new ReadOnlyCollection<Branch>(branches);
		}

		private void Start()
		{
			// If there are already tiles and the connection graph isn't initialised yet,
			// this script is likely already present in the scene (from generating the dungeon in-editor).
			// We just need to finalise the dungeon info from data we already have available
			if (allTiles.Count > 0 && ConnectionGraph == null)
				FinaliseDungeonInfo();
		}

		public IEnumerable<Tile> FindTilesWithTag(Tag tag) => allTiles.Where(t => t.Tags.HasTag(tag));

		public IEnumerable<Tile> FindTilesWithAnyTag(params Tag[] tags) => allTiles.Where(t => t.Tags.HasAnyTag(tags));

		public IEnumerable<Tile> FindTilesWithAllTags(params Tag[] tags) => allTiles.Where(t => t.Tags.HasAllTags(tags));

		internal void AddAdditionalDoor(Door door)
		{
			if (door != null && !doors.Contains(door.gameObject))
				doors.Add(door.gameObject);
		}

		internal void PreGenerateDungeon(DungeonGenerator dungeonGenerator)
		{
			DungeonFlow = dungeonGenerator.DungeonFlow;
		}

		internal void PostGenerateDungeon(DungeonGenerator dungeonGenerator)
		{
			FinaliseDungeonInfo();
		}

		private void FinaliseDungeonInfo()
		{
			var additionalTiles = new List<Tile>();

			if (attachmentTile != null)
				additionalTiles.Add(attachmentTile);

			ConnectionGraph = new DungeonGraph(this, additionalTiles);
			Bounds = UnityUtil.CombineBounds(allTiles.Select(x => x.Placement.Bounds).ToArray());
			GatherBranches();
		}

		/// <summary>
		/// Gathers all branches into lists for easy access
		/// </summary>
		private void GatherBranches()
		{
			var branchTiles = new Dictionary<int, List<Tile>>();

			// Gather branch tiles
			foreach (var branchTile in branchPathTiles)
			{
				int branchIndex = branchTile.Placement.BranchId;

				if(!branchTiles.TryGetValue(branchIndex, out var branchTileList))
				{
					branchTileList = new List<Tile>();
					branchTiles[branchIndex] = branchTileList;
				}

				branchTileList.Add(branchTile);
			}

			// Create branch objects
			foreach (var kvp in branchTiles)
			{
				int index = kvp.Key;
				var tiles = kvp.Value;

				branches.Add(new Branch(index, tiles));
			}
		}

		public void Clear()
		{
			Clear(TileInstanceSource.DespawnTile);
		}

		public void Clear(Action<Tile> destroyTileDelegate)
		{
			// Destroy all tiles
			foreach (var tile in allTiles)
				destroyTileDelegate(tile);

			// Destroy anything else attached to this dungeon
			for (int i = 0; i < transform.childCount; i++)
			{
				GameObject child = transform.GetChild(i).gameObject;
				UnityUtil.Destroy(child);
			}

			allTiles.Clear();
			mainPathTiles.Clear();
			branchPathTiles.Clear();
			doors.Clear();
			connections.Clear();
			branches.Clear();
			attachmentTile = null;
		}

		public Doorway GetConnectedDoorway(Doorway doorway)
		{
			foreach (var conn in connections)
				if (conn.A == doorway)
					return conn.B;
				else if (conn.B == doorway)
					return conn.A;

			return null;
		}

		public IEnumerator FromProxy(DungeonProxy proxyDungeon,
			DungeonGenerator generator,
			Func<bool> shouldSkipFrame)
		{
			Clear();

			var proxyToTileMap = new Dictionary<TileProxy, Tile>();

			// We're attaching to a previous dungeon
			if (generator.AttachmentSettings != null &&
				generator.AttachmentSettings.TileProxy != null)
			{
				// We need to manually inject the dummy TileProxy used to connect to a Tile in the previous dungeon
				var attachmentProxy = generator.AttachmentSettings.TileProxy;
				attachmentTile = generator.AttachmentSettings.GetAttachmentTile();
				proxyToTileMap[attachmentProxy] = attachmentTile;

				// We also need to manually process the doorway in the other dungeon
				var usedDoorwayProxy = attachmentProxy.UsedDoorways.First();
				var usedDoorway = attachmentTile.AllDoorways[usedDoorwayProxy.Index];

				usedDoorway.ProcessDoorwayObjects(true, generator.RandomStream);

				attachmentTile.UsedDoorways.Add(usedDoorway);
				attachmentTile.UnusedDoorways.Remove(usedDoorway);
			}

			foreach (var tileProxy in proxyDungeon.AllTiles)
			{
				// Instantiate & re-position tile
				var tileObj = TileInstanceSource.SpawnTile(tileProxy.PrefabTile, tileProxy.Placement.Position, tileProxy.Placement.Rotation);

				// Add tile to lists
				var tile = tileObj.GetComponent<Tile>();
				tile.Dungeon = this;
				tile.Placement = new TilePlacementData(tileProxy.Placement);
				tile.Prefab = tileProxy.Prefab;
				proxyToTileMap[tileProxy] = tile;
				allTiles.Add(tile);

				// Now that the tile is actually attached to the root object, we need to update our transform to match
				tile.Placement.SetPositionAndRotation(tileObj.transform.position, tileObj.transform.rotation);

				if (tile.Placement.IsOnMainPath)
					mainPathTiles.Add(tile);
				else
					branchPathTiles.Add(tile);

				// Place trigger volume
				if (generator.TriggerPlacement != TriggerPlacementMode.None)
				{
					tile.AddTriggerVolume(generator.TriggerPlacement == TriggerPlacementMode.TwoDimensional);
					tile.gameObject.layer = generator.TileTriggerLayer;
				}

				// Process doorways
				var allDoorways = tileObj.GetComponentsInChildren<Doorway>();

				foreach (var doorway in allDoorways)
				{
					if (tile.AllDoorways.Contains(doorway))
						continue;

					doorway.Tile = tile;
					doorway.placedByGenerator = true;
					doorway.HideConditionalObjects = false;

					tile.AllDoorways.Add(doorway);
				}

				foreach (var doorwayProxy in tileProxy.UsedDoorways)
				{
					var doorway = allDoorways[doorwayProxy.Index];
					tile.UsedDoorways.Add(doorway);

					doorway.ProcessDoorwayObjects(true, generator.RandomStream);
				}

				foreach (var doorwayProxy in tileProxy.UnusedDoorways)
				{
					var doorway = allDoorways[doorwayProxy.Index];
					tile.UnusedDoorways.Add(doorway);

					doorway.ProcessDoorwayObjects(false, generator.RandomStream);
				}

				// Let the user know a new tile has been instantiated
				TileInstantiated?.Invoke(this, tile, allTiles.Count, proxyDungeon.AllTiles.Count);

				if (shouldSkipFrame != null && shouldSkipFrame())
					yield return null;
			}

			// Add doorway connections
			foreach (var proxyConn in proxyDungeon.Connections)
			{
				var tileA = proxyToTileMap[proxyConn.A.TileProxy];
				var tileB = proxyToTileMap[proxyConn.B.TileProxy];

				var doorA = tileA.AllDoorways[proxyConn.A.Index];
				var doorB = tileB.AllDoorways[proxyConn.B.Index];

				doorA.ConnectedDoorway = doorB;
				doorB.ConnectedDoorway = doorA;

				var conn = new DoorwayConnection(doorA, doorB);
				connections.Add(conn);

				SpawnDoorPrefab(doorA, doorB, generator.RandomStream);
			}
		}

		private void SpawnDoorPrefab(Doorway a, Doorway b, RandomStream randomStream)
		{
			// This door already has a prefab instance placed, exit early
			if (a.HasDoorPrefabInstance || b.HasDoorPrefabInstance)
				return;

			// Add door prefab
			Doorway chosenDoor;

			bool doorwayAHasEntries = a.ConnectorPrefabWeights.HasAnyViableEntries();
			bool doorwayBHasEntries = b.ConnectorPrefabWeights.HasAnyViableEntries();

			// No doorway has a prefab to place, exit early
			if (!doorwayAHasEntries && !doorwayBHasEntries)
				return;

			// If both doorways have door prefabs..
			if (doorwayAHasEntries && doorwayBHasEntries)
			{
				// ..A is selected if its priority is greater than or equal to B..
				if (a.DoorPrefabPriority >= b.DoorPrefabPriority)
					chosenDoor = a;
				// .. otherwise, B is chosen..
				else
					chosenDoor = b;
			}
			// ..if only one doorway has a prefab, use that one
			else
				chosenDoor = (doorwayAHasEntries) ? a : b;


			GameObject doorPrefab = chosenDoor.ConnectorPrefabWeights.GetRandom(randomStream);

			if (doorPrefab != null)
			{
				GameObject door = Instantiate(doorPrefab, chosenDoor.transform);
				door.transform.localPosition = chosenDoor.DoorPrefabPositionOffset;

				if (chosenDoor.AvoidRotatingDoorPrefab)
					door.transform.rotation = Quaternion.Euler(chosenDoor.DoorPrefabRotationOffset);
				else
					door.transform.localRotation = Quaternion.Euler(chosenDoor.DoorPrefabRotationOffset);

				doors.Add(door);

				DungeonUtil.AddAndSetupDoorComponent(this, door, chosenDoor);

				a.SetUsedPrefab(door);
				b.SetUsedPrefab(door);
			}
		}

		public void OnDrawGizmos()
		{
			if (DebugRender)
				DebugDraw();
		}

		public void DebugDraw()
		{
			Color mainPathStartColour = Color.red;
			Color mainPathEndColour = Color.green;
			Color branchPathStartColour = Color.blue;
			Color branchPathEndColour = new Color(0.5f, 0, 0.5f);
			float boundsBoxOpacity = 0.75f;

			foreach (var tile in allTiles)
			{
				Bounds bounds = tile.Placement.Bounds;
				bounds.size = bounds.size * 1.01f;

				Color tileColour = (tile.Placement.IsOnMainPath) ?
									Color.Lerp(mainPathStartColour, mainPathEndColour, tile.Placement.NormalizedDepth) :
									Color.Lerp(branchPathStartColour, branchPathEndColour, tile.Placement.NormalizedDepth);

				tileColour.a = boundsBoxOpacity;
				Gizmos.color = tileColour;

				Gizmos.DrawCube(bounds.center, bounds.size);
			}
		}
	}
}
