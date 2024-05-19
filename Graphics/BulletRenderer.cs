using Blastula.LowLevel;
using Blastula.Operations;
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Blastula.BNodeFunctions;

namespace Blastula.Graphics
{
    /// <summary>
    /// Functions to track and render bullet graphics. 
    /// </summary>
    public unsafe static class BulletRenderer
    {
        /// <summary>
        /// Counts the number of bullets which are being tracked for render positioning, including those off-screen.
        /// </summary>
        public static int totalRendered = 0;
        public static CircularQueue<int>* bNodesFromRenderIDs = null;
        /// <summary>
        /// Tracks the render IDs which currently exist in at least one bullet.
        /// </summary>
        public static HashSet<int> nonzeroRenderIDs = new HashSet<int>();
        /// <summary>
        /// Outer index: render ID. <br />
        /// Each inner array should resize to avoid the Multimesh bottleneck.
        /// </summary>
        public static float[][] renderedTransformArrays = null;
        /// <summary>
        /// Stores positions of BNodes in their respective render queue, for later deletion.
        /// </summary>
        public static int* queuePositions;
        /// <summary>
        /// Shader parameter used to infer the color of the bullet for the deletion effect.
        /// </summary>
        public static readonly string DELETION_COLOR_SHADER_PARAM = "tint";
        /// <summary>
        /// Maps internal render ID to the color of the deletion effect.
        /// </summary>
        public static Color* deletionColorFromRenderIDs = null;
        /// <summary>
        /// Maps internal render ID to mesh size (for the purpose of determining deletion effect size).
        /// </summary>
        public static Vector2* meshSizeFromRenderIDs = null;
        /// <summary>
        /// Maps internal render ID to collider info.
        /// </summary>
        public static Collision.BulletColliderInfo* colliderInfoFromRenderIDs = null;
        /// <summary>
        /// Maps internal render ID to whether the graphic should be rotated.
        /// </summary>
        public static bool* unrotatedGraphicFromRenderIDs = null;
        /// <summary>
        /// Maps internal render ID to information determining which multimesh fields (color / custom data) exist.
        /// </summary>
        public static GraphicInfo.ExtraMultimeshFields* extraMultimeshFieldsFromRenderIDs = null;
        /// <summary>
        /// Used to help populate the MultiMesh. 
        /// "Stride" is the number of floats per mesh instance in the buffer.
        /// It can be 8, 12, or 16.
        /// </summary>
        public static int* strideFromRenderIDs = null;
        /// <summary>
        /// Stores z-index of render IDs to make them dynamically editable in-game.
        /// </summary>
        public static int* zIndexFromRenderIDs = null;

        private static int idCount = 0;
        private static object lockSetRenderID = new object();

