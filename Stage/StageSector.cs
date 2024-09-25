using Blastula.Coroutine;
using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
	/// <summary>
	/// Defines a portion of a stage, or the stage itself.
	/// It is meant to recursively contain smaller sections of the stage.
	/// It can also be used to simply load a scene.
	/// </summary>
	[GlobalClass]
	[Icon(Persistent.NODE_ICON_PATH + "/scheduleStage.png")]
	public partial class StageSector : StageSchedule
	{
		/// <summary>
		/// The role of a StageSector; important for parsing.
		/// </summary>
		public enum Role
		{
			/// <summary>
			/// A StageSector for an arbitrary purpose.
			/// </summary>
			Unknown,
			/// <summary>
			/// Encompasses a whole stage.
			/// </summary>
			Stage,
			/// <summary>
			/// Encompasses a self-contained section of a stage.
			/// </summary>
			Chapter,
			/// <summary>
			/// Spawns and encompasses boss (and midboss) fights. Expected child of Stage sector.
			/// </summary>
			Boss,
			/// <summary>
			/// Groups attacks together under the same healthbar, if possible.
			/// Can be used for a phased attack, as well.
			/// </summary>
			BossAttackGroup,
			/// <summary>
			/// A "standard" boss attack. Expected child of Boss sector.
			/// </summary>
			BossLife,
			/// <summary>
			/// A "special" boss attack. Expected child of Boss sector.
			/// </summary>
			BossBomb,
			/// <summary>
			/// A boss attack where offense is useless -- the player must survive. Expected child of Boss sector.
			/// </summary>
			BossTimeout,
		}

		[Export] public Role role = Role.Unknown;

		/// <summary>
		/// This portion of the stage will end after this number of seconds, if it's not yet ended.
		/// Leave blank to have infinite time.
		/// </summary>
		[Export] public string duration = "";
		/// <summary>
		/// If true, end when all child schedules have been completed.
		/// </summary>
		[Export] public bool endWhenChildrenComplete = true;
		[Export] public bool shouldUseTimer = false;
		/// <summary>
		/// A scene which is loaded when this sector becomes executed.
		/// This could be a wave of enemies, but it could also be completely different.
		/// For example, we could load the overlay that introduces the stage.
		/// We could also load a boss, whose attacks are then loaded in child sectors.
		/// </summary>
		[ExportGroup("Formation")]
		[Export] public PackedScene formation = null;
		/// <summary>
		/// The spawned formation instance is deleted after this number of seconds.
		/// Leave blank to have infinite time, but be advised that the Node will not be deleted automatically,
		/// which may cause the game's memory to be slowly burdened.
		/// </summary>
		[Export] public string formationDeletionDelay = "0";
		/// <summary>
		/// If any existing BossEnemy's health fraction becomes at least this low, 
		/// and the BossEnemy has decided to respond to this value, the sector is made to end immediately.
		/// Normally, this is zero because we want the sector to end when one of their health is empty.
		/// However, it may be above zero for phased attacks, 
		/// or below zero to avoid triggering the next sector this way (it's impossible for enemies to have negative health).
		/// </summary>
		[ExportGroup("Boss")]
		[Export] public float bossHealthCutoff = 0f;
		/// <summary>
		/// If true, this sector will be skipped immediately if the health cutoff is satisfied.
		/// This effectively skips the sector if the enemy health has been sufficiently lowered.
		/// It can be used to make a punishment attack phase if the user tries to timeout.
		/// </summary>
		[Export] public bool ragePhase = false;

		private Node formationInstance = null;

		/// <summary>
		/// It becomes true when the schedule has ticked itself to 0. Used internally to track when an attack has been timed out.
		/// </summary>
		private bool timeoutFlag = false;

		public enum State
		{
			Inactive, Active
		}
		
		public State state { get; private set; } = State.Inactive;
		private double timeRemaining = 0;
		private static Stack<StageSector> sectorStack = new Stack<StageSector>();

		/// <summary>
		/// Get the sector at the top of the stack; the most specific one currently ongoing.
		/// </summary>
		public static StageSector GetCurrentSector()
		{
			if (sectorStack.Count == 0) { return null; }
			return sectorStack.Peek();
		}

		/// <summary>
		/// Remove all sectors from the stack, effectively ending all waves and stages.
		/// </summary>
		public static void DumpStack()
		{
			sectorStack.Clear();
		}

		/// <summary>
		/// Returns the sector with this role nearest the top of the stack, or null if none exist.
		/// </summary>
		public static StageSector GetActiveSectorByRole(Role role)
		{
			Stack<StageSector> tempStack = new Stack<StageSector>(sectorStack);
			while (tempStack.Count > 0)
			{
				StageSector e = tempStack.Pop();
				if (e.role == role) { return e; }
			}
			return null;
		}

		public static EnemyFormation GetCurrentEnemyFormation()
		{
			if (GetCurrentSector() == null) { return null; }
			Node fi = GetCurrentSector().formationInstance;
			if (fi is not EnemyFormation) { return null; }
			return (EnemyFormation)fi;
		}

		private static FrameCounter.Cache<double> timeRemainingCache = new FrameCounter.Cache<double>();
		/// <summary>
		/// Gets the remaining duration of the most specific StageSector with timer display (that is, nearest the top of the stack).
		/// </summary>
		public static double GetTimeRemaining()
		{
			if (timeRemainingCache.IsValid()) { return timeRemainingCache.data; }
			// We try to find that most specific StageSector with timer.
			Stack<StageSector> tempStack = new Stack<StageSector>(sectorStack);
			StageSector stageSector = null;
			while (tempStack.Count > 0)
			{
				StageSector testSector = tempStack.Pop();
				if (testSector.shouldUseTimer) { stageSector = testSector; break; }
			}
			// And upon finding it, return its remaining time.
			if (stageSector == null) { return 0; }
			timeRemainingCache.Update(stageSector.timeRemaining);
			return stageSector.timeRemaining;
		}

		/// <summary>
		/// Get a unique ID; may be different than the kernel path in the future (assuming StageSector is in kernel).
		/// </summary>
		/// <returns></returns>
		public string GetUniqueId()
		{
			return "Path@" + GetPath();
		}

		public bool ShouldBeExecuting()
		{
			return sectorStack.Contains(this) && timeRemaining >= 0.0001;
		}

		/// <remarks>
		/// Call this only after the sector definitely ran. Otherwise, this can easily desync replays through inconsistent state.
		/// </remarks>
		public bool HasBeenTimedOut()
		{
			return duration is not (null or "") && timeoutFlag;
		}

		public IEnumerator RunTime()
		{
			timeoutFlag = false;
			while (ShouldBeExecuting())
			{
				yield return new WaitOneFrame();
				bool runTimeThisFrame = true;
				runTimeThisFrame &= !GameSpeed.pseudoStopped;
				double oldTimeRemaining = timeRemaining;
				if (runTimeThisFrame)
				{
					timeRemaining -= Engine.TimeScale / Persistent.SIMULATED_FPS;
				}
				if (timeRemaining < 0.0001) 
				{ 
					if (oldTimeRemaining >= 0.0001)
					{
						timeoutFlag = true;
					}
					timeRemaining = 0; 
				}
			}
		}

		public static void EndCurrentSectorImmediately()
		{
			if (GetCurrentSector() != null)
			{
				GetCurrentSector().EndImmediately();
			}
		}

		public void EndImmediately()
		{
			timeRemaining = 0;
		}

		private bool finishedExecuteChildren;
		private IEnumerator ExecuteChildren()
		{
			finishedExecuteChildren = false;
			foreach (Node child in GetChildren())
			{
				if (!ShouldBeExecuting()) { break; }
				yield return ExecuteOrShoot(null, child);
			}
			finishedExecuteChildren = true;
		}

		public override Action<CoroutineUtility.Coroutine> GetCancelMethod()
		{
			return (_) =>
			{
				timeRemaining = 0;
				if (formationInstance != null && formationDeletionDelay != null && formationDeletionDelay != "")
				{
					float fdd = Solve(PropertyName.formationDeletionDelay).AsSingle();
					if (Session.main == null || !Session.main.inSession)
					{
						formationInstance.QueueFree();
					}
					else
					{
						this.StartCoroutine(
							CoroutineUtility.DelayedQueueFree(formationInstance, fdd, Wait.TimeUnits.Seconds)
						);
					}
				}
				formationInstance = null;
				while (sectorStack.Contains(this)) { sectorStack.Pop(); }
				state = State.Inactive;
			};
		}

		public override IEnumerator Execute()
		{
			if (state == State.Active) { yield break; }
			state = State.Active;
			timeRemainingCache.Invalidate();
			sectorStack.Push(this);
			if (role == Role.Stage)
			{
				if (FrameCounter.main != null) { FrameCounter.main.ResetStageFrame(); }
			}
			if (formationInstance == null && formation != null)
			{
				formationInstance = formation.Instantiate();
			}
			while (Persistent.GetMainScene() == null) { yield return new WaitOneFrame(); }
			if (formationInstance != null)
			{
				Persistent.GetMainScene().AddChild(formationInstance);
			}
			timeRemaining = double.PositiveInfinity;
			if (duration != null && duration != "")
			{
				timeRemaining = Solve(PropertyName.duration).AsSingle();
				this.StartCoroutine(RunTime());
			}
			//GD.Print(Name, " sector has began");
			if (sectorStack.Count == 1)
			{
				StageManager.main.EmitSignal(StageManager.SignalName.StageChanged, this);
			}
			StageManager.main.EmitSignal(StageManager.SignalName.StageSectorChanged, this);
			this.StartCoroutine(ExecuteChildren());
			yield return new WaitCondition(() => !ShouldBeExecuting() || finishedExecuteChildren);
			if (!endWhenChildrenComplete) {
				yield return new WaitCondition(() => !ShouldBeExecuting()); 
			}
			GetCancelMethod()(null);
			//GD.Print(Name, " sector has ended");
		}

		private static void RecursivePreload(Node n)
		{
			if (n is StageSector)
			{
				StageSector s = (StageSector)n;
				// "Preload" the scene. This is to avoid lag when any action is happening.
				// It will begin to unfold once we place it in the main scene later on.
				if (s.formationInstance == null && s.formation != null)
				{
					s.formationInstance = s.formation.Instantiate();
				}
			}

			foreach (Node child in n.GetChildren())
			{
				RecursivePreload(child);
			}
		}

		public void Preload()
		{
			RecursivePreload(this);
		}
	}
}
