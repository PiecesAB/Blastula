using Blastula.Input;
using Blastula.Menus;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
    /// <summary>
    /// Handles the input named "Menu/Pause" to give control to the pause menu.
    /// Also appears on game over.
    /// </summary>
    public partial class PauseMenuManager : Control
    {
        public enum Mode
        {
            /// <summary>
            /// Normal pause version of the menu.
            /// </summary>
            Pause, 
            /// <summary>
            /// Game over with the option to continue.
            /// </summary>
            GameOver, 
            /// <summary>
            /// Game over without the option to continue.
            /// </summary>
            GameOverNoContinue
        }

        /// <summary>
        /// This sets the animation state to the Mode as a string,
        /// which causes the menu to change its options.
        /// Only the first frame of the animation is played,
        /// because we expect an instant transition.
        /// </summary>
        [Export] public AnimationPlayer modeAnimPlayer;
        public Mode mode { get; private set; } = Mode.Pause;
        [Export] public VerticalListMenu mainMenu;

        public enum State
        {
            Unpaused, Pausing, Paused, Unpausing
        }
        public State state = State.Unpaused;
        public const int PAUSE_OPEN_CLOSE_DELAY = 5;
        private int animationTimer = 0;

        public static PauseMenuManager main { get; private set; }

        public void UnpausingAnimation()
        {
            int i = 9 - animationTimer;
            if (i > 0)
            {
                Modulate = new Color(1, 1, 1, i / (float)(10));
            }
            else
            {
                Modulate = new Color(1, 1, 1, 0);
                Visible = false;
                Session.main.Unpause();
                state = State.Unpaused;
            }
            ++animationTimer;
        }

        public void PausingAnimation()
        {
            int i = 1 + animationTimer;
            if (i < 10)
            {
                Modulate = new Color(1, 1, 1, i / (float)(10));
                Visible = true;
            }
            else
            {
                Modulate = new Color(1, 1, 1, 1);
                state = State.Paused;
            }
            ++animationTimer;
        }

        public void SetMode(Mode newMode)
        {
            if (mode != newMode)
            {
                mode = newMode;
                modeAnimPlayer.Play(mode.ToString());
                modeAnimPlayer.Pause();
                modeAnimPlayer.Advance(0.1f);
            }
        }

        public void PrepareToOpen()
        {
            state = State.Pausing;
            mainMenu.Open();
            animationTimer = 0;
            Session.main.Pause();
            PausingAnimation();
        }

        public override void _Ready()
        {
            Visible = false;
            main = this;
            modeAnimPlayer.Play(mode.ToString());
            modeAnimPlayer.Stop(true);
            ProcessPriority = Persistent.Priorities.PAUSE;
        }

        public override void _Process(double delta)
        {
            if (Session.main == null) { return; }
            bool pausePressed = InputManager.ButtonPressedThisFrame("Menu/Pause");
            if (pausePressed && state == State.Paused)
            {
                pausePressed &= mainMenu.cancelable;
            }
            if (pausePressed && state == State.Paused)
            {
                mainMenu.PlayBackSFX();
            }
            pausePressed |= state == State.Paused && !mainMenu.IsInStack();
            if (pausePressed)
            {
                if (state == State.Paused && Session.main.paused)
                {
                    state = State.Unpausing;
                    mainMenu.Close();
                    animationTimer = 0;
                    UnpausingAnimation();
                }
                else if (state == State.Unpaused && !Session.main.paused)
                {
                    SetMode(Mode.Pause);
                    PrepareToOpen();
                }
            }

            // No async used because that will easily cause the game state to change when it's paused.
            // We want to ensure all pauses/unpauses are solved after all game action is processed.
            if (state == State.Unpausing) { UnpausingAnimation(); }
            else if (state == State.Pausing) { PausingAnimation(); }
        }
    }
}
