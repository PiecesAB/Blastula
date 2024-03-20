using Blastula.Operations;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blastula
{
    /// <summary>
    /// Spawn enemies using... you guessed it... a schedule.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/enemySpawner.png")]
    public partial class Spawner : Node2D, IVariableContainer
    {
        [Export] public BaseSchedule spawnSchedule;
        [Export] public PackedScene[] enemySamples;
        [Export] public Repaint.PatternMode repeatMode = Repaint.PatternMode.Loop;
        /// <summary>
        /// The spawnID starts at this number.
        /// A "spawn_id" variable is set in enemies to be the count at the time it was created
        /// (starting with this one -- by default the first enemy spawn has spawn_id == 0)
        /// </summary>
        [Export] public int spawnID = 0;
        [Export] public float selfLifespan = 5;
        [Export] public Wait.TimeUnits selfLifespanUnits = Wait.TimeUnits.Seconds;

        private int spawnCount = 0;
        public Dictionary<string, Variant> customData { get; set; } = new Dictionary<string, Variant>();
        public HashSet<string> specialNames { get; set; } = new HashSet<string>()
        {
             "pos", "spawn_id", "spawn_count"
        };

        private EnemyFormation formation;

        public Variant GetSpecial(string varName)
        {
            switch (varName)
            {
                case "pos":
                    return GlobalPosition;
                case "spawn_id":
                    return spawnID;
                case "spawn_count":
                    return spawnCount;
            }
            return default;
        }

        private Node cachedParent = null;

        public void Spawn()
        {
            //Stopwatch s = Stopwatch.StartNew();
            int selectIndex = Repaint.SolvePatternIndex(spawnID, enemySamples.Length, repeatMode);
            if (selectIndex == -1) { return; }
            Node2D newNode = enemySamples[selectIndex].Instantiate<Node2D>();
            if (newNode == null) { return; }
            if (newNode is not Enemy) { newNode.QueueFree(); return; }
            Enemy newEnemy = (Enemy)newNode;
            if (cachedParent == null) { cachedParent = GetParent(); }
            if (cachedParent.IsNodeReady())
            {
                cachedParent.AddChild(newEnemy);
            }
            else
            {
                cachedParent.CallDeferred(Node.MethodName.AddChild, newEnemy);
            }
            ExpressionSolver.currentLocalContainer = this;
            newEnemy.GlobalPosition = GlobalPosition;
            ((IVariableContainer)newEnemy).SetVar("spawn_id", spawnID);
            ++spawnID;
            ++spawnCount;
            //s.Stop();
            //GD.Print("spawning ", s.Elapsed.TotalMilliseconds, " ms");
        }

        public override void _Ready()
        {
            base._Ready();
            formation = StageSector.GetCurrentEnemyFormation();
            if (formation != null)
            {
                formation.IncrementSpawner();
            }
            if (spawnSchedule != null)
            {
                spawnSchedule.Execute(this);
            }
            Waiters.DelayedQueueFree(this, selfLifespan, selfLifespanUnits);
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (formation != null)
            {
                formation.DecrementSpawner();
                formation = null;
            }
        }
    }
}

