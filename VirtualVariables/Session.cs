using Blastula.Schedules;
using Godot;
using System.Collections.Generic;
using System.Numerics;

namespace Blastula.VirtualVariables
{
	/// <summary>
	/// These variables are meant to persist within game sessions (between going back to the start menu).
	/// </summary>
	public partial class Session : Node, IVariableContainer, IPersistForReplay
	{
		/// <summary>
		/// Whether the game is actually in session. Otherwise, we are in a title menu.
		/// </summary>
		public bool inSession { get; private set; } = false;
		/// <summary>
		/// True if the game can be paused.
		/// </summary>
		public bool canPause { get; private set; } = true;
		/// <summary>
		/// True if the game is paused.
		/// </summary>
		public bool paused { get; private set; } = false;
		/// <summary>
		/// The time scale used for bullet behavior.
		/// </summary>
		public double timeScale { get; private set; } = 1.0;
		/// <summary>
		/// This integer is the game's difficulty, if you choose to use it.
		/// </summary>
		/// <remarks>
		/// Standard convention is 0: easy, 1: normal, 2: hard, 3: (whatever funny name you give to the hardest difficulty), [4: extra].
		/// This allows you to select values in expressions using the difficulty as an array index, like
		/// [8, 16, 20, 24][dif] for the number of bullets in a ring.
		/// </remarks>
		public int difficulty { get; private set; } = 1;
		/// <summary>
		/// This is the game's rank, if you choose to use it.
		/// </summary>
		/// <remarks>
		/// Rank is a common element in classic STGs, similar to a sub-difficulty, and usually adapts to how well the player progresses
		/// by changing bullet pattern density or speed. 
		/// </remarks>
		/// <example>
		/// I suggest varying it within the interval [0, 1]. This allows you to interpolate values in expressions, like
		/// lerp(400, 800, rank) for a bullet's speed.
		/// </example>
		public float rank { get; private set; } = 0.5f;
		/// <summary>
		/// True if we don't want the rank to change. Good for testing and practicing.
		/// </summary>
		public bool rankFrozen = false;
		/// <summary>
		/// The score achieved in this game session.
		/// </summary>
		public BigInteger score { get; private set; } = BigInteger.Zero;
		/// <summary>
		/// The highest score possible to achieve.
		/// </summary>
		public BigInteger maxScore { get; private set; } = BigInteger.Pow(new BigInteger(10), 100) - 1;
		/// <summary>
		/// Should be the highest score ever achieved by the player. 
		/// This class doesn't handle saving and loading.
		/// </summary>
		public BigInteger recordScore { get; private set; } = 0;
		/// <summary>
		/// Number of bullets grazed throughout the session. Mainly for single-player use.
		/// </summary>
		public ulong grazeCount { get; private set; } = 0;
		/// <summary>
		/// Number of point items collected throughout the session. Mainly for single-player use.
		/// </summary>
		public ulong pointItemCount { get; private set; } = 0;
		/// <summary>
		/// The full score awarded upon collecting a point item (possibly only above the item get line).
		/// </summary>
		public BigInteger pointItemValue { get; private set; } = 10000;
		/// <summary>
		/// The lowest possible full point item value.
		/// </summary>
		public BigInteger minPointItemValue { get; private set; } = 10000;
		/// <summary>
		/// The highest possible full point item value.
		/// </summary>
		public BigInteger maxPointItemValue { get; private set; } = 999990;
		/// <summary>
		/// Number of power items collected throughout the session. Mainly for single-player use.
		/// </summary>
		public ulong powerItemCount { get; private set; } = 0;
		/// <summary>
		/// The full score awarded upon collecting a power item.
		/// </summary>
		public BigInteger powerItemValue { get; private set; } = 10;
		/// <summary>
		/// The lowest possible full point item value.
		/// </summary>
		public BigInteger minPowerItemValue { get; private set; } = 10;
		/// <summary>
		/// The highest possible full point item value.
		/// </summary>
		public BigInteger maxPowerItemValue { get; private set; } = 100000;
		/// <summary>
		/// Number of cancel items collected throughout the session. Mainly for single-player use.
		/// </summary>
		public BigInteger cancelItemCount { get; private set; } = 0;
		/// <summary>
		/// The full score awarded upon collecting a cancel item.
		/// </summary>
		public BigInteger cancelItemValue { get; private set; } = 300;
		/// <summary>
		/// If true, the player can continue ("insert credit") when they lost all lives and try to respawn.
		/// </summary>
		public bool canContinue = true;
		/// <summary>
		/// The number of times the player has continued.
		/// </summary>
		public ulong continueCount { get; private set; } = 0;
		/// <summary>
		/// If nonnegative, overrides the player's initial life count.
		/// </summary>
		public float lifeOverride { get; private set; } = -1;
		/// <summary>
		/// If nonnegative, overrides the player's bombs per life.
		/// </summary>
		public float bombOverride { get; private set; } = -1;

		public void StartInSession()
		{
			inSession = true;
		}

		public void EndInSession()
		{
			inSession = false;
		}

		public void SetCanPause(bool s)
		{
			canPause = s;
		}

