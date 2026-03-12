using DunGen.Collision;
using DunGen.Generation;
using DunGen.Graph;
using DunGen.LockAndKey;
using DunGen.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace DunGen
{
	public delegate void TileInjectionDelegate(RandomStream randomStream, ref List<InjectedTile> tilesToInject);
	public delegate void GenerationFailureReportProduced(DungeonGenerator generator, GenerationFailureReport report);
	public enum AxisDirection
	{
		[InspectorName("+X")]
		PosX,
		[InspectorName("-X")]
		NegX,
		[InspectorName("+Y")]
		PosY,
		[InspectorName("-Y")]
		NegY,
		[InspectorName("+Z")]
		PosZ,
		[InspectorName("-Z")]
		NegZ,
	}

	public enum TriggerPlacementMode
	{
		[InspectorName("None")]
		None,
		[InspectorName("3D")]
		ThreeDimensional,
		[InspectorName("2D")]
		TwoDimensional,
	}

	[Serializable]
	public class DungeonGenerator : ISerializationCallbackReceiver
	{
		public const int CurrentFileVersion = 3;

		#region Legacy Properties

		// Legacy properties only exist to avoid breaking existing projects
		// Converting old data structures over to the new ones

		[SerializeField]
		[FormerlySerializedAs("AllowImmediateRepeats")]
		private bool allowImmediateRepeats = false;

		[Obsolete("Use the 'CollisionSettings' property instead")]
		public float OverlapThreshold = 0.01f;

		[Obsolete("Use the 'CollisionSettings' property instead")]
		public float Padding = 0f;

		[Obsolete("Use the 'CollisionSettings' property instead")]
		public bool DisallowOverhangs = false;

		[Obsolete("Use the 'CollisionSettings' property instead")]
		public bool AvoidCollisionsWithOtherDungeons = true;

		[Obsolete("Use the 'CollisionSettings' property instead")]
		public readonly List<Bounds> AdditionalCollisionBounds = new List<Bounds>();

		[Obsolete("Use the 'CollisionSettings' property instead")]
		public AdditionalCollisionsPredicate AdditionalCollisionsPredicate { get; set; }

		[Obsolete("Use the 'TriggerPlacement' enum property instead")]
		public bool PlaceTileTriggers = true;

		#endregion

		#region Helper Struct

		struct PropProcessingData
		{
			public RandomProp PropComponent;
			public int HierarchyDepth;
			public Tile OwningTile;
		}

		#endregion

		public int Seed;
		public bool ShouldRandomizeSeed = true;
		public RandomStream RandomStream { get; protected set; }
		public int MaxAttemptCount = 20;
		public bool UseMaximumPairingAttempts = false;
		public int MaxPairingAttempts = 5;
		public AxisDirection UpDirection = AxisDirection.PosY;
		[FormerlySerializedAs("OverrideAllowImmediateRepeats")]
		public bool OverrideRepeatMode = false;
		public TileRepeatMode RepeatMode = TileRepeatMode.Allow;
		public bool OverrideAllowTileRotation = false;
		public bool AllowTileRotation = false;
		public bool DebugRender = false;
		public float LengthMultiplier = 1.0f;
		public TriggerPlacementMode TriggerPlacement = TriggerPlacementMode.ThreeDimensional;
		public int TileTriggerLayer = 2;
		public bool GenerateAsynchronously = false;
		public float MaxAsyncFrameMilliseconds = 10;
		public float PauseBetweenRooms = 0;
		public bool RestrictDungeonToBounds = false;
		public Bounds TilePlacementBounds = new Bounds(Vector3.zero, Vector3.one * 10f);
		public DungeonCollisionSettings CollisionSettings = new DungeonCollisionSettings();

		public Vector3 UpVector
		{
			get
			{
				return UpDirection switch
				{
					AxisDirection.PosX => new Vector3(+1, 0, 0),
					AxisDirection.NegX => new Vector3(-1, 0, 0),
					AxisDirection.PosY => new Vector3(0, +1, 0),
					AxisDirection.NegY => new Vector3(0, -1, 0),
					AxisDirection.PosZ => new Vector3(0, 0, +1),
					AxisDirection.NegZ => new Vector3(0, 0, -1),
					_ => throw new NotImplementedException("AxisDirection '" + UpDirection + "' not implemented"),
				};
			}
		}

		public event GenerationStatusDelegate OnGenerationStatusChanged;
		public event DungeonGenerationDelegate OnGenerationStarted;
		public event DungeonGenerationDelegate OnGenerationComplete;
		public static event GenerationStatusDelegate OnAnyDungeonGenerationStatusChanged;
		public static event DungeonGenerationDelegate OnAnyDungeonGenerationStarted;
		public static event DungeonGenerationDelegate OnAnyDungeonGenerationComplete;
		public event TileInjectionDelegate TileInjectionMethods;
		public event Action Cleared;
		public event Action Retrying;
		public static event GenerationFailureReportProduced OnGenerationFailureReportProduced;

		public GameObject Root;
		public DungeonFlow DungeonFlow;
		public GenerationStatus Status { get; private set; }
		public GenerationStats GenerationStats { get; private set; }
		public int ChosenSeed { get; protected set; }
		public Dungeon CurrentDungeon { get; private set; }
		public bool IsGenerating { get; private set; }
		public bool IsAnalysis { get; set; }
		public bool AllowTilePooling { get; set; }

		/// <summary>
		/// Settings for generating the new dungeon as an attachment to a previous dungeon
		/// </summary>
		public DungeonAttachmentSettings AttachmentSettings { get; set; }
		public DungeonCollisionManager CollisionManager { get; private set; }

		protected int retryCount;
		protected DungeonProxy proxyDungeon;
		protected readonly List<TilePlacementResult> tilePlacementResults = new List<TilePlacementResult>();
		protected readonly List<GameObject> useableTiles = new List<GameObject>();
		protected int targetLength;
		protected List<InjectedTile> tilesPendingInjection;
		protected List<DungeonGeneratorPostProcessStep> postProcessSteps = new List<DungeonGeneratorPostProcessStep>();

		[SerializeField]
		private int fileVersion;
		private int nextNodeIndex;
		private DungeonArchetype currentArchetype;
		private GraphLine previousLineSegment;
		private readonly Dictionary<GameObject, TileProxy> preProcessData = new Dictionary<GameObject, TileProxy>();
		private readonly Dictionary<TileProxy, InjectedTile> injectedTiles = new Dictionary<TileProxy, InjectedTile>();
		private readonly Stopwatch yieldTimer = new Stopwatch();
		private readonly BucketedObjectPool<TileProxy, TileProxy> tileProxyPool;
		private readonly TileInstanceSource tileInstanceSource;


		public DungeonGenerator()
		{
			AllowTilePooling = true;
			GenerationStats = new GenerationStats();
			CollisionManager = new DungeonCollisionManager();

			tileProxyPool = new BucketedObjectPool<TileProxy, TileProxy>(
				objectFactory: template => new TileProxy(template),
				takeAction: x =>
				{
					x.Placement.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
				}
				);

			tileInstanceSource = new TileInstanceSource();
			tileInstanceSource.TileInstanceSpawned += (tilePrefab, tileInstance, fromPool) =>
			{
				GenerationStats.TileAdded(tilePrefab, fromPool);
			};
		}

		public DungeonGenerator(GameObject root)
			: this()
		{
			Root = root;
		}

		public void Generate()
		{
			if (IsGenerating)
				return;

			OnGenerationStarted?.Invoke(this);
			OnAnyDungeonGenerationStarted?.Invoke(this);

			// Detach the previous dungeon if we're generating the new one as an attachment
			// We need to do this to avoid overwriting the previous dungeon
			if (AttachmentSettings != null && CurrentDungeon != null)
				DetachDungeon();

			if(CollisionSettings == null)
				CollisionSettings = new DungeonCollisionSettings();

			if(CollisionManager == null)
				CollisionManager = new DungeonCollisionManager();

			CollisionManager.Settings = CollisionSettings;
			DoorwayPairFinder.SortCustomConnectionRules();

			IsGenerating = true;
			Wait(OuterGenerate());
		}

		public void Cancel()
		{
			if (!IsGenerating)
				return;

			Clear(true);
			IsGenerating = false;
		}

		public Dungeon DetachDungeon()
		{
			if (CurrentDungeon == null)
				return null;

			Dungeon dungeon = CurrentDungeon;
			CurrentDungeon = null;
			Root = null;
			Clear(true);

			// If the dungeon is empty, we should just destroy it
			if (dungeon.transform.childCount == 0)
				UnityEngine.Object.DestroyImmediate(dungeon.gameObject);

			return dungeon;
		}

		protected virtual IEnumerator OuterGenerate()
		{
			Clear(false);

			yieldTimer.Restart();

			Status = GenerationStatus.NotStarted;
			tilePlacementResults.Clear();

#if UNITY_EDITOR
			// Validate the dungeon archetype if we're running in the editor
			DungeonArchetypeValidator validator = new DungeonArchetypeValidator(DungeonFlow);

			if (!validator.IsValid())
			{
				ChangeStatus(GenerationStatus.Failed);
				IsGenerating = false;
				yield break;
			}
#endif

			ChosenSeed = (ShouldRandomizeSeed) ? new RandomStream().Next() : Seed;
			RandomStream = new RandomStream(ChosenSeed);

			if (Root == null)
				Root = new GameObject(Constants.DefaultDungeonRootName);

			bool enableTilePooling = AllowTilePooling && DunGenSettings.Instance.EnableTilePooling;
			tileInstanceSource.Initialise(enableTilePooling, Root);

			yield return Wait(InnerGenerate(false));

			IsGenerating = false;
		}

		private Coroutine Wait(IEnumerator routine)
		{
			if (GenerateAsynchronously)
				return CoroutineHelper.Start(routine);
			else
			{
				while (routine.MoveNext()) { }
				return null;
			}
		}

		public void RandomizeSeed()
		{
			Seed = new RandomStream().Next();
		}

		protected virtual IEnumerator InnerGenerate(bool isRetry)
		{
			if (isRetry)
			{
				ChosenSeed = RandomStream.Next();
				RandomStream = new RandomStream(ChosenSeed);

				if (retryCount >= MaxAttemptCount && Application.isEditor)
				{
					// Generate a failure report if we're not running an analysis
					if (!IsAnalysis)
					{
						Debug.LogError(TilePlacementResult.ProduceReport(tilePlacementResults, MaxAttemptCount));
						OnGenerationFailureReportProduced?.Invoke(this, new GenerationFailureReport(MaxAttemptCount, tilePlacementResults));
					}

					ChangeStatus(GenerationStatus.Failed);
					yield break;
				}

				retryCount++;
				GenerationStats.IncrementRetryCount();

				Retrying?.Invoke();
			}
			else
			{
				retryCount = 0;
				GenerationStats.Clear();
			}

			CurrentDungeon = Root.GetComponent<Dungeon>();

			if (CurrentDungeon == null)
				CurrentDungeon = Root.AddComponent<Dungeon>();

			CurrentDungeon.TileInstanceSource = tileInstanceSource;
			CollisionManager.Initialize(this);

			CurrentDungeon.DebugRender = DebugRender;
			CurrentDungeon.PreGenerateDungeon(this);

			Clear(false);
			targetLength = Mathf.RoundToInt(DungeonFlow.Length.GetRandom(RandomStream) * LengthMultiplier);
			targetLength = Mathf.Max(targetLength, 2);

// PauseBetweenRooms is for debug purposes only, so we should disable it in regular builds to improve performance
#if !UNITY_EDITOR
			PauseBetweenRooms = 0.0f;
#endif

			Transform debugVisualsRoot = (PauseBetweenRooms > 0f) ? Root.transform : null;
			proxyDungeon = new DungeonProxy(debugVisualsRoot);

			// Tile Injection
			GenerationStats.BeginTime(GenerationStatus.TileInjection);
			ChangeStatus(GenerationStatus.TileInjection);

			if (tilesPendingInjection == null)
				tilesPendingInjection = new List<InjectedTile>();
			else
				tilesPendingInjection.Clear();

			injectedTiles.Clear();
			GatherTilesToInject();

			// Pre-Processing
			GenerationStats.BeginTime(GenerationStatus.PreProcessing);
			PreProcess();

			// Main Path Generation
			GenerationStats.BeginTime(GenerationStatus.MainPath);
			yield return Wait(GenerateMainPath());

			// We may have had to retry when generating the main path, if so, the status will be either Complete or Failed and we should exit here
			if (Status == GenerationStatus.Complete || Status == GenerationStatus.Failed)
				yield break;

			// Branch Paths Generation
			GenerationStats.BeginTime(GenerationStatus.Branching);
			yield return Wait(GenerateBranchPaths());

			// If there are any required tiles missing from the tile injection stage, the generation process should fail
			foreach (var tileInjection in tilesPendingInjection)
				if (tileInjection.IsRequired)
				{
					tilePlacementResults.Add(new RequiredTileInjectionFailedResult(tileInjection.TileSet));
					yield return Wait(InnerGenerate(true));
					yield break;
				}

			// We may have missed some required injected tiles and have had to retry, if so, the status will be either Complete or Failed and we should exit here
			if (Status == GenerationStatus.Complete || Status == GenerationStatus.Failed)
				yield break;

			GenerationStats.BeginTime(GenerationStatus.BranchPruning);
			ChangeStatus(GenerationStatus.BranchPruning);

			// Prune branches if we have any tags set up
			if (DungeonFlow.BranchPruneTags.Count > 0)
				PruneBranches();

			// Instantiate Tiles
			GenerationStats.BeginTime(GenerationStatus.InstantiatingTiles);
			ChangeStatus(GenerationStatus.InstantiatingTiles);

			proxyDungeon.ConnectOverlappingDoorways(DungeonFlow.DoorwayConnectionChance, DungeonFlow, RandomStream);
			yield return Wait(CurrentDungeon.FromProxy(proxyDungeon, this, () => ShouldSkipFrame(false)));

			// Post-Processing
			yield return Wait(PostProcess());

			proxyDungeon.ClearDebugVisuals();

			// Waiting one frame so objects are in their expected state
			yield return null;

			// Inform objects in the dungeon that generation is complete
			foreach (var callbackReceiver in CurrentDungeon.gameObject.GetComponentsInChildren<IDungeonCompleteReceiver>(false))
				callbackReceiver.OnDungeonComplete(CurrentDungeon);

			ChangeStatus(GenerationStatus.Complete);

			bool charactersShouldRecheckTile = true;

#if UNITY_EDITOR
			charactersShouldRecheckTile = UnityEditor.EditorApplication.isPlaying;
#endif

			// Let DungenCharacters know that they should re-check the Tile they're in
			if (charactersShouldRecheckTile)
			{
				foreach (var character in UnityUtil.FindObjectsByType<DungenCharacter>())
					character.ForceRecheckTile();
			}
		}

		private void PruneBranches()
		{
			var branchTips = new Stack<TileProxy>();

			foreach (var tile in proxyDungeon.BranchPathTiles)
			{
				var connectedTiles = tile.UsedDoorways.Select(d => d.ConnectedDoorway.TileProxy);

				// If we're not connected to another tile with a higher branch depth, this is a branch tip
				if (!connectedTiles.Any(t => t.Placement.BranchDepth > tile.Placement.BranchDepth))
					branchTips.Push(tile);
			}

			while (branchTips.Count > 0)
			{
				var tile = branchTips.Pop();

				bool isRequiredTile = tile.Placement.InjectionData != null && tile.Placement.InjectionData.IsRequired;
				bool shouldPruneTile = !isRequiredTile && DungeonFlow.ShouldPruneTileWithTags(tile.PrefabTile.Tags);

				if (shouldPruneTile)
				{
					// Find that tile that came before this one
					var precedingTileConnection = tile.UsedDoorways
						.Select(d => d.ConnectedDoorway)
						.Where(d => d.TileProxy.Placement.IsOnMainPath || d.TileProxy.Placement.BranchDepth < tile.Placement.BranchDepth)
						.Select(d => new ProxyDoorwayConnection(d, d.ConnectedDoorway))
						.First();

					// Remove tile and connection
					proxyDungeon.RemoveTile(tile);
					CollisionManager.RemoveTile(tile);
					proxyDungeon.RemoveConnection(precedingTileConnection);
					GenerationStats.PrunedBranchTileCount++;

					var precedingTile = precedingTileConnection.A.TileProxy;

					// The preceding tile is the new tip of this branch
					if (!precedingTile.Placement.IsOnMainPath)
						branchTips.Push(precedingTile);
				}
			}
		}

		public virtual void Clear(bool stopCoroutines)
		{
			if (stopCoroutines)
				CoroutineHelper.StopAll();

			if (proxyDungeon != null)
				proxyDungeon.ClearDebugVisuals();

			proxyDungeon = null;

			if (CurrentDungeon != null)
				CurrentDungeon.Clear();

			useableTiles.Clear();
			preProcessData.Clear();

			previousLineSegment = null;

			Cleared?.Invoke();
		}

		private void ChangeStatus(GenerationStatus status)
		{
			var previousStatus = Status;
			Status = status;

			if (status == GenerationStatus.Complete || status == GenerationStatus.Failed)
				IsGenerating = false;

			if (status == GenerationStatus.Failed)
				Clear(true);

			if (previousStatus != status)
			{
				OnGenerationStatusChanged?.Invoke(this, status);
				OnAnyDungeonGenerationStatusChanged?.Invoke(this, status);

				if (status == GenerationStatus.Complete)
				{
					OnGenerationComplete?.Invoke(this);
					OnAnyDungeonGenerationComplete?.Invoke(this);
				}
			}
		}

		protected virtual void PreProcess()
		{
			if (preProcessData.Count > 0)
				return;

			ChangeStatus(GenerationStatus.PreProcessing);

			var usedTileSets = DungeonFlow.GetUsedTileSets().Concat(tilesPendingInjection.Select(x => x.TileSet)).Distinct();

			foreach (var tileSet in usedTileSets)
				foreach (var tile in tileSet.TileWeights.Weights)
				{
					if (tile.Value != null)
					{
						useableTiles.Add(tile.Value);
						tile.TileSet = tileSet;
					}
				}
		}

		protected virtual void GatherTilesToInject()
		{
			var injectionRandomStream = new RandomStream(ChosenSeed);

			// Gather from DungeonFlow
			foreach (var rule in DungeonFlow.TileInjectionRules)
			{
				// Ignore invalid rules
				if (rule.TileSet == null || (!rule.CanAppearOnMainPath && !rule.CanAppearOnBranchPath))
					continue;

				bool isOnMainPath = (!rule.CanAppearOnBranchPath) ? true : (!rule.CanAppearOnMainPath) ? false : injectionRandomStream.NextDouble() > 0.5;
				var injectedTile = new InjectedTile(rule, isOnMainPath, injectionRandomStream);

				tilesPendingInjection.Add(injectedTile);
			}

			// Gather from external delegates
			TileInjectionMethods?.Invoke(injectionRandomStream, ref tilesPendingInjection);
		}

		protected virtual IEnumerator GenerateMainPath()
		{
			ChangeStatus(GenerationStatus.MainPath);
			nextNodeIndex = 0;
			var handledNodes = new List<GraphNode>(DungeonFlow.Nodes.Count);
			bool isDone = false;
			int i = 0;

			// Keep track of these now, we'll need them later when we know the actual length of the dungeon
			var placementSlots = new List<TilePlacementParameters>(targetLength);
			var slotTileSets = new List<List<TileSet>>();

			// We can't rigidly stick to the target length since we need at least one room for each node and that might be more than targetLength
			while (!isDone)
			{
				float depth = Mathf.Clamp(i / (float)(targetLength - 1), 0, 1);
				GraphLine lineSegment = DungeonFlow.GetLineAtDepth(depth);

				// This should never happen
				if (lineSegment == null)
				{
					Debug.LogError("DungeonFlow returned a null line segment at depth " + depth);
                    yield return Wait(InnerGenerate(true));
					yield break;
				}

				// We're on a new line segment, change the current archetype
				if (lineSegment != previousLineSegment)
				{
					currentArchetype = lineSegment.GetRandomArchetype(RandomStream, placementSlots.Select(x => x.Archetype));
					previousLineSegment = lineSegment;
				}

				List<TileSet> useableTileSets = null;
				GraphNode nextNode = null;
				var orderedNodes = DungeonFlow.Nodes.OrderBy(x => x.Position).ToArray();

				// Determine which node comes next
				foreach (var node in orderedNodes)
				{
					if (depth >= node.Position && !handledNodes.Contains(node))
					{
						nextNode = node;
						handledNodes.Add(node);
						break;
					}
				}

				var placementParams = new TilePlacementParameters();
				placementSlots.Add(placementParams);

				// Assign the TileSets to use based on whether we're on a node or a line segment
				if (nextNode != null)
				{
					useableTileSets = nextNode.TileSets;
					nextNodeIndex = (nextNodeIndex >= orderedNodes.Length - 1) ? -1 : nextNodeIndex + 1;
					placementParams.Node = nextNode;

					if (nextNode == orderedNodes[orderedNodes.Length - 1])
						isDone = true;
				}
				else
				{
					useableTileSets = currentArchetype.TileSets;
					placementParams.Archetype = currentArchetype;
					placementParams.Line = lineSegment;
				}

				slotTileSets.Add(useableTileSets);
				i++;
			}

			int tileRetryCount = 0;
			int totalForLoopRetryCount = 0;

			for (int j = 0; j < placementSlots.Count; j++)
			{
				var attachTo = (j == 0) ? null : proxyDungeon.MainPathTiles[proxyDungeon.MainPathTiles.Count - 1];
				var tile = AddTile(attachTo, slotTileSets[j], j / (float)(placementSlots.Count - 1), placementSlots[j]);

				// if no tile could be generated delete last successful tile and retry from previous index
				// else return false
				if (j > 5 && tile == null && tileRetryCount < 5 && totalForLoopRetryCount < 20)
				{
					TileProxy previousTile = proxyDungeon.MainPathTiles[j - 1];

					// If the tile we're removing was placed by tile injection, be sure to place the injected tile back on the pending list
					InjectedTile previousInjectedTile;
					if (injectedTiles.TryGetValue(previousTile, out previousInjectedTile))
					{
						tilesPendingInjection.Add(previousInjectedTile);
						injectedTiles.Remove(previousTile);
					}

					proxyDungeon.RemoveLastConnection();
					proxyDungeon.RemoveTile(previousTile);
					CollisionManager.RemoveTile(previousTile);

					j -= 2; // -2 because loop adds 1
					tileRetryCount++;
					totalForLoopRetryCount++;
				}
				else if (tile == null)
				{
					yield return Wait(InnerGenerate(true));
					yield break;
				}
				else
				{
					tile.Placement.PlacementParameters = placementSlots[j];
					tileRetryCount = 0;


					// Wait for a frame to allow for animated loading screens, etc
					if (ShouldSkipFrame(true))
						yield return GetRoomPause();
				}
			}

			yield break; // Required for generation to run synchronously
		}

		private bool ShouldSkipFrame(bool isRoomPlacement)
		{
			if (!GenerateAsynchronously)
				return false;

			if (isRoomPlacement && PauseBetweenRooms > 0)
				return true;
			else
			{
				bool frameWasTooLong =	MaxAsyncFrameMilliseconds <= 0 ||
										yieldTimer.Elapsed.TotalMilliseconds >= MaxAsyncFrameMilliseconds;

				if (frameWasTooLong)
				{
					yieldTimer.Restart();
					return true;
				}
				else
					return false;
			}
		}

		private YieldInstruction GetRoomPause()
		{
			if (PauseBetweenRooms > 0)
				return new WaitForSeconds(PauseBetweenRooms);
			else
				return null;
		}

		protected virtual IEnumerator GenerateBranchPaths()
		{
			ChangeStatus(GenerationStatus.Branching);

			int[] mainPathBranches = new int[proxyDungeon.MainPathTiles.Count];
			BranchCountHelper.ComputeBranchCounts(DungeonFlow, RandomStream, proxyDungeon, ref mainPathBranches);

			int branchId = 0;

			for (int b = 0; b < mainPathBranches.Length; b++)
			{
				var tile = proxyDungeon.MainPathTiles[b];
				int branchCount = mainPathBranches[b];

				// This tile was created from a graph node, there should be no branching
				if (tile.Placement.Archetype == null)
					continue;

				if (branchCount == 0)
					continue;

				for (int i = 0; i < branchCount; i++)
				{
					TileProxy previousTile = tile;
					int branchDepth = tile.Placement.Archetype.BranchingDepth.GetRandom(RandomStream);

					for (int j = 0; j < branchDepth; j++)
					{
						List<TileSet> useableTileSets;

						// Branch start tiles
						if (j == 0 && tile.Placement.Archetype.GetHasValidBranchStartTiles())
						{
							if (tile.Placement.Archetype.BranchStartType == BranchCapType.InsteadOf)
								useableTileSets = tile.Placement.Archetype.BranchStartTileSets;
							else
								useableTileSets = tile.Placement.Archetype.TileSets.Concat(tile.Placement.Archetype.BranchStartTileSets).ToList();
						}
						// Branch cap tiles
						else if (j == (branchDepth - 1) && tile.Placement.Archetype.GetHasValidBranchCapTiles())
						{
							if (tile.Placement.Archetype.BranchCapType == BranchCapType.InsteadOf)
								useableTileSets = tile.Placement.Archetype.BranchCapTileSets;
							else
								useableTileSets = tile.Placement.Archetype.TileSets.Concat(tile.Placement.Archetype.BranchCapTileSets).ToList();
						}
						// Other tiles
						else
							useableTileSets = tile.Placement.Archetype.TileSets;

						float normalizedDepth = (branchDepth <= 1) ? 1 : j / (float)(branchDepth - 1);
						var newTile = AddTile(previousTile, useableTileSets, normalizedDepth, tile.Placement.PlacementParameters);

						if (newTile == null)
							break;

						newTile.Placement.BranchDepth = j;
						newTile.Placement.NormalizedBranchDepth = normalizedDepth;
						newTile.Placement.BranchId = branchId;
						newTile.Placement.PlacementParameters = previousTile.Placement.PlacementParameters;
						previousTile = newTile;

						// Wait for a frame to allow for animated loading screens, etc
						if (ShouldSkipFrame(true))
							yield return GetRoomPause();
					}

					branchId++;
				}
			}

			yield break;
		}

		protected virtual TileProxy AddTile(TileProxy attachTo, IEnumerable<TileSet> useableTileSets, float normalizedDepth, TilePlacementParameters placementParams)
		{
			bool isOnMainPath = (Status == GenerationStatus.MainPath);
			bool isFirstTile = attachTo == null;

			// If we're attaching to an existing dungeon, generate a dummy attachment point
			if(isFirstTile && AttachmentSettings != null)
			{
				var attachmentProxy = AttachmentSettings.GenerateAttachmentProxy(UpVector, RandomStream);
				attachTo = attachmentProxy;
			}

			// Check list of tiles to inject
			InjectedTile chosenInjectedTile = null;
			int injectedTileIndexToRemove = -1;

			bool isPlacingSpecificRoom = isOnMainPath && (placementParams.Archetype == null);

			if (tilesPendingInjection != null && !isPlacingSpecificRoom)
			{
				float pathDepth = (isOnMainPath) ? normalizedDepth : attachTo.Placement.PathDepth / (targetLength - 1f);
				float branchDepth = (isOnMainPath) ? 0 : normalizedDepth;

				for (int i = 0; i < tilesPendingInjection.Count; i++)
				{
					var injectedTile = tilesPendingInjection[i];

					if (injectedTile.ShouldInjectTileAtPoint(isOnMainPath, pathDepth, branchDepth))
					{
						chosenInjectedTile = injectedTile;
						injectedTileIndexToRemove = i;

						break;
					}
				}
			}


			// Select appropriate tile weights
			IEnumerable<GameObjectChance> chanceEntries;

			if (chosenInjectedTile != null)
				chanceEntries = new List<GameObjectChance>(chosenInjectedTile.TileSet.TileWeights.Weights);
			else
				chanceEntries = useableTileSets.SelectMany(x => x.TileWeights.Weights);


			// Leave the decision to allow rotation up to the new tile by default
			bool? allowRotation = null;
			
			// Apply constraint overrides
			if (OverrideAllowTileRotation)
				allowRotation = AllowTileRotation;


			DoorwayPairFinder doorwayPairFinder = new DoorwayPairFinder()
			{
				DungeonFlow = DungeonFlow,
				RandomStream = RandomStream,
				PlacementParameters = placementParams,
				GetTileTemplateDelegate = GetTileTemplate,
				IsOnMainPath = isOnMainPath,
				NormalizedDepth = normalizedDepth,
				PreviousTile = attachTo,
				UpVector = UpVector,
				AllowRotation = allowRotation,
				TileWeights = new List<GameObjectChance>(chanceEntries),
				DungeonProxy = proxyDungeon,

				IsTileAllowedPredicate = (TileProxy previousTile, TileProxy potentialNextTile, ref float weight) =>
				{
					bool isImmediateRepeat = previousTile != null && (potentialNextTile.Prefab == previousTile.Prefab);
					var repeatMode = TileRepeatMode.Allow;

					if (OverrideRepeatMode)
						repeatMode = RepeatMode;
					else if (potentialNextTile != null)
						repeatMode = potentialNextTile.PrefabTile.RepeatMode;

					bool allowTile = true;

					switch (repeatMode)
					{
						case TileRepeatMode.Allow:
							allowTile = true;
							break;

						case TileRepeatMode.DisallowImmediate:
							allowTile = !isImmediateRepeat;
							break;

						case TileRepeatMode.Disallow:
							allowTile = !proxyDungeon.AllTiles.Where(t => t.Prefab == potentialNextTile.Prefab).Any();
							break;

						default:
							throw new NotImplementedException("TileRepeatMode " + repeatMode + " is not implemented");
					}

					return allowTile;
				},
			};

			int? maxPairingAttempts = (UseMaximumPairingAttempts) ? (int?)MaxPairingAttempts : null;
			Queue<DoorwayPair> pairsToTest = doorwayPairFinder.GetDoorwayPairs(maxPairingAttempts);
			TilePlacementResult lastTileResult = null;
			TileProxy createdTile = null;

			if (pairsToTest.Count == 0)
				tilePlacementResults.Add(new NoMatchingDoorwayPlacementResult(attachTo));

			while (pairsToTest.Count > 0)
			{
				var pair = pairsToTest.Dequeue();

				lastTileResult = TryPlaceTile(pair, placementParams, out createdTile);

				if (lastTileResult is SuccessPlacementResult)
					break;
				else
					tilePlacementResults.Add(lastTileResult);
			}

			// Successfully placed the tile
			if (lastTileResult is SuccessPlacementResult)
			{
				// We've successfully injected the tile, so we can remove it from the pending list now
				if (chosenInjectedTile != null)
				{
					createdTile.Placement.InjectionData = chosenInjectedTile;

					injectedTiles[createdTile] = chosenInjectedTile;
					tilesPendingInjection.RemoveAt(injectedTileIndexToRemove);

					if (isOnMainPath)
						targetLength++;
				}

				return createdTile;
			}
			else
				return null;
		}

		protected TilePlacementResult TryPlaceTile(DoorwayPair pair, TilePlacementParameters placementParameters, out TileProxy tile)
		{
			tile = null;

			var toTemplate = pair.NextTemplate;
			var fromDoorway = pair.PreviousDoorway;

			if (toTemplate == null)
				return new NullTemplatePlacementResult();

			int toDoorwayIndex = pair.NextTemplate.Doorways.IndexOf(pair.NextDoorway);
			tile = tileProxyPool.TakeObject(toTemplate);
			tile.Placement.IsOnMainPath = Status == GenerationStatus.MainPath;
			tile.Placement.PlacementParameters = placementParameters;
			tile.Placement.TileSet = pair.NextTileSet;

			if (fromDoorway != null)
			{
				// Move the proxy object into position
				var toProxyDoor = tile.Doorways[toDoorwayIndex];
				tile.PositionBySocket(toProxyDoor, fromDoorway);

				Bounds proxyBounds = tile.Placement.Bounds;

				// Check if the new tile is outside of the valid bounds
				if (RestrictDungeonToBounds && !TilePlacementBounds.Contains(proxyBounds))
				{
					tileProxyPool.ReturnObject(tile);
					return new OutOfBoundsPlacementResult(toTemplate);
				}

				// Check if the new tile is colliding with any other
				bool isColliding = CollisionManager.IsCollidingWithAnyTile(UpDirection, tile, fromDoorway.TileProxy);

				if (isColliding)
				{
					tileProxyPool.ReturnObject(tile);
					return new TileIsCollidingPlacementResult(toTemplate);
				}
			}

			if (tile.Placement.IsOnMainPath)
			{
				if (pair.PreviousTile != null)
					tile.Placement.PathDepth = pair.PreviousTile.Placement.PathDepth + 1;
			}
			else
			{
				tile.Placement.PathDepth = pair.PreviousTile.Placement.PathDepth;
				tile.Placement.BranchDepth = (pair.PreviousTile.Placement.IsOnMainPath) ? 0 : pair.PreviousTile.Placement.BranchDepth + 1;
			}

			var toDoorway = tile.Doorways[toDoorwayIndex];

			if (fromDoorway != null)
				proxyDungeon.MakeConnection(fromDoorway, toDoorway);

			proxyDungeon.AddTile(tile);
			CollisionManager.AddTile(tile);

			return new SuccessPlacementResult();
		}

		protected TileProxy GetTileTemplate(GameObject prefab)
		{
			// No proxy has been loaded yet, we should create one
			if (!preProcessData.TryGetValue(prefab, out var template))
			{
				template = new TileProxy(prefab);
				preProcessData.Add(prefab, template);
			}

			return template;
		}

		protected TileProxy PickRandomTemplate(DoorwaySocket socketGroupFilter)
		{
			// Pick a random tile
			var tile = useableTiles[RandomStream.Next(0, useableTiles.Count)];
			var template = GetTileTemplate(tile);

			// If there's a socket group filter and the chosen Tile doesn't have a socket of this type, try again
			if (socketGroupFilter != null && !template.UnusedDoorways.Where(d => d.Socket == socketGroupFilter).Any())
				return PickRandomTemplate(socketGroupFilter);

			return template;
		}

		protected int NormalizedDepthToIndex(float normalizedDepth)
		{
			return Mathf.RoundToInt(normalizedDepth * (targetLength - 1));
		}

		protected float IndexToNormalizedDepth(int index)
		{
			return index / (float)targetLength;
		}

		/// <summary>
		/// Registers a post-process step with the generator which allows for a callback function to be invoked during the PostProcess step
		/// </summary>
		/// <param name="postProcessCallback">The callback to invoke</param>
		/// <param name="priority">The priority which determines the order in which post-process steps are invoked (highest to lowest).</param>
		/// <param name="phase">Which phase to run the post-process step. Used to determine whether the step should run before or after DunGen's built-in post-processing</param>
		public void RegisterPostProcessStep(Action<DungeonGenerator> postProcessCallback, int priority = 0, PostProcessPhase phase = PostProcessPhase.AfterBuiltIn)
		{
			postProcessSteps.Add(new DungeonGeneratorPostProcessStep(postProcessCallback, priority, phase));
		}

		/// <summary>
		/// Unregisters an existing post-process step registered using RegisterPostProcessStep()
		/// </summary>
		/// <param name="postProcessCallback">The callback to remove</param>
		public void UnregisterPostProcessStep(Action<DungeonGenerator> postProcessCallback)
		{
			for (int i = 0; i < postProcessSteps.Count; i++)
				if (postProcessSteps[i].PostProcessCallback == postProcessCallback)
					postProcessSteps.RemoveAt(i);
		}

		protected virtual IEnumerator PostProcess()
		{
			GenerationStats.BeginTime(GenerationStatus.PostProcessing);
			ChangeStatus(GenerationStatus.PostProcessing);
			int length = proxyDungeon.MainPathTiles.Count;

			// Calculate maximum branch depth
			int maxBranchDepth = 0;

			if (proxyDungeon.BranchPathTiles.Count > 0)
			{
				foreach(var branchTile in proxyDungeon.BranchPathTiles)
				{
					int branchDepth = branchTile.Placement.BranchDepth;

					if (branchDepth > maxBranchDepth)
						maxBranchDepth = branchDepth;
				}
			}

			// Waiting one frame so objects are in their expected state
			yield return null;


			// Order post-process steps by priority
			postProcessSteps.Sort((a, b) =>
			{
				return b.Priority.CompareTo(a.Priority);
			});

			// Apply any post-process to be run BEFORE built-in post-processing is run
			foreach (var step in postProcessSteps)
			{
				if (ShouldSkipFrame(false))
					yield return null;

				if (step.Phase == PostProcessPhase.BeforeBuiltIn)
					step.PostProcessCallback(this);
			}


			// Waiting one frame so objects are in their expected state
			yield return null;

			foreach (var tile in CurrentDungeon.AllTiles)
			{
				if (ShouldSkipFrame(false))
					yield return null;

				tile.Placement.NormalizedPathDepth = tile.Placement.PathDepth / (float)(length - 1);
			}

			CurrentDungeon.PostGenerateDungeon(this);


			// Process random props
			ProcessLocalProps();
			ProcessGlobalProps();

			if (DungeonFlow.KeyManager != null)
				PlaceLocksAndKeys();

			GenerationStats.SetRoomStatistics(CurrentDungeon.MainPathTiles.Count, CurrentDungeon.BranchPathTiles.Count, maxBranchDepth);
			preProcessData.Clear();


			// Waiting one frame so objects are in their expected state
			yield return null;


			// Apply any post-process to be run AFTER built-in post-processing is run
			foreach (var step in postProcessSteps)
			{
				if (ShouldSkipFrame(false))
					yield return null;

				if (step.Phase == PostProcessPhase.AfterBuiltIn)
					step.PostProcessCallback(this);
			}


			// Finalise
			GenerationStats.EndTime();

			// Activate all door gameobjects that were added to doorways
			foreach (var door in CurrentDungeon.Doors)
				if (door != null)
					door.SetActive(true);
		}

		protected virtual void ProcessLocalProps()
		{
			void GetHierarchyDepth(Transform transform, ref int depth)
			{
				if (transform.parent != null)
				{
					depth++;
					GetHierarchyDepth(transform.parent, ref depth);
				}
			}

			var props = Root.GetComponentsInChildren<RandomProp>();
			var propData = new List<PropProcessingData>();

			foreach (var prop in props)
			{
				int depth = 0;
				GetHierarchyDepth(prop.transform, ref depth);

				propData.Add(new PropProcessingData()
				{
					PropComponent = prop,
					HierarchyDepth = depth,
					OwningTile = prop.GetComponentInParent<Tile>()
				});
			}

			// Order by hierarchy depth to ensure a parent prop group is processed before its children
			propData = propData.OrderBy(x => x.HierarchyDepth).ToList();

			var spawnedObjects = new List<GameObject>();

			for (int i = 0; i < propData.Count; i++)
			{
				var data = propData[i];

				if (data.PropComponent == null)
					continue;

				spawnedObjects.Clear();
				data.PropComponent.Process(RandomStream, data.OwningTile, ref spawnedObjects);

				// Gather any spawned sub-props and insert them into the list to be processed too
				var spawnedSubProps = spawnedObjects.SelectMany(x => x.GetComponentsInChildren<RandomProp>()).Distinct();

				foreach (var subProp in spawnedSubProps)
				{
					propData.Insert(i + 1, new PropProcessingData()
					{
						PropComponent = subProp,
						HierarchyDepth = data.HierarchyDepth + 1,
						OwningTile = data.OwningTile
					});
				}
			}
		}

		protected virtual void ProcessGlobalProps()
		{
			Dictionary<int, GameObjectChanceTable> globalPropWeights = new Dictionary<int, GameObjectChanceTable>();

			foreach (var tile in CurrentDungeon.AllTiles)
			{
				foreach (var prop in tile.GetComponentsInChildren<GlobalProp>(true))
				{
					GameObjectChanceTable table = null;

					if (!globalPropWeights.TryGetValue(prop.PropGroupID, out table))
					{
						table = new GameObjectChanceTable();
						globalPropWeights[prop.PropGroupID] = table;
					}

					float weight = (tile.Placement.IsOnMainPath) ? prop.MainPathWeight : prop.BranchPathWeight;
					weight *= prop.DepthWeightScale.Evaluate(tile.Placement.NormalizedDepth);

					table.Weights.Add(new GameObjectChance(prop.gameObject, weight, 0, null));
				}
			}

			foreach (var chanceTable in globalPropWeights.Values)
				foreach (var weight in chanceTable.Weights)
					weight.Value.SetActive(false);

			List<int> processedPropGroups = new List<int>(globalPropWeights.Count);

			foreach (var pair in globalPropWeights)
			{
				if (processedPropGroups.Contains(pair.Key))
				{
					Debug.LogWarning("Dungeon Flow contains multiple entries for the global prop group ID: " + pair.Key + ". Only the first entry will be used.");
					continue;
				}

				var prop = DungeonFlow.GlobalProps.Where(x => x.ID == pair.Key).FirstOrDefault();

				if (prop == null)
					continue;

				var weights = pair.Value.Clone();
				int propCount = prop.Count.GetRandom(RandomStream);
				propCount = Mathf.Clamp(propCount, 0, weights.Weights.Count);

				for (int i = 0; i < propCount; i++)
				{
					var chosenEntry = weights.GetRandom(RandomStream,
						isOnMainPath: true,
						normalizedDepth: 0,
						previouslyChosen: null,
						allowImmediateRepeats: true,
						removeFromTable: true);

					if (chosenEntry != null && chosenEntry.Value != null)
						chosenEntry.Value.SetActive(true);
				}

				processedPropGroups.Add(pair.Key);
			}
		}

		protected virtual void PlaceLocksAndKeys()
		{
			var nodes = CurrentDungeon.ConnectionGraph.Nodes.Select(x => x.Tile.Placement.GraphNode).Where(x => { return x != null; }).Distinct().ToArray();
			var lines = CurrentDungeon.ConnectionGraph.Nodes.Select(x => x.Tile.Placement.GraphLine).Where(x => { return x != null; }).Distinct().ToArray();

			Dictionary<Doorway, Key> lockedDoorways = new Dictionary<Doorway, Key>();

			// Lock doorways on nodes
			foreach (var node in nodes)
			{
				foreach (var l in node.Locks)
				{
					var tile = CurrentDungeon.AllTiles.Where(x => { return x.Placement.GraphNode == node; }).FirstOrDefault();
					var connections = CurrentDungeon.ConnectionGraph.Nodes.Where(x => { return x.Tile == tile; }).FirstOrDefault().Connections;
					Doorway entrance = null;
					Doorway exit = null;

					foreach (var conn in connections)
					{
						if (conn.DoorwayA.Tile == tile)
							exit = conn.DoorwayA;
						else if (conn.DoorwayB.Tile == tile)
							entrance = conn.DoorwayB;
					}

					var key = node.Graph.KeyManager.GetKeyByID(l.ID);

					if (entrance != null && (node.LockPlacement & NodeLockPlacement.Entrance) == NodeLockPlacement.Entrance)
						lockedDoorways.Add(entrance, key);

					if (exit != null && (node.LockPlacement & NodeLockPlacement.Exit) == NodeLockPlacement.Exit)
						lockedDoorways.Add(exit, key);
				}
			}

			// Lock doorways on lines
			foreach (var line in lines)
			{
				var doorways = CurrentDungeon.ConnectionGraph.Connections.Where(x =>
				{
					var tileSet = x.DoorwayA.Tile.Placement.TileSet;

					if (tileSet == null)
						return false;

					bool isDoorwayAlreadyLocked = lockedDoorways.ContainsKey(x.DoorwayA) || lockedDoorways.ContainsKey(x.DoorwayB);
					bool doorwayHasLockPrefabs = tileSet.LockPrefabs.Count > 0;

					return x.DoorwayA.Tile.Placement.GraphLine == line &&
							x.DoorwayB.Tile.Placement.GraphLine == line &&
							!isDoorwayAlreadyLocked &&
							doorwayHasLockPrefabs;

				}).Select(x => x.DoorwayA).ToList();

				if (doorways.Count == 0)
					continue;

				foreach (var l in line.Locks)
				{
					int lockCount = l.Range.GetRandom(RandomStream);
					lockCount = Mathf.Clamp(lockCount, 0, doorways.Count);

					for (int i = 0; i < lockCount; i++)
					{
						if (doorways.Count == 0)
							break;

						var doorway = doorways[RandomStream.Next(0, doorways.Count)];
						doorways.Remove(doorway);

						if (lockedDoorways.ContainsKey(doorway))
							continue;

						var key = line.Graph.KeyManager.GetKeyByID(l.ID);
						lockedDoorways.Add(doorway, key);
					}
				}
			}


			// Lock doorways on injected tiles
			foreach (var tile in CurrentDungeon.AllTiles)
			{
				if (tile.Placement.InjectionData != null && tile.Placement.InjectionData.IsLocked)
				{
					var validLockedDoorways = new List<Doorway>();

					foreach (var doorway in tile.UsedDoorways)
					{
						bool isDoorwayAlreadyLocked = lockedDoorways.ContainsKey(doorway) || lockedDoorways.ContainsKey(doorway.ConnectedDoorway);
						bool doorwayHasLockPrefabs = tile.Placement.TileSet.LockPrefabs.Count > 0;
						bool isEntranceDoorway = tile.GetEntranceDoorway() == doorway;

						if (!isDoorwayAlreadyLocked &&
							doorwayHasLockPrefabs &&
							isEntranceDoorway)
						{
							validLockedDoorways.Add(doorway);
						}
					}

					if (validLockedDoorways.Any())
					{
						var doorway = validLockedDoorways.First();
						var key = DungeonFlow.KeyManager.GetKeyByID(tile.Placement.InjectionData.LockID);

						lockedDoorways.Add(doorway, key);
					}
				}
			}

			var locksToRemove = new List<Doorway>();
			var usedSpawnComponents = new List<IKeySpawner>();

			foreach (var pair in lockedDoorways)
			{
				var doorway = pair.Key;
				var key = pair.Value;
				var possibleSpawnTiles = new List<Tile>();

				foreach (var t in CurrentDungeon.AllTiles)
				{
					if (t.Placement.NormalizedPathDepth >= doorway.Tile.Placement.NormalizedPathDepth)
						continue;

					bool canPlaceKey = false;

					if (t.Placement.GraphNode != null && t.Placement.GraphNode.Keys.Where(x => { return x.ID == key.ID; }).Count() > 0)
						canPlaceKey = true;
					else if (t.Placement.GraphLine != null && t.Placement.GraphLine.Keys.Where(x => { return x.ID == key.ID; }).Count() > 0)
						canPlaceKey = true;

					if (!canPlaceKey)
						continue;

					possibleSpawnTiles.Add(t);
				}

				var possibleSpawnComponents = possibleSpawnTiles
					.SelectMany(x => x.GetComponentsInChildren<Component>()
					.OfType<IKeySpawner>())
					.Except(usedSpawnComponents)
					.Where(x => x.CanSpawnKey(DungeonFlow.KeyManager, key))
					.ToArray();

				GameObject lockedDoorPrefab = null;
				
				if(possibleSpawnComponents.Any())
					lockedDoorPrefab = TryGetRandomLockedDoorPrefab(doorway, key, DungeonFlow.KeyManager);

				if (!possibleSpawnComponents.Any() || lockedDoorPrefab == null)
					locksToRemove.Add(doorway);
				else
				{
					doorway.LockID = key.ID;

					var keySpawnParameters = new KeySpawnParameters(key, DungeonFlow.KeyManager, this);

					int keysToSpawn = key.KeysPerLock.GetRandom(RandomStream);
					keysToSpawn = Math.Min(keysToSpawn, possibleSpawnComponents.Length);

					for (int i = 0; i < keysToSpawn; i++)
					{
						int chosenSpawnerIndex = RandomStream.Next(0, possibleSpawnComponents.Length);
						var keySpawner = possibleSpawnComponents[chosenSpawnerIndex];

						keySpawnParameters.OutputSpawnedKeys.Clear();
						keySpawner.SpawnKey(keySpawnParameters);

						foreach(var receiver in keySpawnParameters.OutputSpawnedKeys)
							receiver.OnKeyAssigned(key, DungeonFlow.KeyManager);

						usedSpawnComponents.Add(keySpawner);
					}

					LockDoorway(doorway, lockedDoorPrefab, key, DungeonFlow.KeyManager);
				}
			}

			foreach (var doorway in locksToRemove)
			{
				doorway.LockID = null;
				lockedDoorways.Remove(doorway);
			}
		}

		protected virtual GameObject TryGetRandomLockedDoorPrefab(Doorway doorway, Key key, KeyManager keyManager)
		{
			var placement = doorway.Tile.Placement;
			var prefabs = doorway.Tile.Placement.TileSet.LockPrefabs.Where(x =>
			{
				if (x == null || x.LockPrefabs == null)
					return false;

				if (!x.LockPrefabs.HasAnyValidEntries(placement.IsOnMainPath, placement.NormalizedDepth, null, true))
					return false;

				var lockSocket = x.Socket;

				if (lockSocket == null)
					return true;
				else
					return DoorwaySocket.CanSocketsConnect(lockSocket, doorway.Socket);

			}).Select(x => x.LockPrefabs).ToArray();

			if (prefabs.Length == 0)
				return null;

			var chosenEntry = prefabs[RandomStream.Next(0, prefabs.Length)].GetRandom(RandomStream, placement.IsOnMainPath, placement.NormalizedDepth, null, true);
			return chosenEntry.Value;
		}

		protected virtual void LockDoorway(Doorway doorway, GameObject doorPrefab, Key key, KeyManager keyManager)
		{
			GameObject doorObj = GameObject.Instantiate(doorPrefab, doorway.transform);

			DungeonUtil.AddAndSetupDoorComponent(CurrentDungeon, doorObj, doorway);

			// Remove any existing door prefab that may have been placed as we'll be replacing it with a locked door
			doorway.RemoveUsedPrefab();

			// Set this locked door as the current door prefab
			doorway.SetUsedPrefab(doorObj);
			doorway.ConnectedDoorway.SetUsedPrefab(doorObj);

			foreach (var keylock in doorObj.GetComponentsInChildren<Component>().OfType<IKeyLock>())
				keylock.OnKeyAssigned(key, keyManager);
		}

		#region ISerializationCallbackReceiver Implementation

		public void OnBeforeSerialize()
		{
			fileVersion = CurrentFileVersion;
		}

		public void OnAfterDeserialize()
		{
#pragma warning disable CS0618 // Type or member is obsolete

			// Upgrade to new repeat mode
			if (fileVersion < 1)
				RepeatMode = (allowImmediateRepeats) ? TileRepeatMode.Allow : TileRepeatMode.DisallowImmediate;

			// Moved collision properties to their own settings class
			if (fileVersion < 2)
			{
				if(CollisionSettings == null)
					CollisionSettings = new DungeonCollisionSettings();

				CollisionSettings.DisallowOverhangs = DisallowOverhangs;
				CollisionSettings.OverlapThreshold = OverlapThreshold;
				CollisionSettings.Padding = Padding;
				CollisionSettings.AvoidCollisionsWithOtherDungeons = AvoidCollisionsWithOtherDungeons;
			}

			if (fileVersion < 3)
				TriggerPlacement = PlaceTileTriggers ? TriggerPlacementMode.ThreeDimensional : TriggerPlacementMode.None;

#pragma warning restore CS0618 // Type or member is obsolete
		}

		#endregion
	}
}
