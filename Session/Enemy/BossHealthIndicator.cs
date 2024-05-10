using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.ComponentModel.Design;
using System.Text;

namespace Blastula
{
    /// <summary>
    /// The parent should be a BossEnemy. 
    /// </summary>
    public partial class BossHealthIndicator : Node
    {
        /// <summary>
        /// The boss name, as it is displayed.
        /// </summary>
        [Export] public string bossName = "Insert boss name here";
        /// <summary>
        /// The label that displays the boss name. 
        /// Due to an idiosyncratic shader mechanism, the boss name given in this script is reversed.
        /// </summary>
        [Export] public Label bossNameLabel;
        /// <summary>
        /// Used to show tokens that hint towards future attacks.
        /// </summary>
        [Export] public Label tokenLabel;
        [Export] public TextureProgressBar lifeBar;
        [Export] public TextureProgressBar bombBar;

        private const char emptyLetter = '0';
        private const char unknownLetter = 'a';
        private const string lifeLetters = "bcde";
        private const string bombLetters = "BCDE";
        private const string timeoutLetters = "1234";
        private const string lifeBombLetters = "ghi";

        private BossEnemy bossNode = null;
        public string fullString { get; private set; } = ""; 

        private string PopulateSingle(StageSector s)
        {
            switch (s.role)
            {
                case StageSector.Role.BossLife: return lifeLetters[0].ToString();
                case StageSector.Role.BossBomb: return bombLetters[0].ToString();
                case StageSector.Role.BossTimeout: return timeoutLetters[0].ToString();
            }
            return unknownLetter.ToString();
        }

        private string PopulateAttackGroup(StageSector s)
        {
            (int lifeCount, int bombCount, int timeoutCount, int unknownCount) = (0, 0, 0, 0);
            int total = 0;
            string firstLetter = "";
            foreach (Node c in s.GetChildren())
            {
                if (c is not StageSector) { continue; }
                StageSector cs = (StageSector)c;
                string newSingle = PopulateSingle(cs);
                if (total == 0) { firstLetter = newSingle; }
                if (newSingle == lifeLetters[0].ToString()) { lifeCount++; }
                else if (newSingle == bombLetters[0].ToString()) { bombCount++; }
                else if (newSingle == timeoutLetters[0].ToString()) { timeoutCount++; }
                else { unknownCount++; }
                total++;
            }
            if (lifeCount > 0 && bombCount + timeoutCount + unknownCount == 0) // Multi-phase life bar
            {
                int lifeIndex = Mathf.Min(lifeCount - 1, lifeLetters.Length - 1);
                return lifeLetters[lifeIndex].ToString();
            }
            else if (bombCount > 0 && lifeCount + timeoutCount + unknownCount == 0) // Multi-phase bomb bar
            {
                int bombIndex = Mathf.Min(bombCount - 1, bombLetters.Length - 1);
                return bombLetters[bombIndex].ToString();
            }
            else if (lifeCount == 1 && bombCount > 0 
                && timeoutCount + unknownCount == 0 
                && firstLetter == lifeLetters[0].ToString()) // Life bar with bomb bar (bomb bar possibly multi-phase)
            {
                int lbIndex = Mathf.Min(bombCount - 1, lifeBombLetters.Length - 1);
                return lifeBombLetters[lbIndex].ToString();
            }
            // Give up and return the separate bars...
            string subString = "";
            foreach (Node c in s.GetChildren())
            {
                if (c is not StageSector) { continue; }
                if (fullString == emptyLetter.ToString()) { fullString = ""; }
                StageSector cs = (StageSector)c;
                subString += PopulateSingle(cs);
            }
            return subString;
        }

        public void PopulateSequenceTokens(StageSector bossSector)
        {
            fullString = emptyLetter.ToString();

            if (bossSector.role != StageSector.Role.Boss)
            {
                GD.PushWarning("Attempted to populate tokens from StageSector that isn't Boss");
                return;
            }

            foreach (Node c in bossSector.GetChildren())
            {
                if (c is not StageSector) { continue; }
                if (fullString == emptyLetter.ToString()) { fullString = ""; }
                StageSector s = (StageSector)c;
                if (s.role == StageSector.Role.BossAttackGroup)
                {
                    fullString += PopulateAttackGroup(s);
                }
                else
                {
                    fullString += PopulateSingle(s);
                }
            }

            tokenLabel.Text = fullString;
        }

        public void UpdateName(string newName = null)
        {
            StringBuilder reversedName = new StringBuilder();
            if (newName == null) { newName = bossName; }
            bossName = newName;
            for (int i = 0; i < newName.Length; ++i)
            {
                reversedName.Append(newName[newName.Length - 1 - i]);
            }
            bossNameLabel.Text = reversedName.ToString();
        }

        public void InitializeFill()
        {
            lifeBar.Value = 0;
        }

        public void UpdateFill()
        {
            if (bossNode != null)
            {
                float newVal = ((IVariableContainer)(Enemy)bossNode).GetSpecial("health_frac").AsSingle();
                lifeBar.Value = Mathf.Lerp(lifeBar.Value, newVal, 0.2f);
            }
        }

        public override void _Ready()
        {
            base._Ready();
            Node parent = GetParent();
            if (parent is BossEnemy) { bossNode = (BossEnemy)parent; }
            PopulateSequenceTokens((StageSector)StageManager.main.FindChild("TestBoss"));
            UpdateName();
            InitializeFill();
            UpdateFill();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            UpdateFill();
        }
    }
}

