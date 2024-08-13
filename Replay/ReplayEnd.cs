using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// This item is a part of the stage which is meant to end the replay.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/replayEnd.png")]
    public partial class ReplayEnd : StageSchedule
    {
        public override Task Execute()
        {
            ReplayManager.main.EndSinglePlayerReplay();
            /*Error e = ReplayManager.main.Save("test");
            if (e != Error.Ok)
            {
                GD.PushError("Error trying to save the replay.");
            }*/
            return Task.CompletedTask;
        }
    }
}
