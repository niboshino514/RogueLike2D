using DunGen.Pooling;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen
{
	[AddComponentMenu("DunGen/Random Props/Random Prefab")]
	public class RandomPrefab : RandomProp, ITileSpawnEventReceiver
	{
		[AcceptGameObjectTypes(GameObjectFilter.Asset)]
		public GameObjectChanceTable Props = new GameObjectChanceTable();
		public bool ZeroPosition = true;
		public bool ZeroRotation = true;

		private GameObject propInstance;


		private void ClearExistingInstances()
		{
			if (propInstance == null)
				return;

			DestroyImmediate(propInstance);
			propInstance = null;
		}

		public override void Process(RandomStream randomStream, Tile tile, ref List<GameObject> spawnedObjects)
		{
			ClearExistingInstances();

			if (Props.Weights.Count <= 0)
				return;

			var chosenEntry = Props.GetRandom(randomStream,
				tile.Placement.IsOnMainPath,
				tile.Placement.NormalizedDepth,
				previouslyChosen: null,
				allowImmediateRepeats: true,
				removeFromTable: false,
				allowNullSelection: true);

			if (chosenEntry == null || chosenEntry.Value == null)
				return;

			var prefab = chosenEntry.Value;

			propInstance = Instantiate(prefab);
			propInstance.transform.parent = transform;

			spawnedObjects.Add(propInstance);

			if (ZeroPosition)
				propInstance.transform.localPosition = Vector3.zero;
			else
				propInstance.transform.localPosition = prefab.transform.localPosition;

			if (ZeroRotation)
				propInstance.transform.localRotation = Quaternion.identity;
			else
				propInstance.transform.localRotation = prefab.transform.localRotation;
		}

		//
		// Begin ITileSpawnEventReceiver implementation

		public void OnTileSpawned(Tile tile) { }

		public void OnTileDespawned(Tile tile) => ClearExistingInstances();

		// End ITileSpawnEventReceiver implementation
		//
	}
}