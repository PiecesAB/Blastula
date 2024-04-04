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
        [Export] public float testValue = 2.6f;
        [Export] public Control[] icons = new Control[8];
        [Export] public string fractionParameterName = "filled_fraction";

        public void UpdateIcons()
        {
            for (int i = 0; i < icons.Length; ++i)
            {
                Control icon = icons[i];
                float fillValue = Mathf.Clamp(testValue - i, 0, 1);
                ((ShaderMaterial)icon.Material).SetShaderParameter(fractionParameterName, fillValue);
            }
        }

        public override void _Ready()
        {
            base._Ready();
            UpdateIcons();
        }
    }
}

