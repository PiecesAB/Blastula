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

        private BigInteger displayedScore = 0;

        private string GetScoreString(BigInteger bigInteger)
        {
            string raw = bigInteger.ToString();
            if (raw.Length <= 10) { return raw; }
            else { return raw[0] + "." + raw.Substring(1, 5) + "e" + (raw.Length - 1).ToString(); }
        }

        private BigInteger GetIncrement(int length)
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
            _Process(0);
        }

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
            }
        }
    }
}

