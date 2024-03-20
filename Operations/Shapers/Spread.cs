using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Also known as a fan. Places an arc of bullets.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/Shapes/spread.png")]
    public partial class Spread : Shaper
    {
        [Export()]
        public string radius = "48";
        [Export()]
        public string angularWidth = "180";

        public override Transform2D GetElementTransform(int i, int totalCount)
        {
            float progress = (totalCount == 1) ? 0.5f : (i / (float)(totalCount - 1));
            float radians = Solve("angularWidth").AsSingle() * (progress - 0.5f) * (Mathf.Pi / 180f);
            return new Transform2D(
                radians,
                Solve("radius").AsSingle() * new Vector2(Mathf.Cos(radians), Mathf.Sin(radians))
            );
        }
    }
}
