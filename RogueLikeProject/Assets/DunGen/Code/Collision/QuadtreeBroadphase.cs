#if DUNGEN_QUADTREE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace DunGen.Collision
{
	[Serializable]
	[DisplayName("Quadtree")]
	public class QuadtreeBroadphaseSettings : BroadphaseSettings
	{
		public Bounds InitialBounds = new Bounds(Vector3.zero, new Vector3(100, 10, 100));

		public override ICollisionBroadphase Create()
		{
			return new QuadtreeBroadphase();
		}
	}

	public class QuadtreeBroadphase : ICollisionBroadphase
	{
		public Quadtree<Bounds> Quadtree { get; private set; }


		public void Init(BroadphaseSettings settings, DungeonGenerator dungeonGenerator)
		{
			if (!(settings is QuadtreeBroadphaseSettings quadtreeSettings))
				throw new ArgumentException("Settings must be of type QuadtreeSettings");

			Bounds initialBounds = quadtreeSettings.InitialBounds;

			// Override initial bounds to be the maximum bounds of the dungeon if applicable
			if (dungeonGenerator.RestrictDungeonToBounds)
				initialBounds = dungeonGenerator.TilePlacementBounds;

			Quadtree = new Quadtree<Bounds>(initialBounds, (b) => b);
		}

		public void Insert(Bounds bounds)
		{
			Quadtree.Insert(bounds);
		}

		public void Query(Bounds bounds, ref List<Bounds> results)
		{
			Quadtree.Query(bounds, ref results);
		}

		public void Remove(Bounds bounds)
		{
			Quadtree.Remove(bounds);
		}

		public void DrawDebug(float duration = 0)
		{
			Quadtree.DrawDebug(duration);
		}
	}
}
#endif