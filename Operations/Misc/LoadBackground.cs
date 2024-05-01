using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Loads and places a background. It can also fade out when the background scene is empty.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/landscape.png")]
    public partial class LoadBackground : Discrete
	{
        [Export] public PackedScene backgroundScene;
        [Export] public float fadeDuration = 1.5f;

        public override void Run()
        {
            if (backgroundScene == null)
            {
                _ = BackgroundHolder.FadeAway(fadeDuration);
            }
            else
            {
                _ = BackgroundHolder.SetBackground(backgroundScene, fadeDuration);
            }
        }
    }
}
