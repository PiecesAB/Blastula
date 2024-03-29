using Blastula.Schedules;
using Godot;
using System;

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

        public static StageManager main { get; private set; } = null;

        public override void _Ready()
        {
            base._Ready();
            main = this;
            // Spawn test scene for now
            RNG.Reseed(0);
            GD.Seed(0);
            StageSector s = (StageSector)GetChild(0);
            s.Preload();
            _ = s.Execute();
        }
    }
}
