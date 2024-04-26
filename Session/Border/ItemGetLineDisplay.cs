using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula.Graphics
{
    /// <summary>
    /// Handles the item get line idiosyncratically in the starter project.
    /// We expect only one to exist per player role.
    /// The "SetItemGetHeight" operation depends on this.
    /// </summary>
    public partial class ItemGetLineDisplay : Control
    {
        /// <summary>
        /// Which player is relevant to this data?
        /// </summary>
        [Export] Player.Role role = Player.Role.SinglePlayer;
        /// <summary>
        /// The line which is displayed within the playfield. Turns invisible when irrelevant.
        /// </summary>
        [Export] public Control line = null;
        [Export] public int displayDurationFrames = 300;
        [Export] public int displayFadeDurationFrames = 20;
        /// <summary>
        /// How fast (in units per second) does the line move to its target value?
        /// </summary>
        [Export] public float movementSpeed = 800;
        private Player player = null;

        public static Dictionary<Player.Role, ItemGetLineDisplay> displaysByPlayerRole = new Dictionary<Player.Role, ItemGetLineDisplay>();

        public override void _Ready()
        {
            displaysByPlayerRole[role] = this;
            if (StageManager.main != null)
            {
                StageManager.main.Connect(
                    StageManager.SignalName.SessionBeginning,
                    new Callable(this, MethodName.OnSessionBeginning)
                );
            }
            _ = SetWithoutLineDisplay();
        }

        public async void OnSessionBeginning()
        {
            await SetWithoutLineDisplay();
            await DisplayLine();
        }

        private float GetOpacityPulse()
        {
            return 0.85f + 0.15f * Mathf.Sin((float)(7.5 * (double)FrameCounter.stageFrame / Persistent.SIMULATED_FPS));
        }

        private void SetPlayerIfNeeded()
        {
            if (player == null || !IsInstanceValid(player))
            {
                player = null;
                if (Player.playersByControl.ContainsKey(role))
                {
                    player = Player.playersByControl[role];
                }
            }
        }

        public async Task SetWithoutLineDisplay()
        {
            SetPlayerIfNeeded();
            if (player == null || !IsInstanceValid(player))
            {
                // Try again to get the player next frame; give up if they still don't exist.
                await this.WaitOneFrame();
                SetPlayerIfNeeded();
                if (player == null || !IsInstanceValid(player)) { return; }
            }
            Position = new Vector2(Position.X, player.itemGetHeight);
        }

        private long displayLineIteration = 0;
        public async Task DisplayLine()
        {
            SetPlayerIfNeeded();
            if (player == null) {
                // Try again to get the player next frame; give up if they still don't exist.
                await this.WaitOneFrame();
                SetPlayerIfNeeded();
                if (player == null) { return; }
            }
            long currentIteration = ++displayLineIteration;
            float targetY = player.itemGetHeight;
            float currentMovementTick = movementSpeed / Persistent.SIMULATED_FPS;
            line.Modulate = new Color(1, 1, 1, 0);
            for (int i = 0; i < displayFadeDurationFrames; ++i)
            {
                if (currentIteration != displayLineIteration) { break; }
                Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, targetY, currentMovementTick));
                line.Modulate = new Color(1, 1, 1, Mathf.MoveToward(line.Modulate.A, GetOpacityPulse(), 1f / displayFadeDurationFrames));
                await this.WaitOneFrame();
            }
            if (currentIteration != displayLineIteration) { return; }
            for (int i = 0; i < displayDurationFrames; ++i)
            {
                if (currentIteration != displayLineIteration) { break; }
                Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, targetY, currentMovementTick));
                line.Modulate = new Color(1, 1, 1, GetOpacityPulse());
                await this.WaitOneFrame();
            }
            if (currentIteration != displayLineIteration) { return; }
            for (int i = 0; i < displayFadeDurationFrames; ++i)
            {
                if (currentIteration != displayLineIteration) { break; }
                Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, targetY, currentMovementTick));
                line.Modulate = new Color(1, 1, 1, Mathf.MoveToward(line.Modulate.A, 0, 1f / displayFadeDurationFrames));
                await this.WaitOneFrame();
            }
            if (currentIteration != displayLineIteration) { return; }
            line.Modulate = new Color(1, 1, 1, 0);
        }
    }
}

