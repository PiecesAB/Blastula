using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
	/// <summary>
	/// This class ensures the animation is NOT affected by framerate,
	/// but instead advances a certain amount every frame.
	/// Also, its control over the animation allows for more customized behavior.
	/// </summary>
	/// <remarks>
	/// We require this to ensure replays are deterministic.
	/// </remarks>
	public partial class AnimationFrameAdvance : AnimationPlayer
	{
		// If true, it will appear to stop with the fake time stop.
		[Export] public bool reactToPseudoStop = false;

		public override void _Ready()
		{
			base._Ready();
			Pause(); // Note: could this be more efficient?
		}

		public override void _Process(double delta)
		{
			base._Process(delta);
			if (!Active) { return; }
			Pause(); // Note: could this be more efficient?
			double animationStep = Engine.TimeScale / Persistent.SIMULATED_FPS;
			bool animateThisFrame = true;
			animateThisFrame &= !reactToPseudoStop || !GameSpeed.pseudoStopped;
			if (animateThisFrame) { Advance(animationStep); }
		}
	}
}

