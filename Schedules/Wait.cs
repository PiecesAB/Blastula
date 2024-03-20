using Blastula.VirtualVariables;
using Godot;
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

        public override async Task Execute(IVariableContainer source)
        {
            if (source != null) { ExpressionSolver.currentLocalContainer = source; }
            float waitTime = Solve("waitTime").AsSingle();
            switch (units)
            {
                case TimeUnits.Seconds:
                default:
                    await this.WaitSeconds(waitTime);
                    break;
                case TimeUnits.Frames:
                    await this.WaitFrames(Mathf.RoundToInt(waitTime));
                    break;
            }
        }
    }
}
