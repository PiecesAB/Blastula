using Blastula.Menus;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula.Sounds
{
    /// <summary>
    /// Represents a track of background music. 
    /// Should have a unique name and be a descendent of the MusicManager.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/wolfDark.png")]
    public partial class Music : AudioStreamPlayer
    {
        /// <summary>
        /// The title of the music, which can be displayed to the player in various situations.
        /// </summary>
        [Export] public string title = "Unnamed Music";
        /// <summary>
        /// The composer of the music.
        /// </summary>
        [Export] public string composer = "";
        /// <summary>
        /// The music's description, which can be displayed to the player in rich text (Godot BBCode) form.
        /// </summary>
        [Export(PropertyHint.MultilineText)] public string description = "";
        /// <summary>
        /// If this is not the zero vector, 
        /// the music will loop back to X seconds after it surpasses Y seconds.
        /// </summary>
        [Export] public Vector2 loopRegion = Vector2.Zero;
        /// <summary>
        /// If true, the music pauses when the game is paused.
        /// </summary>
        [Export] public bool pausesWithGame = true;
        /// <summary>
        /// If true, when the music changes to this one, its name is displayed in the playfield.
        /// For more information, see "MusicNotifier" class.
        /// </summary>
        [Export] public bool displaysNotification = true;

        /// <summary>
        /// Describes how a track appears in the music room.
        /// </summary>
        public enum MusicRoomDisplay
        {
            /// <summary>
            /// The track will appear as a "locked" form which unlocks once encountered in the game.
            /// </summary>
            Normal, 
            /// <summary>
            /// The track will always appear unlocked.
            /// </summary>
            AlwaysUnlocked, 
            /// <summary>
            /// The track never has an entry in the music room.
            /// </summary>
            Hidden
        }

        /// <summary>
        /// How does this track appear in the music room?
        /// </summary>
        [Export] public MusicRoomDisplay musicRoomDisplay = MusicRoomDisplay.Normal;

        /// <summary>
        /// Consults a file to determine whether a track has been encountered in a session.
        /// </summary>
        public bool IsEncountered() => 
            MusicMenuOrchestrator.HasMusicBeenEncountered(this);

        private int trackNumber = -1;
        public int GetTrackNumber()
        {
            if (!areTracksEnumerated)
                EnumerateTracks();
            if (!areTracksEnumerated)
                GD.PushWarning("Music track numbers were not enumerated due to abnormal game state. It may be incorrect.");
            return trackNumber;
        }

        private static bool areTracksEnumerated = false;
        public static void EnumerateTracks()
        {
            if (areTracksEnumerated) return;
            if (MusicManager.main == null) return;
            IReadOnlyList<Music> all = MusicManager.main.GetAllMusics();
            for (int i = 0; i < all.Count; ++i) all[i].trackNumber = i + 1;
            areTracksEnumerated = true;
        }
    }
}
