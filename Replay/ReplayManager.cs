using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula;

public partial class ReplayManager : Node
{
	public enum Mode { Record, Playback }
	public enum PlayState { NotPlaying, GettingReadyToPlay, Playing }

	public static ReplayManager main;
	public Mode mode = Mode.Record;
	public PlayState playState = PlayState.NotPlaying;

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

	/// <summary>
	/// Store information about the final state of the session for record purposes; 
	/// it is certainly not full replay, and it may not correspond to any one replay section.
	/// </summary>
	public class FinalResultsStore
	{
		public string playerEntry;
		public Godot.Collections.Dictionary<string, string> sessionSnapshot;
	}

	public ReplayStore playbackStore = null;

	/// <summary>
	/// The grouping directory to store replay segments and final state info for this game session.
	/// </summary>
	public string sessionDirectoryName = "";
	public string currentSectionFileName = "";

	public Error SetSessionStart(string givenDirectory = "")
	{
		if (mode == Mode.Record)
		{
			sessionDirectoryName = Guid.NewGuid().ToString() + "/";
			while (DirAccess.DirExistsAbsolute($"{SAVE_DIR}sections/{sessionDirectoryName}"))
			{
				sessionDirectoryName = Guid.NewGuid().ToString() + "/";
			}
			Error e;
			if (!DirAccess.DirExistsAbsolute(SAVE_DIR))
			{
				e = DirAccess.MakeDirAbsolute(SAVE_DIR);
				if (e != Error.Ok) return e;
			}
			if (!DirAccess.DirExistsAbsolute($"{SAVE_DIR}sections/"))
			{
				e = DirAccess.MakeDirAbsolute($"{SAVE_DIR}sections/");
				if (e != Error.Ok) return e;
			}
			if (!DirAccess.DirExistsAbsolute($"{SAVE_DIR}sections/{sessionDirectoryName}"))
			{
				e = DirAccess.MakeDirAbsolute($"{SAVE_DIR}sections/{sessionDirectoryName}");
				if (e != Error.Ok) return e;
			}
		}
		else
		{
			if (givenDirectory is null or "")
			{
				GD.PushError("You are in playback mode while starting the replay session, but no directory is given. I can't play anything like this.");
				return Error.FileBadPath;
			}
			sessionDirectoryName = givenDirectory;
		}
		return Error.Ok;
	}

	public Error SetSessionEnd()
	{
		if (mode == Mode.Record && sessionDirectoryName != "")
		{
			// We shall now save the final session info.
			string path = $"{SAVE_DIR}sections/{sessionDirectoryName}results.bin";
			FileAccess resultsFile = FileAccess.Open(path, FileAccess.ModeFlags.Write);
			if (resultsFile == null) { return FileAccess.GetOpenError(); }

			FinalResultsStore finalStore = new();
			finalStore.playerEntry = playbackStore.playerEntry;
			finalStore.sessionSnapshot = Session.main.CreateReplaySnapshot();

			resultsFile.StorePascalString("Blastula Final Results Data\n");
			resultsFile.StorePascalString("\nplayerEntry\n");
			resultsFile.StorePascalString(finalStore.playerEntry);
			resultsFile.StorePascalString("\nsessionSnapshot\n");
			resultsFile.StorePascalString(Json.Stringify(finalStore.sessionSnapshot));
			resultsFile.Close();

			// Don't clear the session directory name here; we may want to erase the folder in the menu.
		}
		return Error.Ok;
	}

	public Error EraseRecordedSessionFolder()
	{
		if (mode == Mode.Record && sessionDirectoryName != "")
		{
			string path = $"{SAVE_DIR}sections/{sessionDirectoryName}";
			// Assume there are no subdirectories in this session directory.
			Error e;
			foreach (string file in DirAccess.GetFilesAt(path)) {
				e = DirAccess.RemoveAbsolute(path + file);
				if (e != Error.Ok) { GD.Print("delete error " + path + file + "  " + e); return e; }
			}
			e = DirAccess.RemoveAbsolute(path);
			if (e != Error.Ok) { GD.Print("delete error " + e); return e; }
			sessionDirectoryName = "";
		}
		return Error.Ok;
	}

	public Error LoadSection()
	{
		if (sessionDirectoryName == "" || currentSectionFileName == "") 
		{ 
			GD.PushError("Can't load replay section without a grouping folder and file name.");
			return Error.FileNotFound;
		}
		string path = $"{SAVE_DIR}sections/{sessionDirectoryName}{currentSectionFileName}.bin";
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

	public Error SaveSection()
	{
		if (sessionDirectoryName == "")
		{
			GD.PushError("Can't save replay section without a grouping folder.");
			return Error.FileNotFound;
		}
		if (playbackStore == null) return Error.InvalidData;
		string path = $"{SAVE_DIR}sections/{sessionDirectoryName}{currentSectionFileName}.bin";
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

	public void ScheduleReplayStart(string newSectionFileName)
	{
		if (playState == PlayState.Playing)
		{
			EndSinglePlayerReplaySection();
		}
		EmitSignal(SignalName.ReplayStartsSoon, 0.25f);
		internalTimeUntilReplay = 0.25f;
		playState = PlayState.GettingReadyToPlay;
		currentSectionFileName = newSectionFileName;
	}

	public void StartSinglePlayerReplaySection()
	{
		if (playState != PlayState.GettingReadyToPlay)
		{
			GD.PushWarning("Starting the replay, but just to let you know, I wasn't ready for it. Probably symptomatic of a deeper problem.");
		}
		playState = PlayState.Playing;
		// Any bullets or homologous items? Too bad. You're getting deleted.
		BNodeFunctions.ResetQueue();
		// Now we need to place or create a snapshot of the game state.
		Player.playersByControl.TryGetValue(Player.Role.SinglePlayer, out Player player);
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

	public void EndSinglePlayerReplaySection()
	{
		if (playState == PlayState.NotPlaying)
		{
			GD.PushWarning("I don't think there is a replay going on; not ending it.");
			return;
		}
		if (playState == PlayState.GettingReadyToPlay)
		{
			GD.PushWarning("You shouldn't be trying to end the replay before it even started. Try waiting a bit.");
			return;
		}
		Player.playersByControl.TryGetValue(Player.Role.SinglePlayer, out Player player);
		if (player == null) throw new System.Exception("Can't end replay: no player.");
		if (player.inputTranslator == null) throw new System.Exception("Can't end replay: Player has no input translator.");
		player.inputTranslator.End();
		SaveSection();
		currentSectionFileName = "";
		playState = PlayState.NotPlaying;
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
				StartSinglePlayerReplaySection();
			}
		}
	}
}
