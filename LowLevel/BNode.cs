using Blastula.Collision;
using Blastula.Graphics;
using Blastula.LowLevel;
using Godot;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Blastula
{

    /// <summary>
    /// A BNode is a tree-based bullet structure, which can appear as a bullet itself, or be an invisible parent of bullets.
    /// </summary>
    /// <remarks>
    /// Widely in the documentation, the collection of a root BNode with its child bullets is usually called a bullet structure.
    /// </remarks>
    public unsafe struct BNode
    {
        /// <summary>
        /// Internal tracking mechanism to ensure a BNode spot in the master queue is free.
        /// </summary>
        public bool initialized;
        /// <summary>
        /// If true, variable transform is the world transform (for optimization purposes), 
        /// otherwise transform is the local transform relative to the BNode's parent.
        /// </summary>
        public bool worldTransformMode;

        /// <summary>
        /// This variable is used to keep track of a BNode's phase/state, which is incidental to shot patterns.
        /// </summary>
        /// <example>
        /// Collectible phase is tracked. 
        /// Collectibles are just "bullets" with complex behavior in three phases:<br />
        /// 0. Emerge from the point at which the enemy was destroyed.<br />
        /// 1. Move up and slowly change to move down by gravity.<br />
        /// 2. If the attractbox is hit, attract toward the player.<br />
        /// The player is interested in phase 1, so that its collision can activate phase 2.
        /// </example>
        public short phase;

        /// <summary>
        /// Normally, the local transform of the BNode.
        /// </summary>
        public Transform2D transform;
        /// <summary>
        /// Indices of child BNodes in the master queue.
        /// </summary>
        public UnsafeArray<int> children;
        /// <summary>
        /// List of behaviors for execution.
        /// </summary>
        public UnsafeArray<BehaviorOrder> behaviors;
        /// <summary>
        /// ID for bullet rendering. No graphic nor collision if renderID &lt; 0.
        /// </summary>
        public int bulletRenderID;
        /// <summary>
        /// ID for laser rendering. For any bullet that makes up the laser. No graphic nor collision if renderID &lt; 0.
        /// </summary>
        /// <remarks>
        /// It should be that when bulletRenderID &gt;= 0, the bulletRenderID takes precedence over laserRenderID for collision.
        /// </remarks>
        public int laserRenderID;
        /// <summary>
        /// treeSize = 1 + (treeSize of all children).
        /// </summary>
        public int treeSize;
        /// <summary>
        /// treeDepth = 0 with no children, otherwise 1 + max(treeDepth of all children).
        /// </summary>
        /// <remarks>
        /// We are lazy to set this. Removing children won't automatically decrease the depth, even though it accurately should.
        /// If a correct depth is needed, please recalculate it.
        /// </remarks>
        public int treeDepth;
        /// <summary>
        /// Index of the parent in the master queue.
        /// </summary>
        public int parentIndex;
        /// <summary>
        /// Position within the parent's child list.
        /// </summary>
        /// <remarks>
        /// Used so that when this BNode is deleted, a hole is made in the parent's child list.
        /// </remarks>
        public int positionInParent;

        /// <summary>
        /// Internal ID used for determining this bullet's collision role. For example enemy shot, player shot, collectible, etc.
        /// </summary>
        public int collisionLayer;
        /// <summary>
        /// Whether this bullet is sleeping / can sleep. Used to optimize collision.
        /// </summary>
        public SleepStatus collisionSleepStatus;

        /// <summary>
        /// How this variable is handled is largely up to player(s).
        /// It's used for the current time (in frames) the player(s) grazed this bullet.
        /// It can be set to a negative number for enemy bullets to disable grazing.
        /// </summary>
        public float graze;
        /// <summary>
        /// How this variable is handled is largely up to colliding objects.
        /// It's used for player shots to actually deal an amount of damage.
        /// </summary>
        public float power;
        /// <summary>
        /// How this variable is handled is largely up to colliding objects.
        /// It is used for handling piercing player shots, and making enemy bullets undeletable.
        /// </summary>
        public float health;
        
        /// <summary>
        /// This is where color or custom data are defined when needed.
        /// </summary>
        public BNodeMultimeshExtras* multimeshExtras;
    }

    public struct BNodeMultimeshExtras
    {
        public Color color;
        public Vector4 custom;
    }

    /// <summary>
    /// Functions that are related to BNode management or master queue management.
    /// </summary>
    public unsafe static class BNodeFunctions
    {
        /// <summary>
        /// The master queue of legends. It is used as a circular queue that holds all BNodes.
        /// At the game's start, it is initialized to have a capacity of mqSize.
        /// </summary>
        public static BNode* masterQueue = null;
        /// <summary>
        /// This should always be the earliest uninitialized BNode in the master queue.
        /// </summary>
        public static int mqHead = 0;
        /// <summary>
        /// This should always be the earliest initialized BNode in the master queue, or the head for an empty queue.
        /// </summary>
        public static int mqTail = 0;
        /// <summary>
        /// This should always be the total number of BNodes in use.
        /// </summary>
        public static int totalInitialized = 0;
        /// <remarks>
        /// We can only store one less than this, or else mqHead == mqTail when full, which is already used for emptiness.
        /// </remarks>
        public const int mqSize = 262144;
        /// <summary>
        /// If the bullet's tree is larger than this, we use multithreading for certain operations.
        /// </summary>
        /// <remarks>
        /// We don't always multithread because Parallel.For has scheduling overhead.
        /// </remarks>
        public const int multithreadCutoff = 256;

        public static BNode* starterBNode = null;

        public static int MasterQueueCount()
        {
            return (mqTail <= mqHead) ? (mqHead - mqTail) : (mqSize - (mqTail - mqHead));
        }

        public static int MasterQueueRemainingCapacity()
        {
            return mqSize - 1 - MasterQueueCount();
        }

        /// <summary>
        /// Allocates the next available BNode, if possible.
        /// </summary>
        /// <returns>The index to that BNode in masterQueue</returns>
        public static int MasterQueuePopOne()
        {
            if (MasterQueueRemainingCapacity() < 1) { return -1; }
            int ret = mqHead;
            BNode* newPtr = masterQueue + mqHead;
            Buffer.MemoryCopy(starterBNode, newPtr, sizeof(BNode), sizeof(BNode));
            mqHead = (mqHead + 1) % mqSize;
            totalInitialized += 1;
            return ret;
        }

        /// <summary>
        /// Allocates n available BNodes, if possible.
        /// </summary>
        /// <returns>The index to the first such BNode in masterQueue.</returns>
        public static int MasterQueuePopN(int n)
        {
            if (n <= 0) { return -1; }
            if (MasterQueueRemainingCapacity() < n) { return -1; }
            int ret = mqHead;
            for (int i = 0; i < n; ++i)
            {
                int j = (mqHead + i) % mqSize;
                BNode* newPtr = masterQueue + j;
                Buffer.MemoryCopy(starterBNode, newPtr, sizeof(BNode), sizeof(BNode));
            }
            mqHead = (mqHead + n) % mqSize;
            totalInitialized += n;
            return ret;
        }

        /// <summary>
        /// Advances the master queue tail until it lands on the head or reaches the earliest initialized BNode.
        /// </summary>
        private static void AdvanceMQTail()
        {
            while (MasterQueueCount() > 0 && !masterQueue[mqTail].initialized)
            {
                mqTail = (mqTail + 1) % mqSize;
            }
        }

        /// <summary>
        /// Retracts the master queue head until it lands on the tail or comes right after the latest initialized BNode.
        /// </summary>
        private static void RetractMQHead()
        {
            int hprev = (mqHead == 0) ? (mqSize - 1) : (mqHead - 1);
            while (MasterQueueCount() > 0 && !masterQueue[hprev].initialized)
            {
                mqHead = hprev;
                hprev = (mqHead == 0) ? (mqSize - 1) : (mqHead - 1);
            }
        }

        /// <summary>
        /// Frees (deletes) the BNode from the master queue.
        /// </summary>
        /// <returns>Whether the deletion was performed.</returns>
        /// <remarks>
        /// This is specifically for independent BNode deletion like Blastodisc master structures.
        /// We expect no parent to exist, because it will not know this BNode is missing.
        /// We also expect any childrens' parent index to be handled separately as well.
        /// (For example, Blastodiscs create a new master structure immediately to "move" it, and children are reparented there.)
        /// </remarks>
        public static bool MasterQueuePushIndependent(int i)
        {
            if (i < 0 || i >= mqSize) { return false; }
            if (!masterQueue[i].initialized) { return false; }
            if (masterQueue[i].multimeshExtras != null)
            {
                Marshal.FreeHGlobal((IntPtr)masterQueue[i].multimeshExtras);
                masterQueue[i].multimeshExtras = null;
            }
            BulletRenderer.SetRenderID(i, -1);
            LaserRenderer.RemoveLaserEntry(i);
            masterQueue[i].initialized = false;
            masterQueue[i].children.Dispose();
            masterQueue[i].behaviors.DisposeBehaviorOrder();
            totalInitialized -= 1;
            AdvanceMQTail(); RetractMQHead();
            return true;
        }

        private static bool MasterQueuePushTree(int i, int recursionDepth)
        {
            if (i < 0 || i >= mqSize) { return false; }
            if (!masterQueue[i].initialized) { return false; }
            if (recursionDepth == 0 && masterQueue[i].parentIndex >= 0 && masterQueue[i].parentIndex < mqSize)
            {
                int parent = masterQueue[i].parentIndex;
                SetChild(parent, masterQueue[i].positionInParent, -1);
            }
            for (int j = 0; j < masterQueue[i].children.count; ++j)
            {
                MasterQueuePushTree(masterQueue[i].children[j], recursionDepth + 1);
            }
            if (masterQueue[i].multimeshExtras != null)
            {
                Marshal.FreeHGlobal((IntPtr)masterQueue[i].multimeshExtras);
                masterQueue[i].multimeshExtras = null;
            }
            BulletRenderer.SetRenderID(i, -1);
            LaserRenderer.RemoveLaserEntry(i);
            masterQueue[i].initialized = false;
            masterQueue[i].children.Dispose();
            masterQueue[i].behaviors.DisposeBehaviorOrder();
            totalInitialized -= 1;
            if (recursionDepth == 0) { AdvanceMQTail(); RetractMQHead(); }
            return true;
        }

        /// <summary>
        /// Frees (deletes) the BNode from the master queue, and also frees all children.
        /// </summary>
        /// <returns>Whether the deletion was performed.</returns>
        public static bool MasterQueuePushTree(int i)
        {
            return MasterQueuePushTree(i, 0);
        }

        // We except this to take up tens of megabytes of memory
        public static void InitializeQueue()
        {
            if (masterQueue == null)
            {
                GD.Print($"master queue occupies {mqSize * sizeof(BNode)} bytes of memory.");
                masterQueue = (BNode*)Marshal.AllocHGlobal(mqSize * sizeof(BNode));
                Parallel.For(0, mqSize, (i) => { masterQueue[i].initialized = false; });
                starterBNode = (BNode*)Marshal.AllocHGlobal(sizeof(BNode));
                *starterBNode = new BNode
                {
                    initialized = true,
                    worldTransformMode = false,
                    phase = 0,
                    bulletRenderID = -1,
                    laserRenderID = -1,
                    treeSize = 1,
                    treeDepth = 0,
                    parentIndex = -1,
                    transform = Transform2D.Identity,
                    collisionLayer = 0,
                    collisionSleepStatus = new SleepStatus { canSleep = true },
                    health = 1,
                    power = 1,
                    graze = 0,
                    multimeshExtras = null
                };
            }
        }

        // A clean-up function has been deemed unnecessary.
        // It would have run at the end of the game, but memory is cleaned by the OS anyway.

        public static int CloneOne(int i)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            int newSpace = MasterQueuePopOne();
            if (newSpace < 0) { return -1; }
            Buffer.MemoryCopy(masterQueue + i, masterQueue + newSpace, sizeof(BNode), sizeof(BNode));
            masterQueue[newSpace].children = masterQueue[newSpace].children.Clone();
            int childCount = masterQueue[newSpace].children.count;
            if (childCount > 0)
            {
                for (int j = 0; j < childCount; ++j)
                {
                    int oldChildIndex = masterQueue[newSpace].children[j];
                    if (oldChildIndex < 0) { continue; }
                    int newChildIndex = masterQueue[newSpace].children[j] = CloneOne(oldChildIndex);
                    if (newChildIndex < 0) { continue; }
                    masterQueue[newChildIndex].parentIndex = newSpace;
                }
            }
            masterQueue[newSpace].behaviors = masterQueue[newSpace].behaviors.CloneBehaviorOrder();
            if (masterQueue[newSpace].bulletRenderID >= 0)
            {
                int newRenderID = masterQueue[newSpace].bulletRenderID;
                masterQueue[newSpace].bulletRenderID = -1;
                BulletRenderer.SetRenderID(newSpace, newRenderID);
            }
            if (masterQueue[newSpace].laserRenderID >= 0 && LaserRenderer.IsBNodeHeadOfLaser(i))
            {
                int newRenderID = masterQueue[newSpace].laserRenderID;
                masterQueue[newSpace].laserRenderID = -1;
                LaserRenderer.SetLaserFromHead(newSpace, newRenderID);
            }
            if (masterQueue[i].multimeshExtras != null)
            {
                masterQueue[newSpace].multimeshExtras = (BNodeMultimeshExtras*)Marshal.AllocHGlobal(sizeof(BNodeMultimeshExtras));
                masterQueue[newSpace].multimeshExtras->color = masterQueue[i].multimeshExtras->color;
                masterQueue[newSpace].multimeshExtras->custom = masterQueue[i].multimeshExtras->custom;
            }
            return newSpace;
        }

        public static int CloneN(int i, int n)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            if (n <= 0) { return -1; }
            int newSpace = MasterQueuePopN(n);
            if (newSpace < 0) { return -1; }
            for (int offset = 0; offset < n; ++offset)
            {
                int j = (newSpace + offset) % mqSize;
                Buffer.MemoryCopy(
                    masterQueue + i,
                    masterQueue + j, sizeof(BNode), sizeof(BNode)
                );
                masterQueue[j].children = masterQueue[j].children.Clone();
                int childCount = masterQueue[j].children.count;
                if (childCount > 0)
                {
                    for (int k = 0; k < childCount; ++k)
                    {
                        int oldChildIndex = masterQueue[j].children[k];
                        if (oldChildIndex < 0) { continue; }
                        int newChildIndex = masterQueue[j].children[k] = CloneOne(oldChildIndex);
                        if (newChildIndex < 0) { continue; }
                        masterQueue[newChildIndex].parentIndex = j;
                    }
                }
                masterQueue[j].behaviors = masterQueue[j].behaviors.CloneBehaviorOrder();
                if (masterQueue[j].bulletRenderID >= 0)
                {
                    int newRenderID = masterQueue[j].bulletRenderID;
                    masterQueue[j].bulletRenderID = -1;
                    BulletRenderer.SetRenderID(j, newRenderID);
                }
                if (masterQueue[j].laserRenderID >= 0 && LaserRenderer.IsBNodeHeadOfLaser(i))
                {
                    int newRenderID = masterQueue[j].laserRenderID;
                    masterQueue[j].laserRenderID = -1;
                    LaserRenderer.SetLaserFromHead(j, newRenderID);
                }
                if (masterQueue[i].multimeshExtras != null)
                {
                    masterQueue[j].multimeshExtras = (BNodeMultimeshExtras*)Marshal.AllocHGlobal(sizeof(BNodeMultimeshExtras));
                    masterQueue[j].multimeshExtras->color = masterQueue[i].multimeshExtras->color;
                    masterQueue[j].multimeshExtras->custom = masterQueue[i].multimeshExtras->custom;
                }
            }
            return newSpace;
        }

        public static void SetTransform2D(int i, Transform2D transform)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            masterQueue[i].transform = transform;
        }

        public static void SetColliderInfo(int i, int bulletLayer, bool canSleep)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            masterQueue[i].collisionLayer = bulletLayer;
            masterQueue[i].collisionSleepStatus.canSleep = canSleep;
        }

        public static void Rotate(int i, float radians)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            masterQueue[i].transform = masterQueue[i].transform.Rotated(radians);
        }

        public static void MakeSpaceForChildren(int i, int howMany)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            masterQueue[i].children.Expand(howMany, -1);
        }

        /// <summary>
        /// Recalculates this BNode's depth and all of its parents' depths too.<br />
        /// We're lazy and only recalculate when necessary!
        /// </summary>
        public static void RecalculateTreeDepth(int i)
        {
            if (i < 0 || i >= mqSize) { return; }
            int myDepth = 0;
            for (int j = 0; j < masterQueue[i].children.count; ++j)
            {
                int childIndex = masterQueue[i].children[j];
                if (childIndex < 0 || childIndex >= mqSize) { continue; }
                myDepth = Mathf.Max(myDepth, masterQueue[childIndex].treeDepth + 1);
            }
            masterQueue[i].treeDepth = myDepth;
            RecalculateTreeDepth(masterQueue[i].parentIndex);
        }

        /// <summary>
        /// Adds to or subtracts from this BNode's treeSize recursively for all parents.
        /// </summary>
        private static void AddToTreeSize(int i, int howMany)
        {
            if (i < 0 || i >= mqSize) { return; }
            masterQueue[i].treeSize += howMany;
            AddToTreeSize(masterQueue[i].parentIndex, howMany);
        }

        /// <param name="i">BNode index in masterQueue.</param>
        /// <param name="ci">Position to modify within the children list</param>
        /// <param name="ti">BNode index of target new child in masterQueue</param>
        /// <returns>The possible index of the old child (in case you want to destroy it)</returns>
        public static int SetChild(int i, int ci, int ti)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            MakeSpaceForChildren(i, ci + 1);
            if (masterQueue[i].children.count == 0) { return -1; }
            int oldIndex = masterQueue[i].children[ci];
            if (oldIndex == ti) { return ti; }
            if (oldIndex >= 0 && oldIndex < mqSize)
            {
                AddToTreeSize(i, -masterQueue[oldIndex].treeSize);
                // Warning: treeDepth doesn't change.
                // This makes setting the child more efficient.
                // If the depth must be accurate, please recalculate it.
                // treeSize of parent doesn't change either!
            }
            masterQueue[i].children[ci] = ti;
            if (ti >= 0 && ti < mqSize)
            {
                AddToTreeSize(i, masterQueue[ti].treeSize);
                masterQueue[i].treeDepth = Mathf.Max(masterQueue[i].treeDepth, 1 + masterQueue[ti].treeDepth);
                masterQueue[ti].parentIndex = i;
                masterQueue[ti].positionInParent = ci;
            }
            return oldIndex;
        }

        public static void DeleteAllChildren(int i)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            int oldTreeSize = masterQueue[i].treeSize;
            for (int j = 0; j < masterQueue[i].children.count; ++j)
            {
                int childIndex = masterQueue[i].children[j];
                if (j < 0 || j >= mqSize) { continue; }
                MasterQueuePushTree(childIndex);
            }
            masterQueue[i].children.Dispose();
            AddToTreeSize(i, 1 - oldTreeSize);
        }

        public static void AddBehavior(int i, BehaviorOrder order)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            masterQueue[i].behaviors.Expand(masterQueue[i].behaviors.count + 1, order);
        }

        /// <summary>
        /// Performs all behaviors of masterQueue[i] recursively.
        /// </summary>
        public static void Execute(int i, float stepSize, bool noMultithreading = false)
        {
            Execute(i, stepSize, noMultithreading, 0, 1);
        }

        /// <summary>
        /// Performs all behaviors of masterQueue[i] recursively.
        /// </summary>
        private static void Execute(int i, float stepSize, bool noMultithreading, int recursionDepth = 0, float stepSizeMultiplier = 1)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            if (!masterQueue[i].initialized) { return; }
            float flow = stepSizeMultiplier;
            float selfFlow = 1;
            float tempFlow = 1;
            for (int j = 0; j < masterQueue[i].behaviors.count; ++j)
            {
                BehaviorReceipt receipt = BehaviorOrderFunctions.Execute(
                    i, masterQueue[i].behaviors.array + j, stepSize * flow * selfFlow * tempFlow
                );
                noMultithreading |= receipt.noMultithreading;
                tempFlow = 1f;
                switch (receipt.throttleMode)
                {
                    case ThrottleMode.Full: flow *= 1f - receipt.throttle; break;
                    case ThrottleMode.Self: selfFlow *= 1f - receipt.throttle; break;
                    case ThrottleMode.Next: tempFlow *= 1f - receipt.throttle; break;
                }
            }
            int childCount = masterQueue[i].children.count;
            if (masterQueue[i].treeSize > multithreadCutoff / 4 && !noMultithreading)
            {
                Parallel.For(0, childCount, (j) =>
                {
                    int ii = i;
                    float f = flow;
                    int childIndex = masterQueue[ii].children[j];
                    if (childIndex < 0 || childIndex >= mqSize) { return; }
                    Execute(childIndex, stepSize, noMultithreading, recursionDepth + 1, f);
                });
            }
            else
            {
                for (int j = 0; j < masterQueue[i].children.count; j++)
                {
                    float f = flow;
                    int childIndex = masterQueue[i].children[j];
                    if (childIndex < 0 || childIndex >= mqSize) { continue; }
                    Execute(childIndex, stepSize, noMultithreading, recursionDepth + 1, f);
                }
            }
        }
    }
}
