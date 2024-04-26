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
    public partial class ResourceNumberDisplay : Node
    {
        public enum Stat
        {
            Graze, PointItemValue
        }

        /// <summary>
        /// Selects a stat to display.
        /// </summary>
        [Export] Stat stat = Stat.Graze;
        /// <summary>
        /// Which player is relevant to this data?
        /// </summary>
        [Export] Player.Role role = Player.Role.SinglePlayer;
        private Player player = null;
        [Export] public string displayedValue = "0";
        /// <summary>
        /// This is a text label that expresses the number.
        /// </summary>
        [Export] public Label label = null;
        /// <summary>
        /// If this isn't (0, 0), then the displayed value will be clamped within this range, as a 32-bit integer.
        /// </summary>
        [Export] public Vector2I bounds = new Vector2I(0, 999990);

        public void UpdateNumber()
        {
            string boundedDisplayedValue = displayedValue;
            if (bounds != Vector2I.Zero && int.TryParse(displayedValue, out int boundedInt))
            {
                boundedDisplayedValue = boundedInt.ToString();
            }
            label.Text = boundedDisplayedValue;
        }

        private string GetTargetValue()
        {
            if (player == null || !IsInstanceValid(player))
            {
                player = null;
                if (Player.playersByControl.ContainsKey(role))
                {
                    player = Player.playersByControl[role];
                }
            }
            if (player == null) { return "0"; }
            switch (stat)
            {
                case Stat.Graze: default: return Session.main.grazeCount.ToString();
                case Stat.PointItemValue: return Session.main.pointItemValue.ToString();
            }
        }

        public override void _Ready()
        {
            base._Ready();
            UpdateNumber();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            string targetValue = GetTargetValue();
            if (targetValue != displayedValue)
            {
                displayedValue = targetValue;
                UpdateNumber();
            }
        }
    }
}

