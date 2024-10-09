using Godot;
using Godot.Collections;
using System;

namespace Blastula.Graphics;

[Tool]
public partial class SpeechBubble : Control
{
	/// <summary>
	/// This is the local position (relative to the dialog holder) which the "arrow" of the speech bubble will point to,
	/// as if the speech had originated there.
	/// </summary>
	[Export] public Vector2 originPoint;
	[Export] public Control arrow;
	[Export] public RichTextLabel textLabel;
	[Export] public Container textLabelVContainer;
	[Export] public Vector2 textMargin;
	[Export] public float arrowOuterMargin;
	[Export] public float arrowInnerMargin;
	[Export] public Vector2[] possibleSpeechBubbleSizes = new Vector2[]
	{
		new Vector2(128, 96),
		new Vector2(192, 96),
		new Vector2(256, 96),
		new Vector2(320, 96),
		new Vector2(384, 96),
		new Vector2(416, 112),
		new Vector2(448, 128),
		new Vector2(480, 144),
		new Vector2(512, 160),
		new Vector2(528, 192),
		new Vector2(544, 224),
		new Vector2(560, 256),
		new Vector2(576, 256),
		new Vector2(640, 256),
		new Vector2(680, 320),
	};

	[Export] public AnimationPlayer bubbleFormAnimator;

	private Vector2 currentSize;

	private void AdjustSizeToText()
	{
		if (textLabel == null || textLabelVContainer == null) return;
		if (!textLabel.Text.StartsWith("[center]")) textLabel.Text = "[center]" + textLabel.Text;
		string oldText = textLabel.Text;
		textLabel.Text = "";
		textLabel.Size = textLabel.CustomMinimumSize = new Vector2(0, 0);
		textLabelVContainer.Size = textLabelVContainer.CustomMinimumSize = new Vector2(0, 0);
		textLabel.Text = oldText;
		// Choose the smallest size that fits the text, but also avoid oscillation due to a horizontal change.
		for (int i = 0; i < possibleSpeechBubbleSizes.Length; ++i)
		{
			Vector2 testSize = possibleSpeechBubbleSizes[i];
			Size = testSize;
			textLabel.Size = textLabel.CustomMinimumSize = testSize - 2f * textMargin;
			textLabelVContainer.Size = textLabelVContainer.CustomMinimumSize = testSize - 2f * textMargin;
			if (textLabelVContainer.Size == textLabelVContainer.CustomMinimumSize) break;
		}
		textLabel.Size = textLabel.CustomMinimumSize = new Vector2(textLabel.Size.X, 0);
	}

	private Control parentContainer;

	private void ClampBubbleToRegion()
	{
		if (parentContainer == null) { parentContainer = (GetParent() is Control c) ? c : null; }
		if (parentContainer == null) { return; }

		Position = Position.Clamp(Vector2.Zero, new Vector2(parentContainer.Size.X - Size.X, parentContainer.Size.Y - Size.Y));
	}

	private void AdjustArrow()
	{
		if (arrowInnerMargin <= 0 || arrowOuterMargin <= 0) return;
		Vector2 arrowTarget = originPoint - Position;
		arrow.Position = arrowTarget;
		Vector2 oldPos = arrow.Position;
		Vector2 clampedOuter = arrow.Position.Clamp(-arrowOuterMargin * Vector2.One, arrowOuterMargin * Vector2.One + Size);
		arrow.Position = clampedOuter;
		arrow.Scale = (oldPos != clampedOuter) ? Vector2.One : Vector2.Zero;
		Vector2 innerFakePos = arrow.Position.Clamp(Vector2.Zero + arrowInnerMargin * Vector2.One, Size - arrowInnerMargin * Vector2.One);
		float corneredAngle = (arrow.Position - innerFakePos).Angle() - 0.5f * Mathf.Pi;
		arrow.Rotation = corneredAngle;
	}

	public void RemoveSelf()
	{
		Visible = false;
		QueueFree();
	}

	public void SetUpFromPortrait(Vector2 targetOriginPosition, Vector2 direction, string text, string form = null)
	{
		textLabel.Text = text;
		AdjustSizeToText();
		// ClampBubbleToRegion();
		Position = targetOriginPosition;
		originPoint = -(Size.X + Size.Y) * direction + Position + 0.5f * Size;
		AdjustArrow();
		// Now put the arrow on the origin
		Position -= arrow.Position;
		if (form != null) bubbleFormAnimator.Play(form);
	}

	public override void _Ready()
	{
		base._Ready();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Engine.IsEditorHint())
		{
			AdjustSizeToText();
			//ClampBubbleToRegion();
			AdjustArrow();
		}
	}
}
