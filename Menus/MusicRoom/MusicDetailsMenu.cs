using Blastula.Input;
using Blastula.Sounds;
using Godot;
using System;
using System.IO;

namespace Blastula.Menus;

/// <summary>
/// Dear god, it's all idiosyncratic. This menu is VERY dependent on the structure of the existing music room.
/// If you're thinking of significantly editing it, you might want to start from scratch.
/// </summary>
public partial class MusicDetailsMenu : ListMenu
{
	[Export] public Label infoLabel;
	[Export] public AnimationPlayer adjustmentAnimator;
	[Export] public MusicDetailsListNode mixButton;
	[ExportGroup("Seeker")]
    [Export] public Control seekerBack;
    [Export] public Control seekerLoopRegion;
    [Export] public Control seekerFront;
    [Export] public Control seekerCircle;
    [Export] public Label seekerCurrentTime;
    [Export] public Label seekerTotalTime;
	[ExportGroup("Info Display")]
	[Export] public Label title;
	[Export] public Label composer;
	[Export] public Label trackNumber;
	[Export] public RichTextLabel description;
	[ExportGroup("Sounds")]
    [Export] public string soundSwitch;
    [Export] public string soundExitAdjust;
    [Export] public string soundAdjustLeft;
    [Export] public string soundAdjustRight;
    /// <summary>
    /// Four of these: Muted, Quiet, Mid, Loud
    /// </summary>
    [ExportGroup("Icons")]
	[Export] public Texture2D[] volumeTextures;
	[Export] public TextureRect volumeTextureItem;
	/// <summary>
	/// Three of these: Slow, Mid, Fast
	/// </summary>
    [Export] public Texture2D[] pitchTextures;
	[Export] public TextureRect pitchTextureItem;
	/// <summary>
	/// Three of these: LoopThisTrack, StopAtEndOfTrack, LeadIntoNextTrack
	/// </summary>
	[Export] public Texture2D[] loopTextures;
	[Export] public TextureRect loopTextureItem;


	private enum AdjustMode
	{
		NotAdjusting, Seek, Volume, Adaptation, LoopMode, Pitch
	}

	private AdjustMode adjustMode = AdjustMode.NotAdjusting;
	private bool wasPausedBeforeSeekBegan = false;
	private float seekedPosition = 0;

	private Music lastMusic = null;

    public override void Open()
    {
        base.Open();
		adjustMode = AdjustMode.NotAdjusting;
		UpdateIcons();
        adjustmentAnimator.Play("Display");
    }

    public override void Close()
	{
		base.Close();
		MusicMenuOrchestrator.SwapToSelectionList();
	}

    #region Start Actions

    public void PlayPause()
	{
		base.Close();
		MusicManager.MusicRoomTogglePause();
		Open();
	}

	public void StartStandardSelections()
	{
		adjustmentAnimator.Play("Edit");
	}

	public void StartSeek()
	{
		StartStandardSelections();
		adjustMode = AdjustMode.Seek;
		if (MusicManager.main.currentMusic != null)
		{
            wasPausedBeforeSeekBegan = MusicManager.main.currentMusic.StreamPaused;
			seekedPosition = MusicManager.main.currentMusic.GetPlaybackPosition();
			MusicManager.main.currentMusic.StreamPaused = true;
        }
    }

	public void StartSetVolume()
	{
        StartStandardSelections();
		adjustMode = AdjustMode.Volume;
    }

	public void StartSetAdaptation()
	{
        StartStandardSelections();
		adjustMode = AdjustMode.Adaptation;
    }

	public void StartSetLoopMode()
	{
        StartStandardSelections();
		adjustMode = AdjustMode.LoopMode;
    }

	public void StartSetPitch()
	{
        StartStandardSelections();
		adjustMode = AdjustMode.Pitch;
    }

	public void EndAdjusting()
	{
        CommonSFXManager.PlayByName(soundExitAdjust);
        if (adjustMode == AdjustMode.Seek)
		{
			MusicManager.main.currentMusic.StreamPaused = false;
            MusicManager.main.currentMusic.Seek(seekedPosition);
            MusicManager.main.currentMusic.StreamPaused = wasPausedBeforeSeekBegan;
        }
		if (adjustMode == AdjustMode.Volume)
		{
			SettingsLoader.Save();
		}
		adjustMode = AdjustMode.NotAdjusting;
		ReturnControl();
        menuNodes[selection].Highlight();
        adjustmentAnimator.Play("Display");
    }

	#endregion

	public void UpdateIcons()
	{
		int volumeInt = int.TryParse(SettingsLoader.Get("music"), out int v) ? v : -1;
		volumeTextureItem.Texture = volumeInt switch
		{
			> 7 => volumeTextures[3],
			> 4 => volumeTextures[2],
			> 0 => volumeTextures[1],
			_ => volumeTextures[0]
		};

		double pitch = MusicManager.main.currentMusic?.PitchScale ?? 1.0;
		pitchTextureItem.Texture = pitch switch
		{
			> 1.001 => pitchTextures[2],
			< 0.999 => pitchTextures[0],
			_ => pitchTextures[1]
		};

		var loopMode = MusicMenuOrchestrator.main.loopMode;
		loopTextureItem.Texture = loopTextures[(int)loopMode];
    }

