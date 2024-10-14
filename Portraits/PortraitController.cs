using Godot;
using System;
using Blastula.Coroutine;
using System.Collections.Generic;
using System.Collections;
using Blastula.VirtualVariables;
using Blastula.Graphics;
using Blastula.Schedules;

namespace Blastula.Portraits;

/// <summary>
/// Place this on the root node of a portrait scene. 
/// It will handle the portrait lifespan and referencing, and maybe other things soon.
/// </summary>
[GlobalClass]
[Icon(Persistent.NODE_ICON_PATH + "/portrait.png")]
public partial class PortraitController : Control
{
	[Export] public AnimationPlayer emotionAnimator;
	[Export] public PackedScene speechBubbleSample;

	/// <summary>
	/// A semantic ID that describes how the portrait is currently being used by other scripts.
	/// </summary>
	private string usageId = null;
	private static Dictionary<string, PortraitController> all = new();

	private CoroutineUtility.Coroutine currentLifespan = null;

	private Dictionary<string, PortraitSpeechOrigin> speechOrigins = new();
	private PortraitSpeechOrigin defaultSpeechOrigin = null;

	public override void _Ready()
	{
		base._Ready();
		Stack<Node> descendants = new();
		foreach (Node child in GetChildren())
			descendants.Push(child);
		while (descendants.Count > 0)
		{
			Node d = descendants.Pop();
			if (d is PortraitSpeechOrigin dso)
			{
				if (defaultSpeechOrigin == null)
					defaultSpeechOrigin = dso;
				if (speechOrigins.ContainsKey(dso.referenceId))
					GD.PushWarning($"Two PortraitSpeechOrigin were found with duplicated reference ID \"{dso.referenceId}\", the later node will be used.");
				speechOrigins[dso.referenceId] = dso;
			}
			foreach (Node child in d.GetChildren())
				descendants.Push(child);
		}
	}

	public static PortraitController FindByUsageId(string usageId)
	{
		if (all.ContainsKey(usageId)) { return all[usageId]; }
		return null;
	}

	public void Speak(string text, string positionReferenceId = null, string bubbleForm = null)
	{
		var newSpeechBubble = speechBubbleSample?.Instantiate<SpeechBubble>();
		DialogSeries.SetSingleSpeechBubble(newSpeechBubble);
		if (newSpeechBubble == null) return;

		AddChild(newSpeechBubble);

		Vector2 targetOriginPosition = defaultSpeechOrigin.Position;
		Vector2 direction = defaultSpeechOrigin.direction;
		if (positionReferenceId is not (null or "") && speechOrigins.TryGetValue(positionReferenceId, out PortraitSpeechOrigin chosenOrigin))
		{
			targetOriginPosition = chosenOrigin.Position;
			direction = chosenOrigin.direction;
		}
		newSpeechBubble.SetUpFromPortrait(targetOriginPosition, direction, text, bubbleForm);
	}

	public string GetReferenceId()
	{
		return usageId;
	}

	public void SetReferenceId(string id)
	{
		if (all.ContainsKey(id)) { 
			all[id].QueueFree();
		}
		all[id] = this;
		usageId = id;
	}

	public void PlayEmotion(string emotion)
	{
		if (emotion is null or "") return;
		if (emotionAnimator == null) return;
		emotionAnimator.Play(emotion);
	}

	private IEnumerator ElapseLifespan(double lifespan)
	{
		yield return new WaitTime(lifespan);
		QueueFree();
	}

	public void SetLifespan(double lifespan)
	{
		if (currentLifespan != null)
		{
			currentLifespan.ManualCancel();
		}
		currentLifespan = this.StartCoroutine(ElapseLifespan(lifespan));
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (usageId != null && all.ContainsKey(usageId))
		{
			all.Remove(usageId);
		}
	}

	public static void ClearByReferenceId(string id)
	{
		if (all.ContainsKey(id))
		{
			all[id].QueueFree();
		}
	}
}
