using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// This item is a part of the stage which is meant to end the replay.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/replayClapper.png")]
    public partial class ReplayEnd : StageSchedule
    {
        public override IEnumerator Execute()
        {
            ReplayManager.main.EndSinglePlayerReplaySection();
            /*Error e = ReplayManager.main.Save("test");
            if (e != Error.Ok)
            {
                GD.PushError("Error trying to save the replay.");
            }*/
            yield break;
        }
    }
}
