using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Blastula.Operations
{
    /// <summary>
    /// An effect that slows or "stops" the progression of time in the game.
    /// The first node of this type in existence is assumed to be in the kernel and functions as a manager.
    /// Via the manager ("main" variable) the EnterPseudoStop / ExitPseudoStop signals are emitted for other game objects to hear.
    /// </summary>
    /// <remarks>
    /// When using it in schedule contexts, be sure to keep in mind this will run instantly,
    /// even though the effect has a duration.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/gameSpeed.png")]
    public partial class GameSpeed : Discrete
	{
        public enum Mode
        {
            /// <summary>
            /// Time becomes slower or faster (within a constant limit, [0.1, 10] by default).
            /// </summary>
            /// <remarks>
            /// Multiple simultaneous effects will stack multiplicatively.
            /// </remarks>
            Alter,
            /// <summary>
            /// Appears to stop time, but it's just an illusion (as stage frame must be increasing for logic sanity purposes).
            /// The player will be unable to do anything or animate, and bullets will not execute any behaviors,
            /// but the boss and enemies will remain normal.
            /// </summary>
            /// <remarks>
            /// These will overlap correctly, such that the earlier stop will not end the effect.
            /// You can customize enemies or background routines to respond to the EnterPseudoStop / ExitPseudoStop signals.
            /// </remarks>
            PseudoStop
        }

        [Export] public Mode mode = Mode.Alter;
        /// <summary>
        /// For the Alter mode, this is the speed of the flow of time.
        /// </summary>
        [Export] public float speedMultiplier = 0.5f;
        /// <summary>
        /// The maximum duration of the effect when evaluated as a number; infinite duration if empty.
        /// </summary>
        [Export] public string effectDuration = "1.5";
        [Export] public Wait.TimeUnits effectDurationUnits = Wait.TimeUnits.Seconds;
        /// <summary>
        /// When a nonempty expression, the effect stops immediately as it becomes true.
        /// </summary>
        [Export] public string effectEndCondition = "";

        [Signal] public delegate void EnterPseudoStopEventHandler();
        [Signal] public delegate void ExitPseudoStopEventHandler();

        public static GameSpeed main { get; private set; } = null;
        public static bool pseudoStopped { get; private set; } = false;
        /// <summary>
        /// When the time scale is altered very drastically, we ensure it won't leave the range [X, Y].
        /// </summary>
        private static Vector2 dilateLimit = new Vector2(0.1f, 10f);

        public override void _Ready()
        {
            base._Ready();
            if (main == null) { main = this; }
        }

        private async Task WaitSub(float t)
        {
            if (effectDurationUnits == Wait.TimeUnits.Seconds)
            {
                await this.WaitSeconds(t);
            }
            else if (effectDurationUnits == Wait.TimeUnits.Frames)
            {
                await this.WaitFrames(Mathf.RoundToInt(t));
            }
        }

        private async Task AlterTime()
        {
            Session.main.SetTimeScale(Session.main.timeScale * speedMultiplier);
            float solvedDuration = Solve(PropertyName.effectDuration).AsSingle();
            await WaitSub(Solve(PropertyName.effectDuration).AsSingle());
            Session.main.SetTimeScale(Session.main.timeScale / speedMultiplier);
        }

        private static long pseudoStopIteration = 0;
        private async Task PseudoStop()
        {
            long currIter = ++pseudoStopIteration;
            if (!pseudoStopped)
            {
                pseudoStopped = true;
                main.EmitSignal(SignalName.EnterPseudoStop);
            }
            await WaitSub(Solve(PropertyName.effectDuration).AsSingle());
            if (pseudoStopped && currIter == pseudoStopIteration)
            {
                pseudoStopped = false;
                main.EmitSignal(SignalName.ExitPseudoStop);
            }
        }

        public override void Run()
        {
            switch (mode)
            {
                case Mode.Alter: _ = AlterTime(); break;
                case Mode.PseudoStop: _ = PseudoStop(); break;
            }
        }
    }
}
