using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Perform another operation indirectly.
    /// Can be used to interchange pipeline pieces during the game!
    /// </summary>
    [GlobalClass]
    public partial class Reference : BaseOperation
    {
        [Export] public string sequenceID = "";
        [Export] public BaseOperation other;

        public override int ProcessStructure(int inStructure)
        {
            if (sequenceID != null && sequenceID != "")
            {
                if (!Sequence.referencesByID.ContainsKey(sequenceID)) 
                {
                    if (other != null) { return other.ProcessStructure(inStructure); }
                    else { return inStructure; }
                }
                return Sequence.referencesByID[sequenceID].ProcessStructure(inStructure);
            }
            else
            {
                if (other != null) { return other.ProcessStructure(inStructure); }
                else { return inStructure; }
            }
        }
    }
}