		private double oldTimeScale = 1.0;
		public void Pause()
		{
			if (!canPause || paused) { return; }
			oldTimeScale = Engine.TimeScale;
			Engine.TimeScale = 0;
			paused = true;
		}

		public void Unpause()
		{
			if (!paused) { return; }
			Engine.TimeScale = oldTimeScale;
			paused = false;
		}

		public void SetTimeScale(double newTimeScale)
		{
			if (paused) { oldTimeScale = newTimeScale; }
			else { Engine.TimeScale = newTimeScale; }
			timeScale = newTimeScale;
		}

		public void Reset()
		{
			canPause = true;
			paused = false;
			timeScale = 1f;
			difficulty = 1;
			rank = 0.5f;
			rankFrozen = false;
			score = 0;
			recordScore = 0;
			grazeCount = 0;
			pointItemCount = 0;
			pointItemValue = minPointItemValue;
			powerItemCount = 0;
			powerItemValue = minPowerItemValue;
			cancelItemCount = 0;
			if (StageManager.main != null)
			{
				StageManager.main.Reset();
			}
			canContinue = true;
			continueCount = 0;
		}

		public void SetRecordScore(BigInteger recordToSet)
		{
			recordScore = recordToSet;
		}

		public Godot.Collections.Dictionary<string, string> CreateReplaySnapshot()
		{
			return new Godot.Collections.Dictionary<string, string>
			{
				{PropertyName.timeScale, timeScale.ToString() },
				{PropertyName.difficulty, difficulty.ToString() },
				{PropertyName.rank, rank.ToString() },
				{PropertyName.rankFrozen, rankFrozen.ToString() },
				{"score", score.ToString() },
				{PropertyName.grazeCount, grazeCount.ToString() },
				{PropertyName.pointItemCount, pointItemCount.ToString() },
				{"pointItemValue", pointItemValue.ToString() },
				{PropertyName.powerItemCount, powerItemCount.ToString() },
				{"powerItemValue", powerItemValue.ToString() },
			};
		}

		public void LoadReplaySnapshot(Godot.Collections.Dictionary<string, string> snapshot)
		{
			try
			{
				timeScale = double.Parse(snapshot[PropertyName.timeScale]);
				difficulty = int.Parse(snapshot[PropertyName.difficulty]);
				rank = float.Parse(snapshot[PropertyName.rank]);
				rankFrozen = bool.Parse(snapshot[PropertyName.rankFrozen]);
				score = BigInteger.Parse(snapshot["score"]);
				grazeCount = ulong.Parse(snapshot[PropertyName.grazeCount]);
				pointItemCount = ulong.Parse(snapshot[PropertyName.pointItemCount]);
				pointItemValue = ulong.Parse(snapshot["pointItemValue"]);
				powerItemCount = ulong.Parse(snapshot[PropertyName.powerItemCount]);
				powerItemValue = ulong.Parse(snapshot["powerItemValue"]);
			} 
			catch
			{
				throw new System.Exception("Session: Unable to load data from replay file.");
			}
		}

		public static bool IsPaused()
		{
			if (main == null) { return true; }
			return main.paused;
		}

		public void SetDifficulty(int newDifficulty)
		{
			difficulty = newDifficulty;
		}

		/// <param name="newRank">Rank value to replace the old one.</param>
		/// <param name="force">If true, change the rank even while it's frozen.</param>
		public void SetRank(float newRank, bool force = false)
		{
			if (rankFrozen && !force) { return; }
			rank = newRank;
		}

		#region Score

		private void ClampScore()
		{
			if (score < 0) { score = 0; }
			if (score > maxScore) { score = maxScore; }
		}

		// Sorry this isn't a signal. BigInteger is not a variant.
		private void OnScoreChange(BigInteger oldScore, BigInteger newScore)
		{
			if (oldScore == newScore) return;
			SetScoreExtends.Check(oldScore, newScore);
		}

		/// <summary>Rounds up to the tens place, and adds to the score.</summary>
		/// <returns>The actual value added.</returns>
		public BigInteger AddScore(int amount)
		{
			int remainder = amount % 10;
			if (remainder != 0) { amount += 10 - remainder; }
			BigInteger oldScore = score;
			score += amount;
			ClampScore();
            OnScoreChange(oldScore, score);
            return amount;
		}

		/// <summary>Rounds up to the tens place, and adds to the score.</summary>
		/// <returns>The actual value added.</returns>
		public BigInteger AddScore(double amount)
		{
			amount = System.Math.Ceiling(amount / 10) * 10;
			amount = Mathf.Max(10, amount);
			BigInteger bi = new BigInteger(amount);
            BigInteger oldScore = score;
            score += bi;
			ClampScore();
			OnScoreChange(oldScore, score);
			return bi;
		}

		/// <summary>Rounds up to the tens place, and adds to the score.</summary>
		/// <returns>The actual value added.</returns>
		public BigInteger AddScore(BigInteger amount)
		{
			int remainder = (int)(amount % 10);
			if (remainder != 0) { amount += 10 - remainder; }
            BigInteger oldScore = score;
            score += amount;
			ClampScore();
            OnScoreChange(oldScore, score);
            return amount;
		}

