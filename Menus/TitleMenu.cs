using Blastula.Coroutine;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Menus
{
    /// <summary>
    /// Handles the main title menu and contains functions for sub-menus as well.
    /// </summary>
    public partial class TitleMenu : ListMenu
    {
        /// <summary>
        /// Root node of the title menu to delete when the game is ready to play.
        /// </summary>
        [Export] public Node root;
        /// <summary>
        /// Background music node name for the main menu.
        /// </summary>
        [Export] public string music;

        public override void _Ready()
        {
            base._Ready();
            Open();
            MusicManager.PlayImmediate(music);
        }

        public void Start()
        {
            // TODO: be able to select stuff.
            MusicManager.Stop();
            if (StageManager.main != null)
            {
                this.StartCoroutine(StageManager.main.InitializeSinglePlayerSession("PictusXXIV", "MainSequence"));
            }
            Close();
            root.QueueFree();
        }

        public void OpenSettingsMenu()
        {
            SettingsMenuManager.SetMode(SettingsMenuManager.Mode.NoSession);
            Loader.LoadExternal(this, Persistent.SETTINGS_MENU_PATH);
        }

        public void QuitGame()
        {
            GetTree().Quit();
        }
    }
}

