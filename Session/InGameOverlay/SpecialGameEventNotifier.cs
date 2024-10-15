using Blastula.Input;
using Blastula.Menus;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
    /// <summary>
    /// Handles the display that shows special game events such as getting a score extend or high score.
    /// </summary>
    public partial class SpecialGameEventNotifier : Control
    {
        public enum EventType
        {
            Extend, CaptureBonus, HighScore
        }

        [Export] public AnimationPlayer animationPlayer;

        public static SpecialGameEventNotifier main { get; private set; }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            // Set the animator time to something very late, as if it has already finished long ago.
            animationPlayer.Seek(1000, true);
        }

        public static void Trigger(EventType eventType)
        {
            if (main == null) return;
            main.animationPlayer.Play(eventType.ToString());
        }
    }
}
