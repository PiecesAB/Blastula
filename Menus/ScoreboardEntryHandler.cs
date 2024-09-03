using Godot;
using System;

namespace Blastula.Menus;

/// <summary>
/// Handles populating the scoreboard and entering the name by keyboard actions.
/// Uses an idiosyncratic visualization that connects the characters with a line. 
/// </summary>
public partial class ScoreboardEntryHandler : Node
{
	[Export] public KeyboardMenu keyboard;
	[Export] public Font nameLabelFont;
    [Export] public float nameLabelFontSizeMultiplier = 2;
    [Export] public float nameLabelFontSizeAdvancement = 32;
    [Export] public Label currentNameLabel;
    [Export] public int maxNameLength = 12;

    [Export] public Line2D connectionLine;
	[Export] public Sprite2D connectionStart;
    [Export] public Sprite2D connectionEnd;
	[Export] public AnimationPlayer joltAnimator;

	private int caretLetterPosition;

	public override void _Ready()
	{
		caretLetterPosition = 0;
		currentNameLabel.Text = "";
	}

	private string LabelBeforeCaret() => currentNameLabel.Text.Substring(0, Math.Min(caretLetterPosition, currentNameLabel.Text.Length));

	private float GetStringSizeX()
		=> nameLabelFont.GetStringSize(LabelBeforeCaret()).X * nameLabelFontSizeMultiplier + nameLabelFontSizeAdvancement;

	private void TypeChar(char c)
	{
		if (caretLetterPosition >= currentNameLabel.Text.Length)
		{
			currentNameLabel.Text += c;
			caretLetterPosition = currentNameLabel.Text.Length;
		}
		else if (caretLetterPosition >= 0)
		{
			currentNameLabel.Text = currentNameLabel.Text.Substring(0, caretLetterPosition) + c + currentNameLabel.Text.Substring(caretLetterPosition + 1);
			caretLetterPosition++;
		}

		if (caretLetterPosition >= maxNameLength) caretLetterPosition = maxNameLength - 1;
	}

	private void RemoveChar()
	{
		if (currentNameLabel.Text == "") return;
        else if (caretLetterPosition >= currentNameLabel.Text.Length)
        {
			currentNameLabel.Text = currentNameLabel.Text.Substring(0, currentNameLabel.Text.Length - 1);
            caretLetterPosition--;
        }
        else if (caretLetterPosition >= 0)
        {
			currentNameLabel.Text = currentNameLabel.Text.Substring(0, caretLetterPosition) + currentNameLabel.Text.Substring(caretLetterPosition + 1);
        }
    }

	public void OnTypeSymbol(string symbol)
	{
        joltAnimator.Stop();
        joltAnimator.Play("Jolt");
		if (symbol.Length == 1)
		{
			TypeChar(symbol[0]);
		}
		else
		{
			switch (symbol)
			{
				case "left":
                    caretLetterPosition = Math.Max(caretLetterPosition - 1, 0);
					break;
				case "right":
                    caretLetterPosition = Math.Min(caretLetterPosition + 1, currentNameLabel.Text.Length);
                    if (caretLetterPosition >= maxNameLength) caretLetterPosition = maxNameLength - 1;
					break;
				case "del":
					RemoveChar();
					break;
				case "clr":
					currentNameLabel.Text = "";
					caretLetterPosition = 0;
					break;
				case "submit":
					throw new NotImplementedException();
            }
		}
	}

	public override void _Process(double delta)
	{
		if (keyboard == null || !keyboard.IsActive()) return;
        ProcessPriority = keyboard.ProcessPriority + 1;
		var targetStart = keyboard.GetCurrentSelectPosition();
        var targetEnd = currentNameLabel.GlobalPosition + new Vector2(GetStringSizeX(), 0.5f * currentNameLabel.Size.Y);
		connectionStart.GlobalPosition = targetStart;
        connectionEnd.GlobalPosition = targetEnd;
        connectionLine.Points = new[] { connectionStart.GlobalPosition, connectionEnd.GlobalPosition };
	}
}
