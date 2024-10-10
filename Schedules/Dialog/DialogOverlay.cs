using Blastula.Portraits;
using Blastula.VirtualVariables;
using Godot;
using Godot.Collections;
using System;

namespace Blastula;

[Icon(Persistent.NODE_ICON_PATH + "/dialogSeries.png")]
public partial class DialogOverlay : Node
{
    public enum PortraitPosition
    {
        Left, Right
    }

    [Export] public AnimationPlayer animator;
    [Export] public Control leftPortraitHolder;
    [Export] public Control rightPortraitHolder;
    private PortraitController leftPortrait = null;
    private PortraitController rightPortrait = null;
    private string currentLeftPortraitName = null;
    private string currentRightPortraitName = null;

    public static DialogOverlay main { get; private set; }

    public void Activate()
    {
        animator.Play("Activate");
    }

    public void Deactivate()
    {
        animator.Play("Deactivate");
        if (leftPortrait != null)
        {
            leftPortrait.RemoveSpeechBubble();
        }
        if (rightPortrait != null)
        {
            leftPortrait.RemoveSpeechBubble();
        }
    }

    public void ClearAllPortraits()
    {
        SetPortrait(PortraitPosition.Left, null);
        SetPortrait(PortraitPosition.Right, null);
    }

    public void SetPortrait(PortraitPosition position, string portraitEntryNodeName)
    {
        switch (position)
        {
            case PortraitPosition.Left:
                if (currentLeftPortraitName == portraitEntryNodeName) return;
                break;
            case PortraitPosition.Right:
                if (currentRightPortraitName == portraitEntryNodeName) return;
                break;
            default:
                break;
        }

        if (portraitEntryNodeName is not (null or "") && PortraitManager.main.GetPortraitClone(portraitEntryNodeName) is PortraitController pc)
        {
            switch (position)
            {
                case PortraitPosition.Left:
                    currentLeftPortraitName = portraitEntryNodeName;
                    leftPortrait = pc;
                    leftPortraitHolder.AddChild(pc);
                    break;
                case PortraitPosition.Right:
                    currentRightPortraitName = portraitEntryNodeName;
                    rightPortrait = pc;
                    rightPortraitHolder.AddChild(pc);
                    break;
                default:
                    pc.QueueFree();
                    break;
            }
        } else { // nothing
            switch (position)
            {
                case PortraitPosition.Left:
                    if (leftPortrait != null)
                    {
                        leftPortrait.QueueFree();
                        leftPortrait = null;
                    }
                    currentLeftPortraitName = null;
                    break;
                case PortraitPosition.Right:
                    if (rightPortrait != null)
                    {
                        rightPortrait.QueueFree();
                        rightPortrait = null;
                    }
                    currentRightPortraitName = null;
                    break;
                default:
                    break;
            }
        }
    }

    public PortraitController GetPortrait(PortraitPosition position)
    {
        switch (position)
        {
            case PortraitPosition.Left:
                return leftPortrait;
            case PortraitPosition.Right:
                return rightPortrait;
            default:
                return null;
        }
    }

    public void ClearPortraits()
    {
        if (leftPortrait != null)
        {
            leftPortrait.QueueFree();
            leftPortrait = null;
        }
        if (rightPortrait != null)
        {
            rightPortrait.QueueFree();
            rightPortrait = null;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        main = this;
    }
}
