using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula.Graphics
{
    /// <summary>
    /// This script should belong to the options (shot-producing helpers) of a player, as a direct child object.
    /// It's only in this way that the Player can find and activate it when necessary.
    /// This class orchestrates AnimationPlayer classes to cause the options to display properly.
    /// </summary>
    public partial class PlayerOptionsAnimator : Node
    {
        /// <summary>
        /// Play the track with a name "1", "2", "3", ... depending on the current Player's shotPowerIndex.
        /// </summary>
        [Export] public AnimationPlayer powerIndexAP;
        /// <summary>
        /// If the player is focused, play track "Focused", else play "Unfocused".
        /// </summary>
        [Export] public AnimationPlayer focusAP;

        private Player player = null;
        private bool isFocused = false;
        private int currPowerIndex = -1;
        private bool needsUpdate = true;

        private const string FOCUSED_WORD = "Focused";
        private const string UNFOCUSED_WORD = "Unfocused";

        public override void _Ready()
        {
            base._Ready();
        }

        public void UpdateAnimations()
        {
            if (!needsUpdate || player == null) { return; }
            needsUpdate = false;
            powerIndexAP.SpeedScale = focusAP.SpeedScale = 1f;
            string focusTrackName = isFocused ? FOCUSED_WORD : UNFOCUSED_WORD;
            // We pause animations immediately to manually handle time in _Process.
            // If not, the game behavior may differ due to framerate, causing inconsistency in replays.
            focusAP.Play(focusTrackName, 0.1f);
            focusAP.Pause();
            powerIndexAP.Play(currPowerIndex.ToString(), 0f);
            powerIndexAP.Pause();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (player == null && GetParent() is Player) { player = (Player)GetParent(); }
            if (player == null) { return; }
            
            if (!Session.main?.paused ?? false)
            {
                // We manually handle time.
                // If not, the game behavior may differ due to framerate, causing inconsistency in replays.
                double animationStep = Engine.TimeScale / Persistent.SIMULATED_FPS;
                focusAP.Advance(animationStep);
                powerIndexAP.Advance(animationStep);
            }

            if (player.IsFocused() != isFocused)
            {
                needsUpdate = true;
                isFocused = player.IsFocused();
            }
            if (player.shotPowerIndex != currPowerIndex)
            {
                needsUpdate = true;
                currPowerIndex = player.shotPowerIndex;
            }
            UpdateAnimations();
        }
    }
}