using Blastula.VirtualVariables;
using Godot;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Executes child sequences and schedules in their tree order.
    /// Loops a number of times you can specify.
    /// </summary>
    /// <remarks>
    /// There's nothing stopping you from creating an infinite loop which runs without waiting, forever. Doing so will crash the game. 
    /// To prevent one, ensure you always wait at some point, or end the cycle after some number of iterations.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/scheduleCycle.png")]
    public partial class Cycle : BaseSchedule
    {
        /// <summary>
        /// Solves this expression at the beginning of execution to determine how many times to loop. Empty string means it loops forever.
        /// </summary>
        [Export] public string cycleCount = "";
        /// <summary>
        /// This would the variable name of the current cycle we are on, starting at 0.
        /// When nonempty, it populates that temporary variable, to use in operations.
        /// </summary>
        [ExportGroup("Advanced")]
        [Export] public string completedCyclesVariableName = "";

        public override async Task Execute(IVariableContainer source)
        {
            int cyclesRemaining = -1;
            if (cycleCount != null && cycleCount != "")
            {
                if (source != null) { ExpressionSolver.currentLocalContainer = source; }
                cyclesRemaining = Solve("cycleCount").AsInt32();
            }
            bool useCompletedCyclesVar = completedCyclesVariableName != null && completedCyclesVariableName != "";
            bool completedCyclesVarWasSet = false;
            int completedCycles = 0;
            while (cyclesRemaining != 0 && IsInsideTree())
            {
                if (source != null && source is Node && !((Node)source).IsInsideTree()) { break; }
                if (useCompletedCyclesVar)
                {
                    completedCyclesVarWasSet = true;
                    if (source != null)
                    {
                        source.SetVar(completedCyclesVariableName, completedCycles);
                    }
                }
                foreach (Node child in GetChildren())
                {
                    await ExecuteOrShoot(source, child);
                }
                if (cyclesRemaining > 0) { cyclesRemaining--; }
                completedCycles++;
            }
            if (useCompletedCyclesVar && completedCyclesVarWasSet)
            {
                if (source != null)
                {
                    source.ClearVar(completedCyclesVariableName);
                }
            }
        }
    }
}