		public void SetScore(BigInteger amount)
		{
			BigInteger oldScore = score;
			score = amount;
			ClampScore();
            OnScoreChange(oldScore, score);
        }

		#endregion

		public void AddGraze(int amount)
		{
			grazeCount += (ulong)amount;
		}

		#region Point Items

		public void AddPointItem(int amount)
		{
			pointItemCount += (ulong)amount;
		}

		public void AddPointItemValue(int amount)
		{
			pointItemValue += amount;
			if (pointItemValue < minPointItemValue) { pointItemValue = minPointItemValue; }
			else if (pointItemValue > maxPointItemValue) { pointItemValue = maxPointItemValue; }
		}

		public void SetPointItemValue(BigInteger amount)
		{
			pointItemValue = amount;
			if (pointItemValue < minPointItemValue) { pointItemValue = minPointItemValue; }
			else if (pointItemValue > maxPointItemValue) { pointItemValue = maxPointItemValue; }
		}

		public void SetMinPointItemValue(BigInteger newValue)
		{
			minPointItemValue = newValue;
			if (pointItemValue < minPointItemValue) { pointItemValue = minPointItemValue; }
		}

		public void SetMaxPointItemValue(BigInteger newValue)
		{
			maxPointItemValue = newValue;
			if (pointItemValue > maxPointItemValue) { pointItemValue = maxPointItemValue; }
		}

		public void AddCancelItem(int amount)
		{
			cancelItemCount += (ulong)amount;
		}

		#endregion

		#region Power Items

		public void AddPowerItem(int amount)
		{
			powerItemCount += (ulong)amount;
		}

		public void AddPowerItemValue(int amount)
		{
			powerItemValue += amount;
			if (powerItemValue < minPowerItemValue) { powerItemValue = minPowerItemValue; }
			else if (powerItemValue > maxPowerItemValue) { powerItemValue = maxPowerItemValue; }
		}

		public void SetPowerItemValue(BigInteger amount)
		{
			powerItemValue = amount;
			if (powerItemValue < minPowerItemValue) { powerItemValue = minPowerItemValue; }
			else if (powerItemValue > maxPowerItemValue) { powerItemValue = maxPowerItemValue; }
		}

		public void SetMinPowerItemValue(BigInteger newValue)
		{
			minPowerItemValue = newValue;
			if (powerItemValue < minPowerItemValue) { powerItemValue = minPowerItemValue; }
		}

		public void SetMaxPowerItemValue(BigInteger newValue)
		{
			maxPowerItemValue = newValue;
			if (powerItemValue > maxPowerItemValue) { powerItemValue = maxPowerItemValue; }
		}

		#endregion

		#region Override Life/Bombs

		public void SetLifeOverride(string setting)
		{
			if (setting == "∞") { lifeOverride = Mathf.Inf; return; }
			else if (float.TryParse(setting, out float settingNum)) { lifeOverride = settingNum; return; }
			lifeOverride = -1; return;
		}

		public void SetBombOverride(string setting)
		{
			if (setting == "∞") { bombOverride = Mathf.Inf; return; }
			else if (float.TryParse(setting, out float settingNum)) { bombOverride = settingNum; return; }
			bombOverride = -1; return;
		}

		#endregion

		// Used when the player runs out of lives and tries to respawn.
		public void SinglePlayerGameOver()
		{
			if (PauseMenuManager.main != null)
			{
				PauseMenuManager.Mode newMode = canContinue ? PauseMenuManager.Mode.GameOver : PauseMenuManager.Mode.GameOverNoContinue;
				PauseMenuManager.main.SetMode(newMode);
				PauseMenuManager.main.PrepareToOpen();
			}
		}

		// Used when the player continues ("insert credit") in a single-player game.
		public void SinglePlayerContinue()
		{
			// The score's units digit is the number of continues used.
			// The other score functions ensure this digit doesn't change otherwise.
			score = score % 10;
			if (score < 9) { score += 1; }
			continueCount += 1;
			if (Player.playersByControl.ContainsKey(Player.Role.SinglePlayer))
			{
				Player singlePlayer = Player.playersByControl[Player.Role.SinglePlayer];
				singlePlayer.SetInitialLives();
				singlePlayer.SetInitialBombs();
			}
		}

		public static Session main { get; private set; } = null;

		public Dictionary<string, Variant> customData { get; set; } = new Dictionary<string, Variant>();

		public HashSet<string> specialNames { get; set; } = new HashSet<string>()
		{
			"can_pause", "paused", "time_scale", "difficulty", "dif", "rank", "score64", "time"
		};

		public override void _Ready()
		{
			base._Ready();
			main = this;
		}

		public Variant GetSpecial(string varName)
		{
			switch (varName)
			{
				case "can_pause": return canPause;
				case "paused": return paused;
				case "time_scale": return timeScale;
				case "rank": return rank;
				case "difficulty": case "dif": return difficulty;
				case "score64": return (long)score;
			}
			return default;
		}
	}
}
