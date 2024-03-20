using Blastula.Collision;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/redCross.png")]
    public unsafe partial class SetCollisionData : Modifier
    {
        [Export] public string newCollisionLayer = "";
        [Export] public string graze = "";
        [Export] public string power = "";
        [Export] public string health = "";

        public override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return; }

            if (newCollisionLayer != null && newCollisionLayer != "")
            {
                int colLayerID = CollisionManager.GetBulletLayerIDFromName(newCollisionLayer);
                BNodeFunctions.SetColliderInfo(inStructure, colLayerID, masterQueue[inStructure].collisionSleepStatus.canSleep);
            }

            if (graze != null && graze != "")
            {
                masterQueue[inStructure].graze = Solve("graze").AsSingle();
            }

            if (power != null && power != "")
            {
                masterQueue[inStructure].power = Solve("power").AsSingle();
            }

            if (health != null && health != "")
            {
                masterQueue[inStructure].health = Solve("health").AsSingle();
            }
        }
    }
}

