using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Produces a bullet structure from nothing.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/creation.png")]
    public abstract partial class Creator : BaseOperation
    {
        public abstract int CreateStructure();

        public sealed override int ProcessStructure(int inStructure)
        {
            // Decided to pass in a BNode anyway?... I will destroy you.
            if (inStructure >= 0 && inStructure < BNodeFunctions.mqSize)
            {
                BNodeFunctions.MasterQueuePushTree(inStructure);
            }
            return CreateStructure();
        }
    }
}