        public static void Initialize(int idCount)
        {
            if (bNodesFromRenderIDs == null)
            {
                BulletRenderer.idCount = idCount;
                bNodesFromRenderIDs = (CircularQueue<int>*)Marshal.AllocHGlobal(sizeof(CircularQueue<int>) * idCount);
                renderedTransformArrays = new float[idCount][];
                deletionColorFromRenderIDs = (Color*)Marshal.AllocHGlobal(sizeof(Color) * idCount);
                meshSizeFromRenderIDs = (Vector2*)Marshal.AllocHGlobal(sizeof(Vector2) * idCount);
                colliderInfoFromRenderIDs = (Collision.BulletColliderInfo*)Marshal.AllocHGlobal(sizeof(Collision.BulletColliderInfo) * idCount);
                unrotatedGraphicFromRenderIDs = (bool*)Marshal.AllocHGlobal(sizeof(bool) * idCount);
                extraMultimeshFieldsFromRenderIDs = (GraphicInfo.ExtraMultimeshFields*)Marshal.AllocHGlobal(sizeof(GraphicInfo.ExtraMultimeshFields) * idCount);
                strideFromRenderIDs = (int*)Marshal.AllocHGlobal(sizeof(int) * idCount);
                zIndexFromRenderIDs = (int*)Marshal.AllocHGlobal(sizeof(int) * idCount);
                queuePositions = (int*)Marshal.AllocHGlobal(sizeof(int) * mqSize);
                GraphicInfo.ExtraMultimeshFields bothMMFields = GraphicInfo.ExtraMultimeshFields.CustomData | GraphicInfo.ExtraMultimeshFields.Color;
                for (int i = 0; i < idCount; ++i)
                {
                    bNodesFromRenderIDs[i].head = bNodesFromRenderIDs[i].tail = 0;
                    bNodesFromRenderIDs[i].capacity = 0;
                    renderedTransformArrays[i] = new float[0];
                    GraphicInfo gi = BulletRendererManager.GetGraphicInfoFromID(i);
                    Variant delCol = gi.material.GetShaderParameter(DELETION_COLOR_SHADER_PARAM);
                    deletionColorFromRenderIDs[i] 
                        = (delCol.VariantType != Variant.Type.Nil) ? delCol.AsColor().Lightened(0.5f) : Colors.White;
                    meshSizeFromRenderIDs[i] = gi.size;
                    colliderInfoFromRenderIDs[i] = new Collision.BulletColliderInfo { shape = gi.collisionShape, size = gi.collisionSize };
                    unrotatedGraphicFromRenderIDs[i] = gi.unrotatedGraphic;

                    extraMultimeshFieldsFromRenderIDs[i] = gi.extraMultimeshFields;
                    if (gi.extraMultimeshFields == 0) { strideFromRenderIDs[i] = 8; }
                    else if (gi.extraMultimeshFields == bothMMFields) { strideFromRenderIDs[i] = 16; }
                    else { strideFromRenderIDs[i] = 12; }
                    zIndexFromRenderIDs[i] = gi.zIndex;
                }
            }
        }

        public static void ChangeZIndex(int renderID, int newZIndex)
        {
            if (renderID < 0 || renderID >= idCount) { return; }
            zIndexFromRenderIDs[renderID] = newZIndex;
            MultimeshBullet multimesh = BulletRendererManager.GetMultiMeshInstanceFromID(renderID);
            if (multimesh != null)
            {
                multimesh.ZIndex = (int)Math.Clamp(newZIndex, RenderingServer.CanvasItemZMin, RenderingServer.CanvasItemZMax);
            }
        }

        public static void ChangeZIndex(string renderName, int newZIndex)
        {
            ChangeZIndex(BulletRendererManager.GetIDFromName(renderName), newZIndex);
        }

        public static void SetRenderID(int bNodeIndex, int newRenderID)
        {
            if (bNodeIndex < 0 || bNodeIndex >= mqSize) { return; }
            if (!masterQueue[bNodeIndex].initialized) { return; }
            if (masterQueue[bNodeIndex].bulletRenderID == newRenderID) { return; }
            lock (lockSetRenderID)
            {
                int oldRenderID = masterQueue[bNodeIndex].bulletRenderID;
                if (oldRenderID >= 0)
                {
                    int oldRenderPosition = queuePositions[bNodeIndex];
                    if (oldRenderPosition >= 0)
                    {
                        bNodesFromRenderIDs[oldRenderID].Remove(oldRenderPosition);
                    }
                    if (bNodesFromRenderIDs[oldRenderID].Count() == 0)
                    {
                        bNodesFromRenderIDs[oldRenderID].Dispose();
                    }
                    totalRendered -= 1;
                }
                masterQueue[bNodeIndex].bulletRenderID = newRenderID;
                if (newRenderID >= 0)
                {
                    if (bNodesFromRenderIDs[newRenderID].capacity == 0)
                    {
                        bNodesFromRenderIDs[newRenderID] = CircularQueueFunctions.Create<int>(mqSize);
                        nonzeroRenderIDs.Add(newRenderID);
                    }
                    queuePositions[bNodeIndex] = bNodesFromRenderIDs[newRenderID].Add(bNodeIndex);
                    totalRendered += 1;
                }
            }
        }

