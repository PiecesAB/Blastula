using Godot;

namespace Blastula.Sounds
{
    /// <summary>
    /// Unfinished!
    /// </summary>
    public partial class Music : AudioStreamPlayer
    {
        public static Music main { get; private set; } = null;

        public override void _Ready()
        {
            base._Ready();
            main = this;
        }
    }
}
