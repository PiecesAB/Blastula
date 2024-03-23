using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// <b>Places</b> a row of bullets with the same direction from the center, but different distance from the center.
    /// </summary>
    /// <remarks>
    /// There is emphasis on <b>places</b> because shapers only position and rotate structures. They don't create movement behavior.
    /// You can use a ForthByPosition operation to convert position + rotation to movement.
    /// </remarks>
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
