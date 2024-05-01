using Godot;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Blastula
{
    /// <summary>
    /// This singleton handles the container for the background.
    /// </summary>
    public partial class BackgroundHolder : Control
    {
        /// <summary>
        /// Backgrounds will be made a child of this scene, which should be a viewport.
        /// </summary>
        [Export] public Node backgroundSceneParent;
        /// <summary>
        /// When the background fades out or is absent, this appears in place of a background.
        /// Its Modulate property is interpolated with fading.
        /// </summary>
        [Export] public Control fadeOverlay;

        public Node currentBackground { get; private set; } = null;

        public static BackgroundHolder main { get; private set; } = null;
        private static bool shouldBeVisible = true;

        private long animCounter = 0;
        private double animStartTime = 0;

        public static async Task SetBackground(PackedScene newBackground, float fadeInDuration = 1.5f)
        {
            if (main == null) { return; }
            if (main.currentBackground != null) 
            {
                main.currentBackground.QueueFree();
                main.currentBackground = null;
            }
            if (newBackground != null)
            {
                Node newBGNode = newBackground.Instantiate();
                main.backgroundSceneParent.AddChild(newBGNode);
                main.currentBackground = newBGNode;
            }
            long currAnimCounter = ++main.animCounter;
            main.animStartTime = FrameCounter.stageTime;
            while (fadeInDuration > 0f && FrameCounter.stageTime - main.animStartTime < fadeInDuration)
            {
                float progress = (float)(FrameCounter.stageTime - main.animStartTime) / fadeInDuration;
                float overlayOpacity = 1f - progress;
                if (currAnimCounter == main.animCounter)
                {
                    main.fadeOverlay.Modulate = new Color(1f, 1f, 1f, overlayOpacity);
                }
                else { break; }
                await main.WaitOneFrame();
            }
            if (currAnimCounter == main.animCounter)
            {
                main.fadeOverlay.Modulate = new Color(1f, 1f, 1f, 0f);
            }
        }

        /// <summary>
        /// This fades in the "blank" overlay panel (appears to remove the true background).
        /// </summary>
        public static async Task FadeAway(float duration = 1.5f)
        {
            if (main == null) { return; }
            long currAnimCounter = ++main.animCounter;
            main.animStartTime = FrameCounter.stageTime;
            while (duration > 0f && FrameCounter.stageTime - main.animStartTime < duration)
            {
                float progress = (float)(FrameCounter.stageTime - main.animStartTime) / duration;
                float overlayOpacity = progress;
                if (currAnimCounter == main.animCounter)
                {
                    main.fadeOverlay.Modulate = new Color(1f, 1f, 1f, overlayOpacity);
                }
                else { break; }
                await main.WaitOneFrame();
            }
            if (currAnimCounter == main.animCounter)
            {
                main.fadeOverlay.Modulate = new Color(1f, 1f, 1f, 1f);
                if (main.currentBackground != null)
                {
                    main.currentBackground.QueueFree();
                    main.currentBackground = null;
                }
            }
        }

        public static void SetVisible(bool newVisible)
        {
            shouldBeVisible = newVisible;
            if (main == null) { return; }
            main.Visible = newVisible;
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            fadeOverlay.Modulate = new Color(1f, 1f, 1f, 1f);
            Visible = shouldBeVisible;
        }
    }
}
