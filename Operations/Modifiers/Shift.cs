using Blastula.VirtualVariables;
using Godot;
using System;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Move the transform of this BNode.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/transform2D.png")]
    public unsafe partial class Shift : Modifier
    {
        public enum Mode
        {
            /// <summary>
            /// Completely replace the BNode's transform with ours.
            /// </summary>
            Set,
            /// <summary>
            /// Add our transform after the existing one: as if our transform were a "child".
            /// </summary>
            ApplyAfter,
            /// <summary>
            /// Add our transform before the existing one: as if our transform were a "parent".
            /// </summary>
            ApplyBefore
        }

        public enum ChildPlacement
        {
            /// <summary>
            /// The default; children move along with this object as expected.
            /// </summary>
            Attached,
            /// <summary>
            /// Children stay in their current place, even though their containing structure has been moved.
            /// </summary>
            Static
        }

        /// <summary>
        /// Chooses which transform parts to apply in Set mode; the remaining will be provided from the BNode as it exists.
        /// </summary>
        [Flags]
        public enum SetFilter
        {
            Position = 1, Rotation = 2, Scale = 4,
        }

        [Export] public Mode mode = Mode.ApplyAfter;
        [Export] public ChildPlacement childPlacement = ChildPlacement.Attached;
        

        [ExportGroup("Transform")]
        /// <summary>
        /// Rotation is in degrees.
        /// </summary>
        [Export] public string myRotation = "0";
        [Export] public string offsetX = "0";
        [Export] public string offsetY = "0";
        [Export] public string scaleX = "1";
        [Export] public string scaleY = "1";

        /// <summary>
        /// Chooses which transform parts to apply in Set mode; the remaining will be provided from the BNode as it exists.
        /// </summary>
        [ExportGroup("Advanced")]
        [Export] public SetFilter setFilter = (SetFilter)7;

        public override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return; }
            Transform2D oldTransform = masterQueue[inStructure].transform;
            Transform2D newTransform = new Transform2D(
                Solve(PropertyName.myRotation).AsSingle() * (Mathf.Pi / 180f),
                new Vector2(Solve(PropertyName.scaleX).AsSingle(), Solve(PropertyName.scaleY).AsSingle()),
                0, new Vector2(Solve(PropertyName.offsetX).AsSingle(), Solve(PropertyName.offsetY).AsSingle())
            );

            switch (mode)
            {
                case Mode.Set:
                    if ((setFilter & SetFilter.Position) == 0)
                    {
                        newTransform.Origin = oldTransform.Origin;
                    }
                    if ((setFilter & SetFilter.Rotation) == 0)
                    {
                        newTransform = new Transform2D(oldTransform.Rotation, newTransform.Scale, 0f, newTransform.Origin);
                    }
                    if ((setFilter & SetFilter.Scale) == 0)
                    {
                        newTransform = new Transform2D(newTransform.Rotation, oldTransform.Scale, 0f, newTransform.Origin);
                    }
                    masterQueue[inStructure].transform = newTransform;
                    break;
                case Mode.ApplyBefore:
                    masterQueue[inStructure].transform
                        = newTransform * masterQueue[inStructure].transform;
                    break;
                case Mode.ApplyAfter:
                default:
                    masterQueue[inStructure].transform
                        = masterQueue[inStructure].transform * newTransform;
                    break;
            }

            switch (childPlacement)
            {
                case ChildPlacement.Attached:
                default:
                    break;
                case ChildPlacement.Static:
                    for (int i = 0; i < masterQueue[inStructure].children.count; ++i)
                    {
                        int childIndex = masterQueue[inStructure].children[i];
                        if (childIndex < 0) { continue; }
                        masterQueue[childIndex].transform
                            = newTransform.AffineInverse() * oldTransform * masterQueue[childIndex].transform;
                    }
                    break;
            }
        }
    }
}