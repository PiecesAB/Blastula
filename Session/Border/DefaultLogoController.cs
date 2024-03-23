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

        private async void DebugAnimation()
        {
            debugNotification.Modulate = new Color(1, 1, 1, 1);
            mainLogo.Modulate = new Color(1, 1, 1, 0);
            await this.WaitFrames(420);
            for (float prog = 0; prog < 1; prog += 0.04f)
            {
                debugNotification.Modulate = new Color(1, 1, 1, 1 - prog);
                mainLogo.Modulate = new Color(1, 1, 1, prog);
                await this.WaitOneFrame();
            }
            debugNotification.Modulate = new Color(1, 1, 1, 0);
            mainLogo.Modulate = new Color(1, 1, 1, 1);
        }

        public override void _Ready()
        {
            if (OS.IsDebugBuild())
            {
                DebugAnimation();
            }
        }
    }
}

