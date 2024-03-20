using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
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
