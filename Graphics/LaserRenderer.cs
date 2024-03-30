using Blastula.LowLevel;
using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Blastula.BNodeFunctions;

namespace Blastula.Graphics
{
    /// <summary>
    /// Functions to track and render laser graphics. 
    /// </summary>
    public unsafe static class LaserRenderer
    {
        /// <summary>
        /// An element in a queue which links to its BNode
        /// and describes its role in the laser.
        /// </summary>
        public struct LaserDataEntry
        {
            public int bNodeIndex;
            public enum Purpose { Mid = 0, Head = 1, Tail = 2, Singleton = 3 }
            public Purpose purpose;
            public ulong lastGrazedFrame;
        }

        /// <summary>
        /// Counts the number of laser pieces (bullets which comprise them) 
        /// which are being tracked for render positioning, including those off-screen.
        /// </summary>
        public static int totalRendered = 0;

        /// <summary>
        /// Outer index: laser render ID.
        /// Stores lasers contiguously in the queue.
        /// </summary>
        public static CircularQueue<LaserDataEntry>* laserDataEntries = null;

        /// <summary>
        /// Tracks the render IDs which currently exist in at least one bullet.
        /// </summary>
        public static HashSet<int> nonzeroRenderIDs = new HashSet<int>();

        /// <summary>
        /// outer index = the laser render ID.
        /// each vertex list is one triangle strip.
        /// </summary>
        public static Vector2[][] renderedVertices = null;
        public static Vector2[][] renderedUVs = null;

        /// <summary>
        /// Stores positions of BNodes in their respective render queue, for later deletion.
        /// </summary>
        public static int* queuePositions;

        public static float* laserRenderWidthFromRenderIDs = null;
        public static float* laserRenderStretchFromRenderIDs = null;
        public static Collision.BulletColliderInfo* colliderInfoFromRenderIDs = null;
        /// <summary>
        /// Stores z-index of render IDs to make them dynamically editable in-game.
        /// </summary>
        public static int* zIndexFromRenderIDs = null;

        private static object lockSetRenderID = new object();
        private static int idCount = 0;

        private static UnsafeArray<int> GetTrail(int headBNodeIndex)
        {
            if (headBNodeIndex < 0) { return UnsafeArrayFunctions.Create<int>(0); }
            int length = 1;
            int currBNode = headBNodeIndex;
            while (masterQueue[currBNode].children.count != 0 && masterQueue[currBNode].children[0] >= 0)
            {
                ++length;
                currBNode = masterQueue[currBNode].children[0];
            }
            UnsafeArray<int> trail = UnsafeArrayFunctions.Create<int>(length);
            int currIndex = 0;
            currBNode = headBNodeIndex;
            while (currBNode >= 0)
            {
                trail[currIndex] = currBNode;
                ++currIndex;
                if (masterQueue[currBNode].children.count == 0) { break; }
                currBNode = masterQueue[currBNode].children[0];
            }
            return trail;
        }

        private static void AddLaserEntry(UnsafeArray<int> bNodeIndices, int newRenderID)
        {
            lock (lockSetRenderID)
            {
                if (bNodeIndices.count == 0 || newRenderID < 0) { return; }
                if (laserDataEntries[newRenderID].capacity == 0)
                {
                    laserDataEntries[newRenderID] = CircularQueueFunctions.Create<LaserDataEntry>(mqSize);
                    nonzeroRenderIDs.Add(newRenderID);
                }
                if (laserDataEntries[newRenderID].SpaceFree() < bNodeIndices.count + 1) { return; }
                int spacer = laserDataEntries[newRenderID].Add(default);
                int laserStart = laserDataEntries[newRenderID].head;
                // Place the laser contiguously within the queue.
                for (int j = 0; j < bNodeIndices.count; ++j)
                {
                    laserDataEntries[newRenderID].Add(new LaserDataEntry
                    {
                        bNodeIndex = bNodeIndices[j],
                        purpose = LaserDataEntry.Purpose.Mid,
                        lastGrazedFrame = 0
                    });
                }
                for (int j = 0; j < bNodeIndices.count; ++j)
                {
                    int queuePosition = (laserStart + j) % laserDataEntries[newRenderID].capacity;
                    int bNodeIndex = bNodeIndices[j];
                    if (bNodeIndex < 0)
                    {
                        laserDataEntries[newRenderID].Remove(queuePosition);
                        continue;
                    }
                    if (j == 0 || bNodeIndices[j - 1] < 0)
                    {
                        laserDataEntries[newRenderID].data[queuePosition].item.purpose |= LaserDataEntry.Purpose.Head;
                    }
                    if (j == bNodeIndices.count - 1 || bNodeIndices[j + 1] < 0)
                    {
                        laserDataEntries[newRenderID].data[queuePosition].item.purpose |= LaserDataEntry.Purpose.Tail;
                    }
                    queuePositions[bNodeIndex] = queuePosition;
                    masterQueue[bNodeIndex].laserRenderID = newRenderID;
                    totalRendered += 1;
                }
                laserDataEntries[newRenderID].Remove(spacer);
            }
        }

