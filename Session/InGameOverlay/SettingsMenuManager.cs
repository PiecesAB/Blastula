using Blastula.Input;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Menus
{
	/// <summary>
	/// Handles logic for setting up and deleting the settings menu. This goes in the scene root of the settings scene.
	/// </summary>
	public partial class SettingsMenuManager : Control
	{
		public enum Mode
		{
			/// <summary>
			/// This is the settings menu outside of a game session.
			/// </summary>
			NoSession,
			/// <summary>
			/// This is the settings menu during a game session. Some options should be absent (like selecting extra life count)
			/// </summary>
			InSession
		}

		[Export] public Node sceneRoot;
		/// <summary>
		/// This sets the animation state to the Mode as a string,
		/// which causes the menu to change its options.
		/// Only the first frame of the animation is played,
		/// because we expect an instant transition.
		/// </summary>
		[Export] public AnimationPlayer modeAnimPlayer;
		public static Mode mode { get; private set; } = Mode.NoSession;
		[Export] public ListMenu mainMenu;

		public static SettingsMenuManager main { get; private set; }

		public static void SetMode(Mode newMode)
		{
			mode = newMode;
		}

		public void OpenInputBindingMenu()
		{
			Loader.LoadExternal(this, Persistent.INPUT_BINDING_MENU_PATH);
		}

		public override void _Ready()
		{
			ProcessPriority = Persistent.Priorities.PAUSE;
			modeAnimPlayer.Play(mode.ToString());
			modeAnimPlayer.Pause();
			modeAnimPlayer.Advance(0.1f);
			mainMenu.Open();
		}

		public override void _Process(double delta)
		{
			if (Session.main == null) { return; }
			if (!mainMenu.IsInStack())
			{
				SettingsLoader.Save();
				sceneRoot.QueueFree();
			}
		}
	}
}
