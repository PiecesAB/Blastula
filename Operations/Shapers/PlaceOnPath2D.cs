using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Place clones along a Path2D. 
    /// Local space in the path translates to local space of the inStructure.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/Shapes/circle.png")]
    public partial class PlaceOnPath2D : Shaper
    {
        [Export] public Path2D path2D;
        [Export] public float shrink = 10;
        [Export] public string extraRotation = "0";
        [Export] public bool circular;

        public override Transform2D GetElementTransform(int i, int totalCount)
        {
            float pathLength = path2D.Curve.GetBakedLength();
            if (pathLength <= 0) { return path2D.Curve.SampleBakedWithRotation(0f); }
            if (circular && totalCount == 1) { return path2D.Curve.SampleBakedWithRotation(0.5f); }
            float pathOffset = (pathLength - shrink) * i / (circular ? (totalCount) : (totalCount - 1));
            pathOffset += 0.5f * shrink;
            float addedRotation = 0.5f * Mathf.Pi - Mathf.DegToRad(Solve("extraRotation").AsSingle());
            return path2D.Curve.SampleBakedWithRotation(pathOffset).RotatedLocal(addedRotation);
        }
    }
}
