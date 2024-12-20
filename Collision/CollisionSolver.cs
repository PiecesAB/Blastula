using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Blastula.Collision
{
	/// <summary>
	/// Collider shape. For now only a circle shape is available.
	/// </summary>
	public enum Shape
	{
		None,
		/// <summary>
		/// collisionSize.X determines the radius of the collider.<br/>
		/// collisionSize.Y doesn't matter!
		/// </summary>
		Circle,
		/// <summary>
		/// Used for straight/solid lasers. It is essentially a capsule collider with shrunk ends.
		/// collisionSize.X determines an <b>unscaled</b> full (not half!) length of the collider, and so it should generally match the graphic width.<br/>
		/// collisionSize.Y determines the radius/half-thickness of the collider.<br/>
		/// (Direction comes from the bullet's rotation).
		/// </summary>
		SolidLaserBody,
	}

	/// <summary>
	/// Information sent to a BlastulaCollider every frame a collision is occurring.
	/// For now it just contains the bNodeIndex.
	/// </summary>
	public struct Collision
	{
		public int bNodeIndex;
	}

	/// <summary>
	/// Low-level information that is provided and updated by a BlastulaCollider.
	/// </summary>
	public struct ObjectColliderInfo
	{
		public Transform2D transform;
		public Shape shape;
		public Vector2 size;
		/// <summary>
		/// Leads to a Blastula.LowLevel.LinkedList&lt;CollisionData&gt;.
		/// Represents the bullets that collided with this object this frame; processed appropriately.
		/// </summary>
		public IntPtr collisionListPtr;
		public long colliderID; // used to help lock the LinkedList.
	}

	/// <summary>
	/// Collider information for a BNode.
	/// </summary>
	public struct BulletColliderInfo
	{
		public Shape shape;
		public Vector2 size;
	}

	/// <summary>
	/// Determines BNode sleeping strategy and state.
	/// </summary>
	/// <remarks>
	/// Sleeping is an optional performance optimization for collision, which assumes BNodes and BlastulaColliders
	/// move slowly in general. When a BNode is far away enough from all relevant BlastulaColliders,
	/// it will fall asleep for up to six frames, not checking any collision.
	/// </remarks>
	public struct SleepStatus
	{
		public bool canSleep;
		public bool isSleeping;
	}

	/// <summary>
	/// Performs collision checks. This is a static utility class, and relies on a manager node to actually do anything.
	/// </summary>
	public unsafe static class CollisionSolver
	{
		private static bool initialized = false;
		/// <summary>
		/// This would be the list of lists in which collision relationships are defined.
		/// </summary>
		private static UnsafeArray<int>* objectsDetectedByBulletLayers;

		/// <summary>
		/// Stores ObjectColliderInfo* as IntPtr, for bullets to detect.
		/// </summary>
		private static LowLevel.LinkedList<IntPtr>* objectRegistry;

		/// <summary>
		/// Used to avoid race conditions with writing to the same LinkedList.
		/// </summary>
		private static object[] collisionListLocks = new object[32];

		public static void Initialize()
		{
			if (initialized) { return; }
			initialized = true;

			objectsDetectedByBulletLayers
				= (UnsafeArray<int>*)Marshal.AllocHGlobal(CollisionManager.bulletLayerCount * sizeof(UnsafeArray<int>));

			int a = 0;
			foreach (List<int> l in CollisionManager.objectsDetectedByBulletLayers)
			{
				objectsDetectedByBulletLayers[a] = UnsafeArrayFunctions.Create<int>(l.Count);
				int b = 0;
				foreach (int m in l)
				{
					objectsDetectedByBulletLayers[a][b] = m;
					++b;
				}
				++a;
			}

			objectRegistry = (LowLevel.LinkedList<IntPtr>*)Marshal.AllocHGlobal(
				CollisionManager.objectLayerCount * sizeof(LowLevel.LinkedList<IntPtr>)
			);

			for (int i = 0; i < CollisionManager.objectLayerCount; ++i)
			{
				objectRegistry[i] = LinkedListFunctions.Create<IntPtr>();
			}

			for (int i = 0; i < collisionListLocks.Length; ++i)
			{
				collisionListLocks[i] = new object();
			}
		}

		/// <summary>
		/// Register an object for bullets to collide with.
		/// IntPtr returned is a LinkedList&lt;IntPtr&gt;.Node for future deletion.
		/// </summary>
		public static IntPtr RegisterObject(IntPtr objectInfoPtr, int objectLayer)
		{
			if (!initialized) { return IntPtr.Zero; }
			return (IntPtr)objectRegistry[objectLayer].AddTail(objectInfoPtr);
		}

		public static void UnregisterObject(IntPtr deletionPtr, int objectLayer)
		{
			if (deletionPtr == IntPtr.Zero) { return; }
			objectRegistry[objectLayer].RemoveByNode((LowLevel.LinkedList<IntPtr>.Node*)deletionPtr);
		}

		/// <summary>
		/// Returns the minimum distance needed to reach a collision. (Used so we can be lazy.)
		/// </summary>
		private static void ExecuteCollision(int bNodeIndex)
		{
			BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
			if (bNodePtr->collisionSleepStatus.isSleeping)
			{
				if ((ulong)(bNodeIndex % 6) == FrameCounter.stageFrame % 6)
				{
					bNodePtr->collisionSleepStatus.isSleeping = false;
				}
				else { return; }
			}
			BulletColliderInfo bColInfo = default;
			if (bNodePtr->bulletRenderID >= 0)
			{
				bColInfo = BulletRenderer.colliderInfoFromRenderIDs[bNodePtr->bulletRenderID];
			}
			else if (bNodePtr->laserRenderID >= 0)
			{
				bColInfo = LaserRenderer.colliderInfoFromRenderIDs[bNodePtr->laserRenderID];
			}
			else { return; }
			int bLayer = bNodePtr->collisionLayer;
			float minSep = float.PositiveInfinity;
			Transform2D bTrs = BulletWorldTransforms.Get(bNodeIndex);
			var oLayers = objectsDetectedByBulletLayers[bLayer];
			for (int i = 0; i < oLayers.count; ++i)
			{
				var oLayer = oLayers[i];
				LowLevel.LinkedList<IntPtr>.Node* rItr = objectRegistry[oLayer].head;
				while (rItr != null)
				{
					ObjectColliderInfo oColInfo = *(ObjectColliderInfo*)rItr->data;
					float sep = float.PositiveInfinity;
					if (bColInfo.shape == Shape.Circle && oColInfo.shape == Shape.Circle)
					{
						float dist = (oColInfo.transform.Origin - bTrs.Origin).Length();
						// We can safely calculate cached position, because the Execute stage of this frame is over.
						Vector2 bScale = bTrs.Scale;
						float bulletSize = bColInfo.size.X * (0.5f * (bScale.X + bScale.Y));
						Vector2 oScale = oColInfo.transform.Scale;
						float objectSize = oColInfo.size.X * (0.5f * (oScale.X + oScale.Y));
						sep = dist - bulletSize - objectSize;
						if (sep < minSep) { minSep = sep; }
						
					}

					if (bColInfo.shape == Shape.SolidLaserBody && oColInfo.shape == Shape.Circle)
					{
						// Find the closest laser point, then perform the circle check between laser and object
						// This is effectively a capsule collider for the laser.
						Vector2 correctedScale = bTrs.Rotated(-bTrs.Rotation).Scale;
                        float unscaledHalfLength = 0.5f * bColInfo.size.X * correctedScale.X;
                        float halfWidth = bColInfo.size.Y * correctedScale.Y;
                        float insertedLength = unscaledHalfLength - halfWidth;
						if (insertedLength > 0)
						{
							Vector2 laserNorm = Vector2.Right.Rotated(bTrs.Rotation);
							Vector2 relativeObjectPosition = oColInfo.transform.Origin - bTrs.Origin;
							float projectedPosition = relativeObjectPosition.Dot(laserNorm);
							Vector2 closestPointOnLaser = bTrs.Origin + Mathf.Clamp(projectedPosition, -insertedLength, insertedLength) * laserNorm;
                            Vector2 oScale = oColInfo.transform.Scale;
                            float objectSize = oColInfo.size.X * (0.5f * (oScale.X + oScale.Y));
                            float dist = (oColInfo.transform.Origin - closestPointOnLaser).Length();
                            sep = dist - halfWidth - objectSize;
                            if (sep < minSep) { minSep = sep; }
                        }
                    }

                    if (sep < 0)
                    {
                        lock (collisionListLocks[oColInfo.colliderID % collisionListLocks.Length])
                        {
                            ((LowLevel.LinkedList<Collision>*)oColInfo.collisionListPtr)->AddTail(
                                new Collision
                                {
                                    bNodeIndex = bNodeIndex,
                                }
                            );
                        }
                    }

                    // Support more collision shapes in the future?
                    rItr = rItr->next;
				}
			}
			if (bNodePtr->collisionSleepStatus.canSleep && minSep >= Persistent.LAZY_SAFE_DISTANCE)
			{
				bNodePtr->collisionSleepStatus.isSleeping = true;
			}
		}

		/// <summary>
		/// Execute this after all behaviors so there is no inconsistency with movement.
		/// </summary>
		public static void ExecuteCollisionAll()
		{
			if (!initialized) { return; }
			int bulletSpaceCount = BNodeFunctions.MasterQueueCount();
			if (bulletSpaceCount >= BNodeFunctions.multithreadCutoff)
			{
				Parallel.For(0, bulletSpaceCount, (i) =>
				{
					int bNodeIndex = (BNodeFunctions.mqTail + i) % BNodeFunctions.mqSize;
					BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
					if (!bNodePtr->initialized) { return; }
					if (bNodePtr->bulletRenderID < 0 && bNodePtr->laserRenderID < 0) { return; }
					if (bNodePtr->collisionLayer == CollisionManager.NONE_BULLET_LAYER) { return; }
					ExecuteCollision(bNodeIndex);
				});
			}
			else
			{
				for (int i = 0; i < bulletSpaceCount; ++i)
				{
					int bNodeIndex = (BNodeFunctions.mqTail + i) % BNodeFunctions.mqSize;
					BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
					if (!bNodePtr->initialized) { continue; }
					if (bNodePtr->bulletRenderID < 0 && bNodePtr->laserRenderID < 0) { continue; }
					if (bNodePtr->collisionLayer == CollisionManager.NONE_BULLET_LAYER) { continue; }
					ExecuteCollision(bNodeIndex);
				}
			}
		}
	}
}


