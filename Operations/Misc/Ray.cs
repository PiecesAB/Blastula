using Blastula.Collision;
using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Add one or two child bullets that represent a straight laser (a.k.a. "ray" as I call it) fired from this bullet, 
    /// in the relative +x direction (where it would move due to a Forth behavior).
    /// The primary new child would be the laser itself, which will have only a RayLifecycle behavior.
    /// </summary>
    /// <remarks>
    /// This bullet as the start point (and the endpoint) are not meant to have collision (cosmetic only)
    /// and collision will be auto-disabled by this operation.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/ray.png")]
    public unsafe partial class Ray : BaseOperation
    {
        /// <summary>
        /// The positive length of the ray, if finite. Leave it blank or nonpositive for an "infinite" (very long - see advanced settings) length.
        /// </summary>
        [Export] public string rayLength = "400";
        /// <summary>
        /// If true, a clone of this bullet is placed as a child at the endpoint of the ray.
        /// (You probably don't want this if the length is "infinite", but it shouldn't matter much.)
        /// </summary>
        [Export] public bool makeEndpoint = false;
        /// <summary>
        /// The appearance of the ray during the main stages of its life (after the warning).
        /// This should have the same length (X-size) as the warning graphic.
        /// </summary>
        [Export] public string sustainAppearance = "BasicLaser/Blue";
        /// <summary>
        /// The ID of the ray appearance during warning stage. 
        /// This should have the same length (X-size) as the laser graphic.
        /// </summary>
        [Export] public string warningAppearance = "LaserWarning";
        [Export] public string warningSeconds = "0.8";
        [Export] public string expandSeconds = "0.2";
        [Export] public string sustainSeconds = "1.5";
        [Export] public string decaySeconds = "0.3";
        /// <summary>
        /// The "infinite" length of the ray. It should be long enough that the end will never appear on screen, and you can make it even longer if that's needed for peculiar patterns.
        /// </summary>
        [ExportGroup("Advanced")]
        [Export] public float infiniteLength = 1440;

        private bool appearanceIsDirty = true;
        private int sustainGraphicId;
        private int warningGraphicId;
        private float laserGraphicWidth;

        public override int ProcessStructure(int inStructure)
        {
            if (inStructure < 0) { return -1; }
            if (masterQueue[inStructure].children.count > 0) { return inStructure; }

            float rayLengthSolved = infiniteLength;
            if (rayLength is not (null or "")) { rayLengthSolved = Solve(PropertyName.rayLength).AsSingle(); }
            if (rayLengthSolved <= 0) rayLengthSolved = infiniteLength;

            int newLaserNodeIndex = MasterQueuePopOne();
            int newClone = -1;
            if (makeEndpoint)
            {
                newClone = CloneOne(inStructure);
            }
            if (newLaserNodeIndex < 0) { MasterQueuePushTree(inStructure); return -1; }
            if (appearanceIsDirty)
            {
                sustainGraphicId = BulletRendererManager.GetIDFromName(sustainAppearance);
                warningGraphicId = BulletRendererManager.GetIDFromName(warningAppearance);
                laserGraphicWidth = BulletRendererManager.GetGraphicInfoFromID(sustainGraphicId).size.X;
                appearanceIsDirty = false;
            }

            BulletRenderer.SetRenderID(newLaserNodeIndex, sustainGraphicId);
            masterQueue[newLaserNodeIndex].transform = new Transform2D(
                0, 
                new Vector2(rayLengthSolved / laserGraphicWidth, 1),
                0, 
                0.5f * rayLengthSolved * Vector2.Right
            );
            masterQueue[newLaserNodeIndex].collisionLayer = masterQueue[inStructure].collisionLayer;
            masterQueue[newLaserNodeIndex].collisionSleepStatus = masterQueue[inStructure].collisionSleepStatus;
            masterQueue[newLaserNodeIndex].health = masterQueue[inStructure].health;
            masterQueue[newLaserNodeIndex].power = masterQueue[inStructure].power;
            masterQueue[newLaserNodeIndex].rayHint = true;
            SetChild(inStructure, 0, newLaserNodeIndex);
            RayLifecycle.Add(newLaserNodeIndex, this);

            masterQueue[inStructure].collisionLayer = CollisionManager.NONE_BULLET_LAYER;
            if (makeEndpoint && newClone >= 0)
            {
                masterQueue[newClone].behaviors.DisposeBehaviorOrder();
                SetChild(inStructure, 1, newClone);
                masterQueue[newClone].transform = new Transform2D(0, Vector2.One, 0, rayLengthSolved * Vector2.Right);
            }

            return inStructure;
        }
    }
}

