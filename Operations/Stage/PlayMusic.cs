using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Plays a music track with the node name/path determined by the Blastula.Sounds.MusicManager node.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/wolfDark.png")]
    public partial class PlayMusic : Discrete
	{
        [Export] public string nodeName = "";
        [Export] public string pitch = "1";
        [Export] public string volume = "1";

        public override void Run()
        {
            float pitchSolved = 1;
            if (pitch != null && pitch != "")
            {
                pitchSolved = Solve("pitch").AsSingle();
            }
            float volSolved = 0.5f;
            if (volume != null && volume != "")
            {
                volSolved = Solve("volume").AsSingle();
            }
            MusicManager.PlayImmediate(nodeName);
            MusicManager.SetPitch(pitchSolved);
            if (MusicManager.main != null)
            {
                MusicManager.SetVolumeMultiplier(volSolved);
            }
        }
    }
}
