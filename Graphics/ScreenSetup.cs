using Godot;

namespace Blastula.Graphics
{
    /// <summary>
    /// Ensures the playfield is a window to the main game world.
    /// </summary>
    public partial class ScreenSetup : Node
    {
        [Export] public SubViewport[] objectViewports;

        public override void _Ready()
        {
            foreach (SubViewport vp in objectViewports)
            {
                vp.World2D = GetWindow().World2D;
            }
        }
    }
}
