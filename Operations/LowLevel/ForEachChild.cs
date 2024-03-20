using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Applies an operation on all children, while linearly interpolating several numeric properties.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/count.png")]
    public unsafe partial class ForEachChild : BaseOperation
    {
        /// <summary>
        /// Interpolates from 0 to 1 as the operation progresses.
        /// </summary>
        [Export] public string progressVariable = "t";
        /// <summary>
        /// If true, 
        /// </summary>
        [Export] public bool circular = false;
        /// <summary>
        /// This variable counts the bullets starting at zero. 0, 1, 2, 3...
        /// </summary>
        [Export] public string countVariable = "";

        public override int ProcessStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= BNodeFunctions.mqSize) { return inStructure; }
            UnsafeArray<int> children = BNodeFunctions.masterQueue[inStructure].children;
            IVariableContainer localContainer = ExpressionSolver.currentLocalContainer;
            bool useProgVar = progressVariable != null && progressVariable != "";
            bool useCountVar = countVariable != null && countVariable != "";
            for (int i = 0; i < children.count; ++i)
            {
                if (children[i] < 0 || children[i] >= BNodeFunctions.mqSize) { continue; }
                if (useProgVar)
                {
                    int div = circular ? children.count : Mathf.Max(1, children.count - 1);
                    localContainer.SetVar(progressVariable, i / (float)div);
                }
                if (useCountVar)
                {
                    localContainer.SetVar(countVariable, i);
                }
                int ci = children[i];
                BNodeFunctions.SetChild(inStructure, i, -1);
                int result = Sequence.ProcessStructureLikeSequence(this, ci);
                BNodeFunctions.SetChild(inStructure, i, result);
            }
            if (useProgVar && children.count > 0)
            {
                localContainer.ClearVar(progressVariable);
            }
            if (useCountVar && children.count > 0)
            {
                localContainer.ClearVar(countVariable);
            }
            return inStructure;
        }
    }
}
