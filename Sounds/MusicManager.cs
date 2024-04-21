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

        private float duckMultiplier = 1f;
        private struct DuckInfo
        {
            public float duration;
            public float multiplier;
        }
        private List<DuckInfo> ongoingDucks = new List<DuckInfo>();

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

        /// <summary>
        /// Cause the music to become quieter for some time.
        /// </summary>
        public static void Duck(float duration, float multiplier = 0.3f)
        {
            if (main == null) { return; }
            main.ongoingDucks.Add(new DuckInfo
            {
                duration = duration,
                multiplier = multiplier,
            });
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            MusicSearch();
            PlayImmediate("Reconception");
            ProcessPriority = Persistent.Priorities.MUSIC_MANAGER;
        }

        private void HandleDuckMultiplier()
        {
            float desiredMultiplier = 1f;
            float timePassed = 1f / Persistent.SIMULATED_FPS;
            for (int i = 0; i < ongoingDucks.Count; ++i)
            {
                DuckInfo di = ongoingDucks[i];
                ongoingDucks[i] = new DuckInfo
                {
                    duration = di.duration - timePassed,
                    multiplier = di.multiplier
                };
                desiredMultiplier = Mathf.Min(desiredMultiplier, di.multiplier);
                if (ongoingDucks[i].duration <= 0f)
                {
                    ongoingDucks.RemoveAt(i);
                    --i;
                }
            }
            if (desiredMultiplier < duckMultiplier)
            {
                duckMultiplier = desiredMultiplier;
            }
            else
            {
                duckMultiplier = Mathf.MoveToward(duckMultiplier, desiredMultiplier, timePassed);
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (currentMusic == null) { return; }

            HandleDuckMultiplier();

            currentMusic.VolumeDb = Mathf.LinearToDb(
                volumeMultiplier 
                * startVolumesByNodeName[currentMusicNodePath]
                * duckMultiplier
            );

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
