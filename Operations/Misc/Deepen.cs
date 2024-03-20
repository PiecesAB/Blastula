using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Puts the children into groups of a new structure such that they become the childrens' children.<br />
    /// You can choose the size of the groups. Extraneous children that can't form a group are deleted.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/deepen.png")]
    public unsafe partial class Deepen : BaseOperation
    {
        [Export] public string groupSize = "12";

        public override int ProcessStructure(int inStructure)
        {
            int groupSize = Solve("groupSize").AsInt32();
            if (inStructure < 0 || inStructure >= mqSize) { MasterQueuePushTree(inStructure); return -1; }
            int groupCount = masterQueue[inStructure].children.count / groupSize;
            if (MasterQueueRemainingCapacity() < groupCount) { MasterQueuePushTree(inStructure); return -1; }
            if (groupCount == 0) { MasterQueuePushTree(inStructure); return -1; }
            // These are the containers.
            int outChildren = MasterQueuePopN(groupCount);
            if (outChildren == -1) { MasterQueuePushTree(inStructure); return -1; }
            int origChildTotal = masterQueue[inStructure].children.count;
            int origChildCounter = 0;
            for (int k = 0; k < groupCount; ++k)
            {
                int outChild = (outChildren + k) % mqSize;
                // Containers inherit their position from the inStructure by default, since they have an identity transformation.
                // Anyway, we need to move appropriate children to their new container.
                for (int l = 0; l < groupSize; ++l)
                {
                    int childIndex = masterQueue[inStructure].children[origChildCounter];
                    SetChild(inStructure, origChildCounter, -1);
                    SetChild(outChild, l, childIndex);
                    ++origChildCounter;
                }
            }
            // The remainder of children (which would make a smaller group) get deleted.
            while (origChildCounter < origChildTotal)
            {
                MasterQueuePushTree(masterQueue[inStructure].children[origChildCounter]);
                ++origChildCounter;
            }
            // Add the containers to the original structure.
            MakeSpaceForChildren(inStructure, groupCount);
            for (int k = 0; k < groupCount; ++k)
            {
                SetChild(inStructure, k, (outChildren + k) % mqSize);
            }
            return inStructure;
        }
    }
}

