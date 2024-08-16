using Blastula.Coroutine;
using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
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
        /// If empty string or negative, the health will remain the same (useful for phased boss attacks).
        /// </summary>
        [Export] public string newMaxHealth = "1200";
        /// <summary>
        /// Number representing the new defense amount for the boss.
        /// If empty string, the defense will be zero.
        /// </summary>
        [Export] public string newDefense = "";
        /// <summary>
        /// If nonempty, a number of seconds for the animation of the health bar to fill.
        /// Setting this to 0 will make it fill instantly.
        /// Setting this to a negative number will cause never-ending fill (useful for timeout).
        /// If empty string, the refill will take one second.
        /// </summary>
        /// <remarks>
        /// If newMaxHealth is empty string or negative, refillDuration naturally has no effect.
        /// </remarks>
        [Export] public string refillDuration = "";
        /// <summary>
        /// If nonempty, a number of seconds between the point of health bar fill complete,
        /// and the point when the boss becomes vulnerable.
        /// Setting this to a negative number will cause the boss to remain invulnerable.
        /// If empty string, there's no delay (boss becomes vulnerable instantly when the health bar is filled).
        /// </summary>
        [Export] public string delayVulnerable = "";

        public override IEnumerator Execute()
        {
            StageSector container = null;
            Node parent;
            if (altStageSector != null) { container = altStageSector; }
            else if ((parent = GetParent()) is StageSector) { container = (StageSector)parent; }
            if (container == null)
            {
                GD.PushWarning("BossRefill should be child of a StageSector; halting.");
                yield break;
            }

            List<BossEnemy> bossList = BossEnemy.GetBosses(bossID);
            if (bossList.Count == 0) 
            {
                GD.PushWarning("BossRefill tried to refill nonexistent boss(es).");
                yield break;
            }
            foreach (BossEnemy b in bossList)
            {
                float nmh = -1f;
                if (newMaxHealth != null && newMaxHealth != "")
                {
                    nmh = Solve(PropertyName.newMaxHealth).AsSingle();
                }
                float ndf = 0f;
                if (newDefense != null && newDefense != "")
                {
                    ndf = Solve(PropertyName.newDefense).AsSingle();
                }
                float dur = 1f;
                if (refillDuration != null && refillDuration != "")
                {
                    dur = Solve(PropertyName.refillDuration).AsSingle();
                }
                float dv = 0f;
                if (delayVulnerable != null && delayVulnerable != "")
                {
                    dv = Solve(PropertyName.delayVulnerable).AsSingle();
                }

                this.StartCoroutine(b.RefillAndBecomeVulnerable(container, nmh, ndf, dur, dv));
            }
        }
    }
}
