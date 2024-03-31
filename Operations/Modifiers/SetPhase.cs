using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Set the phase number of a bullet for incidental tracking purposes.
    /// </summary>
    /// <example>
    /// Collectible phase is tracked. 
    /// Collectibles are just "bullets" with complex behavior in three phases:<br />
    /// 0. Emerge from the point at which the enemy was destroyed.<br />
    /// 1. Move up and slowly change to move down by gravity.<br />
    /// 2. If the attractbox is hit, attract toward the player.<br />
    /// The player is interested in phase 1, so that its collision can activate phase 2.
    /// </example>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/nova.png")]
    public unsafe partial class SetPhase : Modifier
    {
        public enum Mode
        {
            /// <summary>
            /// Set the phase directly.
            /// </summary>
            Set, 
            /// <summary>
            /// Add to the existing phase.
            /// </summary>
            Add
        }

        [Export] public Mode mode = Mode.Set;
        [Export] public string newPhase = "0";

        public override void ModifyStructure(int inStructure)
        {
            if (inStructure >= 0 && newPhase != null && newPhase != "")
            {
                short np = Solve("newPhase").AsInt16();
                if (mode == Mode.Add) { np += BNodeFunctions.masterQueue[inStructure].phase; }
                BNodeFunctions.masterQueue[inStructure].phase = np;
            }
        }
    }
}

