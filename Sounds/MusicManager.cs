using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;

namespace Blastula.Sounds
{
    /// <summary>
    /// Handles the game's background music.
    /// </summary>
    public partial class MusicManager : Node
    {
        [Export] public float volumeMultiplier = 1f;

        public static MusicManager main { get; private set; } = null;

        private string currentMusicNodePath = "";
        public Music currentMusic { get; private set; } = null;

        private Dictionary<string, Music> musicsByNodeName = new Dictionary<string, Music>();
        /// <summary>
        /// Linear volumes that the AudioStreamPlayer nodes begin with.
        /// </summary>
        private Dictionary<string, float> startVolumesByNodeName = new Dictionary<string, float>();

        private void MusicSearch()
        {
            UtilityFunctions.PathBuilder(
                this, 
                (c, path) => { 
                    if (c is Music) { 
                        musicsByNodeName[path] = (Music)c;
                        startVolumesByNodeName[path] = Mathf.DbToLinear(((Music)c).VolumeDb);
                    } 
                }, 
                true
            );
        }

        /// <summary>
        /// Play a piece of music as referenced by its name (or possibly path) in the Godot hierarchy.
        /// This ends the current music immediately.
        /// </summary>
        public static void PlayImmediate(string nodeName)
        {
            if (main == null) { return; }
            if (main.currentMusic != null) 
            { 
                main.currentMusic.Stop(); 
            }
            if (!main.musicsByNodeName.ContainsKey(nodeName))
            {
                return;
            }
            main.currentMusicNodePath = nodeName;
            main.currentMusic = main.musicsByNodeName[nodeName];
            main.currentMusic.Play();
        }

        /// <summary>
        /// Places the current music at this time in seconds.
        /// </summary>
        public static void Seek(float time)
        {
            if (main == null) { return; }
            if (main.currentMusic == null) { return; }
            main.currentMusic.Seek(time);
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            MusicSearch();
            PlayImmediate("Reconception");
            ProcessPriority = Persistent.Priorities.MUSIC_MANAGER;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (currentMusic == null) { return; }

            currentMusic.VolumeDb = Mathf.LinearToDb(volumeMultiplier * startVolumesByNodeName[currentMusicNodePath]);

            if (currentMusic.pausesWithGame && Session.main != null)
            {
                if (Session.main.paused && !currentMusic.StreamPaused)
                {
                    currentMusic.StreamPaused = true;
                }

                if (!Session.main.paused && currentMusic.StreamPaused)
                {
                    currentMusic.StreamPaused = false;
                }
            }

            if (currentMusic.loopRegion != Vector2.Zero)
            {
                float currPos = currentMusic.GetPlaybackPosition();
                if (currPos >= currentMusic.loopRegion.Y)
                {
                    currentMusic.Seek(currPos - (currentMusic.loopRegion.Y - currentMusic.loopRegion.X));
                }
            }
        }
    }
}
