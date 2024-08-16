using Blastula.Coroutine;
using Godot;
using System.Collections;

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
        private IEnumerator DebugAnimation()
        {
            long currAnimIter = ++animIteration;
            yield return new SetCancel((_) =>
            {
                if (animIteration != currAnimIter) { return; }
                debugNotification.Modulate = new Color(1, 1, 1, 0);
                mainLogo.Modulate = new Color(1, 1, 1, 1);
            });
            debugNotification.Modulate = new Color(1, 1, 1, 1);
            mainLogo.Modulate = new Color(1, 1, 1, 0);
            yield return new WaitFrames(420);
            if (animIteration != currAnimIter) { yield break; }
            for (float prog = 0; prog < 1; prog += 0.04f)
            {
                if (animIteration != currAnimIter) { yield break; }
                debugNotification.Modulate = new Color(1, 1, 1, 1 - prog);
                mainLogo.Modulate = new Color(1, 1, 1, prog);
                yield return new WaitOneFrame();
            }
            if (animIteration != currAnimIter) { yield break; }
            debugNotification.Modulate = new Color(1, 1, 1, 0);
            mainLogo.Modulate = new Color(1, 1, 1, 1);
        }

        public void DebugAnimationWrapper()
        {
            this.StartCoroutine(DebugAnimation());
        }

        public override void _Ready()
        {
            if (OS.IsDebugBuild())
            {
                this.StartCoroutine(DebugAnimation());
                if (StageManager.main != null)
                {
                    StageManager.main.Connect(
                        StageManager.SignalName.SessionBeginning,
                        new Callable(this, MethodName.DebugAnimationWrapper)
                    );
                }
            }
        }
    }
}

