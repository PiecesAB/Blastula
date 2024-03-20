using Blastula.Collision;
using Blastula.Graphics;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    [GlobalClass]
    public partial class CreateSingle : Creator
    {
        [Export] public string renderName = "None";
        [Export] public string collisionLayerName = "EnemyShot";
        [Export] public bool sleepyCollision = true;

        public override int CreateStructure()
        {
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

