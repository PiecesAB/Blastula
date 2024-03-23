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

        private int registeredCount = 0;

        /// <summary>
        /// Play a sound by direct reference to the AudioStreamPlayer.
        /// </summary>
        /// <param name="position">If move == true, moves spatial sounds to this position.</param>
        public static void Play(Node n, float pitch = 1f, float volume = 1f, Vector2 position = default, bool move = false)
        {
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
            Play(soundPlayerFromID[id], pitch, volume, position, move);
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
