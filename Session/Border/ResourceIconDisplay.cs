using Blastula.VirtualVariables;
using Godot;
using System.Numerics;

namespace Blastula.Graphics
{
    /// <summary>
    /// Handles a resource icon series, particularly the series of icons for lives and bombs.
    /// </summary>
    /// <remarks>
    /// Also, this assumes each icon is a Control and has a ShaderMaterial 
    /// with the given parameter to set what "fraction" of a resource is available.
    /// </remarks>
    public partial class ResourceIconDisplay : Node
    {
        public enum PlayerStat
        {
            Lives, Bombs
        }

        /// <summary>
        /// Selects a player stat to display.
        /// </summary>
        [Export] PlayerStat stat = PlayerStat.Lives;
        /// <summary>
        /// Which player is relevant to this data?
        /// </summary>
        [Export] Player.Role role = Player.Role.SinglePlayer;
        private Player player = null;
        [Export] public float displayedValue = 0f;
        [Export] public Control[] icons = new Control[8];
        [Export] public string fractionParameterName = "filled_fraction";

        public void UpdateIcons()
        {
            for (int i = 0; i < icons.Length; ++i)
            {
                Control icon = icons[i];
                float fillValue = Mathf.Clamp(displayedValue - i, 0, 1);
                ((ShaderMaterial)icon.Material).SetShaderParameter(fractionParameterName, fillValue);
            }
        }

        private float GetTargetValue()
        {
            if (player == null || !IsInstanceValid(player))
            {
                player = null;
                if (Player.playersByControl.ContainsKey(role)) 
                { 
                    player = Player.playersByControl[role]; 
                }
            }
            if (player == null) { return 0f; }
            switch (stat)
            {
                case PlayerStat.Lives: default: return player.lives;
                case PlayerStat.Bombs: return player.bombs;
            }
        }

        public override void _Ready()
        {
            base._Ready();
            UpdateIcons();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            float targetValue = GetTargetValue();
            if (targetValue != displayedValue)
            {
                displayedValue = targetValue;
                UpdateIcons();
            }
        }
    }
}

