using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// A parametric "line segment", in polar coordinates. <br />
    /// This versatile shaper can create rings, spreads, stacks, and more!<br />
    /// (Of course the classic shapes still exist for convenience.)
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/Shapes/scrimble.png")]
    public partial class Scrimble : Shaper
    {
        [Export] public string startRadius = "48";
        [Export] public string endRadius = "96";
        [Export] public string startAngle = "0";
        [Export] public string endAngle = "90";
        /// <summary>
        /// If true, interpolation is contracted so that a circle doesn't have a redundant first point.
        /// </summary>
        [Export] public bool circular = false;

        public override Transform2D GetElementTransform(int i, int totalCount)
        {
            float startRadius = Solve("startRadius").AsSingle();
            float endRadius = Solve("endRadius").AsSingle();
            float startAngle = Solve("startAngle").AsSingle();
            float endAngle = Solve("endAngle").AsSingle();
            if (!circular && totalCount == 1)
            {
                float sr = startAngle * (Mathf.Pi / 180f);
                return new Transform2D(
                    sr,
                    startRadius * new Vector2(Mathf.Cos(sr), Mathf.Sin(sr))
                );
            }
            float progress = (circular) ? (i / (float)totalCount) : (i / (float)(totalCount - 1));
            float rotation = Mathf.Lerp(startAngle, endAngle, progress) * (Mathf.Pi / 180f);
            float radius = Mathf.Lerp(startRadius, endRadius, progress);
            return new Transform2D(
                rotation,
                radius * Vector2.FromAngle(rotation)
            );
        }
    }
}
