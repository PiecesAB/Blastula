using Blastula.Sounds;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Menus;

/// <summary>
/// A menu that handles the selection of music in the music room;
/// in playing music and scrolling more items than fit on screen (by squashing items out of view), 
/// and more such idiosyncratic functionality.
/// </summary>
public partial class MusicSelectionMenu : ListMenu
{
	[Export] public Node sceneRoot;
	[Export] public PackedScene sampleAvailableMusic;
	[Export] public PackedScene sampleLockedMusic;
	/// <summary>
	/// If music is muted, this UI element is visible, notifying the player.
	/// </summary>
	[Export] Control mutedMessage;
	/// <summary>
	/// This is the max items which can possibly be in view, and the menu squashes items accordingly.
	/// </summary>
	[ExportGroup("SelectionScrollView")][Export] public int maxItemsInView = 13;
	/// <summary>
	/// On selection change, the old music stops. 
	/// The new music begins when the selection is highlighted for this number of frames.
	/// </summary>
	[Export] public int waitFramesUntilPlay = 60;

	//private List<MusicSelectionListNode> existingChildren = new();
	private Music originalMusic = null;

	private int currentFramesHighlighted = 0;

	private Callable onSelectCallable = new();
	private bool onSelectCallableSet = false;

	public void OnSelectListNode()
	{
		MusicSelectionListNode selection = GetSelectionNode();
		MusicMenuOrchestrator.SwapToDetails(selection.music);
		// avoid replaying the music (there are other measures in place but we're being super safe)
		currentFramesHighlighted = waitFramesUntilPlay + 1;
    }

	public void RegenerateChildren()
	{
		onSelectCallable = new Callable(this, MethodName.OnSelectListNode);
		onSelectCallableSet = true;

		foreach (MusicSelectionListNode child in menuNodes) 
		{
			if (child.IsConnected(ListNode.SignalName.SelectAction, onSelectCallable))
				child.Disconnect(ListNode.SignalName.SelectAction, onSelectCallable);
			child.QueueFree();
		}
		menuNodes = new();
		int orderNumber = 1;
		foreach (Music music in MusicManager.main.GetAllMusics())
		{
			if (music.musicRoomDisplay == Music.MusicRoomDisplay.Hidden) continue;
			PackedScene sampleScene = music.IsEncountered() ? sampleAvailableMusic : sampleLockedMusic;
			MusicSelectionListNode newListNode = sampleScene.Instantiate<MusicSelectionListNode>();
			AddChild(newListNode);
			newListNode.SetText(orderNumber.ToString(), music);
			newListNode.music = music;
			newListNode.selectable = true;
			newListNode.Visible = true;
			newListNode.InstantSquash();
			Callable onSelect = new Callable(this, MethodName.OnSelectListNode);
			newListNode.Connect(ListNode.SignalName.SelectAction, onSelect);
			menuNodes.Add(newListNode);
			orderNumber++;
		}
	}

	public override void _Ready()
	{
		originalMusic = MusicManager.main.currentMusic;
		MusicManager.Stop();
		RegenerateChildren();
		InstantChangeView();
		base._Ready();
		Open();
	}

	public override void Close()
	{
		base.Close();
		sceneRoot.QueueFree();
		if (originalMusic != null)
		{
			MusicManager.PlayImmediate(originalMusic.Name);
		}
	}

	public void ForceHighlightSelection()
	{
		menuNodes[selection].Highlight();
	}

	public void InstantChangeView()
	{
		if (menuNodes.Count <= maxItemsInView)
		{
			foreach (MusicSelectionListNode child in menuNodes) 
				child.InstantUnsquash();
			return;
		}

		int midpoint = maxItemsInView / 2;
		int lowestIndex = Mathf.Clamp(selection - midpoint, 0, menuNodes.Count - maxItemsInView);
		for (int i = 0; i < menuNodes.Count; ++i)
		{
			if (i >= lowestIndex && i < lowestIndex + maxItemsInView)
				(menuNodes[i] as MusicSelectionListNode).InstantUnsquash();
			else
                (menuNodes[i] as MusicSelectionListNode).InstantSquash();
		}
	}

	public void ChangeView()
	{
		if (menuNodes.Count <= maxItemsInView)
		{
			foreach (MusicSelectionListNode child in menuNodes)
				child.InstantUnsquash();
			return;
		}

		int midpoint = maxItemsInView / 2;
		int lowestIndex = Mathf.Clamp(selection - midpoint, 0, menuNodes.Count - maxItemsInView);
		for (int i = 0; i < menuNodes.Count; ++i)
		{
			if (i >= lowestIndex && i < lowestIndex + maxItemsInView)
				(menuNodes[i] as MusicSelectionListNode).Unsquash();
			else
				(menuNodes[i] as MusicSelectionListNode).Squash();
		}
	}

	protected override void HighlightChanged(int oldSelection, int newSelection)
	{
		currentFramesHighlighted = 0;
		MusicManager.Stop();
	}

	public MusicSelectionListNode GetSelectionNode()
		=> menuNodes[selection] as MusicSelectionListNode;

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (IsActive())
		{
			ChangeView();
		}
		currentFramesHighlighted++;
		MusicSelectionListNode selectionNode = GetSelectionNode();
		if (currentFramesHighlighted == waitFramesUntilPlay && selectionNode.music.IsEncountered())
		{
			MusicManager.PlayImmediate(selectionNode.music.Name, true, true);
			MusicMenuOrchestrator.ResetLoopMode();
		}

		if (mutedMessage != null)
		{
			mutedMessage.Visible = MusicManager.IsMusicMuted();
		}
	}
}
