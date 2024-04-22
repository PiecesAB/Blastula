using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Plays a sound effect by direct reference or a common name.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/wolf.png")]
    public partial class PlaySound : Discrete
	{
        [Export] public Node soundObject;
        [Export] public string commonSFXName = "";
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
            Vector2 pos = Vector2.Zero;
            if (ExpressionSolver.currentLocalContainer != null)
            {
                if (ExpressionSolver.currentLocalContainer is Node2D)
                {
                    pos = ((Node2D)ExpressionSolver.currentLocalContainer).GlobalPosition;
                }
            }
            if (soundObject != null)
            {
                CommonSFXManager.Play(soundObject, pitchSolved, volSolved, pos, false);
            }
            else if (commonSFXName != null && commonSFXName != "")
            {
                CommonSFXManager.PlayByName(commonSFXName, pitchSolved, volSolved, pos, true);
            }
        }
    }
}