        private static bool BNodeToLaserEntryCheckPreamble(int bNodeIndex)
        {
            if (laserDataEntries == null) { return false; }
            if (bNodeIndex < 0) { return false; }
            int lrID = masterQueue[bNodeIndex].laserRenderID;
            if (lrID < 0) { return false; }
            if (laserDataEntries[lrID].capacity == 0) { return false; }
            return true;
        }

        public static bool IsBNodeHeadOfLaser(int bNodeIndex)
        {
            if (!BNodeToLaserEntryCheckPreamble(bNodeIndex)) { return false; }
            int lrID = masterQueue[bNodeIndex].laserRenderID;
            int pos = queuePositions[bNodeIndex];
            if (pos < 0) { return false; }
            return (laserDataEntries[lrID].data[pos].item.purpose & LaserDataEntry.Purpose.Head) != 0;
        }

        public static bool IsBNodePartOfLaser(int bNodeIndex)
        {
            if (!BNodeToLaserEntryCheckPreamble(bNodeIndex)) { return false; }
            int lrID = masterQueue[bNodeIndex].laserRenderID;
            int pos = queuePositions[bNodeIndex];
            if (pos < 0) { return false; }
            return true;
        }

        public static bool IsBNodeTailOfLaser(int bNodeIndex)
        {
            if (!BNodeToLaserEntryCheckPreamble(bNodeIndex)) { return false; }
            int lrID = masterQueue[bNodeIndex].laserRenderID;
            int pos = queuePositions[bNodeIndex];
            if (pos < 0) { return false; }
            return (laserDataEntries[lrID].data[pos].item.purpose & LaserDataEntry.Purpose.Tail) != 0;
        }

        public static int GetHeadBNodeOfLaser(int bNodeIndex)
        {
            if (!BNodeToLaserEntryCheckPreamble(bNodeIndex)) { return -1; }
            int lrID = masterQueue[bNodeIndex].laserRenderID;
            int pos = queuePositions[bNodeIndex];
            if (pos < 0) { return -1; }
            // We now search the queue for the head, taking advantage of contiguous storage.
            // This will still work even when we add lasers that are not a parent-child chain.
            int currQueuePos = pos;
            CircularQueue<LaserDataEntry>.TInit currData = laserDataEntries[lrID].data[currQueuePos];
            while (currData.initialized && (currData.item.purpose & LaserDataEntry.Purpose.Head) == 0)
            {
                currQueuePos = (currQueuePos == 0) ? (mqSize - 1) : (currQueuePos - 1);
                currData = laserDataEntries[lrID].data[currQueuePos];
            }
            if (currData.initialized && (currData.item.purpose & LaserDataEntry.Purpose.Head) != 0)
            {
                return currData.item.bNodeIndex;
            }
            return -1;
        }

