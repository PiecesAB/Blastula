using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Directly sets the container's position (and possibly rotation in degrees).
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/setPosition.png")]
    public partial class SetContainerPosition : BaseSchedule
    {
        [Export] public string X = "0";
        [Export] public string Y = "0";
        [Export] public string myRotation = "0";
        [Export] public Node2D referencePoint;
        [Export] public bool usePosition = true;
        [Export] public bool useRotation = false;
        [Export] public bool useGlobalPosition = true;
        [Export] public string setOldPositionVariable = "";

        public override Task Execute(IVariableContainer source)
        {
            if (source == null) { return Task.CompletedTask; }
            if (source is not Node2D) { return Task.CompletedTask; }
            Node2D source2D = (Node2D)source;
            ExpressionSolver.currentLocalContainer = source;
            if (setOldPositionVariable != null && setOldPositionVariable != "")
            {
                if (useGlobalPosition) { source.SetVar(setOldPositionVariable, source2D.GlobalPosition); }
                else { source.SetVar(setOldPositionVariable, source2D.Position); }
            }
            Vector2 newPosition = new Vector2(Solve("X").AsSingle(), Solve("Y").AsSingle());
            float newRotation = Mathf.DegToRad(Solve("myRotation").AsSingle());
            if (useGlobalPosition) 
            {
                newPosition += referencePoint?.GlobalPosition ?? Vector2.Zero;
                newRotation += referencePoint?.GlobalRotation ?? 0f;
                if (usePosition) { source2D.GlobalPosition = newPosition; }
                if (useRotation) { source2D.GlobalRotation = newRotation; }
            }
            else 
            {
                newPosition += referencePoint?.Position ?? Vector2.Zero;
                newRotation += referencePoint?.Rotation ?? 0f;
                if (usePosition) { source2D.Position = newPosition; }
                if (useRotation) { source2D.Rotation = newRotation; }
            }
            return Task.CompletedTask;
        }
    }
}

