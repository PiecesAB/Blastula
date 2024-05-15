using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Applys a volume-modifying effect to the currently playing background music (see Blastula.Sounds.MusicManager).
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/duck.png")]
    public partial class MusicEnvelope : Discrete
	{
        public enum Mode
        {
            FadeIn, 
            FadeOut, 
            Duck
        }

        [Export] public Mode mode = Mode.Duck;
        /// <summary>
        /// The length of the effect.
        /// </summary>
        [Export] public string duration = "3";
        /// <summary>
        /// For the duck effect, the lowered volume throughout it.
        /// </summary>
        [Export] public string volume = "0";

        public override void Run()
        {
            float durationSolved = 3;
            if (duration != null && duration != "")
            {
                durationSolved = Solve("duration").AsSingle();
            }
            float volSolved = 0.5f;
            if (volume != null && volume != "")
            {
                volSolved = Solve("volume").AsSingle();
            }

            if (MusicManager.main != null)
            {
                switch (mode)
                {
                    case Mode.Duck: default:
                        MusicManager.Duck(durationSolved, volSolved);
                        break;
                    case Mode.FadeOut:
                        MusicManager.FadeOut(durationSolved);
                        break;
                    case Mode.FadeIn:
                        MusicManager.FadeIn(durationSolved);
                        break;
                }
            }
        }
    }
}
