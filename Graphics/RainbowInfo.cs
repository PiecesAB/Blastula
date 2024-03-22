using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Graphics
{
    /// <summary>
    /// Holds the data to a type of rainbow, for use in applying them to bullet appearances.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/rainbow.png")]
    public partial class RainbowInfo : Node
    {
        [Export] public string shaderParamaterName = "tint";
        [Export] public Color[] colors;
        [Export] public string[] names;
    }
}