        /// <summary>
        /// Determines if a new graze is possible for a laser in this frame.
        /// If so, return true, and no more grazes will be possible this frame.
        /// Otherwise return false.
        /// We need this because multiple bullets in a laser can be grazed in the same frame.
        /// </summary>
        public static bool NewGrazeThisFrame(int bNodeIndex, out int headBNodeIndex)
        {
            headBNodeIndex = GetHeadBNodeOfLaser(bNodeIndex);
            if (headBNodeIndex < 0) { return false; }
            int lrID = masterQueue[headBNodeIndex].laserRenderID;
            int pos = queuePositions[headBNodeIndex];
            var purpose = laserDataEntries[lrID].data[pos].item.purpose;
            if ((purpose & LaserDataEntry.Purpose.Singleton) == LaserDataEntry.Purpose.Singleton)
            {
                // A singleton is invisible and makes no sense to graze.
                return false;
            }
            ulong lastGrazedFrame = laserDataEntries[lrID].data[pos].item.lastGrazedFrame;
            if (lastGrazedFrame != FrameCounter.stageFrame)
            {
                laserDataEntries[lrID].data[pos].item.lastGrazedFrame = FrameCounter.stageFrame;
                return true;
            }
            return false;
        }

        public static void RemoveLaserEntry(int bNodeIndex)
        {
            lock (lockSetRenderID)
            {
                if (laserDataEntries == null) { return; }
                if (bNodeIndex < 0) { return; }
                int renderID = masterQueue[bNodeIndex].laserRenderID;
                if (renderID < 0) { return; }
                if (laserDataEntries[renderID].capacity == 0) { masterQueue[bNodeIndex].laserRenderID = -1; return; }
                int queuePosition = queuePositions[bNodeIndex];
                queuePositions[bNodeIndex] = -1;
                int prev = (queuePosition == 0) ? (mqSize - 1) : (queuePosition - 1);
                int next = (queuePosition + 1) % mqSize;
                var purpose = laserDataEntries[renderID].data[queuePosition].item.purpose;
                if ((purpose & LaserDataEntry.Purpose.Head) != 0 && (purpose & LaserDataEntry.Purpose.Tail) == 0)
                {
                    // If this is a head which isn't a tail, the new head inherits the last grazed frame.
                    // Otherwise we could cause havoc with grazing and deleting.
                    laserDataEntries[renderID].data[prev].item.lastGrazedFrame = laserDataEntries[renderID].data[queuePosition].item.lastGrazedFrame;
                }
                if ((purpose & LaserDataEntry.Purpose.Head) == 0)
                {
                    // not the head of the laser; prev becomes a tail
                    laserDataEntries[renderID].data[prev].item.purpose |= LaserDataEntry.Purpose.Tail;
                }
                if ((purpose & LaserDataEntry.Purpose.Tail) == 0)
                {
                    // not the tail of the laser; next becomes a head
                    laserDataEntries[renderID].data[next].item.purpose |= LaserDataEntry.Purpose.Head;
                }
                laserDataEntries[renderID].data[queuePosition].item.bNodeIndex = -1;
                laserDataEntries[renderID].Remove(queuePosition);
                masterQueue[bNodeIndex].laserRenderID = -1;
                totalRendered -= 1;
            }
        }

        private static void SetLaser(UnsafeArray<int> bNodeIndices, int newRenderID)
        {
            if (bNodeIndices.count == 0) { return; }
            lock (lockSetRenderID)
            {
                for (int j = 0; j < bNodeIndices.count; ++j) { RemoveLaserEntry(bNodeIndices[j]); }
                AddLaserEntry(bNodeIndices, newRenderID);
            }
        }

        public static void SetLaserFromHead(int headBNodeIndex, int newRenderID)
        {
            UnsafeArray<int> trail = GetTrail(headBNodeIndex);
            SetLaser(trail, newRenderID);
            trail.Dispose();
        }

