using Godot;
using System.Runtime.InteropServices;
using static Blastula.BNodeFunctions;

namespace Blastula.LowLevel
{
    /// <summary>
    /// This class updates the world position + rotation + scale of all BNodes as they appear in the masterQueue.
    /// </summary>
    /// <remarks>
    /// Very important utility class. We require world transforms for rendering and collision checking.
    /// </remarks>
    public unsafe static class BulletWorldTransforms
    {
        private static bool initialized = false;
        private struct TransformScratchSpace
        {
            public Transform2D transform;
            public ulong versionNumber;
        }

        private static TransformScratchSpace* transformScratchSpace = null;

        public static void Initialize()
        {
            if (!initialized)
            {
                transformScratchSpace = (TransformScratchSpace*)Marshal.AllocHGlobal(sizeof(TransformScratchSpace) * mqSize);
                initialized = true;
            }
        }

        /// <summary>
        /// Get the world transform of a bullet.
        /// </summary>
        /// <remarks>
        /// Referring to the function's implementation: it's important that version number is set in the current order. 
        /// Moving it around can upset multithreading.
        /// </remarks>
        public static Transform2D Get(int bNodeIndex)
        {
            if (!initialized) { Initialize(); }
            if (bNodeIndex < 0 || bNodeIndex >= mqSize) { return Transform2D.Identity; }
            if (transformScratchSpace[bNodeIndex].versionNumber == FrameCounter.stageFrame)
            {
                return transformScratchSpace[bNodeIndex].transform;
            }
            if (masterQueue[bNodeIndex].worldTransformMode)
            {
                return transformScratchSpace[bNodeIndex].transform = masterQueue[bNodeIndex].transform;
            }
            Transform2D ret = Get(masterQueue[bNodeIndex].parentIndex) * masterQueue[bNodeIndex].transform;
            transformScratchSpace[bNodeIndex].versionNumber = FrameCounter.stageFrame;
            return transformScratchSpace[bNodeIndex].transform = ret;
        }

        /// <summary>
        /// Helper to set the local transform that results in the desired world transform.
        /// </summary>
        public static void Set(int bNodeIndex, Transform2D newWorldTransform)
        {
            if (bNodeIndex < 0) { return; }
            if (masterQueue[bNodeIndex].worldTransformMode)
            {
                masterQueue[bNodeIndex].transform = newWorldTransform;
                return;
            }
            Transform2D currentLocalTransform = masterQueue[bNodeIndex].transform;
            Transform2D currentWorldTransform = Get(bNodeIndex);
            masterQueue[bNodeIndex].transform = currentLocalTransform * currentWorldTransform.AffineInverse() * newWorldTransform;
            Invalidate(bNodeIndex);
        }

        /// <summary>
        /// This forces us to recalculate the world transform.
        /// </summary>
        /// <remarks>
        /// Important for lasers to not look extremely stupid.
        /// </remarks>
        public static void Invalidate(int bNodeIndex)
        {
            if (!initialized) { Initialize(); }
            if (bNodeIndex < 0 || bNodeIndex >= mqSize) { return; }
            transformScratchSpace[bNodeIndex].versionNumber = 0;
        }
    }
}

