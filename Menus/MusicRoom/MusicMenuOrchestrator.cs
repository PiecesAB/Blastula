using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;

namespace Blastula.Menus;

public partial class MusicMenuOrchestrator : Node
{
    [Export] public MusicSelectionMenu selectionMenu;
    [Export] public MusicDetailsMenu detailsMenu;
    [Export] public Sprite2D pausePlayForm;
    [Export] public Vector2 pausePlayFormSelectOffset = new Vector2(-70, 30);
    [Export] public Vector2 pausePlayDetailsPosition = new Vector2(600, 310);
    [Export] public AnimationPlayer modeSwapAnimator;

    public enum LoopMode
    {
        Loop_this_track = 0, Stop_at_end_of_track = 1, Lead_into_next_track = 2, Count = 3,
    }

    public LoopMode loopMode { get; set; } = LoopMode.Loop_this_track;

    public static void ResetLoopMode()
    {
        if (main == null) return;
        main.loopMode = LoopMode.Loop_this_track;
    }

    public const string ENCOUNTERED_SAVE_PATH = "user://encountered_music.csv";

    public static MusicMenuOrchestrator main;

    private int stunPausePlayForm = 0;

    #region Encountered Music Tracking

    private static HashSet<string> encounteredNodeNameSet = new();

    public static void LoadEncounteredMusic()
    {
        string ReadLine(FileAccess file)
        {
            string[] rowStrings = file.GetCsvLine();
            if (rowStrings == null || rowStrings.Length != 1) return null;
            if (rowStrings[0] == "") return null;
            return rowStrings[0];
        }

        FileAccess file = null;
        try
        {
            // This node should be in the kernel. Therefore, it will load the history file into memory before any session starts.
            file = Persistent.OpenOrCreateFile(ENCOUNTERED_SAVE_PATH, FileAccess.ModeFlags.Read);
            encounteredNodeNameSet = new();
            string line = null;
            while ((line = ReadLine(file)) != null) encounteredNodeNameSet.Add(line);
        }
        catch (Exception e)
        {
            GD.PushError($"Problem loading music encounters file: {e.Message}");
        }
        finally
        {
            if (file != null) file.Close();
        }
    }

    public static void SaveAllEncounteredMusic()
    {
        FileAccess file = null;
        try
        {
            file = Persistent.OpenOrCreateFile(ENCOUNTERED_SAVE_PATH, FileAccess.ModeFlags.Write);
            foreach (var s in encounteredNodeNameSet) file.StoreCsvLine(new string[] { s });
        }
        catch (Exception e)
        {
            GD.PushError($"Problem saving music encounters file: {e.Message}");
        }
        finally
        {
            if (file != null) file.Close();
        }
    }

    public static bool HasMusicBeenEncountered(Music music)
    {
        if (music.musicRoomDisplay == Music.MusicRoomDisplay.AlwaysUnlocked) return true;
        // music.Name uniquely identifies the music because it should've been found in the MusicManager node
        return encounteredNodeNameSet.Contains(music.Name);
    }

    /// <summary>
    /// Don't forget to save to file! Use SaveAllEncounteredMusic()
    /// </summary>
    public static void SetMusicEncountered(Music music)
    {
        if (music.musicRoomDisplay == Music.MusicRoomDisplay.AlwaysUnlocked) return;
        // music.Name uniquely identifies the music because it should've been found in the MusicManager node
        encounteredNodeNameSet.Add(music.Name);
    }

    #endregion

    public static void SwapToDetails(Music music)
    {
        if (main == null) throw new Exception("How swap to details without music room menu?");
        if (music.IsEncountered()) 
        { 
            MusicManager.PlayImmediate(music.Name, true, true);
            ResetLoopMode();
        }
        if (main.modeSwapAnimator != null)
        {
            main.modeSwapAnimator.Play("Details");
        }
        main.detailsMenu.Open();
    }

    public static void SwapToSelectionList()
    {
        if (main == null) throw new Exception("How swap to selection without music room menu?");
        if (main.modeSwapAnimator != null)
        {
            main.modeSwapAnimator.Play("Select");
        }
        main.stunPausePlayForm = 2;
    }

    public override void _Ready()
    {
        base._Ready();
        main = this;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (main == this) 
            main = null;
    }

    public override void _Process(double delta)
	{
        if (pausePlayForm != null)
        {
            Vector2 targetPosition = pausePlayForm.GlobalPosition;
            int targetFrame = 0;
            if (MusicManager.main.currentMusic != null && !MusicManager.main.currentMusic.StreamPaused)
            {
                targetFrame = pausePlayForm.Hframes * pausePlayForm.Vframes - 1;
            }

            if (selectionMenu.IsActive())
            {
                targetPosition = selectionMenu.GetSelectionNode().GlobalPosition + pausePlayFormSelectOffset;
            }

            if (detailsMenu.IsActive())
            {
                targetPosition = pausePlayDetailsPosition;
            }

            if (stunPausePlayForm > 0) { stunPausePlayForm--; }
            else { pausePlayForm.GlobalPosition = pausePlayForm.GlobalPosition.Lerp(targetPosition, 0.35f); }
            if (pausePlayForm.Frame < targetFrame) pausePlayForm.Frame++;
            if (pausePlayForm.Frame > targetFrame) pausePlayForm.Frame--;
        }
    }
}
