using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// An operation that does absolutely nothing to any bullet structure.
    /// Mainly used in certain self-contained operations,
    /// such as setting a variable or playing a sound.
    /// </summary>
    [GlobalClass]
    public abstract partial class Discrete : Modifier
    {
        public virtual void Run() { }

        public sealed override void ModifyStructure(int inStructure)
        {
            Run();
        }
    }
}


