using Godot;
using System.Diagnostics;

namespace Blastula
{
    public partial class PostExecuteManager : Node
    {
        public static Stopwatch debugTimer;

        public override void _Ready()
        {
            ProcessPriority = VirtualVariables.Persistent.Priorities.POST_EXECUTE;
        }

        public override void _Process(double delta)
        {
            if (Debug.StatsViews.currentMode == "timings") { debugTimer = Stopwatch.StartNew(); }
            PostExecute.PerformScheduled();
            if (Debug.StatsViews.currentMode == "timings") { debugTimer.Stop(); }
        }
    }
}

