using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using System.Reflection;

namespace Blastula;

public partial class ReplayManager : Node
{
    public enum Mode { Record, Playback }

    public static ReplayManager main;
    public Mode mode = Mode.Playback;

    public const string SAVE_DIR = "user://replays/";

    [Signal] public delegate void ReplayStartsSoonEventHandler(float remainingSeconds);
    [Signal] public delegate void ReplayStartsNowEventHandler();

    /// <summary>
    /// Single player replay information storage.
    /// </summary>
    public class ReplayStore
    {
        public string rngSeed;
        public string stageSchedule;
        public string playerEntry;
        public Godot.Collections.Dictionary<string, string> sessionSnapshot;
        public Godot.Collections.Dictionary<string, string> stageSnapshot;
        public Godot.Collections.Dictionary<string, string> playerSnapshot;
        public List<byte> playerMovement;
    }

    public ReplayStore playbackStore = null;

    public Error Load(string fileNameNoExtension)
    {
        string path = $"{SAVE_DIR}{fileNameNoExtension}.rpy";
        FileAccess settingsFile = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (settingsFile == null) { return FileAccess.GetOpenError(); }
        playbackStore = new ReplayStore();
        settingsFile.GetPascalString(); // "Blastula Replay Data\n"
        settingsFile.GetPascalString(); // "\nrngSeed\n"
        playbackStore.rngSeed = settingsFile.GetPascalString();
        settingsFile.GetPascalString(); // "\nstageSchedule\n"
        playbackStore.stageSchedule = settingsFile.GetPascalString();
        settingsFile.GetPascalString(); // "\nplayerEntry\n"
        playbackStore.playerEntry = settingsFile.GetPascalString();
        settingsFile.GetPascalString(); // "\nsessionSnapshot\n"
        playbackStore.sessionSnapshot = Json.ParseString(settingsFile.GetPascalString()).AsGodotDictionary<string, string>();
        settingsFile.GetPascalString(); // "\nstageSnapshot\n"
        playbackStore.stageSnapshot = Json.ParseString(settingsFile.GetPascalString()).AsGodotDictionary<string, string>();
        settingsFile.GetPascalString(); // "\nplayerSnapshot\n"
        playbackStore.playerSnapshot = Json.ParseString(settingsFile.GetPascalString()).AsGodotDictionary<string, string>();
        settingsFile.GetPascalString(); // "\nplayerMovement\n"
        playbackStore.playerMovement = new List<byte>(System.Text.Encoding.UTF8.GetBytes(settingsFile.GetPascalString()));
        settingsFile.Close();
        return Error.Ok;
    }

    public Error Save(string fileNameNoExtension)
    {
        if (playbackStore == null) return Error.InvalidData;
        if (!DirAccess.DirExistsAbsolute(SAVE_DIR))
        {
            Error e = DirAccess.MakeDirAbsolute(SAVE_DIR);
            if (e != Error.Ok) return e;
        }
        string path = $"{SAVE_DIR}{fileNameNoExtension}.rpy";
        FileAccess settingsFile = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (settingsFile == null) { return FileAccess.GetOpenError(); }
        settingsFile.StorePascalString("Blastula Replay Data\n");
        settingsFile.StorePascalString("\nrngSeed\n");
        settingsFile.StorePascalString(playbackStore.rngSeed);
        settingsFile.StorePascalString("\nstageSchedule\n");
        settingsFile.StorePascalString(playbackStore.stageSchedule);
        settingsFile.StorePascalString("\nplayerEntry\n");
        settingsFile.StorePascalString(playbackStore.playerEntry);
        settingsFile.StorePascalString("\nsessionSnapshot\n");
        settingsFile.StorePascalString(Json.Stringify(playbackStore.sessionSnapshot));
        settingsFile.StorePascalString("\nstageSnapshot\n");
        settingsFile.StorePascalString(Json.Stringify(playbackStore.stageSnapshot));
        settingsFile.StorePascalString("\nplayerSnapshot\n");
        settingsFile.StorePascalString(Json.Stringify(playbackStore.playerSnapshot));
        settingsFile.StorePascalString("\nplayerMovement\n");
        settingsFile.StorePascalString(System.Text.Encoding.UTF8.GetString(playbackStore.playerMovement.ToArray()));
        settingsFile.Close();
        return Error.Ok;
    }

    private float internalTimeUntilReplay = 0f;

    public void ScheduleReplayStart()
    {
        EmitSignal(SignalName.ReplayStartsSoon, 0.25f);
        internalTimeUntilReplay = 0.25f;
    }

    public void StartSinglePlayerReplay()
    {
        // Any bullets or homologous items? Too bad. You're getting deleted.
        BNodeFunctions.ResetQueue();
        // Now we need to place or create a snapshot of the game state.
        Player player = Player.playersByControl[Player.Role.SinglePlayer];
        if (player == null) throw new System.Exception("Can't start replay: no player.");
        if (player.inputTranslator == null) throw new System.Exception("Can't start replay: Player has no input translator.");
        player.inputTranslator.Reset();
        player.inputTranslator.mode = mode;
        if (mode == Mode.Record)
        {
            uint rngSeed = StageManager.main.ReseedRNG();
            playbackStore = new()
            {
                rngSeed = rngSeed.ToString(),
                stageSchedule =
                    ReplayStart.current?.GetPath()
                    ?? throw new System.Exception("Can't start replay: no ReplayStart is listening."),
                playerEntry = player.entry.GetPath(),
                sessionSnapshot = Session.main.CreateReplaySnapshot(),
                stageSnapshot = StageManager.main.CreateReplaySnapshot(),
                playerSnapshot = player.CreateReplaySnapshot(),
                // The pass by reference is intentional.
                playerMovement = player.inputTranslator.currentRecording
            };
        }
        else
        {
            if (playbackStore == null) throw new System.Exception("Can't view replay: No data.");
            if (player.entry.GetPath() != playbackStore.playerEntry) throw new System.Exception("Can't view replay: Wrong player was loaded.");
            if (!(uint.Parse(playbackStore?.rngSeed ?? "") is uint rngSeed)) throw new System.Exception("Can't view replay: Malformed RNG.");
            // Assuming the replay store, stage schedule, and player have already been loaded by the replay menu
            StageManager.main.ReseedRNG(rngSeed);
            Session.main.LoadReplaySnapshot(playbackStore.sessionSnapshot);
            StageManager.main.LoadReplaySnapshot(playbackStore.stageSnapshot);
            player.LoadReplaySnapshot(playbackStore.playerSnapshot);
            player.inputTranslator.currentRecording = playbackStore.playerMovement;
        }
    }

    public void EndSinglePlayerReplay()
    {
        Player player = Player.playersByControl[Player.Role.SinglePlayer];
        if (player == null) throw new System.Exception("Can't end replay: no player.");
        if (player.inputTranslator == null) throw new System.Exception("Can't end replay: Player has no input translator.");
        player.inputTranslator.End();
    }

    public override void _Ready()
    {
        main = this;
        // This forces the Replay to start very early in the frame, just in case.
        ProcessPriority = Persistent.Priorities.REPLAY_MANAGER;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (internalTimeUntilReplay > 0)
        {
            internalTimeUntilReplay -= 1f / Persistent.SIMULATED_FPS;
            if (internalTimeUntilReplay < 0.001f)
            {
                internalTimeUntilReplay = 0;
                // The player should have responded to the signal.
                EmitSignal(SignalName.ReplayStartsNow);
                StartSinglePlayerReplay();
            }
        }
    }
}
