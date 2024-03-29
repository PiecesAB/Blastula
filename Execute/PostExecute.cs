using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.Operations;
using System.Runtime.InteropServices;
using static Blastula.BNodeFunctions;

namespace Blastula
{
    /// <summary>
    /// There exist operations we want to do on the main thread after Execute, as scheduled by bullet structures.
    /// This class contains functions to handle the scheduling and performing of such operations.
    /// This is a static utility class, and relies on a manager node to actually do anything.
    /// </summary>
    public unsafe static class PostExecute
    {
        private struct DeleteOrder
        {
            public int bNodeIndex;
            public bool useDeletionEffect;
        }

        /// <summary>
        /// List of indices to delete the next time PerformScheduled runs.
        /// There are 16 of them to help with multithreading.
        /// </summary>
        private static LowLevel.LinkedList<DeleteOrder>* toDelete = null;

        private struct OperationOrder
        {
            public int bNodeIndex;
            public long operationID;
        }

        /// <summary>
        /// There are also 16 of these.
        /// </summary>
        private static LowLevel.LinkedList<OperationOrder>* toOperate = null;



        private static bool initialized = false;

        private static object initLock = new object();

        public static void Initialize()
        {
            lock (initLock)
            {
                if (initialized) { return; }
                toDelete = (LowLevel.LinkedList<DeleteOrder>*)Marshal.AllocHGlobal(sizeof(LowLevel.LinkedList<DeleteOrder>) * 16);
                toOperate = (LowLevel.LinkedList<OperationOrder>*)Marshal.AllocHGlobal(sizeof(LowLevel.LinkedList<OperationOrder>) * 16);
                lockScheduleDeletions = new object[16];
                lockScheduleOperations = new object[16];
                for (int i = 0; i < 16; ++i)
                {
                    toDelete[i] = LinkedListFunctions.Create<DeleteOrder>();
                    toOperate[i] = LinkedListFunctions.Create<OperationOrder>();
                    lockScheduleDeletions[i] = new object();
                    lockScheduleOperations[i] = new object();
                }
                initialized = true;
            }
        }

        private static object[] lockScheduleDeletions;
        /// <summary>
        /// This will delete the BNode the next time PerformScheduled runs.
        /// </summary>
        /// <remarks>
        /// A bullet structure must never try to delete itself during execution, because of potential multithreading.
        /// If we tried that, the integrity of our master queue, among other things, could be mutilated by race conditions.
        /// </remarks>
        public static void ScheduleDeletion(int bNodeIndex, bool useDeletionEffect)
        {
            if (!initialized) { Initialize(); }
            lock (lockScheduleDeletions[bNodeIndex % 16])
            {
                toDelete[bNodeIndex % 16].AddTail(
                    new DeleteOrder
                    {
                        bNodeIndex = bNodeIndex,
                        useDeletionEffect = useDeletionEffect
                    }
                );
            }
        }

        private static object[] lockScheduleOperations;

        public static void ScheduleOperation(int bNodeIndex, long opID)
        {
            if (!initialized) { Initialize(); }
            lock (lockScheduleOperations[bNodeIndex % 16])
            {
                if (!initialized) { Initialize(); }
                toOperate[bNodeIndex % 16].AddTail(new OperationOrder { operationID = opID, bNodeIndex = bNodeIndex });
            }
        }

        /// <summary>
        /// Performs all scheduled deletions and operations on bullet structures.
        /// </summary>
        public static void PerformScheduled()
        {
            if (!initialized) { Initialize(); }
            for (int i = 0; i < 16; ++i)
            {
                while (toOperate[i].count > 0)
                {
                    OperationOrder order = toOperate[i].RemoveHead();
                    BaseOperation operation = null;
                    if (order.operationID >= 0 && BaseOperation.operationFromID.ContainsKey(order.operationID))
                    {
                        operation = BaseOperation.operationFromID[order.operationID];
                    }
                    if (operation == null) { BulletRenderer.ConvertToDeletionEffects(order.bNodeIndex); continue; }
                    if (!masterQueue[order.bNodeIndex].initialized) { continue; }
                    int parent = masterQueue[order.bNodeIndex].parentIndex;
                    int positionInParent = masterQueue[order.bNodeIndex].positionInParent;
                    if (parent >= 0) { SetChild(parent, positionInParent, -1); }
                    int outStructure = operation.ProcessStructure(order.bNodeIndex);
                    if (parent >= 0) { SetChild(parent, positionInParent, outStructure); }
                }

                while (toDelete[i].count > 0)
                {
                    DeleteOrder d = toDelete[i].RemoveHead();
                    if (d.useDeletionEffect)
                    {
                        BulletRenderer.ConvertToDeletionEffects(d.bNodeIndex);
                    }
                    else
                    {
                        MasterQueuePushTree(d.bNodeIndex);
                    }
                }
            }
        }
    }
}

