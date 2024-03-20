using Godot;

namespace Blastula.Graphics
{
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
