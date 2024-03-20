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
    public enum Shape
    {
        None,
        /// <summary>
        /// collisionSize.X determines the radius of the collider. collisionSize.Y doesn't matter!
        /// </summary>
        Circle
    }

    public struct Collision
    {
        public int bNodeIndex;
    }

    public struct ObjectColliderInfo
    {
        public Transform2D transform;
        public Shape shape;
        public Vector2 size;
        /// <summary>
        /// Leads to a Blastula.LowLevel.LinkedList<CollisionData>.
        /// Represents the bullets that collided with this object this frame; processed appropriately.
        /// </summary>
        public IntPtr collisionListPtr;
        public long colliderID; // used to help lock the LinkedList.
    }

    public struct BulletColliderInfo
    {
        public Shape shape;
        public Vector2 size;
    }

    public struct SleepStatus
    {
        public bool canSleep;
        public bool isSleeping;
    }

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
        /// IntPtr returned is a LinkedList<IntPtr>.Node* for future deletion.
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
                        sep = dist - bColInfo.size.X - oColInfo.size.X;
                        if (sep < minSep) { minSep = sep; }
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
                    }
                    // TODO: support more collision shapes.
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
                    if (bNodePtr->collisionLayer == 0) { return; }
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
                    if (bNodePtr->collisionLayer == 0) { continue; }
                    ExecuteCollision(bNodeIndex);
                }
            }
        }
    }
}