        private static bool IsBulletOnScreen(int bNodeIndex, BNode* bNodePtr)
        {
            if (bNodePtr == null) { return false; }
            float extent = 0;
            int bulletRenderID = bNodePtr->bulletRenderID;
            if (bulletRenderID >= 0)
            {
                extent = 0.5f * Mathf.Max(meshSizeFromRenderIDs[bulletRenderID].X, meshSizeFromRenderIDs[bulletRenderID].Y);
            }
            else if (bNodePtr->laserRenderID >= 0)
            {
                extent = LaserRenderer.laserRenderWidthFromRenderIDs[bNodePtr->laserRenderID];
            }
            else { return false; }
            extent *= 1.42f; // Diagonal correction
            Transform2D worldTransform = BulletWorldTransforms.Get(bNodeIndex);
            float scale = Mathf.Max(Mathf.Abs(worldTransform.Scale.X), Mathf.Abs(worldTransform.Scale.Y));
            return MainBoundary.IsOnScreen(worldTransform.Origin, -extent * scale);
        }

        /// <summary>
        /// May give false positives due to padding around bullets. Used to ensure when a bullet is NOT on the screen.
        /// </summary>
        public static bool IsBulletOnScreen(int bNodeIndex)
        {
            if (bNodeIndex < 0) { return false; }
            return IsBulletOnScreen(bNodeIndex, masterQueue + bNodeIndex);
        }

        public const string DELETION_EFFECT_NAME = "Deletion";
        public const float DELETION_EFFECT_FRAMES = 40;
        public const float DELETION_EFFECT_RECT_SIZE = 40;
        private static int cachedDeletionRenderID = -1;
        private static float currentStageTime;
        //private static Stopwatch st = null;
        private static void ConvertToDeletionEffects(int bNodeIndex, int depth)
        {
            if (bNodeIndex < 0) { return; }

            //if (depth == 0) { st = Stopwatch.StartNew(); }

            BNode* bNodePtr = masterQueue + bNodeIndex;

            bNodePtr->behaviors.DisposeBehaviorOrder();
            if (depth == 0)
            {
                Lifespan.Add(bNodeIndex, DELETION_EFFECT_FRAMES, Schedules.Wait.TimeUnits.Frames);
                if (cachedDeletionRenderID < 0)
                {
                    cachedDeletionRenderID = BulletRendererManager.GetIDFromName(DELETION_EFFECT_NAME);
                }
                currentStageTime = (float)(FrameCounter.GetStageTime() % BulletRendererManager.STAGE_TIME_ROLLOVER);
            }
            
            int bulletRenderID = bNodePtr->bulletRenderID;
            if (bulletRenderID != cachedDeletionRenderID)
            {
                if (IsBulletOnScreen(bNodeIndex))
                {
                    float averageRectSize = 0f;
                    if (bulletRenderID >= 0)
                    {
                        averageRectSize = meshSizeFromRenderIDs[bulletRenderID].X + meshSizeFromRenderIDs[bulletRenderID].Y;
                        averageRectSize = Mathf.Lerp(averageRectSize, DELETION_EFFECT_RECT_SIZE, 0.8f);
                    }
                    else if (bNodePtr->laserRenderID >= 0)
                    {
                        averageRectSize = 2f * LaserRenderer.laserRenderWidthFromRenderIDs[bNodePtr->laserRenderID];
                    }
                    float newScale = averageRectSize / DELETION_EFFECT_RECT_SIZE;
                    BulletRenderer.SetRenderID(bNodeIndex, cachedDeletionRenderID);
                    if (bNodePtr->multimeshExtras == null) { bNodePtr->multimeshExtras = SetMultimeshExtraData.NewPointer(); }
                    bNodePtr->multimeshExtras->color = deletionColorFromRenderIDs[bulletRenderID];
                    bNodePtr->multimeshExtras->custom = new Vector4(newScale, currentStageTime, 0, 0);
                }
                else
                {
                    // No chance to re-appear on screen later!
                    BulletRenderer.SetRenderID(bNodeIndex, -1);
                }
            }
            LaserRenderer.RemoveLaserEntry(bNodeIndex);

            for (int i = 0; i < bNodePtr->children.count; ++i)
            {
                ConvertToDeletionEffects(bNodePtr->children[i], depth + 1);
            }

            //if (depth == 0) { st.Stop(); GD.Print("apply deletion effect took ", st.Elapsed.TotalMilliseconds, " ms"); }
        }

