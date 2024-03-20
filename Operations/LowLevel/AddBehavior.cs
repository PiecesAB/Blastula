using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// A type of operation that adds behavior to a BNode.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/behaviorDefault.png")]
    public abstract partial class AddBehavior : Modifier
    {
        public virtual BehaviorOrder CreateOrder(int inStructure)
        {
            return BehaviorOrderFunctions.empty;
        }

        public sealed override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= BNodeFunctions.mqSize) { return; }
            BNodeFunctions.AddBehavior(inStructure, CreateOrder(inStructure));
        }
    }
}
