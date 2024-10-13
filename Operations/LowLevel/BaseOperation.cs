using Blastula.VirtualVariables;
using Godot;
using Godot.Collections;
using System.Threading;

namespace Blastula.Operations
{
    /// <summary>
    /// Base class for all Blastula "operations". These normally work together to create a bullet structure in a Sequence,
    /// but some can act independently in any schedule context.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/operationDefault.png")]
    public abstract partial class BaseOperation : Node
    {
        /// <summary>
        /// Processes a bullet tree rooted at a certain BNode.
        /// </summary>
        /// <param name="inStructure">Index of the input BNode in the master queue</param>
        /// <returns>Index of the output BNode in the master queue.</returns>
        public virtual int ProcessStructure(int inStructure) { return inStructure; }

        protected static long IDCounter;
        /// <summary>
        /// We can be sure that no other operation will ever have this ID.
        /// </summary>
        private long ID;
        public static Dictionary<long, BaseOperation> operationFromID = new Dictionary<long, BaseOperation>();

        public static bool OperationIDExists(long ID)
        {
            return operationFromID.ContainsKey(ID);
        }

        public long GetOperationID()
        {
            return ID;
        }

        private System.Collections.Generic.Dictionary<string, Variant> constants = new System.Collections.Generic.Dictionary<string, Variant>();

        /// <summary>
        /// Shorthand for using ExpressionSolver.Solve on variables in classes that descend from BaseOperation.
        /// </summary>
        /// <remarks>
        /// The local variable container should always be a Blastodisc when this is called.
        /// </remarks>
        public Variant Solve(string varName)
        {
            if (constants.TryGetValue(varName, out Variant cachedResult)) { return cachedResult; }
            Variant result = ExpressionSolver.Solve(this, varName, Get(varName).AsString(), out ExpressionSolver.SolveStatus solveStatus);
            if (solveStatus == ExpressionSolver.SolveStatus.SolvedConstant) { constants[varName] = result; }
            return result;
        }

        /// <summary>
        /// Solves an expression string directly.
        /// </summary>
        /// <remarks>
        /// There is no caching expression values here, so overuse can hurt performance.
        /// </remarks>
        public Variant SolveDirect(string directExpression)
        {
            return ExpressionSolver.Solve(this, null, directExpression, out ExpressionSolver.SolveStatus solveStatus);
        }

        /// <summary>
        /// Force recalculation of the expression at varName; for example, when it's changed.
        /// </summary>
        public void Unsolve(string varName)
        {
            ExpressionSolver.Unsolve(this, varName);
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            ID = Interlocked.Increment(ref IDCounter);
            operationFromID[ID] = this;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            operationFromID.Remove(ID);
            ExpressionSolver.ClearNode(this);
        }
    }
}

