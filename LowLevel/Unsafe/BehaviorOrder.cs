using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Blastula.LowLevel
{
    /// <summary>
    /// This return information affects future behaviors after the one just executed.
    /// </summary>
    public struct BehaviorReceipt
    {
        public float throttle;
        public bool delete;
        public bool useDeletionEffect;
        public bool noMultithreading;
        public BehaviorReceipt()
        {
            throttle = 0;
            delete = false;
            useDeletionEffect = false;
            noMultithreading = false;
        }
    }

    /// <summary>
    /// Orders a behavior to occur; stores data needed to execute it.
    /// </summary>
    public unsafe struct BehaviorOrder
    {
        /// <summary>
        /// The behavior that can be executed.
        /// </summary>
        public delegate*<int, float, void*, BehaviorReceipt> func;
        /// <summary>
        /// optional: custom way to clone the data when a shallow clone isn't enough.
        /// </summary>
        public delegate*<void*, void*> clone;
        /// <summary>
        /// optional: frees elements of the data if needed, when the behavior is deleted.
        /// </summary>
        public delegate*<void*, void> dispose;
        /// <summary>
        /// Data specific to the BNode.
        /// </summary>
        public void* data;
        /// <summary>
        /// Size of data (bytes). Used for cloning purposes.
        /// </summary>
        public int dataSize;
    }

    public unsafe static class BehaviorOrderFunctions
    {
        public static BehaviorOrder empty = new BehaviorOrder() { func = null, data = null, dataSize = 0 };

        public static BehaviorOrder Clone(BehaviorOrder* b)
        {
            void* dataClone = null;
            if (b->dataSize > 0)
            {
                if (b->clone != null)
                {
                    dataClone = b->clone(b->data);
                }
                else
                {
                    dataClone = (void*)Marshal.AllocHGlobal(b->dataSize);
                    Buffer.MemoryCopy(b->data, dataClone, b->dataSize, b->dataSize);
                }
            }
            return new BehaviorOrder()
            {
                func = b->func,
                clone = b->clone,
                dispose = b->dispose,
                data = dataClone,
                dataSize = b->dataSize
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BehaviorReceipt Execute(int nodeIndex, BehaviorOrder* b, float stepSize)
        {
            if (b == null || b->func == null) { return new BehaviorReceipt(); }
            return b->func(nodeIndex, stepSize, b->data);
        }

        public static void Dispose(BehaviorOrder* b)
        {
            if (b->data != null)
            {
                if (b->dispose != null) { b->dispose(b->data); }
                Marshal.FreeHGlobal((IntPtr)b->data);
            }
            b->func = null;
            b->data = null;
            b->dataSize = 0;
        }
    }
}
