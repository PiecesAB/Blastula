using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula.Schedules.EnemySchedules
{
    /// <summary>
    /// Set interpolation data for an EnemyMover to change the style.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/easing.png")]
    public partial class SetInterpolationData : EnemySchedule
    {
        [Export] public string moverID = "A";
        [Export] public Tween.TransitionType easingTransition = Tween.TransitionType.Quad;
        [Export] public Tween.EaseType easingType = Tween.EaseType.InOut;
        [Export] public string tweenDuration = "0.5";
        [Export] public bool radialVelocityInterpolation = false;

        public override Task Execute(IVariableContainer source)
        {
            if (source is not Enemy) { return Task.CompletedTask; }
            ExpressionSolver.currentLocalContainer = source;
            EnemyMover mover = ((Enemy)source).AddOrGetEnemyMover(moverID);
            mover.easingTransition = easingTransition;
            mover.easingType = easingType;
            if (tweenDuration != null && tweenDuration != "")
            {
                mover.tweenDuration = Solve("tweenDuration").AsSingle();
            }
            mover.radialVelocityInterpolation = radialVelocityInterpolation;
            return Task.CompletedTask;
        }
    }
}

