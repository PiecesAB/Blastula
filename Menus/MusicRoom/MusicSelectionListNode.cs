using Blastula.Sounds;
using Godot;
using System;

namespace Blastula.Menus;

public partial class MusicSelectionListNode : ListNode
{
	[Export] public AnimationPlayer squashAnimator;
	[Export] public RichTextLabel mainLabel;
	[Export] public RichTextLabel highlightLabel;

	public Music music = null;

	[Export(PropertyHint.MultilineText)] public string template = "[i][font_size=24]{order}.[/font_size][/i] {title}";

	private bool? squashed = null;

	public void SetText(string order, Music music)
	{
		string storedText = template.Replace("{order}", order).Replace("{title}", music.title);
		mainLabel.Text = highlightLabel.Text = storedText;
	}

	public void PlayCommonSFX(string sfxName)
	{
		CommonSFXManager.StopByName(sfxName);
		CommonSFXManager.PlayByName(sfxName);
	}

	public void InstantUnsquash()
	{
		if (squashed == false) return;
		squashed = false;
		squashAnimator.Play("FullHeight", 0);
	}

	public void Unsquash()
	{
		if (squashed == false) return;
		squashed = false;
		squashAnimator.Play("FullHeight");
	}

	public void InstantSquash()
	{
		if (squashed == true) return;
		squashed = true;
		squashAnimator.Play("Squashed", 0);
	}

	public void Squash()
	{
		if (squashed == true) return;
		squashed = true;
		squashAnimator.Play("Squashed");
	}
}
