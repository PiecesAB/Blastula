using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Macro that places a polygon.<br />
    /// </summary>
    [GlobalClass]
    public partial class Polygon : Macro
    {
        [Export] public string sides = "5";
        [Export] public string radius = "100";
        [Export] public string bulletsPerSide = "10";
        protected override void CreateSuboperations()
        {
            Circle circleOp = AddOperation<Circle>();
            circleOp.number = sides;
            circleOp.radius = radius;
            Connect connectOp = AddOperation<Connect>();
            connectOp.structure = Operations.Connect.Structure.Flat;
            connectOp.lineType = Operations.Connect.LineType.Line;
            connectOp.number = $"({bulletsPerSide}) - 1";
            connectOp.circular = true;
        }
    }
}
