using Blastula.Coroutine;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections;

namespace Blastula.Schedules;

/// <summary>
/// Encompasses a dialog sequence.
/// </summary>
[GlobalClass]
[Icon(Persistent.NODE_ICON_PATH + "/dialogSeries.png")]
public partial class DialogSeries : BaseSchedule
{
    private static DialogSeries current = null;

    public static DialogSeries GetSeries(DialogTrigger trigger)
    {
        Node currentCheck = trigger.GetParent();
        while (currentCheck is not (null or DialogSeries)) currentCheck = currentCheck.GetParent();
        return (currentCheck is DialogSeries ds) ? ds : null;
    }

    public override IEnumerator Execute(IVariableContainer source)
    {
        if (current != null)
        {
            GD.PushError("Can't start a dialog while one is already ongoing!");
            yield break;
        }

        if (Session.main.timeScale != 1.0)
        {
            GD.Print("The time scale is being set to 1 as dialog begins (it wasn't already 1).");
            Session.main.SetTimeScale(1.0);
        }

        current = this;

        if (ReplayManager.main.mode == ReplayManager.Mode.Record && ReplayManager.main.playState == ReplayManager.PlayState.Playing)
        {
            GD.Print("You are starting a dialog series while recording a replay section, so I'm ending that section.");
            ReplayManager.main.EndSinglePlayerReplaySection();
        }
        // Now that the replay is ended, we supposedly have control over all player inputs.
        // Force them to only be able to move, and allow all control again after dialog ends.
        foreach (var plrRole in new[] { Player.Role.SinglePlayer, Player.Role.LeftPlayer, Player.Role.RightPlayer })
        {
            if (!Player.playersByControl.TryGetValue(plrRole, out Player plr)) continue;
            plr.inputTranslator?.ForbidNonMovementInput();
        }

        DialogOverlay.main.Activate();

        yield return new WaitTime(0.25);

        // behave as a single-loop Cycle
        foreach (Node child in GetChildren())
        {
            yield return ExecuteOrShoot(source, child);
        }

        DialogOverlay.main.Deactivate();

        yield return new WaitTime(0.25);

        foreach (var plrRole in new[] { Player.Role.SinglePlayer, Player.Role.LeftPlayer, Player.Role.RightPlayer })
        {
            if (!Player.playersByControl.TryGetValue(plrRole, out Player plr)) continue;
            plr.inputTranslator?.AllowAllInput();
        }

        current = null;
    }
}
