using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
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
