using Blastula.Coroutine;
using Blastula.Input;
using Blastula.Portraits;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections;

namespace Blastula.Schedules;

[GlobalClass]
[Icon(Persistent.NODE_ICON_PATH + "/dialog.png")]
public partial class DialogTrigger : BaseSchedule
{
    [Export] public DialogOverlay.PortraitPosition portraitPosition = DialogOverlay.PortraitPosition.Left;
    [Export] public bool changePortrait = true;
    [Export] public string portraitEntryNodeName;
    [Export] public string portraitEmotion = null;
    [Export(PropertyHint.MultilineText)] public string speech; // TODO: regex for funny variables.
    [ExportGroup("Advanced")]
    [Export] public string speechBubbleForm;
    [Export] public string speechBubbleOriginId;

    public override IEnumerator Execute(IVariableContainer source)
    {
        if (DialogSeries.GetSeries(this) is not DialogSeries series)
        {
            GD.PushError("You're trying to start dialog outside of a DialogSeries. I don't think that makes sense.");
            yield break;
        }

        if (DialogOverlay.main == null)
        {
            GD.PushWarning("You're trying to trigger dialog, but there's no DialogOverlay.");
            yield break;
        }

        if (changePortrait)
        {
            DialogOverlay.main.SetPortrait(portraitPosition, portraitEntryNodeName);
        }

        PortraitController currPortrait = DialogOverlay.main.GetPortrait(portraitPosition);
        if (currPortrait != null)
        {
            currPortrait.Speak(speech, speechBubbleOriginId, speechBubbleForm);
            if (portraitEmotion is not (null or "")) currPortrait.PlayEmotion(portraitEmotion);
        }

        while (true)
        {
            yield return new WaitOneFrame();
            if (InputManager.ButtonPressedThisFrame("Menu/Select")) break;
        }

        yield break;
    }
}
