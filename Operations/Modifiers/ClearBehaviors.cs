using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Deletes the behavior list of this BNode.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/behaviorClear.png")]
    public unsafe partial class ClearBehaviors : Modifier
    {
        public override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return; }
            masterQueue[inStructure].behaviors.DisposeBehaviorOrder();
        }
    }
}

