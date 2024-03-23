using Blastula.Graphics;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// A base class for operations which create clones of bullet structures,
    /// make the clones children of an overarching bullet structure at the original position + rotation,
    /// then apply new position + rotations for each child.
    /// </summary>
    /// <example>
    /// Blastula has several built-in shapers which can create common shapes such as circles and spreads of bullets.
    /// </example>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/shaper.png")]
    public unsafe abstract partial class Shaper : BaseOperation
    {
        public enum ShapingMode
        {
            /// <summary>
            /// Restructures clones of this structure into the shape as children of a new node.
            /// </summary>
            Clone,
            /// <summary>
            /// Alters the transforms of children in this bullet structure. Works best when "number" equals child count.
            /// </summary>
            Place,
            /// <summary>
            /// Alters the transform of this object as if it were the 0th element of its parent.
            /// Useful particularly for random shots.
            /// </summary>
            SelfAtZeroth
        }

        public enum TransformOrder
        {
            /// <summary>
            /// Add our transform before the existing one: as if our transform were a "parent".
            /// </summary>
            ApplyBefore,
            /// <summary>
            /// Add our transform after the existing one: as if our transform were a "child".
            /// </summary>
            ApplyAfter
        }

        public enum RotationMode
        {
            /// <summary>
            /// Don't rotate the children at all.
            /// </summary>
            None,
            /// <summary>
            /// Rotates the children of the new structure, but un-rotates the grandchildren.
            /// As such, grandchildren seem to remain unrotated, though their holder has been rotated.
            /// </summary>
            Root,
            /// <summary>
            /// Rotates the whole structure.
            /// </summary>
            Full
        }


        [Export] public string number = "12";
        [Export] public ShapingMode shapingMode = ShapingMode.Clone;
        [Export] public RotationMode rotationMode = RotationMode.Full;
        [Export] public TransformOrder transformOrder = TransformOrder.ApplyBefore;

        /// <summary>
        /// i is the child index - imagine 0 to be the first and (number - 1) to be the last.
        /// </summary>
        public abstract Transform2D GetElementTransform(int i, int totalCount);

        private int ProcessStructureClone(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return -1; }
            int number = Solve("number").AsInt32();
            if (number <= 0) { MasterQueuePushTree(inStructure); return -1; }
            // Ensure we have enough space to complete the new structure.
            // If not, abort this construction process.
            int spaceNeeded = 1 + (number - 1) * masterQueue[inStructure].treeSize;
            if (MasterQueueRemainingCapacity() < spaceNeeded) { MasterQueuePushTree(inStructure); return -1; }

            int outStructure = MasterQueuePopOne();
            if (outStructure == -1) { return -1; }
            BulletRenderer.SetRenderID(outStructure, -1);
            MakeSpaceForChildren(outStructure, number);
            Transform2D inTransform = masterQueue[inStructure].transform;
            int clones = CloneN(inStructure, number - 1);
            for (int j = 0; j < number; ++j)
            {
                int k = (j == 0) ? (inStructure) : ((clones + j - 1) % mqSize);
                SetChild(outStructure, j, k);
                Transform2D ej = GetElementTransform(j, number);
                switch (rotationMode)
                {
                    case RotationMode.Full:
                    default:
                        break;
                    case RotationMode.None:
                        ej = new Transform2D(0, ej.Origin);
                        break;
                    case RotationMode.Root:
                        for (int l = 0; l < masterQueue[k].children.count; ++l)
                        {
                            int childIndex = masterQueue[k].children[l];
                            if (childIndex < 0 || childIndex >= mqSize) { continue; }
                            if (transformOrder == TransformOrder.ApplyBefore)
                            {
                                masterQueue[childIndex].transform = new Transform2D(-ej.Rotation, Vector2.Zero) * masterQueue[childIndex].transform;
                            }
                            else
                            {
                                masterQueue[childIndex].transform = masterQueue[childIndex].transform * new Transform2D(-ej.Rotation, Vector2.Zero);
                            }
                        }
                        break;
                }
                if (transformOrder == TransformOrder.ApplyBefore) { SetTransform2D(k, ej * inTransform); }
                else { SetTransform2D(k, inTransform * ej); }
            }
            return outStructure;
        }

        private int ProcessStructurePlace(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return -1; }
            int childCount = masterQueue[inStructure].children.count;
            if (childCount == 0) { return inStructure; }
            for (int j = 0; j < childCount; ++j)
            {
                int childIndex = masterQueue[inStructure].children[j];
                if (childIndex < 0 || childIndex >= mqSize) { continue; }
                Transform2D ej = GetElementTransform(j, childCount);
                switch (rotationMode)
                {
                    case RotationMode.Full:
                    default:
                        break;
                    case RotationMode.None:
                        ej = new Transform2D(0, ej.Origin);
                        break;
                    case RotationMode.Root:
                        for (int l = 0; l < masterQueue[childIndex].children.count; ++l)
                        {
                            int child2Index = masterQueue[childIndex].children[l];
                            if (child2Index < 0 || child2Index >= mqSize) { continue; }
                            if (transformOrder == TransformOrder.ApplyBefore)
                            {
                                masterQueue[child2Index].transform = new Transform2D(-ej.Rotation, Vector2.Zero) * masterQueue[child2Index].transform;
                            }
                            else
                            {
                                masterQueue[child2Index].transform = masterQueue[child2Index].transform * new Transform2D(-ej.Rotation, Vector2.Zero);
                            }
                        }
                        break;
                }
                if (transformOrder == TransformOrder.ApplyBefore) { SetTransform2D(childIndex, ej * masterQueue[childIndex].transform); }
                else { SetTransform2D(childIndex, masterQueue[childIndex].transform * ej); }
            }
            return inStructure;
        }

        private int ProcessStructureSelf(int inStructure, int fakeIndex)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return -1; }
            int number = Solve("number").AsInt32();
            Transform2D ej = GetElementTransform(fakeIndex, number);
            switch (rotationMode)
            {
                case RotationMode.Full:
                default:
                    break;
                case RotationMode.None:
                    ej = new Transform2D(0, ej.Origin);
                    break;
                case RotationMode.Root:
                    for (int l = 0; l < masterQueue[inStructure].children.count; ++l)
                    {
                        int childIndex = masterQueue[inStructure].children[l];
                        if (childIndex < 0 || childIndex >= mqSize) { continue; }
                        if (transformOrder == TransformOrder.ApplyBefore)
                        {
                            masterQueue[childIndex].transform = new Transform2D(-ej.Rotation, Vector2.Zero) * masterQueue[childIndex].transform;
                        }
                        else
                        {
                            masterQueue[childIndex].transform = masterQueue[childIndex].transform * new Transform2D(-ej.Rotation, Vector2.Zero);
                        }
                    }
                    break;
            }
            if (transformOrder == TransformOrder.ApplyBefore) { SetTransform2D(inStructure, ej * masterQueue[inStructure].transform); }
            else { SetTransform2D(inStructure, masterQueue[inStructure].transform * ej); }
            return inStructure;
        }


        public sealed override int ProcessStructure(int inStructure)
        {
            switch (shapingMode)
            {
                case ShapingMode.Clone:
                default:
                    return ProcessStructureClone(inStructure);
                case ShapingMode.Place:
                    return ProcessStructurePlace(inStructure);
                case ShapingMode.SelfAtZeroth:
                    return ProcessStructureSelf(inStructure, 0);
            }
        }
    }
}
