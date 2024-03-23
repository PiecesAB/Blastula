using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Clones the bullet structure without performing any movement.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/Shapes/inPlace.png")]
    public partial class CloneInPlace : Shaper
    {
        public override Transform2D GetElementTransform(int i, int totalCount)
        {
            return Transform2D.Identity;
        }
    }
}
