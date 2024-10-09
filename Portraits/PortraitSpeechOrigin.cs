using Godot;
using System;

namespace Blastula.Portraits;

/// <summary>
/// A point at which the portrait will appear to speak from (a speech bubble will be placed there).
/// </summary>
[Tool]
public partial class PortraitSpeechOrigin : Node2D
{
    // Reference ID within the PortraitController to access this point.
    [Export] public string referenceId = "Main";
    // The speech bubble's body is placed in this direction from the origin.
    [Export] public Vector2 direction = new Vector2(1, -1);

    public override void _Ready()
    {
        base._Ready();
        ProcessMode = Engine.IsEditorHint() ? ProcessModeEnum.Always : ProcessModeEnum.Disabled;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Engine.IsEditorHint())
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        base._Draw();
        if (!Engine.IsEditorHint()) { return; }
        DrawCircle(Vector2.Zero, 4f, Colors.White);
        DrawLine(Vector2.Zero, 12 * direction.Normalized(), Colors.White, 2f);
    }
}
