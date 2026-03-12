using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen.Collision
{
	/// <summary>
	/// Used to add custom collision detection when deciding where to place tiles
	/// </summary>
	/// <param name="tileBounds">The tile bounds to check collisions for</param>
	/// <param name="isCollidingWithDungeon">If the tile is already deemed to be colliding the dungeon itself</param>
	/// <returns>True if the new tile bounds are blocked</returns>
	public delegate bool AdditionalCollisionsPredicate(Bounds tileBounds, bool isCollidingWithDungeon);

	[Serializable]
	public class DungeonCollisionSettings
	{
		/// <summary>
		/// If true, tiles will not be allowed to overhang other tiles
		/// </summary>
		public bool DisallowOverhangs = false;

		/// <summary>
		/// The maximum amount of overlap allowed between two connected tiles
		/// </summary>
		public float OverlapThreshold = 0.01f;

		/// <summary>
		/// The amount of padding to add to the bounds of each tile when checking for collisions
		/// </summary>
		public float Padding = 0.0f;

		/// <summary>
		/// An optional additional set of bounds to test against when determining if a tile will collide or not.
		/// Useful for preventing the dungeon from being generated in specific areas
		/// </summary>
		public readonly List<Bounds> AdditionalCollisionBounds = new List<Bounds>();

		/// <summary>
		/// If true, the dungeon will avoid collisions with other dungeons
		/// </summary>
		public bool AvoidCollisionsWithOtherDungeons = false;

		/// <summary>
		/// An optional predicate to test for additional collisions
		/// </summary>
		public AdditionalCollisionsPredicate AdditionalCollisionsPredicate;
	}
}
