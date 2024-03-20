using Blastula.Input;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
    public partial class DefaultPauseMenu : Control
    {
        public enum State
        {
            Unpaused, Pausing, Paused, Unpausing
        }
        public State state = State.Unpaused;
        public const int PAUSE_OPEN_CLOSE_DELAY = 5;

        public async void UnpausingAnimation()
        {
            for (int i = 9; i > 0; --i)
            {
                Modulate = new Color(1, 1, 1, i / (float)(10));
                await this.WaitOneFrame(true);
            }
            Modulate = new Color(1, 1, 1, 0);
            Session.main.Unpause();
            await this.WaitFrames(PAUSE_OPEN_CLOSE_DELAY, true);
            state = State.Unpaused;
        }

        public async void PausingAnimation()
        {
            Modulate = new Color(1, 1, 1, 0);
            Visible = true;
            for (int i = 1; i < 10; ++i)
            {
                Modulate = new Color(1, 1, 1, i / (float)(10));
                await this.WaitOneFrame(true);
            }
            Modulate = new Color(1, 1, 1, 1);
            await this.WaitFrames(PAUSE_OPEN_CLOSE_DELAY, true);
            state = State.Paused;
        }

        public override void _Ready()
        {
            Visible = false;
            ProcessPriority = Persistent.Priorities.PAUSE;
        }

        public override void _Process(double delta)
        {
            if (Session.main == null) { return; }
            bool pausePressed = InputManager.ButtonPressedThisFrame("Pause");
            if (pausePressed)
            {
                if (state == State.Paused && Session.main.paused)
                {
                    state = State.Unpausing;
                    UnpausingAnimation();
                }
                else if (state == State.Unpaused && !Session.main.paused)
                {
                    state = State.Pausing;
                    Session.main.Pause();
                    PausingAnimation();
                }
            }
        }
    }
}
