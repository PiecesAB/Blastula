using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula.Schedules.EnemySchedules
{
    /// <summary>
    /// Set the target velocity of an EnemyMover.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/setVelocityEnemy.png")]
    public partial class SetTargetVelocity : EnemySchedule
    {
        [Export] public string moverID = "A";
        /// <summary>
        /// In radial mode, X is the magnitude. Otherwise it is the X-component of the velocity.
        /// </summary>
        [Export] public string X = "0";
        /// <summary>
        /// In radial mode, Y is the direction in degrees (clockwise of the rightward direction). 
        /// Otherwise it is the Y-component of the velocity.
        /// </summary>
        [Export] public string Y = "100";

        public override Task Execute(IVariableContainer source)
        {
            if (source is not Enemy) { return Task.CompletedTask; }
            ExpressionSolver.currentLocalContainer = source;
            EnemyMover mover = ((Enemy)source).AddOrGetEnemyMover(moverID);
            Vector2 newVelocity = new Vector2(Solve("X").AsSingle(), Solve("Y").AsSingle());
            if (mover.radialVelocityInterpolation)
            {
                newVelocity = new Vector2(newVelocity.X, Mathf.DegToRad(newVelocity.Y));
            }
            mover.SetTargetVelocity(newVelocity);
            return Task.CompletedTask;
        }
    }
}

