using Blastula.VirtualVariables;
using Godot;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Blastula.Graphics
{
    /// <summary>
    /// This script's Node should be a direct child of the Player.
    /// It orchestrates AnimationPlayer classes to cause certain 
    /// the options to display properly.
    /// </summary>
    public partial class PlayerAnimOrchestrator : Node
    {
        /// <summary>
        /// For each AnimationPlayer in the list,
        /// play the track with a name "1", "2", "3", ... depending on the current Player's shotPowerIndex.
        /// </summary>
        [Export] public AnimationPlayer[] powerIndexAPs;
        /// <summary>
        /// For each AnimationPlayer in the list,
        /// if the player is focused, play track "Focused", else play "Unfocused".
        /// </summary>
        [Export] public AnimationPlayer[] focusAPs;

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
            string focusTrackName = isFocused ? FOCUSED_WORD : UNFOCUSED_WORD;
            string powerIndexName = currPowerIndex.ToString();
            // We pause animations immediately to manually handle time in _Process.
            // If not, the game behavior may differ due to framerate, causing inconsistency in replays.
            foreach (AnimationPlayer focusAP in focusAPs)
            {
                focusAP.Play(focusTrackName);
                focusAP.Pause();
            }
            foreach (AnimationPlayer powerIndexAP in powerIndexAPs)
            {
                powerIndexAP.Play(powerIndexName);
                powerIndexAP.Pause();
            }
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
                foreach (AnimationPlayer focusAP in focusAPs)
                {
                    focusAP.Advance(animationStep);
                }
                foreach (AnimationPlayer powerIndexAP in powerIndexAPs)
                {
                    powerIndexAP.Advance(animationStep);
                }
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
