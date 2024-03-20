using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Operates on all nodes at a certain depth level within a BNode's structure.
    /// Only ModifyOperation is allowed.
    /// Depth is the maximum distance to a BNode with no children.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/cascade.png")]
    public unsafe partial class Cascade : Modifier
    {
        public enum Mode
        {
            /// <summary>
            /// Run on every node in the tree.
            /// </summary>
            All,
            /// <summary>
            /// Run on every node with a particular depth.
            /// </summary>
            Depth,
            /// <summary>
            /// Run on every node with a particular distance from the root.
            /// </summary>
            DistanceFromRoot
        }

        [Export] public Modifier subOperation;
        [Export] public Mode mode = Mode.Depth;
        [Export] public int targetValue = 0;
        [Export] public bool recalculateDepths = false;

        private void RecursiveModify(int i, int targetDepth, int recursiveDepth)
        {
            if (i < 0 || i >= mqSize) { return; }
            if (recalculateDepths) { RecalculateTreeDepth(i); }
            if (masterQueue[i].treeDepth == targetDepth || recursiveDepth == targetDepth) { subOperation.ModifyStructure(i); }
            else if (masterQueue[i].treeDepth < targetDepth) { return; }
            for (int j = 0; j < masterQueue[i].children.count; ++j)
            {
                int childIndex = masterQueue[i].children[j];
                if (childIndex < 0 || childIndex >= mqSize) { continue; }
                RecursiveModify(childIndex, targetDepth, recursiveDepth + 1);
            }
        }

        private void RecursiveModify(int i)
        {
            if (i < 0 || i >= mqSize) { return; }
            subOperation.ModifyStructure(i);
            for (int j = 0; j < masterQueue[i].children.count; ++j)
            {
                int childIndex = masterQueue[i].children[j];
                if (childIndex < 0 || childIndex >= mqSize) { continue; }
                RecursiveModify(childIndex);
            }
        }

        public override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return; }
            switch (mode)
            {
                case Mode.All:
                    RecursiveModify(inStructure);
                    break;
                case Mode.Depth:
                default:
                    RecursiveModify(inStructure, targetValue, int.MinValue);
                    break;
                case Mode.DistanceFromRoot:
                    RecursiveModify(inStructure, -1, 0);
                    break;
            }
        }
    }
}