	#region Set Labels

	public void SetPlayPauseLabel()
	{
        infoLabel.Text = MusicManager.main.currentMusic?.Playing == true ? "Playing" : "Paused";
    }

    public void SetSeekLabel()
    {
        infoLabel.Text = "Seek";
    }

    public void SetVolumeLabel()
    {
        infoLabel.Text = $"Volume: {(int.TryParse(SettingsLoader.Get("music"), out int v) ? v.ToString() : "N/A")}";
    }

    public void SetAdaptationLabel()
    {
        infoLabel.Text = $"Mix: {GetAdaptationName()}";
    }

    public void SetLoopModeLabel()
    {
        infoLabel.Text = MusicMenuOrchestrator.main.loopMode.ToString().Replace("_", " ");
    }

    public void SetPitchLabel()
    {
		if (MusicManager.main.currentMusic == null)
		{
            infoLabel.Text = $"Pitch: N/A";
			return;
        }
		float pitchRaw = MusicManager.main.currentMusic.PitchScale;
		int pitchSemitones = Mathf.RoundToInt(Math.Log(pitchRaw, Mathf.Pow(2f, 1f / 12f)));
		string semitonesText = pitchSemitones switch {
			0 => "\u00b10",
			>0 => $"\u266f{pitchSemitones}",
			<0 => $"\u266d{-pitchSemitones}"
		};
        infoLabel.Text = $"Pitch: {semitonesText}";
    }

	#endregion

	public bool LeftPressed()
	{
		return InputManager.ButtonPressedThisFrame("Menu/Left")
			|| (InputManager.GetButtonHeldFrames("Menu/Left") is ulong u and >= 24 && (u - 24) % 8 == 0);
	}

	public bool RightPressed()
	{
        return InputManager.ButtonPressedThisFrame("Menu/Right")
            || (InputManager.GetButtonHeldFrames("Menu/Right") is ulong u and >= 24 && (u - 24) % 8 == 0);
    }

	public string FormatSeconds(double seconds)
		=> TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");

	public void SetSeeker()
	{
		Music currentMusic = MusicManager.main?.currentMusic;
		if (currentMusic == null) 
		{ 
			seekerBack.Visible = false;
			seekerCurrentTime.Text = seekerTotalTime.Text = "-";
			return;
		}
		double currPos = adjustMode == AdjustMode.Seek ? seekedPosition : currentMusic.GetPlaybackPosition();
		double duration = currentMusic.Stream.GetLength();
		if (duration == 0)
		{
            seekerBack.Visible = false;
            seekerCurrentTime.Text = seekerTotalTime.Text = "-";
            return;
        }
        seekerBack.Visible = true;
        seekerCurrentTime.Text = FormatSeconds(currPos);
		seekerTotalTime.Text = FormatSeconds(duration);
		float seekerCurrLength = (float)(currPos / duration) * seekerBack.Size.X;
        seekerCircle.Position = new Vector2(seekerCurrLength, seekerCircle.Position.Y);
		seekerFront.Size = new Vector2(seekerCurrLength, seekerFront.Size.Y);
		if (currentMusic.loopRegion != Vector2.Zero)
		{
            seekerLoopRegion.Position = new Vector2(
                (float)(currentMusic.loopRegion.X / duration) * seekerBack.Size.X,
                seekerLoopRegion.Position.Y);
            seekerLoopRegion.Size = new Vector2(
                (float)((currentMusic.loopRegion.Y - currentMusic.loopRegion.X) / duration) * seekerBack.Size.X,
                seekerLoopRegion.Size.Y);
        } 
		else
		{
            seekerLoopRegion.Size = new Vector2(0, seekerLoopRegion.Size.Y);
        }
    }

	public void SetInfoDisplay()
	{
        Music currentMusic = MusicManager.main.currentMusic;
		if (currentMusic == null)
		{
			title.Text = description.Text = trackNumber.Text = composer.Text = "";
            return;
		}
        title.Text = currentMusic.title.Length == 0 ? "(no title)" : currentMusic.title;
		description.Text = currentMusic.description.Length == 0 ? "(no description)" : currentMusic.description;
		trackNumber.Text = $"Track {currentMusic.GetTrackNumber()}";
		composer.Text = currentMusic.composer;
	}

