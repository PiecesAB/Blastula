using Godot;
using System.Diagnostics;

namespace Blastula
{
    /// <summary>
    /// This node is meant to be a singleton in the kernel. 
    /// It executes the certain actions that bullet structures have scheduled in their behavior,
    /// such as deletion or applying new operations.
    /// </summary>
    public partial class PostExecuteManager : Node
    {
        public static Stopwatch debugTimer;

        public static PostExecuteManager main = null;

        public override void _Ready()
        {
            main = this;
            ProcessPriority = VirtualVariables.Persistent.Priorities.POST_EXECUTE;
        }

        public override void _Process(double delta)
        {
            if (main != this) { return; }
            if (Debug.StatsViews.currentMode == "timings") { debugTimer = Stopwatch.StartNew(); }
            PostExecute.PerformScheduled();
            if (Debug.StatsViews.currentMode == "timings") { debugTimer.Stop(); }
        }
    }
}