        public static void Initialize(int idCount)
        {
            if (laserDataEntries == null)
            {
                LaserRenderer.idCount = idCount;
                laserDataEntries = (CircularQueue<LaserDataEntry>*)Marshal.AllocHGlobal(sizeof(CircularQueue<LaserDataEntry>) * idCount);
                renderedVertices = new Vector2[idCount][];
                renderedUVs = new Vector2[idCount][];
                laserRenderWidthFromRenderIDs = (float*)Marshal.AllocHGlobal(sizeof(float) * idCount);
                laserRenderStretchFromRenderIDs = (float*)Marshal.AllocHGlobal(sizeof(float) * idCount);
                colliderInfoFromRenderIDs = (Collision.BulletColliderInfo*)Marshal.AllocHGlobal(sizeof(Collision.BulletColliderInfo) * idCount);
                zIndexFromRenderIDs = (int*)Marshal.AllocHGlobal(sizeof(int) * idCount);
                queuePositions = (int*)Marshal.AllocHGlobal(sizeof(int) * mqSize);
                for (int i = 0; i < idCount; ++i)
                {
                    laserDataEntries[i].head = laserDataEntries[i].tail = 0;
                    laserDataEntries[i].capacity = 0;
                    renderedVertices[i] = new Vector2[0];
                    renderedUVs[i] = new Vector2[0];
                    GraphicInfo gi = LaserRendererManager.GetGraphicInfoFromID(i);
                    laserRenderWidthFromRenderIDs[i] = gi.size.X;
                    laserRenderStretchFromRenderIDs[i] = Mathf.Clamp(gi.size.Y, 0, 1);
                    colliderInfoFromRenderIDs[i] = new Collision.BulletColliderInfo
                    {
                        shape = gi.collisionShape,
                        size = gi.collisionSize
                    };
                    zIndexFromRenderIDs[i] = gi.zIndex;
                }
            }
        }

        public static void ChangeZIndex(int renderID, int newZIndex)
        {
            if (renderID < 0 || renderID >= idCount) { return; }
            zIndexFromRenderIDs[renderID] = newZIndex;
            MeshInstance2D mesh = LaserRendererManager.GetMeshInstanceFromID(renderID);
            if (mesh != null)
            {
                mesh.ZIndex = (int)Math.Clamp(newZIndex, RenderingServer.CanvasItemZMin, RenderingServer.CanvasItemZMax);
            }
        }

        public static void ChangeZIndex(string renderName, int newZIndex)
        {
            ChangeZIndex(LaserRendererManager.GetIDFromName(renderName), newZIndex);
        }

        /// <summary>
        /// Populates a laser into the list for rendering. Returns the length of that laser.
        /// </summary>
        private static int RenderSingleLaser(int renderID, int startListIndex)
        {
            float thickness = laserRenderWidthFromRenderIDs[renderID] * 0.5f;
            int currListIndex = startListIndex;
            while (true)
            {
                LaserDataEntry lde = laserDataEntries[renderID].GetList(currListIndex).item;
                ++currListIndex;
                if ((lde.purpose & LaserDataEntry.Purpose.Tail) != 0) { break; }
            }
            int laserLength = currListIndex - startListIndex;
            currListIndex = startListIndex;
            while (true)
            {
                LaserDataEntry lde = laserDataEntries[renderID].GetList(currListIndex).item;
                Transform2D worldTransform = BulletWorldTransforms.Get(lde.bNodeIndex);
                Vector2 currPos = worldTransform.Origin;
                float thicknessMultiplier = masterQueue[lde.bNodeIndex].transform.Scale.X;
                float desiredAngle = 0f;
                float stretchedUV = 0f;
                switch (lde.purpose)
                {
                    case LaserDataEntry.Purpose.Head: stretchedUV = 0; break;
                    case LaserDataEntry.Purpose.Tail: stretchedUV = 1; break;
                    case LaserDataEntry.Purpose.Mid:
                    case LaserDataEntry.Purpose.Singleton:
                    default: stretchedUV = 0.5f; break;
                }
                desiredAngle = worldTransform.Rotation;
                Vector2 normal = Vector2.FromAngle(desiredAngle + 0.5f * Mathf.Pi);
                float horizontalProgress = (currListIndex - startListIndex) / (float)(laserLength - 1);
                float uvX = Mathf.Lerp(horizontalProgress, stretchedUV, laserRenderStretchFromRenderIDs[renderID]);
                renderedVertices[renderID][2 * currListIndex] = currPos - thickness * thicknessMultiplier * normal;
                renderedVertices[renderID][2 * currListIndex + 1] = currPos + thickness * thicknessMultiplier * normal;
                renderedUVs[renderID][2 * currListIndex] = new Vector2(uvX, 0);
                renderedUVs[renderID][2 * currListIndex + 1] = new Vector2(uvX, 1);

                // Ensures the spacing is degenerate vertex pairs, to separate a laser correctly.
                if (2 * currListIndex - 1 >= 0
                && (lde.purpose & LaserDataEntry.Purpose.Head) != 0)
                {
                    renderedVertices[renderID][2 * currListIndex - 1] = renderedVertices[renderID][2 * currListIndex];
                    if (2 * currListIndex - 4 >= 0 && !laserDataEntries[renderID].GetList(currListIndex - 2).initialized)
                    {
                        renderedVertices[renderID][2 * currListIndex - 2] = renderedVertices[renderID][2 * currListIndex];
                    }
                }
                if (2 * currListIndex + 2 < renderedVertices[renderID].Length
                && (lde.purpose & LaserDataEntry.Purpose.Tail) != 0)
                {
                    renderedVertices[renderID][2 * currListIndex + 2] = renderedVertices[renderID][2 * currListIndex + 1];
                    if (2 * currListIndex + 4 < renderedVertices[renderID].Length && !laserDataEntries[renderID].GetList(currListIndex + 2).initialized)
                    {
                        renderedVertices[renderID][2 * currListIndex + 3] = renderedVertices[renderID][2 * currListIndex + 1];
                    }
                }

                ++currListIndex;
                if ((lde.purpose & LaserDataEntry.Purpose.Tail) != 0) { break; }
            }
            return laserLength;
        }

