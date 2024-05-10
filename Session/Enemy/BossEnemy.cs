using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula
{
    /// <summary>
    /// Boss enemy.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/enemy.png")]
    public partial class BossEnemy : Enemy
    {
        /// <summary>
        /// Globally identify the boss. Need not be unique.
        /// </summary>
        [Export] public string ID =  "Main";
        private static Dictionary<string, List<BossEnemy>> bossByID = new Dictionary<string, List<BossEnemy>>();

        private StageSector currentSector = null;

        public static List<BossEnemy> GetBosses(string searchID)
        {
            if (!bossByID.ContainsKey(searchID)) { return new List<BossEnemy>(); }
            return bossByID[searchID];
        }

        /// <summary>
        /// We have to detect time out before any operations try to refill the boss health!
        /// </summary>
        public void WhenStageSectorChanged(StageSector _)
        {
            if (currentSector != null && !currentSector.ShouldBeExecuting())
            {
                // When the sector has timed out:
                BecomeDefeated();
            }
        }

        private long refillAnimIteration = 0;
        /// <summary>
        /// Refill the boss health to a new amount over a time, 
        /// </summary>
        public async Task RefillAndBecomeVulnerable(StageSector sector, float newMaxHealth, float refillTime = 1f)
        {
            currentSector = sector;
            long currAnimIteration = ++refillAnimIteration;
            health = 0;
            maxHealth = newMaxHealth;
            float refillProgress = 0;
            while (refillTime > 0 && refillAnimIteration == currAnimIteration && refillProgress < 1)
            {
                health = maxHealth * refillProgress;
                await this.WaitOneFrame();
                refillProgress += 1f / (refillTime * Persistent.SIMULATED_FPS);
            }
            if (refillAnimIteration == currAnimIteration)
            {
                health = maxHealth;
                deflectAllDamage = false;
                defeated = false;
            }
        }

        public override void BecomeDefeated()
        {
            if (defeated) { return; }
            // End any refill if it's still going for some reason
            refillAnimIteration++;
            defeated = true;
            deflectAllDamage = true;
            health = 0;
            if (currentSector != null && currentSector.ShouldBeExecuting())
            {
                currentSector.EndImmediately();
            }
        }

        public override void _Ready()
        {
            base._Ready();
            StageManager.main.Connect(
                StageManager.SignalName.StageSectorChanged, 
                new Callable(this, MethodName.WhenStageSectorChanged)
            );
            defeated = true;
            deflectAllDamage = true;
            health = 0;
            if (ID != null && ID != "") 
            { 
                if (!bossByID.ContainsKey(ID))
                {
                    bossByID[ID] = new List<BossEnemy>();
                }
                bossByID[ID].Add(this); 
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (bossByID.ContainsKey(ID))
            {
                bossByID[ID].Remove(this);
                if (bossByID[ID].Count == 0)
                {
                    bossByID.Remove(ID);
                }
            }
        }
    }
}