        /// <summary>
        /// Turns all visible BNodes rooted at this index into deletion effects.
        /// </summary>
        /// <remarks>
        /// The deletion effect is made of the same BNodes.
        /// They just now have an altered appearance and behavior.
        /// </remarks>
        public static void ConvertToDeletionEffects(int bNodeIndex)
        {
            ConvertToDeletionEffects(bNodeIndex, 0);
        }

        /// <summary>
        /// Turns all bullets controlled by the Blastodisc into deletion effects.
        /// </summary>
        public static void ConvertToDeletionEffects(Blastodisc blastodisc)
        {
            ConvertToDeletionEffects(blastodisc.masterStructure);
        }

        // Render one bullet.
        private static void RenderOne(int nonzeroRenderID, int i)
        {
            int stride = strideFromRenderIDs[nonzeroRenderID];
            var bNodeIndexMaybe = bNodesFromRenderIDs[nonzeroRenderID].GetList(i);
            int bNodeIndex = bNodeIndexMaybe.initialized ? bNodeIndexMaybe.item : -1;
            // This is the array in which we need to enter multimesh data.
            float[] renderedTransformArray = renderedTransformArrays[nonzeroRenderID];
            int offset = i * stride;
            if (bNodeIndex < 0)
            {
                // Render nothing; there is no bullet here.
                renderedTransformArray[offset + 0] = 0;
                renderedTransformArray[offset + 1] = 0;
                renderedTransformArray[offset + 2] = 0;
                renderedTransformArray[offset + 3] = 0;
                renderedTransformArray[offset + 4] = 0;
                renderedTransformArray[offset + 5] = 0;
                renderedTransformArray[offset + 6] = 0;
                renderedTransformArray[offset + 7] = 0;
            }
            else
            {
                // Render this bullet at this position.
                Transform2D t = BulletWorldTransforms.Get(bNodeIndex);
                if (unrotatedGraphicFromRenderIDs[nonzeroRenderID])
                {
                    t = new Transform2D(0, t.Scale, 0, t.Origin);
                }
                renderedTransformArray[offset + 0] = t[0, 0];
                renderedTransformArray[offset + 1] = t[1, 0];
                renderedTransformArray[offset + 2] = 0;
                renderedTransformArray[offset + 3] = t[2, 0];
                renderedTransformArray[offset + 4] = t[0, 1];
                renderedTransformArray[offset + 5] = t[1, 1];
                renderedTransformArray[offset + 6] = 0;
                renderedTransformArray[offset + 7] = t[2, 1];
                // Add color or custom data if it exists.
                BNodeMultimeshExtras* extrasPtr = masterQueue[bNodeIndex].multimeshExtras;
                GraphicInfo.ExtraMultimeshFields extraMultimeshFields = extraMultimeshFieldsFromRenderIDs[nonzeroRenderID];
                offset = i * stride + 8;
                if (extrasPtr != null)
                {
                    if ((extraMultimeshFields & GraphicInfo.ExtraMultimeshFields.Color) != 0)
                    {
                        renderedTransformArray[offset + 0] = extrasPtr->color.R;
                        renderedTransformArray[offset + 1] = extrasPtr->color.G;
                        renderedTransformArray[offset + 2] = extrasPtr->color.B;
                        renderedTransformArray[offset + 3] = extrasPtr->color.A;
                        offset += 4;
                    }
                    if ((extraMultimeshFields & GraphicInfo.ExtraMultimeshFields.CustomData) != 0)
                    {
                        renderedTransformArray[offset + 0] = extrasPtr->custom[0];
                        renderedTransformArray[offset + 1] = extrasPtr->custom[1];
                        renderedTransformArray[offset + 2] = extrasPtr->custom[2];
                        renderedTransformArray[offset + 3] = extrasPtr->custom[3];
                    }
                }
                else 
                {
                    if ((extraMultimeshFields & GraphicInfo.ExtraMultimeshFields.Color) != 0)
                    {
                        // The bullet contains no color, even though we expect a color. Set it to white by default.
                        // Otherwise it would be clear/invisible, which sucks.
                        renderedTransformArray[offset + 0] = 1f;
                        renderedTransformArray[offset + 1] = 1f;
                        renderedTransformArray[offset + 2] = 1f;
                        renderedTransformArray[offset + 3] = 1f;
                        offset += 4;
                    }
                    if ((extraMultimeshFields & GraphicInfo.ExtraMultimeshFields.CustomData) != 0)
                    {
                        // The bullet contains no custom data. If we don't zero it out, we may use
                        // uninitialized data, which also sucks.
                        renderedTransformArray[offset + 0] = 0f;
                        renderedTransformArray[offset + 1] = 0f;
                        renderedTransformArray[offset + 2] = 0f;
                        renderedTransformArray[offset + 3] = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Populates the series of arrays which will be used for MultiMesh data in this frame's rendering.
        /// </summary>
        /// <returns>The list of structures that have been removed due to being empty.</returns>
        public static List<int> RenderAll()
        {
            // Resize render buffers to be large enough to hold all items,
            // But not too much larger. Also we don't want to resize too often.
            // So we use the powers of two...
            List<int> toRemove = new List<int>();
            foreach (int nonzeroRenderID in nonzeroRenderIDs)
            {
                int stride = strideFromRenderIDs[nonzeroRenderID];
                // Why can this be negative, why is it subtracting 1?
                // Because of a hack around a Multimesh AABB problem, in which we render a tiny secret bullet.
                int oldCount = (renderedTransformArrays[nonzeroRenderID].Length / stride) - 1;
                if (oldCount < 0) { oldCount = 0; }
                int newCount = bNodesFromRenderIDs[nonzeroRenderID].Count();
                if (newCount == 0) { toRemove.Add(nonzeroRenderID); continue; }
                // Perform a funny series of operations for the next higher power of two
                newCount--;
                newCount |= newCount >> 1;
                newCount |= newCount >> 2;
                newCount |= newCount >> 4;
                newCount |= newCount >> 8;
                newCount |= newCount >> 16;
                newCount++;
                if (newCount != oldCount)
                {
                    // Why is this newCount + 1?
                    // Because of a hack around a Multimesh AABB problem, in which we render a tiny secret bullet.
                    renderedTransformArrays[nonzeroRenderID] = new float[stride * (newCount + 1)];
                }
            }
            foreach (int removeIndex in toRemove)
            {
                nonzeroRenderIDs.Remove(removeIndex);
            }

            // Now we actually render each type of bullet
            foreach (int nonzeroRenderID in nonzeroRenderIDs)
            {
                int listCount = bNodesFromRenderIDs[nonzeroRenderID].Count();

                if (listCount >= multithreadCutoff)
                {
                    int nzr = nonzeroRenderID;
                    Parallel.For(0, listCount, (i) => { RenderOne(nzr, i); });
                }
                else
                {
                    for (int i = 0; i < listCount; ++i) { RenderOne(nonzeroRenderID, i); }
                }

                // A hack around a Multimesh AABB problem, in which we render a tiny secret bullet.
                int stride = strideFromRenderIDs[nonzeroRenderID];
                int final = listCount * stride;
                for (int j = 0; j < stride; ++j)
                {
                    renderedTransformArrays[nonzeroRenderID][final + j] = 0;
                }
            }

            return toRemove;
        }
    }
}

