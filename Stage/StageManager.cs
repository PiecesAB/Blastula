using Blastula.Schedules;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula
{
    /// <summary>
    /// Handles actually starting stages in order. But it's incomplete. At the moment, it just spawns the first
    /// stage that is a child of this node.
    /// </summary>
	public partial class StageManager : Node
	{
        [Signal] public delegate void StageSectorChangedEventHandler(StageSector newSector);
        [Signal] public delegate void StageChangedEventHandler(StageSector newStage);

        /// <summary>
        /// The number of bullets grazed in this stage. Mainly for single-player use.
        /// </summary>
        public ulong grazeCount { get; private set; } = 0;
        /// <summary>
        /// Number of point items collected in this stage. Mainly for single-player use.
        /// </summary>
        public ulong pointItemCount { get; private set; } = 0;
        /// <summary>
        /// Number of point items collected throughout the session. Mainly for single-player use.
        /// </summary>
        public ulong powerItemCount { get; private set; } = 0;

        public static StageManager main { get; private set; } = null;

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

        /// <summary>
        /// Begins a single player game.
        /// </summary>
        public async Task InitializeSinglePlayerSession(string playerPath, string stageGroupName)
        {
            while (PlayerManager.main == null) { await this.WaitOneFrame(); }
            await PlayerManager.main.SpawnPlayer(playerPath);
            // Spawn test scene for now
            RNG.Reseed(0);
            GD.Seed(0);
            StageSector s = (StageSector)GetChild(0);
            s.Preload();
            _ = s.Execute();
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            _ = InitializeSinglePlayerSession("TestPlayer", "MainSequence");
            
        }
    }
}
