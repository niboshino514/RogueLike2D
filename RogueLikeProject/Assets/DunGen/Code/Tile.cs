using DunGen.Pooling;
using DunGen.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace DunGen
{
	[AddComponentMenu("DunGen/Tile")]
	public class Tile : MonoBehaviour, ISerializationCallbackReceiver
	{
		public const int CurrentFileVersion = 3;

		#region Legacy Properties

		// Legacy properties only exist to avoid breaking existing projects
		// Converting old data structures over to the new ones

		[SerializeField]
		[FormerlySerializedAs("AllowImmediateRepeats")]
		private bool allowImmediateRepeats = true;

		[SerializeField]
		[Obsolete("'Entrance' is no longer used. Please use the 'Entrances' list instead", false)]
		public Doorway Entrance;

		[SerializeField]
		[Obsolete("'Exit' is no longer used. Please use the 'Exits' list instead", false)]
		public Doorway Exit;

		#endregion

		/// <summary>
		/// Should this tile be allowed to rotate to fit in place?
		/// </summary>
		public bool AllowRotation = true;

		/// <summary>
		/// Should this tile be allowed to be placed next to another instance of itself?
		/// </summary>
		public TileRepeatMode RepeatMode = TileRepeatMode.Allow;

		/// <summary>
		/// Should the automatically generated tile bounds be overridden with a user-defined value?
		/// </summary>
		public bool OverrideAutomaticTileBounds = false;

		/// <summary>
		/// Optional tile bounds to override the automatically calculated tile bounds
		/// </summary>
		public Bounds TileBoundsOverride = new Bounds(Vector3.zero, Vector3.one);

		/// <summary>
		/// An optional collection of entrance doorways. DunGen will try to use one of these doorways as the entrance to the tile if possible
		/// </summary>
		public List<Doorway> Entrances = new List<Doorway>();

		/// <summary>
		/// An optional collection of exit doorways. DunGen will try to use one of these doorways as the exit to the tile if possible
		/// </summary>
		public List<Doorway> Exits = new List<Doorway>();

		/// <summary>
		/// Should this tile override the connection chance globally defined in the DungeonFlow?
		/// </summary>
		public bool OverrideConnectionChance = false;

		/// <summary>
		/// The overridden connection chance value. Only used if <see cref="OverrideConnectionChance"/> is true.
		/// If both tiles have overridden the connection chance, the lowest value is used
		/// </summary>
		public float ConnectionChance = 0f;

		/// <summary>
		/// A collection of tags for this tile. Can be used with the dungeon flow asset to restrict which
		/// tiles can be attached
		/// </summary>
		public TagContainer Tags = new TagContainer();

		/// <summary>
		/// The calculated world-space bounds of this Tile
		/// </summary>
		[HideInInspector]
		public Bounds Bounds { get { return transform.TransformBounds(Placement.LocalBounds); } }

		/// <summary>
		/// Information about the tile's position in the generated dungeon
		/// </summary>
		public TilePlacementData Placement
		{
			get { return placement; }
			internal set { placement = value; }
		}
		/// <summary>
		/// The dungeon that this tile belongs to
		/// </summary>
		public Dungeon Dungeon { get; internal set; }

		public List<Doorway> AllDoorways = new List<Doorway>();
		public List<Doorway> UsedDoorways = new List<Doorway>();
		public List<Doorway> UnusedDoorways = new List<Doorway>();
		public GameObject Prefab { get; internal set; }
		public bool HasValidBounds => Placement != null && Placement.LocalBounds.extents.sqrMagnitude > 0f;

		[SerializeField]
		private TilePlacementData placement;
		[SerializeField]
		private int fileVersion;

		private BoxCollider triggerVolume;
		private BoxCollider2D triggerVolume2D;

		private readonly List<ITileSpawnEventReceiver> spawnEventReceivers = new List<ITileSpawnEventReceiver>();


		public void RefreshTileEventReceivers()
		{
			spawnEventReceivers.Clear();
			GetComponentsInChildren(true, spawnEventReceivers);
		}

		internal void TileSpawned()
		{
			foreach (var receiver in spawnEventReceivers)
				receiver.OnTileSpawned(this);
		}

		internal void TileDespawned()
		{
			Dungeon = null;

			foreach (var doorway in AllDoorways)
				doorway.ResetInstanceData();

			placement.SetPositionAndRotation(Vector2.zero, Quaternion.identity);

			UsedDoorways.Clear();
			UnusedDoorways.Clear();

			foreach(var receiver in spawnEventReceivers)
				receiver.OnTileDespawned(this);
		}

		internal void AddTriggerVolume(bool use2dCollider)
		{
			if (use2dCollider)
			{
				if (triggerVolume2D == null)
					triggerVolume2D = gameObject.AddComponent<BoxCollider2D>();

				triggerVolume2D.offset = Placement.LocalBounds.center;
				triggerVolume2D.size = Placement.LocalBounds.size;
				triggerVolume2D.isTrigger = true;
			}
			else
			{
				if(triggerVolume == null)
					triggerVolume = gameObject.AddComponent<BoxCollider>();

				triggerVolume.center = Placement.LocalBounds.center;
				triggerVolume.size = Placement.LocalBounds.size;
				triggerVolume.isTrigger = true;
			}

		}

		private void OnTriggerEnter(Collider other)
		{
			if (other == null)
				return;

			if (other.gameObject.TryGetComponent<DungenCharacter>(out var character))
				character.OnTileEntered(this);
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (other == null)
				return;

			if (other.gameObject.TryGetComponent<DungenCharacter>(out var character))
				character.OnTileEntered(this);
		}

		private void OnTriggerExit(Collider other)
		{
			if (other == null)
				return;

			if (other.gameObject.TryGetComponent<DungenCharacter>(out var character))
				character.OnTileExited(this);
		}
		private void OnTriggerExit2D(Collider2D other)
		{
			if (other == null)
				return;

			if (other.gameObject.TryGetComponent<DungenCharacter>(out var character))
				character.OnTileExited(this);
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.red;
			Bounds? bounds = null;

			if (OverrideAutomaticTileBounds)
				bounds = transform.TransformBounds(TileBoundsOverride);
			else if (placement != null)
				bounds = Bounds;

			if (bounds.HasValue)
				Gizmos.DrawWireCube(bounds.Value.center, bounds.Value.size);
		}

		public IEnumerable<Tile> GetAdjacentTiles()
		{
			return UsedDoorways.Select(x => x.ConnectedDoorway.Tile).Distinct();
		}

		public bool IsAdjacentTo(Tile other)
		{
			foreach (var door in UsedDoorways)
				if (door.ConnectedDoorway.Tile == other)
					return true;

			return false;
		}

		public Doorway GetEntranceDoorway()
		{
			foreach (var doorway in UsedDoorways)
			{
				var connectedTile = doorway.ConnectedDoorway.Tile;

				if (Placement.IsOnMainPath)
				{
					if (connectedTile.Placement.IsOnMainPath && Placement.PathDepth > connectedTile.Placement.PathDepth)
						return doorway;
				}
				else
				{
					if (connectedTile.Placement.IsOnMainPath || Placement.Depth > connectedTile.Placement.Depth)
						return doorway;
				}
			}

			return null;
		}

		public Doorway GetExitDoorway()
		{
			foreach (var doorway in UsedDoorways)
			{
				var connectedTile = doorway.ConnectedDoorway.Tile;

				if (Placement.IsOnMainPath)
				{
					if (connectedTile.Placement.IsOnMainPath && Placement.PathDepth < connectedTile.Placement.PathDepth)
						return doorway;
				}
				else
				{
					if (!connectedTile.Placement.IsOnMainPath && Placement.Depth < connectedTile.Placement.Depth)
						return doorway;
				}
			}

			return null;
		}

		/// <summary>
		/// Recalculates the Tile's bounds based on the geometry inside the prefab
		/// </summary>
		/// <returns>True if the bounds changed when recalculated</returns>
		public bool RecalculateBounds()
		{
			if (Placement == null)
				Placement = new TilePlacementData();

			var oldBounds = Placement.LocalBounds;

			if (OverrideAutomaticTileBounds)
				Placement.LocalBounds = TileBoundsOverride;
			else
			{
				var tileBounds = UnityUtil.CalculateObjectBounds(gameObject,
					false,
					DunGenSettings.Instance.BoundsCalculationsIgnoreSprites,
					true);

				tileBounds = UnityUtil.CondenseBounds(tileBounds, GetComponentsInChildren<Doorway>(true));

				// Convert tileBounds to local space
				tileBounds = transform.InverseTransformBounds(tileBounds);
				Placement.LocalBounds = tileBounds;
			}

			var bounds = Placement.LocalBounds;
			bool haveBoundsChanged = bounds != oldBounds;

			// Let the user know that the tile's bounds are invalid
			if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
				Debug.LogError(string.Format("Tile prefab '{0}' has automatic bounds that are zero or negative in size. The bounding volume for this tile will need to be manually defined.", gameObject), gameObject);

			//if (haveBoundsChanged)
			//	Debug.Log($"Updated bounds for '{gameObject.name}'");
			//else
			//	Debug.Log($"RecalculateBounds(): Bounds were already up-to-date for '{gameObject.name}'");

			return haveBoundsChanged;
		}

		public void CopyBoundsFrom(Tile otherTile)
		{
			if (otherTile == null)
				return;

			if(Placement == null)
				Placement = new TilePlacementData();

			Placement.LocalBounds = otherTile.Placement.LocalBounds;
		}

		#region ISerializationCallbackReceiver Implementation

		public void OnBeforeSerialize()
		{
			fileVersion = CurrentFileVersion;
		}

		public void OnAfterDeserialize()
		{
#pragma warning disable 618

			// AllowImmediateRepeats (bool) -> TileRepeatMode (enum)
			if (fileVersion < 1)
				RepeatMode = (allowImmediateRepeats) ? TileRepeatMode.Allow : TileRepeatMode.DisallowImmediate;

			// Converted individual Entrance and Exit doorways to collections
			if (fileVersion < 2)
			{
				if (Entrances == null)
					Entrances = new List<Doorway>();

				if (Exits == null)
					Exits = new List<Doorway>();

				if (Entrance != null)
					Entrances.Add(Entrance);

				if(Exit != null)
					Exits.Add(Exit);

				Entrance = null;
				Exit = null;
			}

#pragma warning restore 618
		}

		#endregion
	}
}
