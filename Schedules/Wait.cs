using Blastula.Coroutine;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Waits an amount of time.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/clock.png")]
    public partial class Wait : BaseSchedule
    {
        public enum TimeUnits
        {
            Seconds, Frames
        }

        [Export] public string waitTime = "1";
        [Export] public TimeUnits units = TimeUnits.Seconds;

        public override IEnumerator Execute(IVariableContainer source)
        {
            if (!CanExecute()) { yield break; }
            if (source != null) { ExpressionSolver.currentLocalContainer = source; }
            float waitTime = Solve("waitTime").AsSingle();
            switch (units)
            {
                case TimeUnits.Seconds:
                default:
                    yield return new WaitTime(waitTime);
                    break;
                case TimeUnits.Frames:
                    yield return new WaitFrames(Mathf.RoundToInt(waitTime));
                    break;
            }
        }
    }
}
