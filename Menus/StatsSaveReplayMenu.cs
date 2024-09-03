using Blastula.Coroutine;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Menus
{
    /// <summary>
    /// The menu after the game ends; asks the user to save score and replay.
    /// </summary>
    public partial class StatsSaveReplayMenu : ListMenu
    {
        /// <summary>
        /// Root node of the menu to delete when it completes its function.
        /// </summary>
        [Export] public Node root;
        /// <summary>
        /// Background music node name for the menu.
        /// This can be the same as the title menu's music for continuation.
        /// </summary>
        [Export] public string music;
        [Export] public AnimationPlayer menuSelector;
        [Export] public BaseMenu replayMenu;

        public override void _Ready()
        {
            base._Ready();
            Open();
            MusicManager.PlayImmediate(music);
        }

        public void NoSave()
        {
            ReplayManager.main?.EraseRecordedSessionFolder();
            Close();
            root.QueueFree();
            Loader.LoadExternal(this, Persistent.TITLE_MENU_PATH);
        }

        public void YesSave()
        {
            menuSelector.Play("Entry");
            replayMenu.Open();
        }

        public override void ReturnControl()
        {
            base.ReturnControl();
            menuSelector.Play("Landing");
        }
    }
}

