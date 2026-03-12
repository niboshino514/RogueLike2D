#if DUNGEN_QUADTREE
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen.Collision
{
	/// <summary>
	/// A generic quadtree that supports insertion, removal, querying, and automatic expansion.
	/// </summary>
	/// <typeparam name="T">The type of objects stored in the quadtree.</typeparam>
	public class Quadtree<T>
	{
		/// <summary>
		/// Internal node class that holds objects and may subdivide into four children.
		/// </summary>
		private class Node
		{
			public Bounds bounds;
			public List<T> objects;
			public Node[] children; // null if leaf

			/// <summary>
			/// True if this node has not been subdivided.
			/// </summary>
			public bool IsLeaf => children == null;

			public Node(Bounds bounds)
			{
				this.bounds = bounds;
				this.objects = new List<T>();
			}

			/// <summary>
			/// Subdivides this node into four children along the X and Z axes.
			/// Assumes a 2D quadtree (ignoring Y, but preserving its height).
			/// </summary>
			public void Subdivide()
			{
				children = new Node[4];
				// Each child has half the size in x and z, but retains the parent's height.
				Vector3 childSize = new Vector3(bounds.size.x / 2f, bounds.size.y, bounds.size.z / 2f);
				Vector3 center = bounds.center;
				float offsetX = childSize.x / 2f;
				float offsetZ = childSize.z / 2f;
				// Bottom-left quadrant (assuming z increases upward)
				children[0] = new Node(new Bounds(center + new Vector3(-offsetX, 0, -offsetZ), childSize));
				// Bottom-right quadrant
				children[1] = new Node(new Bounds(center + new Vector3(offsetX, 0, -offsetZ), childSize));
				// Top-left quadrant
				children[2] = new Node(new Bounds(center + new Vector3(-offsetX, 0, offsetZ), childSize));
				// Top-right quadrant
				children[3] = new Node(new Bounds(center + new Vector3(offsetX, 0, offsetZ), childSize));
			}
		}

		// The current quadtree root.
		private Node root;
		// Delegate to obtain a Bounds from an object of type T.
		private readonly Func<T, Bounds> getBounds;
		// Maximum objects a node can hold before subdividing.
		private readonly int maxObjectsPerNode;
		// Maximum allowed depth (to prevent infinite subdivision).
		private readonly int maxDepth;

		/// <summary>
		/// Constructs a quadtree with an initial bounding region.
		/// </summary>
		/// <param name="initialBounds">The starting bounds of the quadtree.</param>
		/// <param name="getBounds">A delegate to extract the Unity Bounds from a T object.</param>
		/// <param name="maxObjectsPerNode">Maximum objects per node before subdivision.</param>
		/// <param name="maxDepth">Maximum subdivision depth.</param>
		public Quadtree(Bounds initialBounds, Func<T, Bounds> getBounds, int maxObjectsPerNode = 4, int maxDepth = 10)
		{
			root = new Node(initialBounds);
			this.getBounds = getBounds;
			this.maxObjectsPerNode = maxObjectsPerNode;
			this.maxDepth = maxDepth;
		}

		/// <summary>
		/// Inserts an object into the quadtree.
		/// Automatically expands the tree if the object does not fit in the current bounds.
		/// </summary>
		/// <param name="obj">The object to insert.</param>
		public void Insert(T obj)
		{
			Bounds objBounds = getBounds(obj);
			// Expand the tree until the new object fits entirely within the root.
			while (!root.bounds.Contains(objBounds.min) || !root.bounds.Contains(objBounds.max))
			{
				ExpandRoot(objBounds);
			}
			Insert(root, obj, 0);
		}

		/// <summary>
		/// Internal recursive insertion method.
		/// </summary>
		private void Insert(Node node, T obj, int depth)
		{
			Bounds objBounds = getBounds(obj);

			// If not a leaf, try to delegate insertion into one of the children.
			if (!node.IsLeaf)
			{
				foreach (Node child in node.children)
				{
					// Insert only if the child completely contains the object's bounds.
					if (child.bounds.Contains(objBounds.min) && child.bounds.Contains(objBounds.max))
					{
						Insert(child, obj, depth + 1);
						return;
					}
				}
			}

			// Otherwise, store the object in this node.
			node.objects.Add(obj);

			// Subdivide this node if too many objects have accumulated and if we haven't reached maximum depth.
			if (node.IsLeaf && node.objects.Count > maxObjectsPerNode && depth < maxDepth)
			{
				node.Subdivide();

				// Reinsert existing objects into children if they fully fit; otherwise they remain in this node.
				for (int i = node.objects.Count - 1; i >= 0; i--)
				{
					T existingObj = node.objects[i];
					Bounds existingBounds = getBounds(existingObj);
					foreach (Node child in node.children)
					{
						if (child.bounds.Contains(existingBounds.min) && child.bounds.Contains(existingBounds.max))
						{
							Insert(child, existingObj, depth + 1);
							node.objects.RemoveAt(i);
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// Removes an object from the quadtree.
		/// </summary>
		/// <param name="obj">The object to remove.</param>
		/// <returns>True if the object was removed; otherwise false.</returns>
		public bool Remove(T obj)
		{
			return Remove(root, obj);
		}

		/// <summary>
		/// Internal recursive removal method.
		/// </summary>
		private bool Remove(Node node, T obj)
		{
			bool removed = false;
			Bounds objBounds = getBounds(obj);

			// Check current node.
			if (node.objects.Remove(obj))
			{
				removed = true;
			}

			// If there are children, check any that might intersect with the object's bounds.
			if (!node.IsLeaf)
			{
				foreach (Node child in node.children)
				{
					// Only search children whose bounds intersect the object's bounds.
					if (child.bounds.Intersects(objBounds))
					{
						removed |= Remove(child, obj);
					}
				}
			}
			return removed;
		}

		/// <summary>
		/// Queries the quadtree for all objects whose bounds overlap the specified region.
		/// </summary>
		/// <param name="queryBounds">The bounds to query.</param>
		/// <returns>A list of objects overlapping the query bounds.</returns>
		public List<T> Query(Bounds queryBounds)
		{
			List<T> results = new List<T>();
			Query(root, queryBounds, results);
			return results;
		}

		public void Query(Bounds queryBounds, ref List<T> results)
		{
			Query(root, queryBounds, results);
		}

		/// <summary>
		/// Recursively collects objects overlapping the query bounds.
		/// </summary>
		private void Query(Node node, Bounds queryBounds, List<T> results)
		{
			if (!node.bounds.Intersects(queryBounds))
			{
				return;
			}

			// Add objects in this node that intersect the query bounds.
			foreach (T obj in node.objects)
			{
				if (getBounds(obj).Intersects(queryBounds))
				{
					results.Add(obj);
				}
			}

			// If the node is subdivided, query its children.
			if (!node.IsLeaf)
			{
				foreach (Node child in node.children)
				{
					Query(child, queryBounds, results);
				}
			}
		}

		/// <summary>
		/// Draws the quadtree nodes and the objects contained in each node using Unity's Debug.DrawLine API.
		/// </summary>
		/// <param name="duration">Duration (in seconds) for which the lines should persist.</param>
		public void DrawDebug(float duration = 0f)
		{
			DrawDebug(root, duration);
		}

		/// <summary>
		/// Recursively draws node boundaries and objects.
		/// </summary>
		private void DrawDebug(Node node, float duration)
		{
			// Draw this node's bounds (white).
			DrawBounds(node.bounds, Color.white, duration);

			// Draw each object's bounds within this node (green).
			foreach (T obj in node.objects)
			{
				Bounds objBounds = getBounds(obj);
				DrawBounds(objBounds, Color.green, duration);
			}

			// Recursively draw children nodes if they exist.
			if (!node.IsLeaf)
			{
				foreach (Node child in node.children)
				{
					DrawDebug(child, duration);
				}
			}
		}

		/// <summary>
		/// Helper method that draws a rectangle representing the bounds on the XZ plane.
		/// </summary>
		private void DrawBounds(Bounds b, Color color, float duration)
		{
			Vector3 center = b.center;
			Vector3 extents = b.extents;

			// Calculate corners (on the XZ plane; Y is kept at the center's Y).
			Vector3 topLeft = new Vector3(center.x - extents.x, center.y, center.z + extents.z);
			Vector3 topRight = new Vector3(center.x + extents.x, center.y, center.z + extents.z);
			Vector3 bottomRight = new Vector3(center.x + extents.x, center.y, center.z - extents.z);
			Vector3 bottomLeft = new Vector3(center.x - extents.x, center.y, center.z - extents.z);

			Debug.DrawLine(topLeft, topRight, color, duration);
			Debug.DrawLine(topRight, bottomRight, color, duration);
			Debug.DrawLine(bottomRight, bottomLeft, color, duration);
			Debug.DrawLine(bottomLeft, topLeft, color, duration);
		}

		/// <summary>
		/// Expands the root bounds so that it fully contains the specified object bounds.
		/// Instead of reinserting every object, the current root is wrapped in a new, larger root.
		/// This implementation uses a doubling strategy so that one child quadrant exactly matches the old root.
		/// </summary>
		private void ExpandRoot(Bounds objBounds)
		{
			// Save current root parameters.
			Vector3 oldCenter = root.bounds.center;
			Vector3 oldSize = root.bounds.size; // Must be nonzero and positive

			// Determine whether the new object is to the left/right and down/up of the current center.
			// If the new object is to the left, we want the old tree to be in the right quadrant, etc.
			bool newObjLeft = objBounds.center.x < oldCenter.x;
			bool newObjDown = objBounds.center.z < oldCenter.z;

			// Use a multiplier of 2 so that each child quadrant of the new root matches the old root exactly.
			Vector3 newSize = new Vector3(oldSize.x * 2f, oldSize.y, oldSize.z * 2f);

			// Determine offsets:
			// We want the child quadrant's center to equal oldCenter.
			// Since each child quadrant is half the new root's size,
			// oldCenter = newRoot.center + (dx * oldSize.x/2, 0, dz * oldSize.z/2)
			// Solve for newRoot.center:
			int dx = newObjLeft ? 1 : -1;  // if new object is left, shift right so that old tree ends up in the opposite (right) quadrant
			int dz = newObjDown ? 1 : -1;  // if new object is down, shift up so that old tree ends up in the opposite (up) quadrant
			Vector3 newRootCenter = oldCenter - new Vector3((dx * oldSize.x) / 2f, 0, (dz * oldSize.z) / 2f);

			// Create the new root with these computed parameters.
			Node newRoot = new Node(new Bounds(newRootCenter, newSize));
			newRoot.Subdivide();

			// Determine which child quadrant of the new root should exactly match the old root.
			// Child quadrant centers (relative to newRoot.center) are:
			// 0: bottom-left (-oldSize.x/2, -oldSize.z/2)
			// 1: bottom-right (oldSize.x/2, -oldSize.z/2)
			// 2: top-left (-oldSize.x/2, oldSize.z/2)
			// 3: top-right (oldSize.x/2, oldSize.z/2)
			int quadrant;
			if (dx == -1 && dz == -1)
				quadrant = 0; // new object is to right/up, so old tree goes bottom-left.
			else if (dx == 1 && dz == -1)
				quadrant = 1; // new object is to left/up, so old tree goes bottom-right.
			else if (dx == -1 && dz == 1)
				quadrant = 2; // new object is to right/down, so old tree goes top-left.
			else // (dx == 1 && dz == 1)
				quadrant = 3; // new object is to left/down, so old tree goes top-right.

			// Place the existing root into the appropriate quadrant.
			newRoot.children[quadrant] = root;
			root = newRoot;
		}


		/// <summary>
		/// Recursively collects all objects stored in the tree.
		/// </summary>
		private List<T> CollectAllObjects(Node node)
		{
			List<T> list = new List<T>(node.objects);
			if (!node.IsLeaf)
			{
				foreach (Node child in node.children)
				{
					list.AddRange(CollectAllObjects(child));
				}
			}
			return list;
		}
	}
}
#endif