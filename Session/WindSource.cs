using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula
{
	/// <summary>
	/// A source that produces a field of wind to move bullets using WindForth.
	/// </summary>
	/// <remarks>
	/// In each channel, WindSource items work together additively.
	/// So in the editor, selecting one WindSource will display the field which results from all WindSource in the channel.
	/// </remarks>
	[GlobalClass, Tool]
	[Icon(Persistent.NODE_ICON_PATH + "/wind.png")]
	public partial class WindSource : Node2D
	{
		public enum Shape
		{
            /// <summary>
            /// The wind field is applied everywhere, rightwards (note that the Node2D can be rotated).
            /// </summary>
            Uniform, 
			/// <summary>
			/// The wind field is nonzero near a horizontal line (note that the Node2D can be rotated).
			/// </summary>
			Streak, 
			/// <summary>
			/// The wind field is nonzero around a point.
			/// </summary>
			Point
		}

        /// <summary>
        /// Which channel this wind source is on. WindForth only responds to one channel at a time.
        /// </summary>
        [Export] public string channel = "Gravity";
		private string channelEditorStored = "";
        [Export] public Shape shape = Shape.Point;
		/// <summary>
		/// The rolloff and strength curve for the wind effect. For Uniform shape, only the value at x-position 0 is relevant.
		/// </summary>
		[Export] public Curve strength;
		/// <summary>
		/// For non-Uniform shapes, the distance that the effect reaches. Scales with the Node2D.
		/// </summary>
		[Export] public float radius = 200;
		/// <summary>
		/// Additional angle in degrees. The force at every position in the field will be rotated.
		/// </summary>
		[Export] public float differentialRotation = 0;

		public static Dictionary<string, HashSet<WindSource>> allInEditor = new Dictionary<string, HashSet<WindSource>>();

		public Vector2 SampleForceEditor(Vector2 sampleGlobalPosition)
		{
			// Simplified global to local calculations to improve performance
			if (GlobalScale.X == 0 || GlobalScale.Y == 0) { return Vector2.Zero; }
			Vector2 sampleLocalPosition = (sampleGlobalPosition - GlobalPosition).Rotated(-GlobalRotation) / GlobalScale;
            Vector2 localForce = Vector2.Zero;
            float distance = 0;
			switch (shape)
			{
				case Shape.Uniform:
					distance = 0;
					localForce = Vector2.Right;
					break;
				case Shape.Streak: 
					distance = Mathf.Abs(sampleLocalPosition.Y);
					localForce = -Mathf.Sign(sampleLocalPosition.Y) * Vector2.Up;
					break;
				case Shape.Point: 
					distance = sampleLocalPosition.Length();
					localForce = sampleLocalPosition.Normalized();
					break;
			}
			if (distance > radius) { distance = radius; }
			localForce *= strength.SampleBaked(distance / radius);
			return localForce.Rotated(Mathf.DegToRad(differentialRotation) + GlobalRotation);
		}

		public static Vector2 SampleForceEditor(string channel, Vector2 sampleGlobalPosition)
		{
			if (!allInEditor.ContainsKey(channel)) { return Vector2.Zero; }
			Vector2 total = Vector2.Zero;
			foreach (WindSource source in allInEditor[channel])
			{
				total += source.SampleForceEditor(sampleGlobalPosition);
			}
			return total;
		}

		private void AddSelf()
		{
			if (Engine.IsEditorHint())
			{
				if (!allInEditor.ContainsKey(channel)) { allInEditor[channel] = new HashSet<WindSource>(); }
				allInEditor[channel].Add(this);
				channelEditorStored = channel;
			}
		}

		private void RemoveSelf()
		{
			if (Engine.IsEditorHint())
			{
				if (allInEditor.ContainsKey(channel)) { allInEditor[channel].Remove(this); }
			}
		}

		public override void _EnterTree()
		{
			base._EnterTree();
			AddSelf();
			if (Engine.IsEditorHint())
			{
				if (strength == null)
				{
					strength = new Curve();
					strength.MaxValue = 200;
					strength.AddPoint(new Vector2(0, 200));
					strength.AddPoint(new Vector2(1, 0));
				}
            }
		}

        public override void _ExitTree()
        {
            base._ExitTree();
			RemoveSelf();
        }

        public override void _Process(double delta)
		{
			if (Engine.IsEditorHint())
			{
				if (Get("channel").AsString() != channelEditorStored)
				{
                    if (allInEditor.ContainsKey(channelEditorStored)) { allInEditor[channelEditorStored].Remove(this); }
					channel = Get("channel").AsString();
					AddSelf();
				}
				QueueRedraw();
			}
		}

#if TOOLS
		private const float ARROW_SPACING = 64;
		private const float ARROW_FORCE_MULTIPLIER = 0.15f;
		public override void _Draw()
        {
            base._Draw();
            switch (shape)
            {
                case Shape.Point: DrawCircle(Vector2.Zero, 10f, Colors.White); break;
                case Shape.Streak: DrawLine(-10000 * Vector2.Right, 10000 * Vector2.Right, Colors.White, 10f); break;
            }
            if ((BlastulaPlugin.selection?.GetInstanceId() ?? ulong.MaxValue) == GetInstanceId())
			{
                foreach (MainBoundary boundary in MainBoundary.boundPerMode)
                {
					if (boundary == null) { continue; }
					Vector2 boundarySize = boundary.CalculateSize();
                    (float minX, float maxX, float minY, float maxY) = (
						boundary.GlobalPosition.X - 0.5f * boundarySize.X,
                        boundary.GlobalPosition.X + 0.5f * boundarySize.X,
                        boundary.GlobalPosition.Y - 0.5f * boundarySize.Y,
                        boundary.GlobalPosition.Y + 0.5f * boundarySize.Y
                    );
					(minX, maxX, minY, maxY) = (
						Mathf.Ceil(minX / ARROW_SPACING) * ARROW_SPACING,
						Mathf.Floor(maxX / ARROW_SPACING) * ARROW_SPACING,
                        Mathf.Ceil(minY / ARROW_SPACING) * ARROW_SPACING,
                        Mathf.Floor(maxY / ARROW_SPACING) * ARROW_SPACING
                    );
                    DrawSetTransformMatrix(GlobalTransform.AffineInverse());
                    for (float x = minX; x < maxX + 0.01f; x += ARROW_SPACING)
					{
                        for (float y = minY; y < maxY + 0.01f; y += ARROW_SPACING)
                        {
							Vector2 pos = new Vector2(x, y);
							DrawCircle(pos, 4f, Colors.Gray);
							Vector2 force = SampleForceEditor(Get("channel").AsString(), pos);
							DrawLine(pos, pos + ARROW_FORCE_MULTIPLIER * force, Colors.Gray, 2);
                        }
                    }
                }
            }
        }
#endif
    }
}
