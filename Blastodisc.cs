using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.Operations;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;

namespace Blastula
{
    /// <summary>
    /// Fires bullet structures!
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/blastodisc.png")]
    public unsafe partial class Blastodisc : Node2D, IVariableContainer
    {
        public enum DeleteAction
        {
            BulletsRemain,
            ClearMyBullets,
            ClearAllBullets
        }

        [Export] public bool enabled = true;
        [Export] public BaseSchedule mainSchedule;
        [Export] public bool bulletsExecutable = true;
        [ExportGroup("Advanced")]
        [Export] public BaseSchedule cleanupSchedule;
        [Export] public DeleteAction deleteAction = DeleteAction.BulletsRemain;
        [Export] public float speedMultiplier = 1f;

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

        public HashSet<string> specialNames { get; set; } = new HashSet<string>()
        {
            "t", "shot_count"
        };

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
        /// Makes the BNode a child of the Blastodisc's master structure.
        /// </summary>
		public bool Inherit(int bNodeIndex)
        {
            if (masterStructure == -1)
            {
                masterStructure = BNodeFunctions.MasterQueuePopOne();
                //GD.Print($"{Name} created master structure; now at index {masterStructure}");
                if (masterStructure == -1) { return false; }
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
                    if (msc[k] == -1)
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
            if (masterStructure == -1) { return; }
            int newMS = BNodeFunctions.MasterQueuePopOne();
            //GD.Print($"{Name} refreshed master structure; now at index {newMS}");
            if (newMS == -1) { return; }
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
                BNodeFunctions.MasterQueuePushOne(masterStructure);
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

        public static void ClearBulletsForAll()
        {
            foreach (Blastodisc bd in all)
            {
                bd.ClearBullets();
            }
        }

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
