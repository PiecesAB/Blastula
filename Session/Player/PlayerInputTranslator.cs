using Blastula.Input;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Blastula;

/// <summary>
/// This is a layer between the InputManager and the player's actions.
/// 1. It should ignore pausing and other stage interruptions.
/// 2. It records and replays inputs directed at the player.
/// </summary>
public partial class PlayerInputTranslator : Node
{
    [Export] public ReplayManager.Mode mode = ReplayManager.Mode.Record;

    private int playbackReadHead = 0;
    public List<byte> currentRecording = new (System.Text.Encoding.UTF8.GetBytes("r 30 R d 12 D 50 l 30 L u 12 U 5")); //new List<byte>();
    private long framesSinceLastInput = 0;
    private bool ended = true;

    public enum ForbidInputType { ForbidNone, ForbidNonMovement, ForbidAll }
    private ForbidInputType inputForbidden = ForbidInputType.ForbidNone;

    public class InputItem
    {
        public string name;
        public bool currentState = false;
        public bool movement = false;
        public char symbol;
    }

    public readonly Dictionary<string, InputItem> inputItems = new()
    {
        { "Left", new() { name = "Left", symbol = 'l', movement = true } },
        { "Right", new() { name = "Right", symbol = 'r', movement = true }},
        { "Up", new() { name = "Up", symbol = 'u', movement = true }},
        { "Down", new() { name = "Down", symbol = 'd', movement = true }},
        { "Shoot", new() { name = "Shoot", symbol = 's' }},
        { "Focus", new() { name = "Focus", symbol = 'f' }},
        { "Bomb", new() { name = "Bomb", symbol = 'b' }},
        { "Special", new() { name = "Special", symbol = 'c' }},
    };
    private IEnumerable<InputItem> inputItemValues;
    private Dictionary<char, InputItem> inputItemsByChar = new();

    [Export] public Player.Role role = Player.Role.SinglePlayer;

    private void Record(string s)
    {
        if (ended) return;
        currentRecording.AddRange(System.Text.Encoding.UTF8.GetBytes(s));
        currentRecording.Add((byte)' ');
    }

    private void Record(char c)
    {
        if (ended) return;
        currentRecording.Add((byte)c);
        currentRecording.Add((byte)' ');
    }

    public void Reset()
    {
        ended = false;
        inputForbidden = ForbidInputType.ForbidNone;
        currentRecording.Clear();
        foreach (InputItem i in inputItems.Values) { i.currentState = false; }
        framesSinceLastInput = -1;
        playbackReadHead = 0;
    }

    public void End()
    {
        ended = true;
        if (mode == ReplayManager.Mode.Record)
        {
            Record(framesSinceLastInput.ToString());
        }
    }

    public void OnReplayStartsSoon()
    {
        inputForbidden = ForbidInputType.ForbidNonMovement;
    }

    public override void _Ready()
    {
        inputItemValues = inputItems.Values;
        ReplayManager.main.Connect(
            ReplayManager.SignalName.ReplayStartsSoon, 
            new Callable(this, MethodName.OnReplayStartsSoon)
        );
        foreach (InputItem i in inputItemValues) 
        {
            inputItemsByChar[i.symbol] = i;
            inputItemsByChar[i.symbol] = i;
        }
        ProcessPriority = Persistent.Priorities.PLAYER_INPUT_TRANSLATOR;
        switch (role)
        {
            case Player.Role.SinglePlayer: default: 
                break;
            case Player.Role.LeftPlayer:
                foreach (InputItem i in inputItemValues) { i.name = "LP/" + i.name; }
                break;
            case Player.Role.RightPlayer:
                foreach (InputItem i in inputItemValues) { i.name = "RP/" + i.name; }
                break;
        }
        Reset();
    }

    private void SolveInputItemThisFrame(InputItem item)
    {
        bool real = InputManager.ButtonIsDown(item.name);
        bool forbidden = (inputForbidden == ForbidInputType.ForbidAll)
            || (inputForbidden == ForbidInputType.ForbidNonMovement && !item.movement);
        if (real != item.currentState && !forbidden)
        {
            if (framesSinceLastInput > 0) { Record(framesSinceLastInput.ToString()); }
            framesSinceLastInput = 0;
            if (real) { Record(item.symbol.ToString()); } // lowercase
            else { Record(((char)((byte)item.symbol - 0x20)).ToString()); } //uppercase
            item.currentState = real;
        }
    }

    private long waitFramesToNextInput = -1;
    private string ReadNext()
    {
        if (playbackReadHead >= currentRecording.Count) return null;
        if (waitFramesToNextInput > 0 && framesSinceLastInput < waitFramesToNextInput) return null;
        int start = playbackReadHead;
        while (playbackReadHead < currentRecording.Count)
        {
            if (currentRecording[playbackReadHead] == (byte)' ') { ++playbackReadHead; break; }
            else { ++playbackReadHead; }
        }
        return System.Text.Encoding.UTF8.GetString(
            currentRecording.GetRange(start, playbackReadHead - start).ToArray()
        ).TrimEnd(' ');
    }

    public override void _Process(double delta)
	{
        if (Session.main?.paused ?? true || Engine.TimeScale <= 0) return;
        ++framesSinceLastInput;
        if (mode == ReplayManager.Mode.Record)
        {
            foreach (InputItem i in inputItemValues) { SolveInputItemThisFrame(i); }
        }
        else if (mode == ReplayManager.Mode.Playback)
        {
            if (ended) { return; }
            string s;
            while ((s = ReadNext()) != null) {
                if (s.Length > 0 && inputItemsByChar.ContainsKey(s[0])) inputItemsByChar[s[0]].currentState = true;
                else if (s.Length > 0 && inputItemsByChar.ContainsKey((char)((byte)s[0] + 0x20))) inputItemsByChar[(char)((byte)s[0] + 0x20)].currentState = false;
                else if (long.TryParse(s, out long l))
                {
                    if (l > 0)
                    {
                        framesSinceLastInput = 0;
                        waitFramesToNextInput = l;
                        break;
                    }
                }
                else { GD.PushWarning("Replay movement instruction was not recognized; skipping it"); }
            }
        }
    }
}
