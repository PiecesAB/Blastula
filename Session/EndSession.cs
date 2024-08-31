using Blastula.Coroutine;
using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// This properly signals the end of the session.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/stopSign.png")]
    public partial class EndSession : StageSchedule
    {
        // This scene will be loaded over the main session.
        // It is intended to be a menu for the ending of the game (or the first menu in a series at the end).
        [Export] public PackedScene nextMenu;

        public override IEnumerator Execute()
        {
            if (ReplayManager.main.playState == ReplayManager.PlayState.Playing) 
            {
                ReplayManager.main.EndSinglePlayerReplaySection();
                yield return new WaitOneFrame();
            }
            StageManager.main.EndSinglePlayerSession();
            if (nextMenu == null)
            {
                GD.PushWarning("Ended the session with no menu to regain control.");
            }
            else
            {
                Loader.LoadExternal(this, nextMenu);
            }
            yield break;
        }
    }
}
