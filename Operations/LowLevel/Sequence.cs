using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;

namespace Blastula.Operations
{
    /// <summary>
    /// Applies (in tree order) all direct child operations of a node.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/sequence.png")]
    public partial class Sequence : BaseOperation
    {
        /// <summary>
        /// Execute the sequence if this is true. Otherwise does nothing.
        /// </summary>
        [Export] public string execute = "true";
        /// <summary>
        /// Optional ID. If nonempty, this sequence can be referenced throughout all scenes, as long as it exists.
        /// </summary>
        [Export] public string referenceID = "";
        public static Dictionary<string, Sequence> referencesByID = new Dictionary<string, Sequence>();

        /// <summary>
        /// Allows any Node to process structures in children, as if they were a Sequence.
        /// </summary>
        public static int ProcessStructureLikeSequence(Node holder, int inStructure)
        {
            int currentStructure = inStructure;
            foreach (Node child in holder.GetChildren(true))
            {
                if (child == null || !(child is BaseOperation)) { continue; }
                currentStructure = (child as BaseOperation).ProcessStructure(currentStructure);
                //if (currentStructure < 0 && child is not Comment) { break; }
            }
            return currentStructure;
        }

        public override int ProcessStructure(int inStructure)
        {
            int currentStructure = inStructure;
            if (Solve("execute").AsBool())
            {
                foreach (Node child in GetChildren(true))
                {
                    if (child == null || !(child is BaseOperation)) { continue; }
                    currentStructure = (child as BaseOperation).ProcessStructure(currentStructure);
                }
            }
            return currentStructure;
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            if (referenceID != null && referenceID != "")
            {
                referencesByID[referenceID] = this;
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (referenceID != null && referenceID != ""
                && referencesByID.ContainsKey(referenceID) && referencesByID[referenceID] == this)
            {
                referencesByID.Remove(referenceID);
            }
        }
    }
}
