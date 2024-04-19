using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
    /// <summary>
    /// This class ensures the animation is NOT affected by framerate,
    /// but instead advances a certain amount every frame.
    /// </summary>
    /// <remarks>
    /// We require this to ensure replays are deterministic.
    /// </remarks>
    public partial class AnimationFrameAdvance : AnimationPlayer
    {
        public override void _Ready()
        {
            base._Ready();
            Pause();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (!Active) { return; }
            Pause();
            double animationStep = Engine.TimeScale / Persistent.SIMULATED_FPS;
            Advance(animationStep);
        }
    }
}

