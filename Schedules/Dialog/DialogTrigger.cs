using Blastula.Coroutine;
using Blastula.Input;
using Blastula.Portraits;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections;
using System.Text;

namespace Blastula.Schedules;

[GlobalClass]
[Icon(Persistent.NODE_ICON_PATH + "/dialog.png")]
public partial class DialogTrigger : BaseSchedule
{
    [Export] public DialogOverlay.PortraitPosition portraitPosition = DialogOverlay.PortraitPosition.Left;
    [Export] public bool changePortrait = true;
    [Export] public string portraitEntryNodeName;
    [Export] public string portraitEmotion = null;
    [Export(PropertyHint.MultilineText)] public string speech;
    /// <summary>
    /// Sound 
    /// </summary>
    [ExportGroup("Advanced")]
    [Export] public string openSound = "Menu/TickRight";
    [Export] public string speechBubbleForm;
    [Export] public string speechBubbleOriginId;

    /// <summary>
    /// Use curly braces {varName} to inject a Godot expression value (with Blastula variables allowed) between the braces. Use {{ and }} to escape the curly braces, preventing misinterpretation of text as a variable.
    /// </summary>
    private string InjectExpressions(string preInjectedSpeech)
    {
        bool isInVariableRegion = false;
        StringBuilder varBuild = new StringBuilder();
        StringBuilder mainBuild = new StringBuilder();
        for (int i = 0; i < preInjectedSpeech.Length; i++)
        {
            char curr = preInjectedSpeech[i];
            char? next = i + 1 < preInjectedSpeech.Length ? preInjectedSpeech[i + 1] : null;
            switch (curr)
            {
                case '{':
                    if (next == '{') {
                        if (isInVariableRegion) { varBuild.Append('{'); }
                        else { mainBuild.Append('{'); }
                        i++;
                    } else if (isInVariableRegion) {
                        // Invalid string!! (expression shouldn't contain bare {)
                        return preInjectedSpeech;
                    } else {
                        isInVariableRegion = true;
                        varBuild.Clear();
                    }
                    break;
                case '}':
                    if (next == '}') {
                        if (isInVariableRegion) { varBuild.Append('}'); }
                        else { mainBuild.Append('}'); }
                        i++;
                    } else if (!isInVariableRegion) {
                        // Invalid string!! (Ended expression but it wasn't started)
                        return preInjectedSpeech;
                    } else {
                        isInVariableRegion = false;
                        string varName = varBuild.ToString();
                        Variant varValue = SolveDirect(varName);
                        mainBuild.Append(varValue.ToDisplayString());
                    }
                    break;
                default:
                    if (isInVariableRegion) {
                        varBuild.Append(curr);
                    } else {
                        mainBuild.Append(curr);
                    }
                    break;
            }
        }

        if (isInVariableRegion)
        {
            // You forgot to end the expression (skull emoji)
            return preInjectedSpeech;
        }
        return mainBuild.ToString();
    }

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

        CommonSFXManager.PlayByName(openSound);

        if (changePortrait)
        {
            DialogOverlay.main.SetPortrait(portraitPosition, portraitEntryNodeName);
        }

        PortraitController currPortrait = DialogOverlay.main.GetPortrait(portraitPosition);
        if (currPortrait != null)
        {
            string injectedSpeech = InjectExpressions(speech);
            currPortrait.Speak(injectedSpeech, speechBubbleOriginId, speechBubbleForm);
            if (portraitEmotion is not (null or "")) currPortrait.PlayEmotion(portraitEmotion);
        }

        while (true)
        {
            yield return new WaitOneFrame();
            bool selectPressed = InputManager.ButtonPressedThisFrame("Menu/Select");
            bool pauseHeldSkip = InputManager.ButtonIsDown("Menu/Pause") && FrameCounter.stageFrame % 4 == 0;
            if (selectPressed || pauseHeldSkip) break;
        }

        yield break;
    }
}
