using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Places clones in a random sector of a washer: 
    /// a region bounded by two lines through the center and two circles around the center.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/Shapes/random.png")]
    public partial class PlaceRandom : Shaper
    {
        [Export] public string minRadius = "48";
        [Export] public string maxRadius = "96";
        [Export] public string minAngle = "0";
        [Export] public string maxAngle = "90";
        /// <summary>
        /// If true, points at smaller radii will have a lower chance of being chosen,
        /// such that there is a uniform chance of bullets spawning throughout the area.
        /// </summary>
        [Export] public bool correctForArea = true;

        private float CorrectedRandomRadius(float minRadius, float maxRadius)
        {
            if (minRadius == maxRadius) { return RNG.Single(); }
            float lower = minRadius; float higher = maxRadius;
            if (lower > higher) { (lower, higher) = (higher, lower); }
            float r = lower / higher; // ratio is in [0, 1)
            return 1 - (1 / (1 - r)) + (1 / (1 - r)) * Mathf.Sqrt(r * r + (1 - r * r) * RNG.Single()); // dear god.
        }

        public override Transform2D GetElementTransform(int i, int totalCount)
        {
            float minRadius = Solve("minRadius").AsSingle();
            float maxRadius = Solve("maxRadius").AsSingle();
            float minAngle = Solve("minAngle").AsSingle();
            float maxAngle = Solve("maxAngle").AsSingle();
            float rotation = Mathf.Lerp(minAngle, maxAngle, RNG.Single()) * (Mathf.Pi / 180f);
            float radius = Mathf.Lerp(minRadius, maxRadius,
                correctForArea ? CorrectedRandomRadius(minRadius, maxRadius) : RNG.Single()
            );
            return new Transform2D(
                rotation,
                radius * new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation))
            );
        }
    }
}
