using Blastula.Collision;
using Blastula.Graphics;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Where it all begins... initializes a single bullet with no behavior.
    /// </summary>
    [GlobalClass]
    public partial class CreateSingle : BaseOperation
    {
        /// <summary>
        /// The name of the bullet graphic, as determined by BulletRendererManager.
        /// </summary>
        [Export] public string renderName = "None";
        /// <summary>
        /// The collision layer of this bullet, as determined by CollisionManager.
        /// </summary>
        [Export] public string collisionLayerName = "EnemyShot";
        /// <summary>
        /// If true, use the sleepy collision optimization.
        /// </summary>
        /// <remarks>
        /// Sleeping is an optional performance optimization for collision, which assumes BNodes and BlastulaColliders
        /// move slowly in general. When a BNode is far away enough from all relevant BlastulaColliders,
        /// it will fall asleep for up to six frames, not checking any collision.
        /// </remarks>
        [Export] public bool sleepyCollision = true;

        public override int ProcessStructure(int inStructure)
        {
            if (inStructure >= 0 && inStructure < mqSize) { MasterQueuePushTree(inStructure); }
            if (MasterQueueRemainingCapacity() < 1) { return -1; } // not even room for one... wow...
            int newBullet = MasterQueuePopOne();
            if (newBullet == -1) { return -1; }
            int newID = BulletRendererManager.GetIDFromName(renderName);
            BulletRenderer.SetRenderID(newBullet, newID);
            int colLayerID = CollisionManager.GetBulletLayerIDFromName(collisionLayerName);
            BNodeFunctions.SetColliderInfo(newBullet, colLayerID, sleepyCollision);
            return newBullet;
        }
    }
}

