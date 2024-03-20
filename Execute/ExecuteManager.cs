using Godot;
using System.Diagnostics;

namespace Blastula
{
    public partial class ExecuteManager : Node
    {
        public static Stopwatch debugTimer;

        public override void _Ready()
        {
            ProcessPriority = VirtualVariables.Persistent.Priorities.EXECUTE;
        }

        public override void _Process(double delta)
        {
            if (Debug.StatsViews.currentMode == "timings") { debugTimer = Stopwatch.StartNew(); }
            Blastodisc.ExecuteAll();
            if (Debug.StatsViews.currentMode == "timings") { debugTimer.Stop(); }
        }
    }
}

