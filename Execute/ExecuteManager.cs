using Godot;
using System.Diagnostics;

namespace Blastula
{
    /// <summary>
    /// This node is meant to be a singleton in the kernel. 
    /// It executes the bullet structure behaviors of every Blastodisc.
    /// </summary>
    public partial class ExecuteManager : Node
    {
        public static Stopwatch debugTimer;

        public static ExecuteManager main = null;

        public override void _Ready()
        {
            main = this;
            ProcessPriority = VirtualVariables.Persistent.Priorities.EXECUTE;
        }

        public override void _Process(double delta)
        {
            if (main != this) { return; }
            if (Debug.StatsViews.currentMode == "timings") { debugTimer = Stopwatch.StartNew(); }
            Blastodisc.ExecuteAll();
            if (Debug.StatsViews.currentMode == "timings") { debugTimer.Stop(); }
        }
    }
}

