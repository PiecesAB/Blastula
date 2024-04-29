using Blastula.Input;
using Blastula.Menus;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
    /// <summary>
    /// Handles the container for the background.
    /// </summary>
    public partial class BackgroundHolder : Control
    {
        public static BackgroundHolder main { get; private set; } = null;

        public static void SetVisible(bool newVisible)
        {
            if (main == null) { return; }
            main.Visible = newVisible;
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
        }
    }
}
