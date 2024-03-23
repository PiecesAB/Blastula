using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Also known as a ring. Place a circle of clones of the bullet structure.
    /// </summary>
    /// <example>
    /// To shoot a circle that expands from a central point, add the Forth behavior first,
    /// then create a circle of those bullets with radius 0. This changes only the direction of the bullets without displacement.
    /// </example>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/Shapes/circle.png")]
    public partial class Circle : Shaper
    {
        [Export] public string radius = "48";

        public override Transform2D GetElementTransform(int i, int totalCount)
        {
            float radians = Mathf.Tau * (i / (float)totalCount);
            return new Transform2D(
                radians,
                Solve("radius").AsSingle() * Vector2.FromAngle(radians)
            );
        }
    }
}