	/// <summary>
	/// Get the index of the current adaptive music selection, assuming it has selected only one track to be played.
	/// </summary>
	/// <returns></returns>
	public int GetNextAdaptationIndex(int direction)
	{
        var targetList = MusicManager.GetSyncedTargetList();
		int maxIndex = 0;
		float maxVolume = targetList[0];
		for (int i = 1; i < targetList.Count; i++)
		{
			if (targetList[i] > maxVolume)
			{
				maxIndex = i;
				maxVolume = targetList[i];
			}
		}
		return (maxIndex + direction + targetList.Count) % targetList.Count;
    }

	public string GetAdaptationName()
		=> MusicManager.CurrentMusicAsSynchronized()
		?.GetSyncStream(GetNextAdaptationIndex(0))
		.ResourcePath is string s 
		? System.IO.Path.GetFileNameWithoutExtension(s).Replace("_", "") 
		: "(none)";

	private int lastSelection = -1;

	public override void _Process(double delta)
	{
		base._Process(delta);
		switch (BaseGetSelectionNode().Name)
		{
			case "PlayPause":
				SetPlayPauseLabel();
				break;
			case "Seek":
				SetSeekLabel();
				break;
			case "Volume":
				SetVolumeLabel();
				break;
			case "Adaptation":
				SetAdaptationLabel();
				break;
			case "LoopMode":
				SetLoopModeLabel();
				break;
			case "Pitch":
				SetPitchLabel();
				break;
			default:
				infoLabel.Text = "";
                break;
		}
		SetSeeker();
		if (MusicManager.main?.currentMusic != lastMusic)
		{
            SetInfoDisplay();
			UpdateIcons();
			lastMusic = MusicManager.main?.currentMusic;
        }
		if (IsActive() && lastSelection != -1 && lastSelection != selection)
		{
			CommonSFXManager.PlayByName(soundSwitch);
		}
		lastSelection = selection;

		if (adjustMode != AdjustMode.NotAdjusting && InputManager.ButtonPressedThisFrame("Menu/Back"))
		{
            EndAdjusting();
		}
		
		if (adjustMode != AdjustMode.NotAdjusting && MusicManager.main.currentMusic is Music currMusic)
		{
			if (adjustMode == AdjustMode.Seek)
			{
				float movement = 0;

                if (!InputManager.ButtonIsDown("Menu/Right") && InputManager.GetButtonHeldFrames("Menu/Left") is ulong l and > 0)
                {
					movement = -Math.Min((l + 30) * 0.0001f, 0.01f);
                }

                if (!InputManager.ButtonIsDown("Menu/Left") && InputManager.GetButtonHeldFrames("Menu/Right") is ulong r and > 0)
                {
                    movement = Math.Min((r + 30) * 0.0001f, 0.01f);
                }

				if (movement != 0)
				{
					double trackLength = currMusic.Stream.GetLength();
					double movementSeconds = movement * trackLength;
					seekedPosition = (float)Mathf.Clamp(seekedPosition + movementSeconds, 0, trackLength);
				}
            }
			else
			{
                int inputNumber = 0;
				if (LeftPressed()) 
				{ 
					inputNumber -= 1;
                    CommonSFXManager.PlayByName(soundAdjustLeft);
                }
				if (RightPressed()) 
				{ 
					inputNumber += 1;
                    CommonSFXManager.PlayByName(soundAdjustRight);
                }
                if (inputNumber != 0)
                {
                    switch (adjustMode)
                    {
                        case AdjustMode.Volume:
                            if (int.TryParse(SettingsLoader.Get("music"), out int v))
                            {
                                SettingsLoader.Set("music", Mathf.Clamp(v + inputNumber, 0, 10).ToString());
                            }
                            break;
                        case AdjustMode.Pitch:
                            currMusic.PitchScale *= Mathf.Pow(2f, inputNumber / 12f);
                            if (currMusic.PitchScale < 0.125f) currMusic.PitchScale = 0.125f;
                            if (currMusic.PitchScale > 8f) currMusic.PitchScale = 8f;
                            if (Mathf.Abs(currMusic.PitchScale - 1f) < 0.001) currMusic.PitchScale = 1f;
                            break;
						case AdjustMode.LoopMode:
							MusicMenuOrchestrator.main.loopMode =
								(MusicMenuOrchestrator.LoopMode)(
									(int)((int)MusicMenuOrchestrator.main.loopMode + inputNumber + MusicMenuOrchestrator.LoopMode.Count) 
									% (int)MusicMenuOrchestrator.LoopMode.Count);
							break;
						case AdjustMode.Adaptation:
							if (MusicManager.main?.currentMusic?.Stream is not AudioStreamSynchronized currSynced) { break; }
                            System.Collections.Generic.List<float> newNextList = new();
							int nextAdaptIndex = GetNextAdaptationIndex(inputNumber);
							for (int i = 0; i < currSynced.StreamCount; ++i)
							{
								newNextList.Add((i == nextAdaptIndex) ? 1 : 0);
							}
							MusicManager.StartSyncedFade(0.5f, newNextList);
                            break;
                        default:
                            break;
                    }
                    UpdateIcons();
                }
            }
		}
	}
}
