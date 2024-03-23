using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Finds grandchildren (or further) of this bullet structure, promotes them to children, 
    /// and deletes the original children (or keeps some of them, depending on the mode).
    /// </summary>
    /// <remarks>
    /// In a sense, this is the opposite of Deepen operation.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/flatten.png")]
    public unsafe partial class Flatten : BaseOperation
    {
        /// <summary>
        /// Determines what to do with children that have zero children.
        /// False: Children with zero children are deleted, to keep remaining leaves on the same former level.
        /// True: Children with zero children are kept, to retain all former leaves.
        /// </summary>
        [Export] public bool keepEmptyChildren = false;
        /// <summary>
        /// If true, this will repeatedly flatten the structure until no child has children.
        /// That is, all depth 0 BNodes in the tree are the new children of this BNode.
        /// </summary>
        [Export] public bool complete = true;

        private void FlattenOnce(int inStructure)
        {
            UnsafeArray<int> origChildren = masterQueue[inStructure].children.Clone();
            // Tabulate the childrens' children.
            List<int> newChildrenList = new List<int>();
            for (int j = 0; j < origChildren.count; ++j)
            {
                int childIndex = origChildren[j];
                if (childIndex < 0 || masterQueue[childIndex].children.count == 0)
                {
                    if (keepEmptyChildren) { newChildrenList.Add(childIndex); }
                    continue;
                }
                for (int k = 0; k < masterQueue[childIndex].children.count; ++k)
                {
                    int grandchildIndex = masterQueue[childIndex].children[k];
                    SetChild(childIndex, k, -1);
                    newChildrenList.Add(grandchildIndex);
                    // Update the transform so that the grandchild appears to remain in place.
                    if (grandchildIndex >= 0)
                    {
                        masterQueue[grandchildIndex].transform = masterQueue[childIndex].transform * masterQueue[grandchildIndex].transform;
                        masterQueue[grandchildIndex].worldTransformMode = masterQueue[childIndex].worldTransformMode;
                    }
                }
                MasterQueuePushTree(childIndex);
            }
            // Add the new children.
            MakeSpaceForChildren(inStructure, newChildrenList.Count);
            for (int j = 0; j < newChildrenList.Count; ++j)
            {
                SetChild(inStructure, j, newChildrenList[j]);
            }
            origChildren.Dispose();
        }

        public override int ProcessStructure(int inStructure)
        {
            if (inStructure < 0) { return -1; }
            if (masterQueue[inStructure].children.count == 0) { return inStructure; }
            if (complete)
            {
                while (masterQueue[inStructure].treeDepth > 1)
                {
                    FlattenOnce(inStructure);
                    RecalculateTreeDepth(inStructure);
                }
            }
            else { FlattenOnce(inStructure); }
            return inStructure;
        }
    }
}

