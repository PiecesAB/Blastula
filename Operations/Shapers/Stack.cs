using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// A row of bullets with the same direction from the center, but different distance from the center.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/Shapes/stack.png")]
    public partial class Stack : Shaper
    {
        [Export] public string startRadius = "48";
        [Export] public string endRadius = "96";
        [Export] public string angle = "0";

        public override Transform2D GetElementTransform(int i, int totalCount)
        {
            float startRadius = Solve("startRadius").AsSingle();
            float endRadius = Solve("endRadius").AsSingle();
            float angle = Solve("angle").AsSingle() * (Mathf.Pi / 180f);
            float progress = i / (float)Mathf.Max(1, totalCount - 1);
            float radius = Mathf.Lerp(startRadius, endRadius, progress);
            return new Transform2D(angle, radius * Vector2.FromAngle(angle));
        }
    }
}
