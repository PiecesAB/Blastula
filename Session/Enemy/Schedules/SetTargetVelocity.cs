using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula.Schedules.EnemySchedules
{
    /// <summary>
    /// Set the target velocity of an enemy.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/setVelocityEnemy.png")]
    public partial class SetTargetVelocity : EnemySchedule
    {
        [Export] public string moverID = "A";
        /// <summary>
        /// If radial mode is on: X is the speed, and Y is the angle of travel in degrees.
        /// If radial mode is off: X and Y are the speeds along their axes.
        /// </summary>
        [Export] public bool radialMode = false;
        [Export] public string X = "0";
        [Export] public string Y = "100";

        public override Task Execute(IVariableContainer source)
        {
            if (source is not Enemy) { return Task.CompletedTask; }
            ExpressionSolver.currentLocalContainer = source;
            EnemyMover mover = ((Enemy)source).AddOrGetEnemyMover(moverID);
            Vector2 newVelocity = new Vector2(Solve("X").AsSingle(), Solve("Y").AsSingle());
            if (radialMode)
            {
                newVelocity = EnemyMover.RadialToCartesian(
                    new Vector2(newVelocity.X, Mathf.DegToRad(newVelocity.Y))
                );
            }
            mover.SetTargetVelocity(newVelocity);
            return Task.CompletedTask;
        }
    }
}

