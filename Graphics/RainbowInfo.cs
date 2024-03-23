using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Graphics
{
    /// <summary>
    /// Gone are the days where you'd create a bunch of pre-rendered bullet colors in a shot sheet,
    /// and then carefully draw rects around each color. A RainbowInfo allows a GraphicInfo to 
    /// auto-generate child IDs, each with a different shader parameter color, and the shader
    /// handles how to apply the color to the base graphic.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/rainbow.png")]
    public partial class RainbowInfo : Node
    {
        [Export] public string shaderParamaterName = "tint";
        /// <summary>
        /// Colors which are set at the shader parameter above.
        /// </summary>
        [Export] public Color[] colors;
        /// <summary>
        /// Color names, like Red, Orange, Goldenrod, etc.
        /// </summary>
        /// <example>
        /// If you apply this rainbow to a GraphicInfo which has ID "Orb/Big", 
        /// the generated ID for the "Silver" name would be "Orb/Big/Silver".
        /// </example>
        [Export] public string[] names;
    }
}
