using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections;
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
        /// <summary>
        /// ID within the enemy, so the movement schedule can reference it later.
        /// </summary>
        [Export] public string moverID = "A";
        [Export] public Tween.TransitionType easingTransition = Tween.TransitionType.Quad;
        [Export] public Tween.EaseType easingType = Tween.EaseType.InOut;
        /// <summary>
        /// Duration of interpolation in seconds.
        /// </summary>
        [Export] public string tweenDuration = "0.5";
        /// <summary>
        /// If true, interpolates velocity by X = magnitude and Y = degrees direction, instead of by X and Y directly.
        /// </summary>
        /// <example>You can produce circular arcs of motion.</example>
        [Export] public bool radialVelocityInterpolation = false;

        public override IEnumerator Execute(IVariableContainer source)
        {
            if (source is not Enemy) { yield break; }
            ExpressionSolver.currentLocalContainer = source;
            EnemyMover mover = ((Enemy)source).AddOrGetEnemyMover(moverID);
            mover.easingTransition = easingTransition;
            mover.easingType = easingType;
            if (tweenDuration != null && tweenDuration != "")
            {
                mover.tweenDuration = Solve("tweenDuration").AsSingle();
            }
            mover.radialVelocityInterpolation = radialVelocityInterpolation;
        }
    }
}

