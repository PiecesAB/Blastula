using Blastula.Coroutine;
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
    /// Spawn enemies using a schedule.
    /// </summary>
    /// <remarks>
    /// This is counted by an EnemyFormation, and can block it from being deleted.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/enemySpawner.png")]
    public partial class Spawner : Node2D, IVariableContainer
    {
        [Export] public BaseSchedule spawnSchedule;
        /// <summary>
        /// A list of enemies to draw from. This way, we can spawn different kinds of enemies in a series.
        /// </summary>
        /// <remarks>
        /// It doesn't necessary need to spawn enemies. In fact, you can spawn effects or other spawners. Why not?
        /// </remarks>
        [Export] public PackedScene[] enemySamples;
        /// <summary>
        /// The way in which enemies are selected for the series.
        /// </summary>
        [Export] public Repaint.PatternMode repeatMode = Repaint.PatternMode.Loop;
        /// <summary>
        /// The spawnID starts at this number.
        /// A "spawn_id" variable is set in enemies to be the count at the time it was created
        /// (starting with this one -- by default the first enemy spawn has spawn_id == 0)
        /// </summary>
        [Export] public int spawnID = 0;
        /// <summary>
        /// The duration for which this spawner exists.
        /// </summary>
        [Export] public float selfLifespan = 5;
        [Export] public Wait.TimeUnits selfLifespanUnits = Wait.TimeUnits.Seconds;

        private int spawnCount = 0;

        /// <summary>
        /// Implemented for IVariableContainer; holds local variables.
        /// </summary>
        public Dictionary<string, Variant> customData { get; set; } = new Dictionary<string, Variant>();
        /// <summary>
        /// Implemented for IVariableContainer; holds special variable names.
        /// </summary>
        public HashSet<string> specialNames { get; set; } = new HashSet<string>()
        {
             "pos", "spawn_id", "spawn_count"
        };

        private EnemyFormation formation;

        /// <summary>
        /// Implemented for IVariableContainer; solves special variable names.
        /// </summary>
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

        /// <summary>
        /// Enemies will spawn at this spawner's position.
        /// </summary>
        public void Spawn()
        {
            //Stopwatch s = Stopwatch.StartNew();
            int selectIndex = Repaint.SolvePatternIndex(spawnID, enemySamples.Length, repeatMode);
            if (selectIndex < 0) { return; }
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
                this.StartCoroutine(spawnSchedule.Execute(this));
            }
            this.StartCoroutine(
                CoroutineUtility.DelayedQueueFree(this, selfLifespan, selfLifespanUnits)
            );
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

