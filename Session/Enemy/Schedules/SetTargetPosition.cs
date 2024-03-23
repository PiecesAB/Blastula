using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula.Schedules.EnemySchedules
{
    /// <summary>
    /// Set the target position of an EnemyMover.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/setPositionEnemy.png")]
    public partial class SetTargetPosition : EnemySchedule
    {
        [Export] public string moverID = "A";
        [Export] public string X = "0";
        [Export] public string Y = "100";

        public override Task Execute(IVariableContainer source)
        {
            if (source is not Enemy) { return Task.CompletedTask; }
            ExpressionSolver.currentLocalContainer = source;
            EnemyMover mover = ((Enemy)source).AddOrGetEnemyMover(moverID);
            Vector2 newPosition = new Vector2(Solve("X").AsSingle(), Solve("Y").AsSingle());
            mover.SetTargetPosition(newPosition);
            return Task.CompletedTask;
        }
    }
}

