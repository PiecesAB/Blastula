using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// We create an auxiliary structure homologous to this one,
    /// apply its position information to the main structure,
    /// and then delete the auxiliary structure.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/innerShift.png")]
    public unsafe partial class InnerShift : Modifier
    {
        public enum RotationMode
        {
            None, FromOtherRotation, FromOtherPosition
        }

        [Export] public BaseOperation auxiliary;
        [Export] public Shift.Mode transformMode;
        [Export] public RotationMode rotationMode = RotationMode.FromOtherRotation;
        [Export] public bool placement = true;
        [Export] public bool movementSpeed = false;
        private Forth moveForwardNode;

        private void Correspond(int realID, int auxID)
        {
            if (realID < 0 || realID >= mqSize) { return; }
            if (auxID < 0 || auxID >= mqSize) { return; }
            Transform2D realT = masterQueue[realID].transform;
            Transform2D auxT = masterQueue[auxID].transform;
            switch (rotationMode)
            {
                case RotationMode.None:
                    auxT = new Transform2D(0, auxT.Origin);
                    break;
                case RotationMode.FromOtherRotation:
                default:
                    break;
                case RotationMode.FromOtherPosition:
                    float angle = Mathf.Atan2(auxT.Origin.Y, auxT.Origin.X);
                    auxT = new Transform2D(angle, auxT.Origin);
                    break;
            }
            if (!placement) { auxT = new Transform2D(auxT.Rotation, Vector2.Zero); }
            switch (transformMode)
            {
                case Shift.Mode.ApplyBefore:
                    masterQueue[realID].transform = auxT * realT;
                    break;
                case Shift.Mode.ApplyAfter:
                    masterQueue[realID].transform = realT * auxT;
                    break;
                case Shift.Mode.Set:
                    masterQueue[realID].transform = auxT;
                    break;
            }
            if (movementSpeed)
            {
                if (moveForwardNode == null)
                {
                    moveForwardNode = new Forth();
                    this.AddChild(moveForwardNode, false, InternalMode.Front);
                }
                moveForwardNode.speed = masterQueue[auxID].transform.Origin.Length().ToString();
                moveForwardNode.ModifyStructure(realID);
            }
            for (int j = 0;
                j < masterQueue[realID].children.count
                && j < masterQueue[auxID].children.count;
                ++j)
            {
                int realChild = masterQueue[realID].children[j];
                int auxChild = masterQueue[auxID].children[j];
                Correspond(realChild, auxChild);
            }
        }

        public override void ModifyStructure(int inStructure)
        {
            int auxOut = auxiliary.ProcessStructure(-1);
            if (auxOut == -1) { return; }
            Correspond(inStructure, auxOut);
            MasterQueuePushTree(auxOut);
        }
    }
}


