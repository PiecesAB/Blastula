using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Refills the health of a boss (or several), and makes them vulnerable at the end of the refill.
    /// The boss(es) use the parent StageSector of this schedule by default to contain this health bar.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/bossHealthBar.png")]
    public partial class BossRefill : StageSchedule
    {
        /// <summary>
        /// If nonempty, used instead to contain the boss attack relevant to the health bar.
        /// </summary>
        [Export] public StageSector altStageSector;
        /// <summary>
        /// Reference ID for which boss(es) are to be refilled.
        /// </summary>
        [Export] public string bossID = "Main";
        /// <summary>
        /// Number representing the new health for the boss.
        /// </summary>
        [Export] public string newMaxHealth = "1200";
        /// <summary>
        /// If nonempty, a number of seconds for the animation of the health bar to fill.
        /// Setting this to 0 will make it fill instantly.
        /// </summary>
        [Export] public string refillDuration = "";

        public override Task Execute()
        {
            StageSector container = null;
            Node parent;
            if (altStageSector != null) { container = altStageSector; }
            else if ((parent = GetParent()) is StageSector) { container = (StageSector)parent; }
            if (container == null)
            {
                GD.PushWarning("BossRefill should be child of a StageSector; halting.");
                return Task.CompletedTask;
            }

            List<BossEnemy> bossList = BossEnemy.GetBosses(bossID);
            if (bossList.Count == 0) 
            {
                GD.PushWarning("BossRefill tried to refill nonexistent boss(es).");
                return Task.CompletedTask;
            }
            foreach (BossEnemy b in bossList)
            {
                float nmh = Solve("newMaxHealth").AsSingle();
                if (refillDuration != null && refillDuration != "")
                {
                    float dur = Solve("refillDuration").AsSingle();
                    _ = b.RefillAndBecomeVulnerable(container, nmh, dur);
                }
                else
                {
                    _ = b.RefillAndBecomeVulnerable(container, nmh);
                }
            }
            return Task.CompletedTask;
        }
    }
}
