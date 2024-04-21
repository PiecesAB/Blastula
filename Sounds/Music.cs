using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula.Sounds
{ 
    /// <summary>
    /// Represents a track of background music. 
    /// Should have a unique name and be a descendent of the MusicManager.
    /// </summary>
    public partial class Music : AudioStreamPlayer
    {
        /// <summary>
        /// The full name of the music, which can be displayed to the player.
        /// </summary>
        [Export] public string fullName;
        /// <summary>
        /// The music's description, which can be displayed to the player in rich text (Godot BBCode) form.
        /// </summary>
        [Export(PropertyHint.MultilineText)] public string description;
        /// <summary>
        /// If this is not the zero vector, 
        /// the music will loop back to X seconds after it surpasses Y seconds.
        /// </summary>
        [Export] public Vector2 loopRegion = Vector2.Zero;
        /// <summary>
        /// If true, the music pauses when the game is paused.
        /// </summary>
        [Export] public bool pausesWithGame = true;
    }
}
