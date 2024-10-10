using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Base class for all schedules.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/scheduleDefault.png")]
    public abstract partial class BaseSchedule : Node
    {
        /// <summary>
        /// Optional ID. If nonempty, this schedule can be referenced throughout all scenes, as long as it exists.
        /// </summary>
        [Export] public string referenceID = "";
        public static Dictionary<string, BaseSchedule> referencesByID = new Dictionary<string, BaseSchedule>();

        private System.Collections.Generic.Dictionary<string, Variant> constants = new System.Collections.Generic.Dictionary<string, Variant>();

        protected IEnumerator ExecuteOrShoot(IVariableContainer source, Node node)
        {
            if (Session.main == null || !Session.main.inSession) { yield break; }
            if (source != null && source is Blastodisc)
            {
                Blastodisc sd = (Blastodisc)source;
                if (!sd.enabled || !sd.IsInsideTree()) { yield break; }
                if (node is BaseSchedule)
                {
                    yield return (node as BaseSchedule).Execute(source);
                }
                else if (node is BaseOperation)
                {
                    sd.Shoot(node as BaseOperation);
                }
            }
            else // Schedules being solved in a non-Blastodisc context.
            {
                if (node is BaseSchedule)
                {
                    yield return (node as BaseSchedule).Execute(source);
                }
                else if (node is BaseOperation)
                {
                    int result = ((BaseOperation)node).ProcessStructure(-1);
                    if (result >= 0)
                    {
                        // What? Why did you shoot bullets outside of an enemy pattern?
                        // Ok... let's make do and have the primordial disc inherit the structure
                        Blastodisc.primordial.Inherit(result);
                    }
                }
            }
        }

        public bool CanExecute()
        {
            return !(Session.main == null || !Session.main.inSession);
        }

        public virtual IEnumerator Execute(IVariableContainer source)
        {
            yield break;
        }

        /// <summary>
        /// Solves the string-valued expression stored at a variable in this node.
        /// </summary>
        /// <param name="varName">The name of the node's variable.</param>
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
            ExpressionSolver.ClearNode(this);
            if (referenceID != null && referenceID != "" 
                && referencesByID.ContainsKey(referenceID) && referencesByID[referenceID] == this)
            {
                referencesByID.Remove(referenceID);
            }
        }
    }
}
