using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Deletes or truncates the behavior list of this BNode.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/behaviorClear.png")]
    public unsafe partial class ClearBehaviors : Modifier
    {
        /// <summary>
        /// If nonempty, expects a positive integer n, such that this operation clears everything past the first n behaviors.
        /// </summary>
        /// <remarks>
        /// If empty, zero or negative, this operation clears all behaviors.
        /// </remarks>
        [Export] public string remain = ""; 

        public override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return; }

            int n = 0;
            if (remain != null && remain != "")
            {
                n = Solve("remain").AsInt32();
            }

            if (n <= 0)
            {
                masterQueue[inStructure].behaviors.DisposeBehaviorOrder();
            }
            else
            {
                masterQueue[inStructure].behaviors.TruncateBehaviorOrder(n);
            }
        }
    }
}

