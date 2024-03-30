using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Blastula.Wind
{
	/// <summary>
	/// A source that produces a field of wind to move bullets using WindForth, in some simple shapes and options.
	/// </summary>
	/// <remarks>
	/// In each channel, BaseWindSource items work together additively.
	/// So in the editor, selecting one BaseWindSource will display the field which results from all BaseWindSource in the channel.
	/// </remarks>
	[GlobalClass, Tool]
	[Icon(Persistent.NODE_ICON_PATH + "/wind.png")]
	public unsafe partial class StandardWindSource : BaseWindSource
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

        [Export] public Shape shape = Shape.Point;
		/// <summary>
		/// The rolloff and strength curve for the wind effect. For Uniform shape, only the value at x-position 0 is relevant.
		/// </summary>
        /// <remarks>
        /// The x-axis represents the ratio of the sample distance from the object to the radius variable.
        /// The y-axis represents the acceleration strength of the field in Godot units per second^2.
        /// </remarks>
		[Export] public Curve strength;
		/// <summary>
		/// For non-Uniform shapes, the distance that the effect reaches. Scales with the Node2D.
		/// </summary>
		[Export] public float radius = 200;
		/// <summary>
		/// Additional angle in degrees. The force at every position in the field will be rotated.
		/// </summary>
		[Export] public float differentialRotation = 0;

		private struct OtherData
		{
			public Shape shape;
			public float radius;
			public float differentialRotation;
            public UnsafeCurve* strength;
		}

		private OtherData* otherData = null;
		private UnsafeCurve* strengthLowLevel = null;

        public static Vector2 SampleForce(Transform2D globalTransform, void* otherData, Vector2 sampleGlobalPosition)
        {
            if (globalTransform.Scale.X == 0 || globalTransform.Scale.Y == 0) { return Vector2.Zero; }
            OtherData* data = (OtherData*)otherData;
            if (data->strength == null) { return Vector2.Zero; }
            // Simplified global to local calculations to improve performance
            Vector2 sampleLocalPosition = (sampleGlobalPosition - globalTransform.Origin).Rotated(-globalTransform.Rotation) / globalTransform.Scale;
            Vector2 localForce = Vector2.Zero;
            float distance = 0;
            switch (data->shape)
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
            if (distance > data->radius) { distance = data->radius; }
            localForce *= data->strength->Evaluate(distance / data->radius);
            return localForce.Rotated(Mathf.DegToRad(data->differentialRotation) + globalTransform.Rotation);
        }

        protected override void AddSelf()
        {
            base.AddSelf();
            if (myNode != null)
            {
				if (otherData == null)
				{
					otherData = (OtherData*)Marshal.AllocHGlobal(sizeof(OtherData));
					if (strengthLowLevel == null)
					{
						strengthLowLevel = UnsafeCurveFunctions.Create(strength, 0, 1, UnsafeCurve.LoopMode.Neither, 0.01f);
					}
					*otherData = new OtherData
					{
						shape = shape,
						radius = radius,
						differentialRotation = differentialRotation,
						strength = strengthLowLevel
					};
					myNode->data = new WindFieldData { otherData = otherData, sampleForce = &SampleForce, transform = GlobalTransform };
				}
            }
        }

        protected override void RemoveSelf()
        {
			if (myNode != null)
			{
                if (strengthLowLevel != null)
                {
                    strengthLowLevel->Dispose();
                    Marshal.FreeHGlobal((IntPtr)strengthLowLevel);
                    strengthLowLevel = null;
                }
                
				if (otherData != null)
				{
					Marshal.FreeHGlobal((IntPtr)otherData);
					otherData = null;
				}
			}
            base.RemoveSelf();
        }

        public override void _EnterTree()
		{
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
            base._EnterTree();
		}

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (otherData != null)
            {
                *otherData = new OtherData
                {
                    shape = shape,
                    radius = radius,
                    differentialRotation = differentialRotation,
                    strength = strengthLowLevel
                };
            }
        }

#if TOOLS
        public override void _Draw()
        {
			if (!Engine.IsEditorHint()) { return; }
            switch (shape)
            {
                case Shape.Point: DrawCircle(Vector2.Zero, 10f, Colors.White); break;
                case Shape.Streak: DrawLine(-10000 * Vector2.Right, 10000 * Vector2.Right, Colors.White, 10f); break;
            }
            base._Draw();
        }
#endif
    }
}
