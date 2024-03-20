using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System;

namespace Blastula
{
    /// <summary>
    /// This is meant to be attached to the root of a scene which as loaded for a StageSector.
    /// It can keep track of enemies and spawners, so that the sector ends when the last enemy is defeated.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/enemyFormation.png")]
    public partial class EnemyFormation : Node
    {
        public int enemyCount { get; private set; } = 0;
        public int spawnerCount { get; private set; } = 0;

        public StageSector myStageSector = null;

        public override void _Ready()
        {
            base._Ready();
            myStageSector = StageSector.GetCurrentSector();
        }

        public void CheckEmpty()
        {
            if (enemyCount <= 0 && spawnerCount <= 0)
            {
                myStageSector.EndImmediately();
            }
        }

        public void IncrementEnemy() { enemyCount++; }
        public void DecrementEnemy() {  enemyCount--; CheckEmpty(); }
        public void IncrementSpawner() { spawnerCount++; }
        public void DecrementSpawner() { spawnerCount--; CheckEmpty(); }
    }
}

