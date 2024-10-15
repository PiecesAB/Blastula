using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Numerics;

namespace Blastula.Graphics
{
    /// <summary>
    /// Handles the score counter as it's seen in the overlay.
    /// </summary>
    public partial class ScoreDisplay : Node
    {
        [Export] public Label currentScore;
        [Export] public Label recordScore;
        [Export] public Label nextExtendScore;

        private BigInteger displayedScore = 0;

        private static string GetScoreString(BigInteger bigInteger)
        {
            string raw = bigInteger.ToString();
            if (raw.Length <= 10) { return raw; }
            else { return raw[0] + "." + raw.Substring(1, 5) + "e" + (raw.Length - 1).ToString(); }
        }

        private static BigInteger GetIncrement(int length)
        {
            return 10 * ((BigInteger.Pow(10, length) / 45) + 1);
        }

        public void RecalculateRecordScore()
        {
            recordScore.Text = GetScoreString(Session.main.recordScore);
        }

        public override void _Ready()
        {
            base._Ready();
            RecalculateRecordScore();
            StageManager.main.Connect(
                StageManager.SignalName.SessionBeginning, 
                new Callable(this, MethodName.RecalculateRecordScore));
            _Process(0);
        }

        private BigInteger? previousScore = null;

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (Session.main == null || Session.main.paused) { return; }
            // If the displayed score is accurate to the tens place or otherwise all visible digits, stop ticking.
            BigInteger difference = (Session.main.score - displayedScore);
            int difLength = difference.ToString().Length;
            if (difference <= (Session.main.score >> 35) || difference <= 10)
            {
                displayedScore = Session.main.score;
            }
            else
            {
                BigInteger increment = GetIncrement(difLength);
                if (increment > difference) { increment = GetIncrement(difLength - 1); }
                displayedScore += increment;
            }
            currentScore.Text = GetScoreString(displayedScore);
            if (displayedScore >= Session.main.recordScore)
            {
                recordScore.Text = currentScore.Text;
                if (previousScore < Session.main.recordScore && Session.main.score > 0)
                {
                    SpecialGameEventNotifier.Trigger(SpecialGameEventNotifier.EventType.HighScore);
                }
            }

            if (nextExtendScore != null)
            {
                BigInteger? maybeNext = SetScoreExtends.GetNextExtendScore();
                if (maybeNext.HasValue)
                {
                    nextExtendScore.Text = maybeNext.Value.ToString();
                } 
                else
                {
                    nextExtendScore.Text = "---";
                }
            }

            previousScore = Session.main.score;
        }
    }
}

