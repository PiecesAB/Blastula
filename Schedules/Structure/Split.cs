using Blastula.Coroutine;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;

namespace Blastula.Schedules
{
    /// <summary>
    /// Run all child schedules and shots at the same time.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/scheduleSplit.png")]
    public partial class Split : BaseSchedule
    {
        public override IEnumerator Execute(IVariableContainer source)
        {
            if (!CanExecute()) { yield break; }
            foreach (var child in GetChildren())
            {
                this.StartCoroutine(ExecuteOrShoot(source, child));
            }
            yield break;
        }
    }
}