        private static void RenderSpacing(int renderID, int listIndex)
        {
            // Ensures the spacing is degenerate vertex pairs, to separate a laser correctly.
            if (2 * listIndex - 2 >= 0 && !laserDataEntries[renderID].GetList(listIndex - 1).initialized
             && 2 * listIndex + 2 < renderedVertices[renderID].Length && !laserDataEntries[renderID].GetList(listIndex + 1).initialized)
            {
                renderedVertices[renderID][2 * listIndex + 1] = renderedVertices[renderID][2 * listIndex];
            }
        }

        private static void RenderRegion(int renderID, int startListIndex, int length)
        {
            length = Mathf.Min(length, laserDataEntries[renderID].Count() - startListIndex);
            if (length <= 0) { return; }
            int currListIndex = startListIndex;
            while (currListIndex < startListIndex + length)
            {
                var ldei = laserDataEntries[renderID].GetList(currListIndex);
                if (!ldei.initialized)
                {
                    RenderSpacing(renderID, currListIndex);
                    ++currListIndex;
                }
                else if ((ldei.item.purpose & LaserDataEntry.Purpose.Head) != 0)
                {
                    int laserLength = RenderSingleLaser(renderID, currListIndex);
                    currListIndex += laserLength;
                }
                else
                {
                    ++currListIndex;
                }
            }
        }

        /// <summary>
        /// Populates the series of arrays which will be used for ArrayMesh data in this frame's rendering.
        /// </summary>
        /// <returns>The list of structures that have been removed due to being empty.</returns>
        public static List<int> RenderAll()
        {
            List<int> toRemove = new List<int>();
            foreach (int nonzeroRenderID in nonzeroRenderIDs)
            {
                int oldCount = renderedVertices[nonzeroRenderID].Length / 2;
                int newCount = laserDataEntries[nonzeroRenderID].Count();
                if (newCount == 0)
                {
                    toRemove.Add(nonzeroRenderID); continue;
                }
                if (newCount != oldCount)
                {
                    renderedVertices[nonzeroRenderID] = new Vector2[2 * newCount];
                    renderedUVs[nonzeroRenderID] = new Vector2[2 * newCount];
                }
            }
            foreach (int removeIndex in toRemove)
            {
                nonzeroRenderIDs.Remove(removeIndex);
                laserDataEntries[removeIndex].Dispose();
            }

            foreach (int nonzeroRenderID in nonzeroRenderIDs)
            {
                int listCount = laserDataEntries[nonzeroRenderID].Count();

                if (listCount >= multithreadCutoff * 4)
                {
                    int regionSize = multithreadCutoff / 2;
                    int nzr = nonzeroRenderID;
                    Parallel.For(0, listCount / regionSize + 1, (i) => { RenderRegion(nzr, i * regionSize, regionSize); });
                }
                else
                {
                    RenderRegion(nonzeroRenderID, 0, listCount);
                }
            }

            return toRemove;
        }
    }
}
