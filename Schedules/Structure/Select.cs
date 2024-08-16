using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Selects a particular schedule at a child index.
    /// "selection" can be a true/false value or an integer.
    /// When it is true/false, evaluating to false will choose the first child (index 0). Evaluating to true chooses the second child (index 1).
    /// When it is an integer, it will choose the child at that index. Negative integers work as they do in Python,
    /// counting from the list's end.
    /// </summary>
    /// <remarks>
    /// Out of bounds selection index will attempt to loop the index so that it's in range.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/scheduleSelect.png")]
    public partial class Select : BaseSchedule
    {
        [Export] public string selection = "0";

        public override IEnumerator Execute(IVariableContainer source)
        {
            if (!CanExecute()) { yield break; }
            if (GetChildCount() == 0) { yield break; }
            if (source != null && source is Node && !((Node)source).IsInsideTree()) { yield break; }
            if (source != null) { ExpressionSolver.currentLocalContainer = source; }
            Variant selectionV = Solve("selection");
            int selectionI = 0;
            if (selectionV.VariantType == Variant.Type.Bool) { selectionI = selectionV.AsBool() ? 1 : 0; }
            else { selectionI = selectionV.AsInt32(); }
            selectionI = MoreMath.RDMod(selectionI, GetChildCount());
            Node selectedNode = GetChild(selectionI);
            yield return ExecuteOrShoot(source, selectedNode);
        }
    }
}
