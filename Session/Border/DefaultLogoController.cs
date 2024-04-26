using Godot;

namespace Blastula.Graphics
{
    /// <summary>
    /// Handles displaying the Blastula default logo and notifying the developer that they can open a debug console.
    /// This is otherwise inconsequential, easily removed from the main scene.
    /// </summary>
    public partial class DefaultLogoController : Node
    {
        [Export] public Control mainLogo;
        [Export] public Control debugNotification;

        private long animIteration = 0;
        private async void DebugAnimation()
        {
            long currAnimIter = ++animIteration;
            debugNotification.Modulate = new Color(1, 1, 1, 1);
            mainLogo.Modulate = new Color(1, 1, 1, 0);
            await this.WaitFrames(420);
            if (animIteration != currAnimIter) { return; }
            for (float prog = 0; prog < 1; prog += 0.04f)
            {
                if (animIteration != currAnimIter) { return; }
                debugNotification.Modulate = new Color(1, 1, 1, 1 - prog);
                mainLogo.Modulate = new Color(1, 1, 1, prog);
                await this.WaitOneFrame();
            }
            if (animIteration != currAnimIter) { return; }
            debugNotification.Modulate = new Color(1, 1, 1, 0);
            mainLogo.Modulate = new Color(1, 1, 1, 1);
        }

        public override void _Ready()
        {
            if (OS.IsDebugBuild())
            {
                DebugAnimation();
                if (StageManager.main != null)
                {
                    StageManager.main.Connect(
                        StageManager.SignalName.SessionBeginning,
                        new Callable(this, MethodName.DebugAnimation)
                    );
                }
            }
        }
    }
}

