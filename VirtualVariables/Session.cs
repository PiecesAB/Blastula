using Godot;
using System.Collections.Generic;

namespace Blastula.VirtualVariables
{
    /// <summary>
    /// These variables are meant to persist within game sessions (between going back to the start menu).
    /// </summary>
	public partial class Session : Node, IVariableContainer
    {
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
        /// Standard convention is 0: easy, 1: normal, 2: hard, 3: (whatever funny name you give to the hardest difficulty)
        /// But anything's possible!
        /// </summary>
        public int difficulty { get; private set; } = 1;
        /// <summary>
        /// This is the game's rank, if you choose to use it.
        /// Rank is a common element in classic STGs, similar to a sub-difficulty, and usually adapts to how well the player progresses
        /// by changing bullet pattern density or speed.
        /// I suggest varying it within the interval [0, 1].
        /// </summary>
        public float rank { get; private set; } = 0.5f;
        /// <summary>
        /// True if we don't want the rank to change. Good for testing and practicing.
        /// </summary>
        public bool rankFrozen = false;

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
            paused = false;
            timeScale = 1f;
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

        /// <param name="force">If true, change the rank even while it's frozen.</param>
        public void SetRank(float newRank, bool force = false)
        {
            if (rankFrozen && !force) { return; }
            rank = newRank;
        }

        public static Session main { get; private set; } = null;

        public Dictionary<string, Variant> customData { get; set; } = new Dictionary<string, Variant>();

        public HashSet<string> specialNames { get; set; } = new HashSet<string>()
        {
            "can_pause", "paused", "time_scale", "difficulty", "dif", "rank"
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
            }
            return default;
        }
    }
}
