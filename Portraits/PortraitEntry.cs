using Blastula.VirtualVariables;
using Godot;
using System;

namespace Blastula.Portraits;

[GlobalClass]
[Icon(Persistent.NODE_ICON_PATH + "/portrait.png")]
public partial class PortraitEntry : Node
{
	/// <summary>
	/// A scene with a root node of PortraitController script.
	/// </summary>
	[Export] public PackedScene portraitScene;
}
