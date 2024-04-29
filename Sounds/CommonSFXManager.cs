using Godot;
using System.Collections.Generic;

namespace Blastula.Sounds
{
    /// <summary>
    /// Container of sound effects that can be referenced and played globally by ID.
    /// </summary>
    public unsafe partial class CommonSFXManager : Node
    {
        /// <summary>
        /// If this string is non-empty, the audio bus of each effect will use it.
        /// </summary>
        [Export] public string sfxBusName = "SFX";

        /// <summary>
        /// There should only be one CommonSFXManager, and this is the one.
        /// </summary>
        public static CommonSFXManager main { get; private set; } = null;

        private static Dictionary<int, string> nameFromID = new Dictionary<int, string>();
        private static Dictionary<string, int> IDFromName = new Dictionary<string, int>();
        private static Dictionary<int, Node> soundPlayerFromID = new Dictionary<int, Node>();
        /// <summary>
        /// This is used in an optimization to prevent the sound from playing many times in the same frame.
        /// Such an action would cause lag, and mitigating it would be unnoticed.
        /// </summary>
        private static Dictionary<Node, ulong> lastPlayedFrame = new Dictionary<Node, ulong>();
        /// <summary>
        /// Linear volumes that the AudioStreamPlayer nodes begin with.
        /// </summary>
        private static Dictionary<string, float> startVolumesByNodeName = new Dictionary<string, float>();

        private int registeredCount = 0;

        /// <summary>
        /// Play a sound by direct reference to the AudioStreamPlayer.
        /// </summary>
        /// <param name="position">If move == true, moves spatial sounds to this position.</param>
        public static void Play(Node n, float pitch = 1f, float volume = 1f, Vector2 position = default, bool move = false)
        {
            if (!lastPlayedFrame.ContainsKey(n))
            {
                lastPlayedFrame[n] = ulong.MaxValue;
            }
            if (lastPlayedFrame[n] != FrameCounter.realGameFrame) { lastPlayedFrame[n] = FrameCounter.realGameFrame; }
            else { return; }
            // Dreams of multiple inheritance...
            if (n is AudioStreamPlayer)
            {
                AudioStreamPlayer asp = n as AudioStreamPlayer;
                asp.PitchScale = pitch;
                asp.VolumeDb = Mathf.LinearToDb(volume);
                asp.Play();
            }
            else if (n is AudioStreamPlayer2D)
            {
                AudioStreamPlayer2D asp = n as AudioStreamPlayer2D;
                if (move) { asp.GlobalPosition = position; }
                asp.PitchScale = pitch;
                asp.VolumeDb = Mathf.LinearToDb(volume);
                asp.Play();
            }
            else if (n is AudioStreamPlayer3D)
            {
                AudioStreamPlayer3D asp = n as AudioStreamPlayer3D;
                if (move) { asp.GlobalPosition = new Vector3(position.X, position.Y, 0); }
                asp.PitchScale = pitch;
                asp.VolumeDb = Mathf.LinearToDb(volume);
                asp.Play();
            }
        }

        /// <summary>
        /// Play a sound by ID.
        /// </summary>
        /// <param name="position">If move == true, moves spatial sounds to this position.</param>
        public static void PlayByName(string name, float pitch = 1f, float volume = 1f, Vector2 position = default, bool move = false)
        {
            if (!IDFromName.ContainsKey(name)) { return; }
            int id = IDFromName[name];
            if (!soundPlayerFromID.ContainsKey(id)) { return; }
            float startVolume = startVolumesByNodeName[name];
            Play(soundPlayerFromID[id], pitch, startVolume * volume, position, move);
        }

        private bool IsSoundPlayer(Node n)
        {
            return n is AudioStreamPlayer || n is AudioStreamPlayer2D || n is AudioStreamPlayer3D;
        }

        private void UseSFXBus(Node n)
        {
            if (sfxBusName == null || sfxBusName == "") { return; }
            n.Set("bus", sfxBusName);
        }

        private void RegisterSoundPlayers(Node root)
        {
            UtilityFunctions.PathBuilder(root, (n, path) =>
            {
                if (IsSoundPlayer(n))
                {
                    if (IDFromName.ContainsKey(path))
                    {
                        GD.PushWarning($"There's a duplicate sound at {path}. It will be ignored.");
                        return;
                    }
                    nameFromID[registeredCount] = path;
                    IDFromName[path] = registeredCount;
                    soundPlayerFromID[registeredCount] = n;
                    if (n is AudioStreamPlayer)
                    {
                        startVolumesByNodeName[path] = Mathf.DbToLinear(((AudioStreamPlayer)n).VolumeDb);
                    }
                    else if (n is AudioStreamPlayer2D)
                    {
                        startVolumesByNodeName[path] = Mathf.DbToLinear(((AudioStreamPlayer2D)n).VolumeDb);
                    }
                    else if (n is AudioStreamPlayer3D)
                    {
                        startVolumesByNodeName[path] = Mathf.DbToLinear(((AudioStreamPlayer3D)n).VolumeDb);
                    }
                    UseSFXBus(n);
                    //GD.Print($"Common sound {path} registered with ID {registeredCount}, bus is {n.Get("bus")}");
                    registeredCount++;
                }
            }, true);
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            RegisterSoundPlayers(this);
        }
    }
}
