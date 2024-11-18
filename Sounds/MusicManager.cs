using Blastula.Menus;
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
        [Export] public float volumeMultiplier { get; private set; } = 1f;

        public float settingMultiplier { get; private set; } = 0;

        [Signal] public delegate void OnMusicChangeEventHandler(Music oldMusic, Music newMusic);
        
        public static MusicManager main { get; private set; } = null;
        private string currentMusicNodePath = "";

        public Music currentMusic { get; private set; } = null;
        private bool currentMusicStartedFromMusicRoom = false;
        public Dictionary<string, Music> musicsByNodeName = new();
        private List<Music> musicInMenuOrder = new();
        /// <summary>
        /// Linear volumes that the AudioStreamPlayer nodes begin with.
        /// </summary>
        private Dictionary<string, float> startVolumesByNodeName = new();

        private float duckMultiplier = 1f;
        private struct DuckInfo
        {
            public float duration;
            public float multiplier;
        }
        private List<DuckInfo> ongoingDucks = new List<DuckInfo>();

        private float fadeMultiplier = 1f;
        private struct VolumeFadeInfo
        {
            public bool fadeIn;
            public float totalDuration;
            public float currTime;
        }
        private VolumeFadeInfo currFadeInfo = new VolumeFadeInfo { totalDuration = 0f };

        private class SyncedFadeInfo
        {
            public List<float> startLinearVolumes = null;
            public List<float> nextLinearVolumes = null;
            public float totalDuration;
            public float currTime;
        }

        private SyncedFadeInfo syncedFadeInfo = new SyncedFadeInfo { totalDuration = 0f };

        public IReadOnlyList<Music> GetAllMusics() => musicInMenuOrder;

        private void MusicSearch()
        {
            UtilityFunctions.PathBuilder(
                this, 
                (c, path) => { 
                    if (c is Music cm) { 
                        musicsByNodeName[path] = cm;
                        startVolumesByNodeName[path] = Mathf.DbToLinear(cm.VolumeDb);
                        musicInMenuOrder.Add(cm);
                        cm.Finished += main.OnMusicAbruptEnd;
                    } 
                }, 
                true
            );
        }

        /// <summary>
        /// If true, this is a synchronized track.
        /// </summary>
        public static AudioStreamSynchronized CurrentMusicAsSynchronized()
            => main?.currentMusic?.Stream is AudioStreamSynchronized s ? s : null;

        /// <summary>
        /// Play a piece of music as referenced by its name (or possibly path) in the Godot hierarchy.
        /// This ends the current music immediately.
        /// </summary>
        /// <param name="continueSameMusic">If true, the music won't reset if the same track is already playing.</param>
        public static void PlayImmediate(string nodeName, bool continueSameMusic = true, bool startedFromMusicRoom = false)
        {
            if (main == null) { return; }
            if (main.currentMusic != null) 
            {
                if (continueSameMusic 
                    && main.currentMusic.Name == nodeName
                    && startedFromMusicRoom == main.currentMusicStartedFromMusicRoom) { 
                    return; 
                }
                else { main.currentMusic.Stop(); }
                main.currentMusic = null;
            }
            if (!main.musicsByNodeName.ContainsKey(nodeName)) { return; }
            Music nextMusic = main.musicsByNodeName[nodeName];
            main.EmitSignal(SignalName.OnMusicChange, main.currentMusic, nextMusic);
            main.currentMusicNodePath = nodeName;
            main.currentMusic = nextMusic;
            main.currentMusic.PitchScale = 1;
            main.fadeMultiplier = 1f;
            main.currFadeInfo = new VolumeFadeInfo { totalDuration = 0f };
            main.syncedFadeInfo = new SyncedFadeInfo { totalDuration = 0f };
            if (CurrentMusicAsSynchronized() is AudioStreamSynchronized currSynced)
            {
                main.syncedFadeInfo = new SyncedFadeInfo() 
                { 
                    startLinearVolumes = new(),
                    nextLinearVolumes = new(),
                    totalDuration = 0f 
                };
                // stub behavior for synced tracks; just play the first one for now
                for (int i = 0; i < currSynced.StreamCount; ++i)
                {
                    if (i == 0) currSynced.SetSyncStreamVolume(0, Mathf.LinearToDb(1f));
                    else currSynced.SetSyncStreamVolume(i, Mathf.LinearToDb(0f));
                    main.syncedFadeInfo.startLinearVolumes.Add((i == 0) ? 1f : 0f);
                    main.syncedFadeInfo.nextLinearVolumes.Add((i == 0) ? 1f : 0f);
                }
            } 
            else
            {
                main.syncedFadeInfo = new SyncedFadeInfo() { totalDuration = 0f };
            }
            main.currentMusic.Play();
            main.currentMusicStartedFromMusicRoom = startedFromMusicRoom;
        }

        public static void MusicRoomTogglePause()
        {
            if (main.currentMusic != null)
            {
                main.currentMusic.StreamPaused = !main.currentMusic.StreamPaused;
            }
        }

        /// <summary>
        /// Stop the music.
        /// </summary>
        public static void Stop()
        {
            PlayImmediate("");
            main.currentMusicNodePath = "";
            main.currentMusic = null;
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
        public static void Duck(float duration, float volume = 0.3f)
        {
            if (main == null) { return; }
            main.ongoingDucks.Add(new DuckInfo
            {
                duration = duration,
                multiplier = volume,
            });
        }

        public static void FadeIn(float duration)
        {
            if (main == null) { return; }
            main.currFadeInfo = new VolumeFadeInfo
            {
                fadeIn = true,
                totalDuration = duration,
                currTime = 0f,
            };
        }

        public static void FadeOut(float duration)
        {
            if (main == null) { return; }
            main.currFadeInfo = new VolumeFadeInfo
            {
                fadeIn = false,
                totalDuration = duration,
                currTime = 0f,
            };
        }

        public static IReadOnlyList<float> GetSyncedTargetList()
        {
            return main?.syncedFadeInfo?.nextLinearVolumes;
        }

        public static void StartSyncedFade(float duration, List<float> nextLinearVolumes)
        {
            if (main == null) { return; }
            if (main?.currentMusic?.Stream is not AudioStreamSynchronized currSynced) { return; }
            List<float> currLinearVolumes = new();
            for (int i = 0; i < currSynced.StreamCount; ++i)
            {
                currLinearVolumes.Add(Mathf.DbToLinear(currSynced.GetSyncStreamVolume(i)));
            }

            main.syncedFadeInfo = new SyncedFadeInfo
            {
                startLinearVolumes = currLinearVolumes,
                nextLinearVolumes = nextLinearVolumes,
                currTime = 0f,
                totalDuration = duration,
            };
        }

        /// <summary>
        /// Set the pitch of the current music.
        /// </summary>
        public static void SetPitch(float newPitch)
        {
            if (main == null) { return; }
            if (main.currentMusic == null) { return; }
            main.currentMusic.PitchScale = newPitch;
        }

        public static void SetVolumeMultiplier(float newMul)
        {
            if (main == null) { return; }
            main.volumeMultiplier = newMul;
        }

        public static void UseMusicSetting(string setting)
        {
            if (main == null) { return; }
            if (int.TryParse(setting, out int settingNum))
            {
                main.settingMultiplier = 0.01f * settingNum * settingNum;
            }
        }

        public static bool IsMusicMuted()
            => main != null ? main.settingMultiplier <= 0 : false;

        public override void _Ready()
        {
            base._Ready();
            main = this;
            MusicMenuOrchestrator.LoadEncounteredMusic();
            MusicSearch();
            ProcessPriority = Persistent.Priorities.MUSIC_MANAGER;
        }

        public void HandleDuckMultiplier()
        {
            float desiredMultiplier = 1f;
            float timePassed = 1f / Persistent.SIMULATED_FPS; // No scaling
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

        public void HandleFadeMultiplier()
        {
            if (currFadeInfo.totalDuration <= 0f) { return; }
            if (currentMusic.StreamPaused) { return; }
            if (currFadeInfo.currTime >= currFadeInfo.totalDuration)
            {
                if (currFadeInfo.fadeIn) { fadeMultiplier = 1f; }
                else { fadeMultiplier = 0f; }
            }
            else
            {
                float progress = Mathf.Clamp(currFadeInfo.currTime / currFadeInfo.totalDuration, 0, 1);
                float timePassed = 1f / Persistent.SIMULATED_FPS; // No scaling
                if (currFadeInfo.fadeIn) { fadeMultiplier = progress; }
                else { fadeMultiplier = 1f - progress; }
                currFadeInfo = new VolumeFadeInfo
                {
                    fadeIn = currFadeInfo.fadeIn,
                    totalDuration = currFadeInfo.totalDuration,
                    currTime = currFadeInfo.currTime + timePassed
                };
            }
        }

        public void HandleSyncedFade()
        {
            if (syncedFadeInfo.totalDuration <= 0f) { return; }
            if (main?.currentMusic?.Stream is not AudioStreamSynchronized currSynced) { return; }
            if (currentMusic.StreamPaused) { return; }
            if (syncedFadeInfo.currTime >= syncedFadeInfo.totalDuration)
            {
                for (int i = 0; i < currSynced.StreamCount; ++i)
                {
                    currSynced.SetSyncStreamVolume(i, Mathf.LinearToDb(syncedFadeInfo.nextLinearVolumes[i]));
                }
                syncedFadeInfo.totalDuration = 0f;
            }
            else
            {
                float progress = Mathf.Clamp(syncedFadeInfo.currTime / syncedFadeInfo.totalDuration, 0, 1);
                float timePassed = 1f / Persistent.SIMULATED_FPS; // No scaling

                for (int i = 0; i < currSynced.StreamCount; ++i)
                {
                    float interpolated = Mathf.Lerp(
                        syncedFadeInfo.startLinearVolumes[i], 
                        syncedFadeInfo.nextLinearVolumes[i], 
                        progress);
                    currSynced.SetSyncStreamVolume(i, Mathf.LinearToDb(interpolated));
                }

                syncedFadeInfo.currTime += timePassed;
            }
        }

        public void HandleVolume()
        {
            currentMusic.VolumeDb = Mathf.LinearToDb(
                volumeMultiplier * settingMultiplier
                * startVolumesByNodeName[currentMusicNodePath]
                * duckMultiplier * fadeMultiplier
            );
        }

        public void HandlePause()
        {
            if (currentMusicStartedFromMusicRoom) return;

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
        }

        public void HandleLoop()
        {
            if (currentMusicStartedFromMusicRoom 
                && MusicMenuOrchestrator.main != null
                && MusicMenuOrchestrator.main.loopMode != MusicMenuOrchestrator.LoopMode.Loop_this_track)
            {
                return;
            }

            if (currentMusic.loopRegion != Vector2.Zero)
            {
                // this is not SyncedMusicManager
                float currPos = currentMusic.GetPlaybackPosition();
                if (currPos >= currentMusic.loopRegion.Y)
                {
                    currentMusic.Seek(currPos - (currentMusic.loopRegion.Y - currentMusic.loopRegion.X));
                }
            }
        }

        public void OnMusicAbruptEnd()
        {
            if (currentMusicStartedFromMusicRoom
                && MusicMenuOrchestrator.main != null)
            {
                MusicMenuOrchestrator.main.TrackAbruptlyEnded();
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (currentMusic == null) { return; }

            HandleDuckMultiplier();
            HandleFadeMultiplier();
            HandleSyncedFade();
            HandleVolume();
            HandlePause();
            HandleLoop();
        }
    }
}
