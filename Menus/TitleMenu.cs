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
            // This is a placeholder.
            MusicManager.Stop();
            if (StageManager.main != null)
            {
                _ = StageManager.main.InitializeSinglePlayerSession("PictusXXIV", "MainSequence");
            }
            root.QueueFree();
        }

        public void QuitGame()
        {
            GetTree().Quit();
        }
    }
}

