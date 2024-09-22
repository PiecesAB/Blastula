using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using Blastula.Coroutine;
using System.Collections;

namespace Blastula.Menus;

/// <summary>
/// Handles populating the scoreboard and entering the name by keyboard actions.
/// Uses an idiosyncratic visualization that connects the characters with a line. 
/// </summary>
public partial class ScoreboardEntryHandler : Node
{
	[Export] public StatsSaveReplayMenu mainMenu;
	[Export] public KeyboardMenu keyboard;
	[Export] public Font nameLabelFont;
	[Export] public float nameLabelFontSizeMultiplier = 2;
	[Export] public float nameLabelFontSizeAdvancement = 32;
	/// <summary>
	/// A list of ten rows that are populated.
	/// </summary>
	[Export] public Control[] leaderboardRowControls;
	private Label currentNameLabel;
	[Export] public int maxNameLength = 12;

	[Export] public Line2D connectionLine;
	[Export] public Sprite2D connectionStart;
	[Export] public Sprite2D connectionEnd;
	[Export] public AnimationPlayer joltAnimator;
	[Export] public string submitSound;

	private List<ScoresLoader.Row> topElements = new();
	ScoresLoader.Row newRow;
	private bool newScoreIsNotTopTen = false;

	private int caretLetterPosition;

	private void SetUpRows()
	{
		for (int i = 0; i < 10; ++i)
		{
			if (i >= topElements.Count) break;
			Control con = leaderboardRowControls[i];
			con.Modulate *= 0.9f;
			ScoresLoader.Row row = topElements[i];
			(con.FindChild("#").FindChild("Label") as Label).Text = (i + 1).ToString();
			(con.FindChild("Name").FindChild("Label") as Label).Text = row.name;
			(con.FindChild("Score").FindChild("Label") as Label).Text = row.finalScore.ToString();
			(con.FindChild("EndStage").FindChild("Label") as Label).Text = row.stageAtEnd.ToString();
			(con.FindChild("Miss").FindChild("Label") as Label).Text = row.miss.ToString();
			(con.FindChild("Bomb").FindChild("Label") as Label).Text = row.bomb.ToString();
		}
	}

	public override void _Ready()
	{
		topElements = ScoresLoader.main.LoadRows();
		topElements.Sort(new ScoresLoader.RowSorter() { sortingMode = ScoresLoader.RowSorter.SortingMode.Score });
		if (topElements.Count > 10) topElements = topElements.GetRange(0, 10);
		newRow = new() { finalScore = Session.main.score, name = "", bomb = -1, miss = -1, stageAtEnd = "TODO", sessionDirectoryName = ReplayManager.main.sessionDirectoryName };
		// Find the position of the new element
		topElements.Add(newRow);
		topElements.Sort(new ScoresLoader.RowSorter() { sortingMode = ScoresLoader.RowSorter.SortingMode.Score });
		int myPosition = topElements.IndexOf(newRow);
		if (myPosition == -1) throw new Exception("...what.");
		if (myPosition == 10)
		{
			newScoreIsNotTopTen = true;
			topElements.RemoveAt(9);
			myPosition--;
		}
		SetUpRows();
		leaderboardRowControls[myPosition].Modulate *= 1.15f;
		if (newScoreIsNotTopTen)
		{
			(leaderboardRowControls[myPosition].FindChild("#").FindChild("Label") as Label).Text = "-";
		}
		currentNameLabel = leaderboardRowControls[myPosition].FindChild("Name").FindChild("Label") as Label;
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

	public IEnumerator BackToMainMenu()
	{
		CommonSFXManager.PlayByName(submitSound);
		keyboard.inputEnabled = false;
		yield return new WaitFrames(20);
		newRow.name = currentNameLabel.Text;
		ScoresLoader.main.SaveRow(newRow);
		mainMenu.CloseWithoutErase();
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
					this.StartCoroutine(BackToMainMenu());
					break;
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
