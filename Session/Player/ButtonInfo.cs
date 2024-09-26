using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Input
{
	/// <summary>
	/// Used to denote a default input for the InputManager.
	/// </summary>
	[Icon(Persistent.NODE_ICON_PATH + "/joystick.png")]
	public partial class ButtonInfo : Node
	{
		/// <summary>
		/// Default key that triggers this button when other inputs aren't configured or aren't available.
		/// </summary>
		[Export] public Key defaultKey = Key.None;
		/// <summary>
		/// If true, the input will only work in a debug context.
		/// </summary>
		[Export] public bool debugOnly = false;
	}
}
