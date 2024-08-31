using Blastula.Coroutine;
using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// This item is a part of the stage which is meant to start a replay section.
    /// It also tries to ensure the game is in a reproducible state at the frame the replay starts.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/replayClapper.png")]
    public partial class ReplayStart : StageSchedule
    {
        /// <summary>
        /// The name of the replay section as it will be stored. Of course, this name should be safe for a file to have; think alphanumeric.
        /// </summary>
        [Export] public string replaySectionName;
        /// <summary>
        /// This is the ReplayStart which is waiting for the real start, in order to find it in the ReplayManager
        /// </summary>
        public static ReplayStart current = null;
        private bool replayHasStarted = false;

        private Callable replayStartListener;

        public void OnReplayStartsNow()
        {
            replayHasStarted = true;
        }

        public override IEnumerator Execute()
        {
            if (ReplayManager.main.playState == ReplayManager.PlayState.Playing)
            {
                ReplayManager.main.EndSinglePlayerReplaySection();
                yield return new WaitOneFrame();
            }
            replayHasStarted = false;
            current = this;
            //ReplayManager.main.LoadSection("test");
            ReplayManager.main.Connect(
                ReplayManager.SignalName.ReplayStartsNow, 
                replayStartListener = new Callable(this, MethodName.OnReplayStartsNow)
            );
            ReplayManager.main.ScheduleReplayStart(replaySectionName);
            // Need to wait until the replay has actually started
            yield return new WaitCondition(() => replayHasStarted);
            ReplayManager.main.Disconnect(
                ReplayManager.SignalName.ReplayStartsNow, 
                replayStartListener
            );
            replayStartListener = default;
            current = null;
            yield return new WaitOneFrame();
        }
    }
}
