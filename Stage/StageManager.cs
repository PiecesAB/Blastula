using Blastula.Coroutine;
using Blastula.Graphics;
using Blastula.Menus;
using Blastula.Schedules;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blastula
{
    /// <summary>
    /// Handles actually starting stages in order. But it's incomplete. At the moment, it just spawns the first
    /// stage that is a child of this node.
    /// </summary>
	public partial class StageManager : Node, IPersistForReplay
	{
        [Signal] public delegate void StageSectorChangedEventHandler(StageSector newSector);
        [Signal] public delegate void StageChangedEventHandler(StageSector newStage);
        [Signal] public delegate void SessionBeginningEventHandler();
        [Signal] public delegate void SessionEndingEventHandler();

        /// <summary>
        /// The number of bullets grazed in this stage. Mainly for single-player use.
        /// </summary>
        public ulong grazeCount { get; private set; } = 0;
        /// <summary>
        /// Number of point items collected in this stage. Mainly for single-player use.
        /// </summary>
        public ulong pointItemCount { get; private set; } = 0;
        /// <summary>
        /// Number of point items collected in this stage. Mainly for single-player use.
        /// </summary>
        public ulong powerItemCount { get; private set; } = 0;
        /// <summary>
        /// Number of cancel items collected in this stage. Mainly for single-player use.
        /// </summary>
        public ulong cancelItemCount { get; private set; } = 0;

        private string currentPlayerPath = "";
        private string currentStageGroupName = "";

        public static StageManager main { get; private set; } = null;

        public void Reset()
        {
            grazeCount = 0;
            pointItemCount = 0;
            powerItemCount = 0;
            cancelItemCount = 0;
        }

        public void AddGraze(int amount)
        {
            grazeCount += (ulong)amount;
        }

        public void AddPointItem(int amount)
        {
            pointItemCount += (ulong)amount;
        }

        public void AddPowerItem(int amount)
        {
            pointItemCount += (ulong)amount;
        }

        public void AddCancelItem(int amount)
        {
            cancelItemCount += (ulong)amount;
        }

        public IEnumerator RetrySinglePlayerSession()
        {
            EndSinglePlayerSession();
            yield return new WaitOneFrame();
            yield return InitializeSinglePlayerSession(currentPlayerPath, currentStageGroupName);
        }

        public uint ReseedRNG(uint? givenSeed = null)
        {
            if (givenSeed == null)
            {
                uint chaosSeed = (uint)(DateTimeOffset.Now.ToUnixTimeMilliseconds() & ((1 << 30) - 1));
                RNG.Reseed(chaosSeed);
                GD.Seed(chaosSeed);
                return chaosSeed;
            }
            else
            {
                RNG.Reseed(givenSeed.Value);
                GD.Seed(givenSeed.Value);
                return givenSeed.Value;
            }
        }

        /// <summary>
        /// Begins a single player game.
        /// </summary>
        public IEnumerator InitializeSinglePlayerSession(string playerPath, string stageGroupName)
        {
            while (Session.main == null || PlayerManager.main == null) { yield return new WaitOneFrame(); }
            BNodeFunctions.ResetQueue();
            Session.main.Reset();
            currentPlayerPath = playerPath; 
            currentStageGroupName = stageGroupName;
            yield return PlayerManager.main.SpawnPlayer(playerPath);
            if (Session.main != null) { Session.main.StartInSession(); }
            if (FrameCounter.main != null) { FrameCounter.main.ResetSessionFrame(); }
            Session.main.SetRecordScore(ScoresLoader.main.GetRecordScore());
            SetScoreExtends.Reset();
            // The RNG is reseeded unreproducibly here;
            // but if a replay is begun, it will be reseeded consistently as it starts.
            ReseedRNG();
            StageSector s = (StageSector)FindChild(stageGroupName);
            s.Preload();
            CoroutineUtility.StartCoroutine(new CoroutineUtility.Coroutine
            {
                func = s.Execute(),
                boundNode = this,
                cancel = s.GetCancelMethod()
            });
            ReplayManager.main?.SetSessionStart();
            EmitSignal(SignalName.SessionBeginning);
        }

        public void EndSinglePlayerSession()
        {
            EmitSignal(SignalName.SessionEnding);
            CoroutineUtility.StopAll();
            if (ReplayManager.main?.playState == ReplayManager.PlayState.Playing) ReplayManager.main?.EndSinglePlayerReplaySection();
            ReplayManager.main?.SetSessionEnd();
            if (Session.main != null) { Session.main.EndInSession(); }
            this.StartCoroutine(BackgroundHolder.FadeAway(0));
            StageSector.DumpStack();
            foreach (var kvp in Player.playersByControl) { kvp.Value.QueueFree(); }
            Player.playersByControl.Clear();
            foreach (Blastodisc bd in Blastodisc.all) 
            {
                BNodeFunctions.MasterQueuePushTree(bd.masterStructure);
                bd.masterStructure = -1; 
            }
            BNodeFunctions.ResetQueue();
            MusicManager.Stop();
            LabelPool.StopAll();
            if (HistoryHandler.main != null) HistoryHandler.Save();
            MusicMenuOrchestrator.SaveAllEncounteredMusic();
        }

        public Godot.Collections.Dictionary<string, string> CreateReplaySnapshot()
        {
            return new Godot.Collections.Dictionary<string, string>
            {
                {PropertyName.grazeCount, grazeCount.ToString() },
                {PropertyName.pointItemCount, pointItemCount.ToString() },
                {PropertyName.powerItemCount, powerItemCount.ToString() },
                {PropertyName.cancelItemCount, cancelItemCount.ToString() },
            };
        }

        public void LoadReplaySnapshot(Godot.Collections.Dictionary<string, string> snapshot)
        {
            try
            {
                grazeCount = ulong.Parse(snapshot[PropertyName.grazeCount]);
                pointItemCount = ulong.Parse(snapshot[PropertyName.pointItemCount]);
                powerItemCount = ulong.Parse(snapshot[PropertyName.powerItemCount]);
                cancelItemCount = ulong.Parse(snapshot[PropertyName.cancelItemCount]);
            }
            catch
            {
                throw new System.Exception("Stage: Unable to load data from replay file.");
            }
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
        }
    }
}
