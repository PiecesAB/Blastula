using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Creates a new bullet structure, modifies the new structure using this operation's children as a sequence, 
    /// makes the output of the sequence inherited by a certain Blastodisc, and returns the original structure.
    /// This allows bullets to shoot from other bullets.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/creation.png")]
    public partial class Fork : Sequence
    {
        public enum StartMode
        {
            /// <summary>
            /// No bullet structure is given to this sequence; it must be created anew.
            /// </summary>
            None, 
            /// <summary>
            /// The input bullet structure is cloned to start the sequence
            /// </summary>
            Clone
        }

        [Export] public StartMode startMode = StartMode.None;
        /// <summary>
        /// If given, the new bullet structure will be inherited by this Blastodisc.
        /// If this and blastodiscID are both empty, it's instead inherited by the primordial Blastodisc.
        /// </summary>
        [Export] public Blastodisc blastodisc;
        /// <summary>
        /// If blastodisc is null, try to find and use the first one with this ID.
        /// The bullet structure will be inherited by the Blastodisc of the ID.
        /// </summary>
        [Export] public string blastodiscID = "";

        public unsafe override int ProcessStructure(int inStructure)
        {
            int subIn = -1;
            switch (startMode)
            {
                case StartMode.None: break;
                case StartMode.Clone: subIn = CloneOne(inStructure); break;
            }
            Blastodisc nextDisc = Blastodisc.primordial;
            if (blastodisc != null) { nextDisc = blastodisc; }
            else if (blastodiscID != null && Blastodisc.allByID.ContainsKey(blastodiscID)) 
            {
                HashSet<Blastodisc> discsOfID = Blastodisc.allByID[blastodiscID];
                if (discsOfID.Count > 0)
                {
                    // Get the first one in a stupid way
                    foreach (Blastodisc b in discsOfID) { nextDisc = b; break; }
                }
            }
            int subOut = base.ProcessStructure(subIn);
            if (inStructure >= 0)
            {
                masterQueue[subOut].transform = masterQueue[inStructure].transform * masterQueue[subOut].transform;
            }
            if (nextDisc != null) { nextDisc.Inherit(subOut); }
            return inStructure;
        }
    }
}
