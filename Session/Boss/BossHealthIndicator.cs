using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
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
        /// <summary>
        /// A node containing a list of subticks as children. 
        /// These indicate the phases of the health bar, 
        /// rotated clockwise in the direction of progress.
        /// There can be at most the number of ticks provided within the list
        /// (any further ticks suggested by the StageSectors are omitted).
        /// </summary>
        [Export] public Node subticks;
        /// <summary>
        /// Denotes the remaning fraction of health when the bar reaches the tick position.
        /// If negative, this tick is invisible.
        /// </summary>
        private float[] subtickPositions = null;
        private Control[] subtickList = null;
        private int subtickUpdateFrames = 0;

        private const char emptyLetter = '0';
        private const char unknownLetter = 'a';
        private const string lifeLetters = "bcde";
        private const string bombLetters = "BCDE";
        private const string timeoutLetters = "1234";
        private const string lifeBombLetters = "ghi";

        private BossEnemy bossNode = null;
        public string fullString { get; private set; } = "";
        public System.Collections.Generic.Dictionary<StageSector, int> sectorToGroupIndex = new System.Collections.Generic.Dictionary<StageSector, int>();
        public System.Collections.Generic.Dictionary<int, List<StageSector>> groupIndexToSectors = new System.Collections.Generic.Dictionary<int, List<StageSector>>();
        public string currentLetter { get; private set; } = "";
        private int currentGroupIndex = 0;
        public enum FillMode { None, Life, Bomb, LifeWithBombBehind }
        public FillMode fillMode { get; private set; } = FillMode.Life;
        public float fillValue { get; private set; } = 0;

        private bool HasLeafRole(StageSector s)
        {
            return s.role == StageSector.Role.BossLife
                || s.role == StageSector.Role.BossBomb
                || s.role == StageSector.Role.BossTimeout;
        }

        private void TabulateSingle(StageSector s)
        {
            if (!HasLeafRole(s)) { return; }
            if (!groupIndexToSectors.ContainsKey(currentGroupIndex))
            {
                groupIndexToSectors[currentGroupIndex] = new List<StageSector>();
            }
            groupIndexToSectors[currentGroupIndex].Add(s);
            sectorToGroupIndex[s] = currentGroupIndex;
        }

        private void TabulateAttackGroup(StageSector container)
        {
            foreach (Node c in container.GetDescendants())
            {
                if (c is StageSector) { TabulateSingle((StageSector)c); }
            }
        }

        private bool DoesTokenCount(StageSector s)
        {
            return !s.ragePhase;
        }

        private string PopulateSingle(StageSector s, bool tabulate = true)
        {
            if (!DoesTokenCount(s)) { return ""; }
            if (tabulate)
            {
                TabulateSingle(s);
                currentGroupIndex++;
            }
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
            if (!DoesTokenCount(s)) { return ""; }
            (int lifeCount, int bombCount, int timeoutCount) = (0, 0, 0);
            int total = 0;
            string firstLetter = "";
            foreach (Node c in s.GetDescendants())
            {
                if (c is not StageSector) { continue; }
                StageSector cs = (StageSector)c;
                if (!DoesTokenCount(s)) { continue; }
                string newSingle = PopulateSingle(cs, false);
                if (newSingle == unknownLetter.ToString()) { continue; }
                if (total == 0) { firstLetter = newSingle; }
                if (newSingle == lifeLetters[0].ToString()) { lifeCount++; }
                else if (newSingle == bombLetters[0].ToString()) { bombCount++; }
                else if (newSingle == timeoutLetters[0].ToString()) { timeoutCount++; }
                total++;
            }
            if (lifeCount > 0 && bombCount + timeoutCount == 0) // Multi-phase life bar
            {
                int lifeIndex = Mathf.Min(lifeCount - 1, lifeLetters.Length - 1);
                TabulateAttackGroup(s);
                currentGroupIndex++;
                return lifeLetters[lifeIndex].ToString();
            }
            else if (bombCount > 0 && lifeCount + timeoutCount == 0) // Multi-phase bomb bar
            {
                int bombIndex = Mathf.Min(bombCount - 1, bombLetters.Length - 1);
                TabulateAttackGroup(s);
                currentGroupIndex++;
                return bombLetters[bombIndex].ToString();
            }
            else if (lifeCount == 1 && bombCount > 0 
                && timeoutCount == 0 
                && firstLetter == lifeLetters[0].ToString()) // Life bar with bomb bar (bomb bar possibly multi-phase)
            {
                int lbIndex = Mathf.Min(bombCount - 1, lifeBombLetters.Length - 1);
                TabulateAttackGroup(s);
                currentGroupIndex++;
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

            if (bossSector == null || bossSector.role != StageSector.Role.Boss)
            {
                GD.PushWarning("Attempted to populate tokens from StageSector that isn't Boss");
                return;
            }

            currentGroupIndex = 0;
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
            currentGroupIndex = 0;
        }

        public void UpdateName(string newName = null)
        {
            bossNameLabel.Text = newName.Reverse(bossName);
        }

        public void InitializeFill()
        {
            lifeBar.Value = 0;
            bombBar.Value = 0;
        }

        public void UpdateFill()
        {
            if (bossNode == null) { return; }
            float healthFrac = ((IVariableContainer)(Enemy)bossNode).GetSpecial("health_frac").AsSingle();

            if (bossNode.refilling)
            {
                // In this state we should only appear to gain health.
                // (particularly FillMode.LifeWithBombBehind must maintain the full bomb bar)
                fillValue = Mathf.Lerp(fillValue, Mathf.Max(healthFrac, fillValue), 0.2f);
            }
            else
            {
                fillValue = Mathf.Lerp(fillValue, healthFrac, 0.2f);
            }
            
            switch (fillMode)
            {
                case FillMode.None:
                    bombBar.Value = Mathf.Lerp(bombBar.Value, 0, 0.2f);
                    lifeBar.Value = Mathf.Lerp(lifeBar.Value, 0, 0.2f);
                    break;
                case FillMode.Bomb:
                    lifeBar.Value = Mathf.Lerp(lifeBar.Value, 0, 0.2f);
                    bombBar.Value = fillValue;
                    break;
                case FillMode.Life:
                    lifeBar.Value = fillValue;
                    bombBar.Value = Mathf.Lerp(bombBar.Value, 0, 0.2f);
                    break;
                case FillMode.LifeWithBombBehind:
                    lifeBar.Value = fillValue;
                    bombBar.Value = Mathf.Lerp(bombBar.Value, 1, 0.2f);
                    break;
            }
        }

        public void UpdateTokens()
        {
            if (currentGroupIndex >= fullString.Length - 1)
            {
                tokenLabel.Text = emptyLetter.ToString();
            }
            else
            {
                tokenLabel.Text = fullString.Substring(currentGroupIndex + 1).Reverse(emptyLetter.ToString());
            }
        }

        public void UpdateFillMode()
        {
            if (bossNode == null || bossNode.currentSector == null) { return; }
            if (currentGroupIndex < 0 || currentGroupIndex > fullString.Length) { return; }
            char currentLetter = fullString[currentGroupIndex];
            if (lifeBombLetters.Contains(currentLetter))
            {
                FillMode oldFillMode = fillMode;
                if (bossNode.currentSector.role == StageSector.Role.BossLife) { fillMode = FillMode.LifeWithBombBehind; }
                else 
                { 
                    fillMode = FillMode.Bomb;
                    if (oldFillMode == FillMode.LifeWithBombBehind)
                    {
                        fillValue = 1f;
                    }
                }
            }
            else if (lifeLetters.Contains(currentLetter)) { fillMode = FillMode.Life; }
            else if (bombLetters.Contains(currentLetter)) { fillMode = FillMode.Bomb; }
            else if (timeoutLetters.Contains(currentLetter)) { fillMode = FillMode.None; }
            else { fillMode = FillMode.Life; }
        }

        private void InitializeSubticks()
        {
            subtickList = new Control[subticks.GetChildCount()];
            subtickPositions = new float[subtickList.Length];
            for (int i = 0; i < subtickList.Length; ++i)
            {
                subtickPositions[i] = -1;
                Node child = subticks.GetChild(i);
                if (child is Control) 
                { 
                    subtickList[i] = (Control)child;
                    subtickList[i].Visible = false;
                    subtickList[i].RotationDegrees = 0;
                }
                else { GD.PushWarning("Subtick should inherit Control class."); }
            }
        }

        private void RecalculateSubticks()
        {
            if (bossNode == null || bossNode.currentSector == null) { return; }
            // In which we search for the next sectors related to the next ticks.
            // How? By finding the current sector and looking ahead for decreasing boss health cutoffs.
            List<StageSector> nextTickSectors = new List<StageSector>();
            if (!groupIndexToSectors.ContainsKey(currentGroupIndex)) { return; }
            bool foundCurrent = false;
            float currentLowCutoff = 1f;
            foreach (StageSector s in groupIndexToSectors[currentGroupIndex])
            {
                if (s == bossNode.currentSector) 
                { 
                    foundCurrent = true;
                    currentLowCutoff = s.bossHealthCutoff;
                    nextTickSectors.Add(s);
                }
                else if (foundCurrent)
                {
                    if (s.bossHealthCutoff < currentLowCutoff)
                    {
                        currentLowCutoff = s.bossHealthCutoff;
                        nextTickSectors.Add(s);
                    }
                    else if (s.bossHealthCutoff > currentLowCutoff) { break; }
                }
            }
            // We'll be populating the ticks in reverse for animation reasons.
            // Unused ticks will be invisible and unmoved.
            for (int i = 0; i < nextTickSectors.Count; ++i)
            {
                StageSector s = nextTickSectors[nextTickSectors.Count - 1 - i];
                if (s.bossHealthCutoff > 0 && s.bossHealthCutoff < 1)
                {
                    subtickList[i].Visible = true;
                    subtickPositions[i] = s.bossHealthCutoff;
                }
                else
                {
                    subtickList[i].Visible = false;
                    subtickPositions[i] = -1;
                    subtickList[i].RotationDegrees = 0;
                }
            }
            for (int i = nextTickSectors.Count; i < subtickList.Length; ++i)
            {
                subtickList[i].Visible = false;
                subtickPositions[i] = -1;
                subtickList[i].RotationDegrees = 0;
            }
            subtickUpdateFrames = 30;
        }

        private void UpdateSubticks()
        {
            if (subtickUpdateFrames <= 0) { return; }
            for (int i = 0; i < subtickList.Length; ++i)
            {
                float targetRotation = (subtickPositions[i] < 0) ? 0 : (360f - 360f * subtickPositions[i]);
                if (subtickList[i] != null)
                {
                    subtickList[i].RotationDegrees = Mathf.Lerp(subtickList[i].RotationDegrees, targetRotation, 0.2f);
                }
            } 
            --subtickUpdateFrames;
            if (subtickUpdateFrames == 0)
            {
                for (int i = 0; i < subtickList.Length; ++i)
                {
                    float targetRotation = (subtickPositions[i] < 0) ? 0 : (360f - 360f * subtickPositions[i]);
                    if (subtickList[i] != null)
                    {
                        subtickList[i].RotationDegrees = targetRotation;
                    }
                }
            }
        }

        public void OnBossRefillStart(StageSector sector)
        {
            currentGroupIndex = fullString.Length - 1;
            if (sectorToGroupIndex.ContainsKey(sector)) {
                currentGroupIndex = sectorToGroupIndex[sector];
            }
            UpdateTokens();
            UpdateFillMode();
            RecalculateSubticks();
        }

        public override void _Ready()
        {
            base._Ready();
            Node parent = GetParent();
            if (parent == null || parent is not BossEnemy)
            {
                GD.PushError("The parent of BossHealthIndicator isn't a BossEnemy.");
                return;
            }
            bossNode = (BossEnemy)parent;
            bossNode.Connect(
                BossEnemy.SignalName.OnRefillStart, 
                new Callable(this, MethodName.OnBossRefillStart)
            );
            PopulateSequenceTokens(bossNode.bossSector);
            UpdateName();
            InitializeFill();
            UpdateTokens();
            UpdateFillMode();
            UpdateFill();
            InitializeSubticks();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (Session.main?.paused ?? true) { return; }
            UpdateFill();
            UpdateSubticks();
        }
    }
}

