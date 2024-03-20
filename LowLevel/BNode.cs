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
    /// A BNode can be a bullet, or a bullet acting as a container for other bullets.
    /// I refer to the collection of a root BNode with its child bullets as a structure.
    /// </summary>
    public unsafe struct BNode
    {
        public bool initialized;
        /// <summary>
        /// If true, transform is the world transform (for optimization purposes), otherwise the local transform.
        /// </summary>
        public bool worldTransformMode;
        public Transform2D transform;
        public UnsafeArray<int> children;
        public UnsafeArray<BehaviorOrder> behaviors;
        /// <summary>
        /// ID for bullet rendering. No graphic nor collision if renderID &lt; 0.
        /// </summary>
        public int bulletRenderID;
        /// <summary>
        /// ID for laser rendering. For any bullet that makes up the laser. No graphic nor collision if renderID &lt; 0.
        /// </summary>
        public int laserRenderID;
        /// <summary>
        /// treeSize = 1 + (treeSize of all children).<br />
        /// We aren't lazy to set it. It should always be accurate.
        /// </summary>
        public int treeSize;
        /// <summary>
        /// treeDepth = 0 with no children, otherwise 1 + max(treeDepth of all children).
        /// We are lazy here: removing children won't decrease the depth even though it should sometimes.
        /// To be sure, please recalculate it.
        /// </summary>
        public int treeDepth;
        /// <summary>
        /// Index of the parent.
        /// </summary>
        public int parentIndex;
        /// <summary>
        /// Position within the parent's child list.
        /// </summary>
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

    public unsafe static class BNodeFunctions
    {
        public static BNode* masterQueue = null; // circular queue
        /// <summary>
        /// This should always be the earliest uninitialized BNode in the master queue.
        /// </summary>
        public static int mqHead = 0;
        /// <summary>
        /// This should always be the earliest initialized BNode in the master queue, or the head for an empty queue.
        /// </summary>
        public static int mqTail = 0;
        /// <summary>
        /// If mqSize is a power of 2, modulus is a bitmask; counting on the compiler to help. <br />
        /// Also, we can only store one less than this, lest mqHead == mqTail when full.
        /// </summary>
        public const int mqSize = 262144;
        /// <summary>
        /// If the bullet's tree is larger than this, we use multithreading for certain operations.<br />
        /// We don't always multithread because Parallel.For has scheduling overhead.
        /// </summary>
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

        // Warning: this doesn't subtract from treeSize or treeDepth of the parent. That would make deletion take longer.
        // Please adjust it externally.
        public static bool MasterQueuePushOne(int i)
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
            AdvanceMQTail(); RetractMQHead();
            return true;
        }

        // Warning: this doesn't subtract from treeSize or treeDepth of the parent. That would make deletion take longer.
        // Please adjust it externally.
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
            if (recursionDepth == 0) { AdvanceMQTail(); RetractMQHead(); }
            return true;
        }

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
            if (newSpace == -1) { return -1; }
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
            if (newSpace == -1) { return -1; }
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
        public static void Execute(int i, float stepSize)
        {
            Execute(i, stepSize, 0, 1);
        }

        /// <summary>
        /// Performs all behaviors of masterQueue[i] recursively.
        /// </summary>
        private static void Execute(int i, float stepSize, int recursionDepth = 0, float stepSizeMultiplier = 1)
        {
            if (i < 0 || i >= mqSize) { throw new IndexOutOfRangeException(); }
            if (!masterQueue[i].initialized) { return; }
            float flow = stepSizeMultiplier;
            bool noMultithreading = false;
            for (int j = 0; j < masterQueue[i].behaviors.count; ++j)
            {
                BehaviorReceipt receipt = BehaviorOrderFunctions.Execute(
                    i, masterQueue[i].behaviors.array + j, stepSize * flow
                );
                if (receipt.delete) { PostExecute.ScheduleDeletion(i, receipt.useDeletionEffect); }
                noMultithreading |= receipt.noMultithreading;
                flow *= 1f - receipt.throttle;
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
                    Execute(childIndex, stepSize, recursionDepth + 1, f);
                });
            }
            else
            {
                for (int j = 0; j < masterQueue[i].children.count; j++)
                {
                    float f = flow;
                    int childIndex = masterQueue[i].children[j];
                    if (childIndex < 0 || childIndex >= mqSize) { continue; }
                    Execute(childIndex, stepSize, recursionDepth + 1, f);
                }
            }
        }
    }
}
