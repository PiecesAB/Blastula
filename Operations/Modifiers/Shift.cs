using Blastula.VirtualVariables;
using Godot;
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

        public override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return; }
            Transform2D oldTransform = masterQueue[inStructure].transform;
            Transform2D newTransform = new Transform2D(
                Solve("myRotation").AsSingle() * (Mathf.Pi / 180f),
                new Vector2(Solve("scaleX").AsSingle(), Solve("scaleY").AsSingle()),
                0, new Vector2(Solve("offsetX").AsSingle(), Solve("offsetY").AsSingle())
            );

            switch (mode)
            {
                case Mode.Set:
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