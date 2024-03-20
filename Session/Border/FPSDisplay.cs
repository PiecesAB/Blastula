using Godot;
using System.Diagnostics;

namespace Blastula.Graphics
{
    public partial class FPSDisplay : Label
    {
        private Stopwatch stopwatch;
        public static float currFPS = 60;
        private double timeSinceLastUpdate = 0.0;

        public static FPSDisplay main;

        public override void _Ready()
        {
            base._Ready();
            main = this;
            stopwatch = Stopwatch.StartNew();
        }

        public override void _Process(double delta)
        {
            stopwatch.Stop();
            currFPS = (float)Mathf.Lerp(currFPS, 1f / stopwatch.Elapsed.TotalSeconds, Mathf.Clamp(delta * 3f, 0, 1));
            timeSinceLastUpdate += delta;
            if (timeSinceLastUpdate >= 0.1f)
            {
                timeSinceLastUpdate = 0;
                Text = $"{currFPS.ToString("F1")} fps";
            }
            stopwatch = Stopwatch.StartNew();
        }
    }
}
