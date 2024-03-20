using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Directly sets the container's position.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/setPosition.png")]
    public partial class SetContainerPosition : BaseSchedule
    {
        [Export] public string X = "0";
        [Export] public string Y = "0";
        [Export] public bool useGlobalPosition = true;
        [Export] public string setPositionVariable = "";

        public override Task Execute(IVariableContainer source)
        {
            if (source == null) { return Task.CompletedTask; }
            if (source is not Node2D) { return Task.CompletedTask; }
            Node2D source2D = (Node2D)source;
            ExpressionSolver.currentLocalContainer = source;
            if (setPositionVariable != null && setPositionVariable != "")
            {
                if (useGlobalPosition) { source.SetVar(setPositionVariable, source2D.GlobalPosition); }
                else { source.SetVar(setPositionVariable, source2D.GlobalPosition); }
            }
            Vector2 newPosition = new Vector2(Solve("X").AsSingle(), Solve("Y").AsSingle());
            if (useGlobalPosition) { source2D.GlobalPosition = newPosition; }
            else { source2D.Position = newPosition; }
            return Task.CompletedTask;
        }
    }
}

