using Blastula.Graphics;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Clone the BNode and reflect it across an axis. Useful for bilaterally symmetric patterns.
    /// </summary>
    /// <remarks>
    /// "number" variable as inherited from Shaper should always be 2.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/reflect.png")]
    public unsafe partial class Reflect : Shaper
    {
        /// <summary>
        /// Angle the reflection axis makes with the transform's rightwards direction, in degrees. Clockwise by Godot convention.
        /// </summary>
        /// <example>0: the reflection occurs across the axis which this structure points along.</example>
        [Export] public string axisAngle = "90";

        public override Transform2D GetElementTransform(int i, int totalCount)
        {
            if (Solve("number").AsInt32() != 2) { GD.PushWarning("Number in reflect shaper should always be 2"); }
            if (i == 0) { return Transform2D.Identity; }
            (float s2, float c2) = Mathf.SinCos(2 * Mathf.DegToRad(Solve("axisAngle").AsSingle()));
            return new Transform2D(c2, s2, s2, -c2, 0, 0);
        }
    }
}
