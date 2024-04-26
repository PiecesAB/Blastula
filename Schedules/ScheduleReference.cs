using Blastula.VirtualVariables;
using Godot;
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

        public override async Task Execute(IVariableContainer source)
        {
            if (base.Execute(source) == null) { return; }
            if (scheduleID != null && scheduleID != "")
            {
                if (waitForIDExistence)
                {
                    await this.WaitUntil(() => referencesByID.ContainsKey(scheduleID));
                }
                if (!referencesByID.ContainsKey(scheduleID)) 
                {
                    if (other != null) { await other.Execute(source); }
                    else { return; }
                }
                await referencesByID[scheduleID].Execute(source);
            }
            else
            {
                if (other != null) { await other.Execute(source); }
                else { return; }
            }
        }
    }
}
