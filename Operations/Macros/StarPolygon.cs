using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Macro that places a star polygon.<br />
    /// </summary>
    [GlobalClass]
    public partial class StarPolygon : Macro
    {
        [Export] public int sides = 5;
        [Export] public int leapSize = 2;
        [Export] public string radius = "100";
        [Export] public string bulletsPerSide = "18";
        /// <summary>
        /// If true, this makes a deeper container with copies of the lower-sided stars,
        /// if the leapSize and sides share a common factor.
        /// This will make a star complete even when made up of lower-order stars. 
        /// </summary>
        /// <example>
        /// The Star of David is two triangles;
        /// it can be produced with sides = 6 and leapSize = 2 and makeGCDCopies = true.
        /// </example>
        [Export] public bool makeGCDCopies = false;

        protected override void CreateSuboperations()
        {
            int gcd = MoreMath.GCD(sides, leapSize);
            int vSides = sides; int vLeapSize = leapSize;
            if (gcd > 1)
            {
                vSides /= gcd; vLeapSize /= gcd;
            }
            Circle circleOp = AddOperation<Circle>();
            circleOp.number = vSides.ToString();
            circleOp.radius = radius;
            Shuffle shuffleOp = AddOperation<Shuffle>();
            shuffleOp.mode = Shuffle.Mode.Leap;
            shuffleOp.n = vLeapSize.ToString();
            Connect connectOp = AddOperation<Connect>();
            connectOp.structure = Operations.Connect.Structure.Flat;
            connectOp.lineType = Operations.Connect.LineType.Line;
            connectOp.number = $"({bulletsPerSide}) - 1";
            connectOp.circular = true;
            if (makeGCDCopies)
            {
                Scrimble scrOp = AddOperation<Scrimble>();
                scrOp.startAngle = "0";
                scrOp.endAngle = (360f / vSides).ToString();
                scrOp.startRadius = scrOp.endRadius = "0";
                scrOp.number = gcd.ToString();
                scrOp.circular = true;
            }
        }
    }
}
