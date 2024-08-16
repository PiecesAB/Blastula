using Blastula.Coroutine;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Executes an external schedule by proxy. Good for reusing schedule portions.
    /// </summary>
    /// <remarks>
    /// The source that started this ScheduleReference will be propagated to the referred schedule.
    /// </remarks>
    [GlobalClass]
    public partial class ScheduleReference : BaseSchedule
    {
        [Export] public string scheduleID = "";
        [Export] public BaseSchedule other;
        [Export] public bool waitForIDExistence = true;

        public override IEnumerator Execute(IVariableContainer source)
        {
            if (!CanExecute()) { yield break; }
            if (scheduleID != null && scheduleID != "")
            {
                if (waitForIDExistence)
                {
                    yield return new WaitCondition(() => referencesByID.ContainsKey(scheduleID));
                }
                if (!referencesByID.ContainsKey(scheduleID)) 
                {
                    if (other != null) { yield return other.Execute(source); }
                    else { yield break; }
                }
                yield return referencesByID[scheduleID].Execute(source);
            }
            else
            {
                if (other != null) { yield return other.Execute(source); }
                else { yield break; }
            }
        }
    }
}
