using Blastula.Coroutine;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula
{
	/// <summary>
	/// Boss enemy; a special enemy that can have multiple phases.
	/// </summary>
	/// <remarks>
	/// - Bosses do not drop collectibles with a defeat schedule, you must trigger that manually with DropCollectible.
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

		/// <summary>
		/// When this stops running, this boss should be truly defeated.
		/// Auto-calculated as the sector on the stack with the Boss role.
		/// </summary>
		public StageSector bossSector { get; private set; } = null;
		/// <summary>
		/// When this stops running, the health bar is depleted or the time ran out.
		/// Provided when health is refilled.
		/// </summary>
		public StageSector currentSector { get; private set; } = null;
		public bool refilling { get; private set; } = false;

		[Signal] public delegate void OnRefillStartEventHandler(StageSector newSector);

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
			// TODO: check for bossSector ended; delete the boss.
			if (currentSector != null && !currentSector.ShouldBeExecuting())
			{
				// When the sector has timed out:
				if (!currentSector.ragePhase && currentSector.bossHealthCutoff > 0 && currentSector.bossHealthCutoff < 1)
				{
					health = currentSector.bossHealthCutoff * maxHealth;
				}
				BecomeDefeated();
			}
		}

		/// <summary>
		/// Used to assert a debounce condition that prevents refill behaviors from interleaving.
		/// </summary>
		private long refillAnimIteration = 0;
		/// <summary>
		/// Used to possibly refill the boss health to a new amount and/or become vulnerable.
		/// </summary>
		public IEnumerator RefillAndBecomeVulnerable(StageSector sector, 
			float newMaxHealth, float newDefense,
			float refillTime = 1f, float delayVulnerable = 0f)
		{
			// Set up the start of the filling.
			currentSector = sector;
			deflectAllDamage = true;
			defeated = true;
			refilling = true;
			EmitSignal(SignalName.OnRefillStart, sector);
			long currAnimIteration = ++refillAnimIteration;
			float startFill = 0;
			// Some special cases and setup to be aware of:
			// - If newMaxHealth is nonpositive, the health and maxHealth remain as they are now.
			// - If refillTime is negative, refilling lasts forever; boss remains invulnerable.
			// - If refillTime is zero, filling animation is instant.
			if (newMaxHealth > 0)
			{
				health = 0;
				defense = newDefense;
				maxHealth = newMaxHealth;
				startFill = health / maxHealth;
			}
			float refillProgress = 0;
			if (refillTime < 0) { yield break; }
			// Actually filling it up every frame.
			while (newMaxHealth > 0 && refillTime > 0 && refillAnimIteration == currAnimIteration && refillProgress < 1)
			{
				health = maxHealth * Mathf.Lerp(startFill, 1f, refillProgress);
				yield return new WaitOneFrame();
				refillProgress += 1f / (refillTime * Persistent.SIMULATED_FPS);
			}
			// The filling is complete.
			// - If delayVulnerable is negative, the boss remains invulnerable.
			// - If delayVulnerable is positive, the boss becomes vulnerable after the delay.
			if (refillAnimIteration == currAnimIteration)
			{
				if (newMaxHealth > 0)
				{
					health = maxHealth;
				}
				if (delayVulnerable < 0f) { yield break; }
				if (delayVulnerable > 0f) { yield return new WaitTime(delayVulnerable); }
			}
			// Become vulnerable, and end the refilling.
			if (refillAnimIteration == currAnimIteration)
			{
				deflectAllDamage = false;
				defeated = false;
				refilling = false;
			}
		}

		public override void BecomeDefeated()
		{
			if (defeated) { return; }
			// Ends any refill if it's still going for some reason.
			refillAnimIteration++;
			defeated = true;
			deflectAllDamage = true;
			if (currentSector != null && currentSector.ShouldBeExecuting())
			{
				currentSector.EndImmediately();
			}
		}

		public override void _EnterTree()
		{
			base._EnterTree();
			// Set the bossSector so the health indicator and other children may know.
			bossSector = StageSector.GetActiveSectorByRole(StageSector.Role.Boss);
			if (bossSector == null)
			{
				GD.PushWarning("BossEnemy spawned outside of a Boss role StageSector. Unintended behavior may occur.");
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

			// Make the ID globally searchable
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

		private bool CanEndSectorByHealth()
		{
			return ((!refilling && !deflectAllDamage) || (currentSector != null && currentSector.ragePhase))
				&& currentSector != null && currentSector.ShouldBeExecuting();
		}

		public override void _Process(double delta)
		{
			base._Process(delta);
			if (CanEndSectorByHealth())
			{
				float healthFrac = GetSpecial("health_frac").AsSingle();
				if (healthFrac <= currentSector.bossHealthCutoff)
				{
					BecomeDefeated();
				}
			}
		}
	}
}

