using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// An extra-versatile shaper that can place clones along any parametric polar graph.<br />
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/Shapes/scrimble.png")]
    public partial class CurveScrimble : Shaper
    {
        /// <summary>
        /// How the radius varies over the course of this shape.
        /// </summary>
        [Export] public Curve radius;
        /// <summary>
        /// How the angle varies over the course of this shape. The output is in degrees.
        /// </summary>
        [Export] public Curve angle;
        /// <summary>
        /// If true, interpolation is contracted so that a circle doesn't have a redundant first point.
        /// </summary>
        [Export] public bool circular = false;
        /// <summary>
        /// An array of four numbers [a, b, c, d] that stretches or shifts the radius curve.
        /// Suppose that the radius curve is a function F. Normally, we are calculating F(t) to determine the radius.
        /// If this is defined, we will instead determine the radius as cF(at + b) + d.
        /// </summary>
        [ExportGroup("Advanced")]
        [Export] public string radiusShift = "";
        [Export] public UnsafeCurve.LoopMode radiusLoopMode = UnsafeCurve.LoopMode.Neither;
        /// <summary>
        /// An array of four numbers [a, b, c, d] that stretches the angle curve.
        /// Suppose that the angle curve is a function F. Normally, we are calculating F(t) to determine the angle.
        /// If this is defined, we will instead determine the angle as cF(at + b) + d.
        /// </summary>
        [Export] public string angleShift = "";
        [Export] public UnsafeCurve.LoopMode angleLoopMode = UnsafeCurve.LoopMode.Neither;

        private float GetAngle(float t)
        {
            Vector4 shift = new Vector4(1, 0, 1, 0);
            if (angleShift != null && angleShift != "")
            {
                float[] a = Solve("angleShift").AsFloat32Array();
                shift = new Vector4(a[0], a[1], a[2], a[3]);
            }
            if (t < 0 && (angleLoopMode & UnsafeCurve.LoopMode.Left) != 0) { t = MoreMath.RDMod(t, 1f); }
            if (t >= 1 && (angleLoopMode & UnsafeCurve.LoopMode.Right) != 0) { t = MoreMath.RDMod(t, 1f); }
            return shift[2] * angle.Sample(shift[0] * t + shift[1]) + shift[3];
        }

        private float GetRadius(float t)
        {
            Vector4 shift = new Vector4(1, 0, 1, 0);
            if (radiusShift != null && radiusShift != "")
            {
                float[] a = Solve("radiusShift").AsFloat32Array();
                shift = new Vector4(a[0], a[1], a[2], a[3]);
            }
            if (t < 0 && (radiusLoopMode & UnsafeCurve.LoopMode.Left) != 0) { t = MoreMath.RDMod(t, 1f); }
            if (t >= 1 && (radiusLoopMode & UnsafeCurve.LoopMode.Right) != 0) { t = MoreMath.RDMod(t, 1f); }
            return shift[2] * radius.Sample(shift[0] * t + shift[1]) + shift[3];
        }

        public override Transform2D GetElementTransform(int i, int totalCount)
        {
            if (!circular && totalCount == 1)
            {
                float sr = GetAngle(0) * (Mathf.Pi / 180f);
                return new Transform2D(
                    sr,
                    GetRadius(0) * new Vector2(Mathf.Cos(sr), Mathf.Sin(sr))
                );
            }
            float progress = (circular) ? (i / (float)totalCount) : (i / (float)(totalCount - 1));
            float rotation = GetAngle(progress) * (Mathf.Pi / 180f);
            return new Transform2D(
                rotation,
                GetRadius(progress) * new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation))
            );
        }
    }
}
