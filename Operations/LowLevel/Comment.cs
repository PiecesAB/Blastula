using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// This operation does absolutely nothing. Insert it into a schedule or operation sequence
    /// when you want to change the node's name to explain something.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/empty.png")]
    public partial class Comment : Modifier
    {
    }
}


