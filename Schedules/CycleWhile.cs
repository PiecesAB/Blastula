using Blastula.VirtualVariables;
using Godot;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Executes child sequences and schedules in their tree order.
    /// Loops until the condition expression becomes false.
    /// </summary>
    /// <remarks>
    /// There's nothing stopping you from creating an infinite loop which runs without waiting, forever. Doing so will crash the game. 
    /// To prevent one, ensure you always wait at some point, or end the cycle after some number of iterations.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/scheduleCycle.png")]
    public partial class CycleWhile : BaseSchedule
    {
        [Export] public string cycleCondition = "t < 5";
        /// <summary>
        /// This would the variable name of the current cycle we are on, starting at 0.
        /// When nonempty, it populates that temporary variable, to use in operations.
        /// </summary>
        [ExportGroup("Advanced")]
        [Export] public string completedCyclesVariableName = "";

        public override async Task Execute(IVariableContainer source)
        {
            int completedCycles = 0;
            while (IsInsideTree())
            {
                if (source != null && source is Node && !((Node)source).IsInsideTree()) { break; }
                if (source != null) { ExpressionSolver.currentLocalContainer = source; }
                bool conditionMet = Solve("cycleCondition").AsBool();
                if (!conditionMet) { break; }
                if (completedCyclesVariableName != null && completedCyclesVariableName != "")
                {
                    if (source != null)
                    {
                        source.SetVar(completedCyclesVariableName, completedCycles);
                    }
                    // TODO: set stage scope variable
                }
                foreach (Node child in GetChildren())
                {
                    await ExecuteOrShoot(source, child);
                }
                completedCycles++;
            }
            if (completedCyclesVariableName != null && completedCyclesVariableName != "")
            {
                if (source != null)
                {
                    source.ClearVar(completedCyclesVariableName);
                }
                // TODO: unset stage scope variable
            }
        }
    }
}
