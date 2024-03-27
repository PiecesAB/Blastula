using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.Operations;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blastula
{
    /// <summary>
    /// This is the most important non-bullet element in the framework.
    /// It initiates a schedule to fire bullet structures, and handles their execution every frame.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/blastodisc.png")]
    public unsafe partial class Blastodisc : Node2D, IVariableContainer
    {
        /// <summary>
        /// Used in deleteAction field to determine what happens when this Blastodisc is deleted.
        /// </summary>
        /// <remarks>
        /// We can't take no action, because Blastodiscs have to manage bullet structure deletion.
        /// If we abandon the bullet structures, they would remain in the master queue forever,
        /// and we would soon run out of bullets to fire.
        /// </remarks>
        public enum DeleteAction
        {
            /// <summary>
            /// The primordial Blastodisc inherits the bullet structures of this Blastodisc,
            /// so that they may continue executing every frame.
            /// </summary>
            BulletsRemain,
            /// <summary>
            /// The bullet structures of this Blastodisc are cleared.
            /// </summary>
            ClearMyBullets,
            /// <summary>
            /// The bullet structures of all Blastodiscs are cleared.
            /// </summary>
            /// <remarks>
            /// This includes collectibles and player shots, which is strange.
            /// More granular deletion will be implemented soon.
            /// </remarks>
            ClearAllBullets
        }

        [Export] public bool enabled = true;
        /// <summary>
        /// This schedule begins once the Blastodisc is enabled.
        /// </summary>
        [Export] public BaseSchedule mainSchedule;
        /// <summary>
        /// If false, bullet structures will not be executed every frame.
        /// </summary>
        [Export] public bool bulletsExecutable = true;
        /// <summary>
        /// This schedule runs when the Blastodisc is about to be deleted. 
        /// </summary>
        /// <remarks>
        /// Be careful if the schedule is a child of this Blastodisc and waits, even one frame.
        /// A deleted schedule won't do anything.
        /// </remarks>
        [ExportGroup("Advanced")]
        [Export] public BaseSchedule cleanupSchedule;
        /// <summary>
        /// For more info, see comments on the enum DeleteAction.
        /// </summary>
        [Export] public DeleteAction deleteAction = DeleteAction.BulletsRemain;
        /// <summary>
        /// Multiplier for the time scale at which bullets are executed.
        /// </summary>
        /// <example>
        /// Try animating this between 0 and 1 to produce a Freeze Sign "Perfect Freeze" effect.
        /// </example>
        /// <remarks>
        /// Setting speed multiplier to 0 is not necessarily the same as stopping execution.
        /// There may be behaviors which occur regardless of time scale, though no examples exist yet.
        /// </remarks>
        [Export] public float speedMultiplier = 1f;

        /// <summary>
        /// Implemented for IVariableContainer; holds local variables.
        /// </summary>
        public Dictionary<string, Variant> customData { get; set; } = new Dictionary<string, Variant>();

        private int shotsFired = 0;
        private double timeWhenEnabled = 0;

        /// <summary>
        /// Children are all structures currently living and shot by this.
        /// </summary>
        private int masterStructure = -1;
        private int masterNextChildIndex = 0;
        private int masterLowerChildSearch = 0;

        private long myCurrentFrame = 0;
        private float[] renderBuffer = new float[0];

        /// <summary>
        /// How many bullets to wait until a Blastodisc's master node is refreshed?
        /// Too long = the queue's tail can get long, taking up lots of space.
        /// Too short = the queue's head can be far in the front, taking up lots of space.
        /// </summary>
        private int refreshMasterNodeCount = 500;
        private int lastRefreshHead = 0;
        private bool scheduleBegan = false;

        /// <summary>
        /// References all Blastodiscs that currently exist in any scene tree.
        /// </summary>
        public static HashSet<Blastodisc> all = new HashSet<Blastodisc>();
        /// <summary>
        /// This must be the first blastodisc that exists, and is within the kernel.
        /// It exists so that when a blastodisc is deleted, there is still a way to handle its bullets.
        /// </summary>
        public static Blastodisc primordial = null;

        public override void _Ready()
        {
            all.Add(this);
            if (primordial == null) { primordial = this; }
            myCurrentFrame = 0;
        }

        /// <summary>
        /// Tries to make a structure from nothing.
        /// </summary>
        private int MakeStructure(BaseOperation operation)
        {
            return operation.ProcessStructure(-1);
        }

        /// <summary>
        /// Implemented for IVariableContainer; holds special variable names.
        /// </summary>
        public HashSet<string> specialNames { get; set; } = new HashSet<string>()
        {
            "t", "shot_count"
        };

        /// <summary>
        /// Implemented for IVariableContainer; solves special variable names.
        /// </summary>
        public Variant GetSpecial(string varName)
        {
            switch (varName)
            {
                case "t":
                    return myCurrentFrame / (float)Persistent.SIMULATED_FPS;
                case "shot_count":
                    return shotsFired;
            }
            return default;
        }

        /// <summary>
        /// Using an operation, create a bullet structure, and inherit it.
        /// </summary>
        /// <param name="operation">Usually a Sequence.</param>
        /// <remarks>
        /// You don't actually need to make a bullet structure here.
        /// There are some operations which can exist independently of any bullet structure, such as playing a sound.
        /// When that happens, we correctly "shoot" it and not inherit anything.
        /// </remarks>
        public void Shoot(BaseOperation operation)
        {
            if (operation == null) { return; }
            //Stopwatch s = Stopwatch.StartNew();
            ExpressionSolver.currentLocalContainer = this;
            int newStructure = MakeStructure(operation);
            if (newStructure < 0) { return; }
            BNodeFunctions.masterQueue[newStructure].transform = GlobalTransform * BNodeFunctions.masterQueue[newStructure].transform;
            bool inheritSuccess = Inherit(newStructure);
            if (!inheritSuccess) { BNodeFunctions.MasterQueuePushTree(newStructure); return; }
            shotsFired++;
            //s.Stop();
            //GD.Print($"Creating that shot took {s.Elapsed.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Makes the bullet structure a child of the Blastodisc's master structure, inheriting it.
        /// </summary>
        /// <remarks>
        /// Inheriting is an important operation to avoid abandoning bullet structures; they must be tracked
        /// to avoid strange problems.
        /// <br /><br />
        /// The "master structure" of a Blastodisc is its way of tracking and inheriting bullet structures,
        /// using an overarching bullet structure.
        /// </remarks>
		public bool Inherit(int bNodeIndex)
        {
            if (masterStructure < 0)
            {
                masterStructure = BNodeFunctions.MasterQueuePopOne();
                //GD.Print($"{Name} created master structure; now at index {masterStructure}");
                if (masterStructure < 0) { return false; }
            }
            int ci = masterNextChildIndex;
            UnsafeArray<int> msc = BNodeFunctions.masterQueue[masterStructure].children;
            BNodeFunctions.SetChild(masterStructure, ci, bNodeIndex);
            masterNextChildIndex++;
            while (masterNextChildIndex < msc.count && msc[masterNextChildIndex] != -1)
            {
                masterNextChildIndex++;
            }
            return true;
        }

        /// <summary>
        /// Deletes all bullets associated with this Blastodisc.
        /// </summary>
        public void ClearBullets(bool deletionEffect = true)
        {
            if (masterStructure >= 0)
            {
                if (deletionEffect)
                {
                    BulletRenderer.ConvertToDeletionEffects(masterStructure);
                    primordial.Inherit(masterStructure);
                }
                else
                {
                    BNodeFunctions.MasterQueuePushTree(masterStructure);
                }
                masterStructure = -1;
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (cleanupSchedule != null) { cleanupSchedule.Execute(this); }
            switch (deleteAction)
            {
                case DeleteAction.BulletsRemain:
                default:
                    if (masterStructure < 0) { break; }
                    if (primordial == null || primordial == this) { ClearBullets(); break; }
                    UnsafeArray<int> msc = BNodeFunctions.masterQueue[masterStructure].children;
                    for (int i = 0; i < msc.count; ++i)
                    {
                        if (msc[i] < 0) { continue; }
                        int childIndex = msc[i];
                        BNodeFunctions.SetChild(masterStructure, i, -1);
                        primordial.Inherit(childIndex);
                    }
                    BNodeFunctions.MasterQueuePushTree(masterStructure);
                    break;
                case DeleteAction.ClearMyBullets:
                    ClearBullets();
                    break;
                case DeleteAction.ClearAllBullets:
                    ClearBulletsForAll();
                    break;
            }
            all.Remove(this);
        }

        private void UpdateMasterStructure()
        {
            if (masterStructure >= 0)
            {
                // If the queue is far enough ahead of the master BNode, move the master BNode.
                if ((BNodeFunctions.mqHead - lastRefreshHead) >= refreshMasterNodeCount
                    || (BNodeFunctions.mqHead - lastRefreshHead) < 0)
                {
                    lastRefreshHead = BNodeFunctions.mqHead;
                    RefreshMasterStructure();
                }

                // We look through up to 100 children of the master BNode every frame to search for holes.
                // If a hole is found, we plan that the next added child would fill it.
                UnsafeArray<int> msc = BNodeFunctions.masterQueue[masterStructure].children;
                for (int k = masterLowerChildSearch;
                    k < masterLowerChildSearch + 100 && k < masterNextChildIndex && k < msc.count;
                    ++k)
                {
                    if (msc[k] < 0)
                    {
                        masterNextChildIndex = k; break;
                    }
                }
                masterLowerChildSearch += 100;
                if (masterLowerChildSearch >= masterNextChildIndex) { masterLowerChildSearch = 0; }
            }
        }

        // Keeps the tail of the masterQueue from staying put
        private void RefreshMasterStructure()
        {
            if (masterStructure < 0) { return; }
            int newMS = BNodeFunctions.MasterQueuePopOne();
            //GD.Print($"{Name} refreshed master structure; now at index {newMS}");
            if (newMS < 0) { return; }
            else
            {
                BNodeFunctions.masterQueue[newMS].treeSize = BNodeFunctions.masterQueue[masterStructure].treeSize;
                BNodeFunctions.masterQueue[newMS].treeDepth = BNodeFunctions.masterQueue[masterStructure].treeDepth;
                BNodeFunctions.masterQueue[newMS].children = BNodeFunctions.masterQueue[masterStructure].children.Clone();
                for (int j = 0; j < BNodeFunctions.masterQueue[newMS].children.count; ++j)
                {
                    int childIndex = BNodeFunctions.masterQueue[newMS].children[j];
                    BNodeFunctions.masterQueue[childIndex].parentIndex = newMS;
                }
                BNodeFunctions.MasterQueuePushIndependent(masterStructure);
                masterStructure = newMS;
            }
        }

        public override void _Process(double delta)
        {
            if (Session.IsPaused() || Debug.GameFlow.frozen) { return; }
            base._Process(delta);

            myCurrentFrame++;
            UpdateMasterStructure();

            if (!scheduleBegan && enabled && mainSchedule != null)
            {
                scheduleBegan = true;
                mainSchedule.Execute(this);
            }
        }

        /// <summary>
        /// Deletes all bullets associated with all Blastodiscs, which should be every bullet in the game.
        /// </summary>
        /// <remarks>
        /// This includes collectibles and player shots, which is strange.
        /// More granular deletion will be implemented soon.
        /// </remarks>
        public static void ClearBulletsForAll()
        {
            foreach (Blastodisc bd in all)
            {
                bd.ClearBullets();
            }
        }

        /// <summary>
        /// Used mainly internally for executing the master structure of all Blastodiscs.
        /// Because execution always cascades to children, this executes all true bullet structures.
        /// </summary>
        /// <remarks>
        /// The "master structure" of a Blastodisc is its way of tracking and inheriting bullet structures,
        /// using an overarching bullet structure.
        /// </remarks>
        public static void ExecuteAll()
        {
            if (Session.IsPaused() || Debug.GameFlow.frozen) { return; }
            foreach (Blastodisc bd in all)
            {
                if (!bd.bulletsExecutable) { continue; }
                if (bd.masterStructure < 0) { continue; }
                BNodeFunctions.Execute(
                    bd.masterStructure,
                    (float)Engine.TimeScale * bd.speedMultiplier
                );
            }
        }
    }
}
