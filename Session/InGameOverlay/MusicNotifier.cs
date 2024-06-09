using Blastula.Input;
using Blastula.Menus;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
    /// <summary>
    /// Handles the display that shows the music's title when it changes.
    /// </summary>
    public partial class MusicNotifier : Control
    {
        /// <summary>
        /// Every time a new music is displayed, the animation is retriggered.
        /// This is driven automatically in Godot (instead of by the script)
        /// because it shouldn't have an effect on any game logic.
        /// </summary>
        [Export] public AnimationPlayer animationPlayer;
        /// <summary>
        /// This text is changed to the Music's full name (not the node name).
        /// </summary>
        [Export] public Label mainText;

        public void OnMusicChange(Music oldMusic, Music newMusic)
        {
            if (Session.main == null || !Session.main.inSession) { return; }
            if (newMusic == null) { return; }
            if (!newMusic.displaysNotification) { return; }
            mainText.Text = newMusic.fullName;
            animationPlayer.Active = true;
            animationPlayer.Stop();
            animationPlayer.Play();
        }

        public override void _Ready()
        {
            base._Ready();
            // Set the animator time to something very late, as if it has already finished long ago.
            animationPlayer.Seek(1000, true);
            animationPlayer.Active = false;
            if (MusicManager.main != null)
            {
                MusicManager.main.Connect(
                    MusicManager.SignalName.OnMusicChange,
                    new Callable(this, MethodName.OnMusicChange)
                );
            }
        }
    }
}
