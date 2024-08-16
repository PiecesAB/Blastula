using Blastula.Coroutine;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;

namespace Blastula.Schedules
{
    /// <summary>
    /// Waits until the expression evaluates to something true.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/clock.png")]
    public partial class WaitUntil : BaseSchedule
    {
        [Export] public string condition = "true";

        public override IEnumerator Execute(IVariableContainer source)
        {
            if (!CanExecute()) { yield break; }
            while (GetTree() != null)
            {
                if (source != null) { ExpressionSolver.currentLocalContainer = source; }
                bool condition = Solve("condition").AsBool();
                if (condition) { yield break; }
                yield return new WaitOneFrame();
            }
        }
    }
}
