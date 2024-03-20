using Blastula.VirtualVariables;
using Godot;

/// <summary>
/// Holds the data to a type of rainbow, for use in applying them to bullet appearances.
/// </summary>
namespace Blastula.Graphics
{
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/rainbow.png")]
    public partial class RainbowInfo : Node
    {
        [Export] public string shaderParamaterName = "tint";
        [Export] public Color[] colors;
        [Export] public string[] names;
    }
}
