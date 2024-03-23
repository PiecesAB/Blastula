using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Takes the local position within each child, and converts it to a Forth behavior where distance becomes speed.
    /// A simple way to add velocity in a certain shape, and apply positioning afterward.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/forthByPosition.png")]
    public unsafe partial class ForthByPosition : Modifier
    {
        [Export] public Forth.Mode forthMode = Forth.Mode.MoveAfter;

        /// <summary>
        /// Forth only moves forward, so we should rotate appropriately to form the shape as movement occurs.
        /// Set type is the most sensible for this purpose, but other rotation types can be used for unusual patterns.
        /// </summary>
        public enum RotationMode
        {
            /// <summary>
            /// Doesn't rotate.
            /// </summary>
            DoNothing,
            /// <summary>
            /// Sets rotation to form the shape as movement occurs.
            /// </summary>
            Set, 
            /// <summary>
            /// Adds to existing rotation.
            /// </summary>
            Add
        }
        [Export] public RotationMode rotationMode = RotationMode.Set;
        /// <summary>
        /// How much to retain the original positions?
        /// </summary>
        /// <example>0: all bullet structures are now at the center.</example>
        /// <example>1: the bullet positions are retained.</example>
        [Export] public string positionMultiplier = "0";

        public override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0) { return; }
            bool changeRotation = Solve("changeRotation").AsBool();
            float positionMultiplier = Solve("positionMultiplier").AsSingle();
            for (int j = 0; j < masterQueue[inStructure].children.count; ++j)
            {
                int childIndex = masterQueue[inStructure].children[j];
                if (childIndex < 0) { continue; }
                Vector2 position = masterQueue[childIndex].transform.Origin;
                float rotation = masterQueue[childIndex].transform.Rotation;
                (float radius, float angle) = (position.Length(), position.Angle());
                Forth.Add(childIndex, radius, forthMode);
                Vector2 newPosition = position * positionMultiplier;
                masterQueue[childIndex].transform = masterQueue[childIndex].transform.Translated(newPosition - position);
                switch (rotationMode)
                {
                    case RotationMode.DoNothing: break;
                    case RotationMode.Set:
                    default:
                        {
                            float newRotation = angle;
                            masterQueue[childIndex].transform = masterQueue[childIndex].transform.RotatedLocal(newRotation - rotation);
                        }
                        break;
                    case RotationMode.Add:
                        {
                            float newRotation = angle + masterQueue[childIndex].transform.Rotation;
                            masterQueue[childIndex].transform = masterQueue[childIndex].transform.RotatedLocal(newRotation - rotation);
                        }
                        break;
                }
            }
        }
    }
}