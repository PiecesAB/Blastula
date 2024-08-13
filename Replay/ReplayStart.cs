using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// This item is a part of the stage which is meant to start the replay.
    /// It also tries to ensure the game is in a reproducible state at the frame the replay starts.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "replayStart.png")]
    public partial class ReplayStart : StageSchedule
    {
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

        private async void What()
        {
            for (int i = 0; i < 10; ++i)
            {
                await this.WaitSeconds(0.4f);
                GD.Print("A random number is ", GD.Randf());
            }
        }

        public override async Task Execute()
        {
            replayHasStarted = false;
            current = this;
            ReplayManager.main.Load("test");
            ReplayManager.main.Connect(
                ReplayManager.SignalName.ReplayStartsNow, 
                replayStartListener = new Callable(this, MethodName.OnReplayStartsNow)
            );
            ReplayManager.main.ScheduleReplayStart();
            // Need to wait until the replay has actually started
            await this.WaitUntil(() => replayHasStarted);
            ReplayManager.main.Disconnect(
                ReplayManager.SignalName.ReplayStartsNow, 
                replayStartListener
            );
            replayStartListener = default;
            current = null;
            What();
            await this.WaitOneFrame();
        }
    }
}
