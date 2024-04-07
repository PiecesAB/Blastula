using Godot;
using System.Diagnostics;

namespace Blastula.Graphics
{
    /// <summary>
    /// Handles the power bar which comes with the framework. 
    /// This is idiosyncratic and expects the bar to be in a very specific form.
    /// It can easily be replaced with your own display.
    /// </summary>
    public partial class PowerDisplay : Node
    {
        /// <summary>
        /// Which player is relevant to this data?
        /// </summary>
        [Export] Player.Role role = Player.Role.SinglePlayer;
        private Player player = null;
        [Export] public int displayedValue = 0;
        [Export] public RichTextLabel barTextNode;
        [Export] public TextureRect barVisualNode;
        [Export] public Texture2D[] barImages;
        [Export] public string fractionParameterName = "filled_fraction";
        [Export] public string backSamplerParameterName = "back_sampler";
        [Export] public string frontSamplerParameterName = "front_sampler";

        public void UpdateBar()
        {
            string decimalText = (displayedValue * 0.01f).ToString("F2");
            int maxPower = player?.shotPowerRange.Y ?? 400;
            string decimalFullText = (maxPower * 0.01f).ToString("F2");
            if (displayedValue == maxPower)
            {
                barTextNode.Text = "[center]MAX[/center]";
                ShaderMaterial mat = (ShaderMaterial)barVisualNode.Material;
                mat.SetShaderParameter(fractionParameterName, 2f);
                ((ShaderMaterial)barTextNode.Material).SetShaderParameter(fractionParameterName, 2f);
                Texture2D maxTex = barImages[barImages.Length - 1];
                mat.SetShaderParameter(backSamplerParameterName, maxTex);
                mat.SetShaderParameter(frontSamplerParameterName, maxTex);
            }
            else
            {
                barTextNode.Text = $"[center]{decimalText}[font_size=18]/{decimalFullText}[/font_size][/center]";
                int backBarImageIndex = Mathf.Clamp(displayedValue / 100 - 1, 0, barImages.Length - 1);
                int frontBarImageIndex = Mathf.Clamp(displayedValue / 100, 0, barImages.Length - 1);
                ShaderMaterial mat = (ShaderMaterial)barVisualNode.Material;
                mat.SetShaderParameter(fractionParameterName, (displayedValue % 100) / 100f);
                ((ShaderMaterial)barTextNode.Material).SetShaderParameter(fractionParameterName, (displayedValue % 100) / 100f);
                mat.SetShaderParameter(backSamplerParameterName, barImages[backBarImageIndex]);
                mat.SetShaderParameter(frontSamplerParameterName, barImages[frontBarImageIndex]);
            }
        }

        private int GetTargetValue()
        {
            if (player == null)
            {
                if (Player.playersByControl.ContainsKey(role)) 
                { 
                    player = Player.playersByControl[role]; 
                }
            }
            if (player == null) { return 0; }
            return player.shotPower;
        }

        public override void _Ready()
        {
            base._Ready();
            UpdateBar();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            int targetValue = GetTargetValue();
            if (targetValue != displayedValue)
            {
                displayedValue = targetValue;
                UpdateBar();
            }
        }
    }
}

