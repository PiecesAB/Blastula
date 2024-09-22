using Blastula.Collision;
using Blastula.Coroutine;
using Blastula.Graphics;
using Blastula.Input;
using Blastula.LowLevel;
using Blastula.Operations;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula
{
	/// <summary>
	/// Do I really need to explain what a player is? Fine.
	/// This is an entity driven by the user's input, and the user is supposed to help them survive.
	/// </summary>
	[GlobalClass]
	[Icon(Persistent.NODE_ICON_PATH + "/player.png")]
	public partial class Player : Node2D
	{
		public enum Role
		{
			/// <summary>
			/// The only player in a one-player game.
			/// </summary>
			SinglePlayer,
			/// <summary>
			/// The left player in a two-player game.
			/// </summary>
			LeftPlayer,
			/// <summary>
			/// The right player in a two-player game.
			/// </summary>
			RightPlayer,
			/// <summary>
			/// Empty value for assorted uses.
			/// </summary>
			None
		}
		/// <summary>
		/// Determines the player's role.
		/// </summary>
		[Export] public Role role = Role.SinglePlayer;
		/// <summary>
		/// The player entry where the player originated from; set by the PlayerEntry. Hopefully this exists.
		/// </summary>
		public PlayerEntry entry;
		/// <summary>
		/// Player's normal speed.
		/// </summary>
		[ExportGroup("Mobility")]
		[Export] public float normalSpeed = 500;
		/// <summary>
		/// Player's speed during the focus input.
		/// </summary>
		[Export] public float focusedSpeed = 200;
		/// <summary>
		/// Unit count which shrinks the boundary that constrains the player to the screen.
		/// </summary>
		[Export] public float boundaryShrink = 30;
		[ExportGroup("Health")]
		[Export] public BlastulaCollider hurtbox;
		/// <summary>
		/// Amount of lives that the player currently has. 
		/// In a single-player context, if 0 lives are left, the player will lose next time they are hit.
		/// </summary>
		[Export] public float lives = 2;
		/// <summary>
		/// When the player "inserts credit" (continues), the lives will be refilled to this amount.
		/// This is set to the lives count at the player's conception.
		/// </summary>
		private float lifeRefillOnContinue;
		/// <summary>
		/// This is spawned in the player's scene when they die.
		/// </summary>
		[Export] public PackedScene deathExplosion;
		/// <summary>
		/// Length of the death animation in seconds. 
		/// Technically, this is the time during which the player's LifeState is "Dying",
		/// and is unable to do anything.
		/// </summary>
		[Export] public float deathAnimationDuration = 1.5f;
		/// <summary>
		/// Together with the grace seconds, this is the length of the "Recovering" life state after death.
		/// The player can do everything, but is temporarily invulnerable.
		/// </summary>
		[Export] public float deathRecoveryDuration = 4.5f;
		/// <summary>
		/// Extra few seconds during which the "Recovering" life state is used,
		/// but the player is unable to distunguish that they're in the state.
		/// It is used for bombing as well as death.
		/// This leniency allows reaction to the knowledge that the player is now vulnerable.
		/// </summary>
		[Export] public float recoverDurationGrace = 1.5f;
		[ExportGroup("Shot Power")]
		[Export] public int shotPower = 100;
		/// <summary>
		/// A list that determines the "power index" from the current shot power.
		/// The power index ranges from 1 to the list length, including both.
		/// It is the lowest index in this list which is strictly greater than the current power value.
		/// This gives Blastodiscs the power_index variable, useful in creating a player shot pattern
		/// which adapts to the current power in a way that upgrades, rather than directly responding to current power.
		/// Additionally, the first and last elements of this list should be the minimum and maximum possible power values.
		/// </summary>
		[Export] public int[] shotPowerCutoffs = new int[] { 100, 200, 300, 400 };
		public int shotPowerIndex { get; private set; } = 1;
		/// <summary>
		/// Straightforward: the player loses this power amount on death.
		/// </summary>
		[ExportSubgroup("Death Power Loss")]
		[Export] public int shotPowerLossOnDeath = 80;
		/// <summary>
		/// The name of the sequence which scatters collectibles when the player dies.
		/// For more information, see the Blastula.CollectibleManager class.
		/// </summary>
		[Export] public string powerDropCollectibleName = "PlayerDeathDropPower";
		/// <summary>
		/// The number of dropped power items when the player dies is
		/// powerDropCollectibleMax or (shot power - minimum shot power) / (this value),
		/// whichever is higher.
		/// </summary>
		[Export] public int powerDropCollectibleValue = 5;
		/// <summary>
		/// The maximum number of power items which are dropped.
		/// </summary>
		[Export] public int powerDropCollectibleMax = 12;
		/// <summary>
		/// Amount of bombs the player currently has.
		/// </summary>
		[ExportGroup("Bomb")]
		[Export] public float bombs = 3;
		/// <summary>
		/// When the player resurrects, the bombs will be refilled to this amount.
		/// This is set to the bomb count at the player's conception.
		/// </summary>
		private float bombRefillOnDeath = 3;
		/// <summary>
		/// The number of frames early the player can press the Bomb input before they are able to use it.
		/// </summary>
		[Export] public int bombStartBufferFrames = 6;
		/// <summary>
		/// The number of leniency frames where the player can bomb after getting hit, cheating death.
		/// </summary>
		[Export] public int deathbombFrames = 8;
		/// <summary>
		/// One of these Node2Ds is spawned and emplaced into the player when they bomb. 
		/// This is the main effect of the bomb.
		/// If the list has length one, it will be used always.
		/// If the list has length two, the first is used when unfocused; the second is used when focused.
		/// </summary>
		/// <remarks>
		/// Please ensure these items delete themselves. Memory leaks suck.
		/// </remarks>
		[Export] public PackedScene[] bombItems;

		/// <summary>
		/// Length of the bomb in seconds. 
		/// After it elapses, the player is able to bomb again.
		/// </summary>
		[Export] public float bombDuration = 4.5f;
		/// <summary>
		/// Together with the grace seconds, this is the length of the "Recovering" life state after bombing.
		/// The player can do everything, but is temporarily invulnerable.
		/// </summary>
		[Export] public float bombRecoveryDuration = 1.5f;
		[ExportGroup("Graze")]
		[Export] public BlastulaCollider grazebox;
		[Export] public float framesBetweenLaserGraze = 8;
		[ExportGroup("Collectibles")]
		[Export] public BlastulaCollider attractbox;
		/// <summary>
		/// Above this Y position, the player will attract all collectibles by making the attractbox extremely large.
		/// Point items will also be worth their full value.
		/// </summary>
		[Export] public float itemGetHeight = -150;
		/// <summary>
		/// The fraction of point item value which is lost when items are collected immediately below the item get height.
		/// </summary
		/// <example>
		/// If this is 0.3, then 30% of value is lost immediately below the item get height.
		/// </example>
		[Export] public float pointItemValueCut = 0.3f;
		/// <summary>
		/// The fraction of point item value which is lost exponentially, for every 100 units below the item get height.
		/// </summary>
		/// <example>
		/// If pointItemValueCut is 0.3, and this is 0.1, then if the player collects a point item 200 units below the item get height,
		/// It will only be worth 70% * 90% * 90% = 56.7% of full value.
		/// </example>
		[Export] public float pointItemValueRolloff = 0.1f;
		private Vector2 attractboxOriginalSize;
		private const string COLLECTIBLE_ATTRACT_SEQUENCE_NAME = "CollectibleAttractPhase";
		private const string SCORE_NUMBER_LABEL_POOL_NAME = "ScoreNumber";

		public enum LifeState
		{
			Normal, 
			Dying, 
			Recovering, 
			Invulnerable
		}
		public LifeState lifeState = LifeState.Normal;
		public bool recoveryGracePeriodActive { get; set; } = false;
		public bool bombing { get; private set; } = false;
		public static bool settingInvulnerable = false;
		public bool debugInvulnerable = false;

		private MainBoundary mainBoundary = null;
		/// <summary>
		/// The number of grazes before _Process can aggregate them, 
		/// which is incremented for each collision event.
		/// </summary>
		private int grazeGetThisFrame = 0;
		/// <summary>
		/// The position of the player when _Ready runs.
		/// When the player dies, they will respawn here.
		/// </summary>
		private Vector2 homePosition;
		private FrameCounter.Buffer bombStartBuffer = new FrameCounter.Buffer(0);
		private FrameCounter.Buffer deathbombBuffer = new FrameCounter.Buffer(0);

		/// <summary>
		/// This will be produced by the player as soon as they come into existence
		/// and drive the controls.
		/// </summary>
		public PlayerInputTranslator inputTranslator { get; private set; } = null;

		/// <summary>
		/// Blastodiscs in this list will recieve important variables such as "shoot" and "focus".
		/// These variables are important to make player shots function correctly.
		/// </summary>
		public List<Blastodisc> varDiscs = new List<Blastodisc>();
		public static System.Collections.Generic.Dictionary<Role, Player> playersByControl = new System.Collections.Generic.Dictionary<Role, Player>();

		public override void _Ready()
		{
			inputTranslator = new PlayerInputTranslator();
			inputTranslator.mode = ReplayManager.Mode.Playback;
			AddChild(inputTranslator);
			if (!playersByControl.ContainsKey(role)) { playersByControl[role] = this; }
			else { GD.PushWarning("Two or more players exist with the same role. This is not expected."); }
			// Override lives and bombs if necessary
			lifeRefillOnContinue = lives;
			bombRefillOnDeath = bombs;
			if (Session.main != null)
			{
				if (Session.main.lifeOverride >= 0) 
				{ 
					lives = Session.main.lifeOverride;
					lifeRefillOnContinue = lives;
				}
				if (Session.main.bombOverride >= 0) 
				{ 
					bombs = Session.main.bombOverride;
					bombRefillOnDeath = bombs;
				}
			}
			FindDiscs();
			SetVarsInDiscs();
			attractboxOriginalSize = attractbox.size;
			homePosition = GlobalPosition;
		}

		private void FindMainBoundary()
		{
			MainBoundary.MainType m = MainBoundary.MainType.Single;
			switch (role)
			{
				case Role.SinglePlayer:
				default:
					break;
				case Role.LeftPlayer:
					m = MainBoundary.MainType.Left;
					break;
				case Role.RightPlayer:
					m = MainBoundary.MainType.Right;
					break;
			}
			if (MainBoundary.boundPerMode[(int)m] != null)
			{
				mainBoundary = MainBoundary.boundPerMode[(int)m];
			}
		}

		private void SetVarsInDiscs()
		{
			if (varDiscs != null && varDiscs.Count > 0)
			{
				foreach (Blastodisc bd in varDiscs)
				{
					((IVariableContainer)bd).SetVar("shoot", IsShooting());
					((IVariableContainer)bd).SetVar("focus", IsFocused());
					((IVariableContainer)bd).SetVar("power", shotPower);
					((IVariableContainer)bd).SetVar("power_index", shotPowerIndex);
				}
			}
		}

		private void FindDiscs(Node n = null)
		{
			if (n == null) { n = this; }
			foreach (Node child in n.GetChildren())
			{
				if (child is Blastodisc) { varDiscs.Add((Blastodisc)child); }
				FindDiscs(child);
			}
		}

		/// <summary>
		/// Returns true when above the item get line.
		/// </summary>
		private bool IsInItemGetMode()
		{
			return lifeState != LifeState.Dying && GlobalPosition.Y <= itemGetHeight;
		}

		public int GetMinPower()
		{
			return shotPowerCutoffs[0];
		}

		public int GetMaxPower()
		{
			return shotPowerCutoffs[shotPowerCutoffs.Length - 1];
		}

		public void RecalculateShotPowerIndex()
		{
			shotPowerIndex = 1;
			while (shotPowerIndex < shotPowerCutoffs.Length && shotPower >= shotPowerCutoffs[shotPowerIndex])
			{
				shotPowerIndex++;
			}
		}

		public void AddLives(float amount)
		{
			float oldLives = lives;
			lives += amount;
			// If we're close enough to a full new life, round up for leniency.
			// This may be undesirable in rare types of fractional life filling.
			if (lives - Mathf.Floor(lives) >= 0.95f)
			{
				lives = Mathf.Ceil(lives);
			}
			// Also round down, but only when it's very close.
			// This prevents us from slowly gaining tiny slivers of lives.
			else if (lives - Mathf.Floor(lives) <= 0.0001f)
			{
				lives = Mathf.Floor(lives);
			}
			// If I now have more full lives, make the major effect,
			// or else a minor effect.
			if (Mathf.Floor(lives) > Mathf.Floor(oldLives)) { PerformExtendEffect(); }
			else { PerformExtendPieceEffect(); }
		}

		public void SetInitialLives()
		{
			lives = lifeRefillOnContinue;
		}

		public void SetInitialBombs()
		{
			bombs = bombRefillOnDeath;
		}

		public void AddBombs(float amount)
		{
			float oldBombs = bombs;
			bombs += amount;
			// If we're close enough to a full new bomb, round up for leniency.
			// This may be undesirable in rare types of fractional bomb filling.
			if (bombs - Mathf.Floor(bombs) >= 0.95f)
			{
				bombs = Mathf.Ceil(bombs);
			}
			// Also round down, but only when it's very close.
			// This prevents us from slowly gaining tiny slivers of bombs.
			else if (bombs - Mathf.Floor(bombs) <= 0.0001f)
			{
				bombs = Mathf.Floor(bombs);
			}
			// If I now have more full bombs, make the major effect,
			// or else a minor effect.
			if (Mathf.Floor(bombs) > Mathf.Floor(oldBombs)) { PerformGetBombEffect(); }
			else { PerformGetBombPieceEffect(); }
		}

		public void PerformExtendEffect()
		{
			CommonSFXManager.PlayByName("Player/Extend", 1, 1f, GlobalPosition, true);
			MusicManager.Duck(2.5f, 0f);
			LabelPool.Play("MajorItem", GlobalPosition + Vector2.Up * 30, "Life get.", Colors.White);
		}

		public void PerformExtendPieceEffect()
		{
			CommonSFXManager.PlayByName("Player/ExtendPiece", 1, 1f, GlobalPosition, true);
			LabelPool.Play("MinorItem", GlobalPosition, "Life piece get.", Colors.White);
		}

		public void PerformGetBombEffect()
		{
			CommonSFXManager.PlayByName("Player/GetBomb", 1, 1f, GlobalPosition, true);
			LabelPool.Play("MajorItem", GlobalPosition + Vector2.Up * 30, "Bomb get.", Colors.White);
		}

		public void PerformGetBombPieceEffect()
		{
			CommonSFXManager.PlayByName("Player/GetBombPiece", 1, 1f, GlobalPosition, true);
			LabelPool.Play("MinorItem", GlobalPosition, "Bomb piece get.", Colors.White);
		}

		public void PerformPowerUpEffect()
		{
			CommonSFXManager.PlayByName("Player/PowerUp", 1, 1f, GlobalPosition, true);
			if (shotPower == GetMaxPower())
			{
				LabelPool.Play("MajorItem", GlobalPosition + Vector2.Up * 30, "Power MAX.", Colors.White);
			}
			else
			{
				LabelPool.Play("MinorItem", GlobalPosition, "Power evolve.", Colors.White);
			}
		}

		private long recoverAnimIteration = 0;
		public IEnumerator Recover(float durationSeconds)
		{
			long currIter = ++recoverAnimIteration;
			lifeState = LifeState.Recovering;
			recoveryGracePeriodActive = false;
			yield return new WaitTime(durationSeconds);
			if (currIter != recoverAnimIteration) { yield break; }
			recoveryGracePeriodActive = true;
			yield return new WaitTime(recoverDurationGrace);
			if (currIter != recoverAnimIteration) { yield break; }
			lifeState = LifeState.Normal;
			recoveryGracePeriodActive = false;
		}

		public IEnumerator ReleaseBomb()
		{
			if (bombs < 1f || bombing) { yield break; }
			++recoverAnimIteration;
			bombs -= 1f;
			bombing = true;
			lifeState = LifeState.Invulnerable;
			CommonSFXManager.PlayByName("BombStart", 1, 1f, GlobalPosition, true);
			// Spawn the item
			PackedScene itemToLoad = null;
			switch (bombItems?.Length ?? 0)
			{
				case 0: GD.PushWarning("There is no bomb item!"); break;
				case 1: itemToLoad = bombItems[0]; break;
				default: itemToLoad = IsFocused() ? bombItems[1] : bombItems[0]; break;
			}
			if (itemToLoad != null)
			{
				Node2D item = itemToLoad.Instantiate<Node2D>();
				Persistent.GetMainScene().AddChild(item);
				item.GlobalPosition = GlobalPosition;
			}
			yield return new WaitTime(bombDuration);
			bombing = false;
			this.StartCoroutine(Recover(bombRecoveryDuration));
		}

		/// <summary>
		/// Try to cause the player to die.
		/// </summary>
		public IEnumerator Die()
		{
			if (debugInvulnerable || settingInvulnerable || lifeState != LifeState.Normal) { yield break; }
			++recoverAnimIteration;
			lifeState = LifeState.Dying;
			recoveryGracePeriodActive = false;
			CommonSFXManager.PlayByName("Player/Struck", 1, 1f, GlobalPosition, true);
			deathbombBuffer.Replenish((ulong)deathbombFrames);
			while (!deathbombBuffer.Elapsed())
			{
				if (IsBombTriggered()) { this.StartCoroutine(ReleaseBomb()); yield break; }
				yield return new WaitOneFrame();
			}
			// No turning back now
			CommonSFXManager.PlayByName("Player/Explode", 1, 1f, GlobalPosition, true);
			// Scatter power collectibles
			int collectibleAmount = (shotPower - GetMinPower()) / powerDropCollectibleValue;
			collectibleAmount = Mathf.Clamp(collectibleAmount, 0, powerDropCollectibleMax);
			CollectibleManager.SpawnItems(powerDropCollectibleName, GlobalPosition, collectibleAmount);
			// Remove power
			shotPower -= shotPowerLossOnDeath;
			if (shotPower < GetMinPower()) { shotPower = GetMinPower(); }
			// The power item value is reset
			if (Session.main != null)
			{
				Session.main.SetPowerItemValue(Session.main.minPowerItemValue);
			}
			RecalculateShotPowerIndex();
			// Create the explosion effect
			if (deathExplosion != null)
			{
				Node daInstance = deathExplosion.Instantiate();
				GetTree().Root.AddChild(daInstance);
				((Node2D)daInstance).GlobalPosition = GlobalPosition;
			}
			yield return new WaitTime(deathAnimationDuration);
			// The life is finally decremented after the explosion effect plays out.
			lives -= 1f;
			// Avoid float imprecision causing a game over.
			if (lives < 0f && lives >= -0.001f) { lives = 0f; }
			if (lives < 0f)
			{
				if (Session.main != null && role == Role.SinglePlayer)
				{
					Session.main.SinglePlayerGameOver();
				}
			}
			else
			{
				SetInitialBombs();
			}
			GlobalPosition = homePosition;
			this.StartCoroutine(Recover(deathRecoveryDuration));
		}

		private unsafe void OnHitHurtIntent(BlastulaCollider collider, int bNodeIndex)
		{
			BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;

			// Because of the way lasers are rendered, the head and tail collisions could be unfair
			// (possible to occur outside the graphic)
			if (LaserRenderer.IsBNodeHeadOfLaser(bNodeIndex) || LaserRenderer.IsBNodeTailOfLaser(bNodeIndex))
			{
				if (bNodePtr->bulletRenderID < 0) { return; }
			}

			if (bNodePtr->health > 1)
			{
				bNodePtr->health -= (float)Engine.TimeScale;
			}
			else
			{
				bNodePtr->health = 0;
				if (bNodePtr->laserRenderID < 0)
				{
					bNodePtr->transform = BulletWorldTransforms.Get(bNodeIndex);
					bNodePtr->worldTransformMode = true;
					PostExecute.ScheduleDeletion(bNodeIndex, true);
				}
				else
				{
					BulletRenderer.SetRenderID(bNodeIndex, -1);
					LaserRenderer.RemoveLaserEntry(bNodeIndex);
				}
			}

			// At this point the hurting actually occurs
			this.StartCoroutine(Die());
		}

		private unsafe void OnHitGrazeIntent(BlastulaCollider collider, int bNodeIndex)
		{
			BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
			if (bNodePtr->graze >= 0)
			{
				bool grazeGet = false;

				if (bNodePtr->bulletRenderID >= 0)
				{
					if (bNodePtr->graze == 0) { grazeGet = true; }
					if (bNodePtr->graze >= 0) { bNodePtr->graze += (float)Engine.TimeScale; }
				}
				else if (bNodePtr->laserRenderID >= 0)
				{
					bool newGrazeThisFrame = LaserRenderer.NewGrazeThisFrame(bNodeIndex, out int headBNodeIndex);
					if (newGrazeThisFrame)
					{
						BNode* headBNodePtr = BNodeFunctions.masterQueue + headBNodeIndex;
						float oldGraze = headBNodePtr->graze;
						float newGraze = oldGraze + (float)Engine.TimeScale;
						if (oldGraze == 0
							|| oldGraze + framesBetweenLaserGraze <= newGraze
							|| oldGraze % framesBetweenLaserGraze >= newGraze % framesBetweenLaserGraze)
						{
							grazeGet = true;
						}
						if (oldGraze < 0) { grazeGet = false; }
						else { headBNodePtr->graze = newGraze; }
					}
				}

				if (grazeGet)
				{
					++grazeGetThisFrame;
					if (grazeGetThisFrame < 5)
					{
						CommonSFXManager.PlayByName("Player/Graze", 1, 1f, GlobalPosition, true);
						GrazeLines.ShowLine(GlobalPosition, BulletWorldTransforms.Get(bNodeIndex).Origin);
						// Without this the bullet wiggles because we tried to calculate the position before the movement.
						// We don't want that to happen, so force recalculate when the time is right.
						BulletWorldTransforms.Invalidate(bNodeIndex);
					}
				}
			}
		}

		private unsafe void OnHitCollectIntent(BlastulaCollider collider, int bNodeIndex)
		{
			BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
			bool itemGetLineActivated = (bNodePtr->phase == 3);
			Vector2 bulletWorldPos = BulletWorldTransforms.Get(bNodeIndex).Origin;

			string collectibleItemName = CollectibleManager.GetItemName(bNodeIndex);

			if (collectibleItemName == "Point")
			{
				int multiplier = Mathf.RoundToInt(bNodePtr->power);
				if (itemGetLineActivated)
				{
					// Add the full value of the point item
					var actualAdded = Session.main.AddScore(multiplier * (Session.main?.pointItemValue ?? 10));
					string scoreString = LabelPool.GetScoreString(actualAdded);
					LabelPool.Play(SCORE_NUMBER_LABEL_POOL_NAME, bulletWorldPos, scoreString, Colors.Cyan);
				}
				else
				{
					// Add a cut value of the point item depending exponentially on the player's screen height
					double fullValue = (double)(Session.main?.pointItemValue ?? 10);
					double cutValue = fullValue
						* (1.0 - pointItemValueCut)
						* System.Math.Pow(1.0 - pointItemValueRolloff, (GlobalPosition.Y - itemGetHeight) / 100.0);
					var actualAdded = Session.main.AddScore(multiplier * cutValue);
					string scoreString = LabelPool.GetScoreString(actualAdded);
					LabelPool.Play(SCORE_NUMBER_LABEL_POOL_NAME, bulletWorldPos, scoreString, Colors.White);
				}

				if (StageManager.main != null) { StageManager.main.AddPointItem(1); }
				if (Session.main != null) { Session.main.AddPointItem(1); }
			}
			else if (collectibleItemName == "Power")
			{
				// Increase power item value if already at max power
				if (shotPower >= GetMaxPower() && Session.main != null)
				{
					int gain = 10;
					gain = gain * Mathf.RoundToInt(bNodePtr->power);
					Session.main.AddPowerItemValue(gain);
				}

				// Increase player's power
				shotPower += Mathf.RoundToInt(bNodePtr->power);
				if (shotPower > GetMaxPower()) { shotPower = GetMaxPower(); }

				// Increase power index if appropriate, creating a "Power Up" type effect when it happens
				bool shotPowerIndexIncreases = false;
				while (shotPowerIndex < shotPowerCutoffs.Length && shotPower >= shotPowerCutoffs[shotPowerIndex])
				{
					shotPowerIndex++;
					shotPowerIndexIncreases = true;
				}
				if (shotPowerIndexIncreases)
				{
					PerformPowerUpEffect();
				}

				if (StageManager.main != null) 
				{ 
					StageManager.main.AddPowerItem(1); 
				}
				if (Session.main != null) 
				{ 
					Session.main.AddPowerItem(1);
					var actualAdded = Session.main.AddScore(Session.main.powerItemValue);
					string scoreString = LabelPool.GetScoreString(actualAdded);
					LabelPool.Play(SCORE_NUMBER_LABEL_POOL_NAME, bulletWorldPos, scoreString, itemGetLineActivated ? Colors.Cyan : Colors.White);
				}
				
			}
			else if (collectibleItemName == "Extend")
			{
				AddLives(bNodePtr->power);
			}
			else if (collectibleItemName == "GetBomb")
			{
				AddBombs(bNodePtr->power);
			}
			else if (collectibleItemName == "CancelItem")
			{
				Session.main.AddScore(Session.main?.cancelItemValue ?? 10);
				if (StageManager.main != null) { StageManager.main.AddCancelItem(1); }
				if (Session.main != null) { Session.main.AddCancelItem(1); }
			}

			PostExecute.ScheduleDeletion(bNodeIndex, false);
			CommonSFXManager.PlayByName("Player/Vacuum", 1, 1f, GlobalPosition, true);
		}

		private unsafe void OnHitAttractCollectIntent(BlastulaCollider collider, int bNodeIndex)
		{
			BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
			short phase = bNodePtr->phase;
			if (phase == 1 && Sequence.referencesByID.ContainsKey(COLLECTIBLE_ATTRACT_SEQUENCE_NAME))
			{
				PostExecute.ScheduleOperation(
					bNodeIndex,
					Sequence.referencesByID[COLLECTIBLE_ATTRACT_SEQUENCE_NAME]?.GetOperationID() ?? -1
				);

				if (IsInItemGetMode())
				{
					// Tint the items so we know which ones have the full value later on
					if (bNodePtr->multimeshExtras == null)
					{
						bNodePtr->multimeshExtras = SetMultimeshExtraData.NewPointer();
					}
					bNodePtr->multimeshExtras->color = 1.2f * Colors.LightBlue;
					bNodePtr->phase++;
				}
			}
		}

		/// <summary>
		/// Response to a collider being hit by a bullet on the "EnemyShot" collision layer.
		/// Also handles grazing, naturally.
		/// </summary>
		public unsafe void OnHit(BlastulaCollider collider, int bNodeIndex)
		{
			if (lifeState == LifeState.Dying) { return; }
			// bNodeIndex is always >= 0, how could we get here otherwise???
			BNode* bNodePtr = BNodeFunctions.masterQueue + bNodeIndex;
			int collisionLayer = bNodePtr->collisionLayer;
			int enemyShotBulletLayer = CollisionManager.GetBulletLayerIDFromName("EnemyShot");
			int collectibleBulletLayer = CollisionManager.GetBulletLayerIDFromName("Collectible");
			if (Engine.TimeScale > 0 && collisionLayer == enemyShotBulletLayer)
			{
				if (collider == hurtbox) { OnHitHurtIntent(collider, bNodeIndex); }
				else if (collider == grazebox) { OnHitGrazeIntent(collider, bNodeIndex); }
			}
			else if (Engine.TimeScale > 0 && collisionLayer == collectibleBulletLayer)
			{
				if (collider == hurtbox) { OnHitCollectIntent(collider, bNodeIndex); }
				else if (collider == attractbox) { OnHitAttractCollectIntent(collider, bNodeIndex); }
			}
		}

		public bool IsShooting() 
		{ 
			if (lifeState == LifeState.Dying) { return false; }
			return inputTranslator.inputItems["Shoot"].currentState; 
		}

		public bool IsFocused()
		{
			if (lifeState == LifeState.Dying && deathbombBuffer.Elapsed()) { return false; }
			return inputTranslator.inputItems["Focus"].currentState;
		}

		private FrameCounter.Cache<Vector2> mvtDirCache = new FrameCounter.Cache<Vector2>();

		/// <summary>
		/// Calculates this frame's movement direction vector, which has length 1 when moving, and is the zero vector when not moving.
		/// </summary>
		public Vector2 GetMovementDirection()
		{
			if (mvtDirCache.IsValid()) { return mvtDirCache.data; }
			Vector2 v = Vector2.Zero;
			if (inputTranslator.inputItems["Left"].currentState) { v += Vector2.Left; }
			if (inputTranslator.inputItems["Right"].currentState) { v += Vector2.Right; }
			if (inputTranslator.inputItems["Up"].currentState) { v += Vector2.Up; }
			if (inputTranslator.inputItems["Down"].currentState) { v += Vector2.Down; }
			Vector2 result = (v != Vector2.Zero) ? v.Normalized() : v;
			mvtDirCache.Update(result);
			return result;
		}

		private unsafe void PerformMovement()
		{
			if (lifeState == LifeState.Dying) { return; }
			float speed = IsFocused() ? focusedSpeed : normalSpeed;
			speed *= (float)Engine.TimeScale;
			speed /= Persistent.SIMULATED_FPS;
			GlobalPosition += speed * GetMovementDirection();
			if (mainBoundary == null) { FindMainBoundary(); }
			if (mainBoundary != null)
			{
				GlobalPosition = Boundary.Clamp(mainBoundary.lowLevelInfo, GlobalPosition, boundaryShrink);
			}
		}

		private bool IsBombTriggered()
		{
			if (inputTranslator.inputItems["Bomb"].currentState) { bombStartBuffer.Replenish((ulong)bombStartBufferFrames); }
			bool isAlive = lifeState != LifeState.Dying || !deathbombBuffer.Elapsed();
			return isAlive && bombs >= 1f && !bombStartBuffer.Elapsed() && !bombing;
		}

		private void CountGrazeGetThisFrame()
		{
			if (StageManager.main != null) 
			{ 
				StageManager.main.AddGraze(grazeGetThisFrame); 
			}

			if (Session.main != null) 
			{
				ulong oldGraze = Session.main.grazeCount;
				Session.main.AddGraze(grazeGetThisFrame);
				// Every four graze = +10 point item value
				int pointItemValueJumps = (int)(Session.main.grazeCount / 4 - oldGraze / 4);
				Session.main.AddPointItemValue(pointItemValueJumps * 10);
			}

			// Customize the framework and its classes however you like.
			// For example, one can add code here that increases the rank per every graze.

			grazeGetThisFrame = 0;
		}

		public override void _Process(double delta)
		{
			if (Session.IsPaused()) { return; }
			if (!GameSpeed.pseudoStopped)
			{
				PerformMovement();
				if (IsBombTriggered()) { this.StartCoroutine(ReleaseBomb()); }
			}
			SetVarsInDiscs();
			CountGrazeGetThisFrame();
			if (IsInItemGetMode()) { attractbox.size = new Vector2(2000, 2000); }
			else { attractbox.size = attractboxOriginalSize; }
		}
	}
}

