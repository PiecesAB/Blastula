using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// An operation that only modifies the bullet structure in place.
    /// </summary>
    [GlobalClass]
    public abstract partial class Modifier : BaseOperation
    {
        public virtual void ModifyStructure(int inStructure) { }

        public sealed override int ProcessStructure(int inStructure)
        {
            ModifyStructure(inStructure);
            return inStructure;
        }
    }
}


