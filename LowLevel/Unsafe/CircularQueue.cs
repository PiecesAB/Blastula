using System;
using System.Runtime.InteropServices;

namespace Blastula.LowLevel
{
    /// <summary>
    /// Stores a list of VALUES (it copies them.)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe struct CircularQueue<T> where T : unmanaged
    {
        public struct TInit
        {
            public T item;
            public bool initialized;
        }

        public TInit* data;
        /// <summary>
        /// The head is the next uninitialized item to claim.
        /// </summary>
        public int head;
        /// <summary>
        /// The tail is the earliest initialized item.
        /// </summary>
        public int tail;
        public int capacity;
    }

    public unsafe static class CircularQueueFunctions
    {
        /// <summary>
        /// Warning: we can only store one less than capacity.
        /// </summary>
        public static CircularQueue<T> Create<T>(int capacity) where T : unmanaged
        {
            CircularQueue<T>.TInit* newData =
                capacity > 0
                ? (CircularQueue<T>.TInit*)Marshal.AllocHGlobal(capacity * sizeof(CircularQueue<T>.TInit))
                : null;
            CircularQueue<T> newQueue = new CircularQueue<T>()
            {
                data = newData,
                capacity = capacity,
                head = 0,
                tail = 0
            };
            return newQueue;
        }

        public static int Count<T>(this ref CircularQueue<T> queue) where T : unmanaged
        {
            return (queue.tail <= queue.head) ? (queue.head - queue.tail) : (queue.capacity - (queue.tail - queue.head));
        }

        public static int SpaceFree<T>(this ref CircularQueue<T> queue) where T : unmanaged
        {
            return queue.capacity - 1 - queue.Count();
        }

        /// <summary>
        /// Returns the index where the space was made, for future tracking.
        /// </summary>
        public static int Add<T>(this ref CircularQueue<T> queue, T item) where T : unmanaged
        {
            if (queue.SpaceFree() < 1) { return -1; }
            int ret = queue.head;
            queue.data[queue.head] = new CircularQueue<T>.TInit { item = item, initialized = true };
            queue.head = (queue.head + 1) % queue.capacity;
            return ret;
        }

        public static bool Remove<T>(this ref CircularQueue<T> queue, int index) where T : unmanaged
        {
            if (index < 0 || index >= queue.capacity) { return false; }
            if (!queue.data[index].initialized) { return false; }
            queue.data[index].initialized = false;
            queue.AdvanceTail(); queue.RetractHead();
            return true;
        }

        /// <summary>
        /// Ensures the tail is the earliest initialized item.
        /// </summary>
        public static void AdvanceTail<T>(this ref CircularQueue<T> queue) where T : unmanaged
        {
            while (queue.Count() > 0 && !queue.data[queue.tail].initialized)
            {
                queue.tail = (queue.tail + 1) % queue.capacity;
            }
        }

        /// <summary>
        /// Ensures the head is the next uninitialized item to claim.
        /// </summary>
        public static void RetractHead<T>(this ref CircularQueue<T> queue) where T : unmanaged
        {
            int hprev = (queue.head == 0) ? (queue.capacity - 1) : (queue.head - 1);
            while (queue.Count() > 0 && !queue.data[hprev].initialized)
            {
                queue.head = hprev;
                hprev = (queue.head == 0) ? (queue.capacity - 1) : (queue.head - 1);
            }
        }

        /// <summary>
        /// Gets the item as if this queue were a list, including holes. tail is index 0.
        /// </summary>
        public static CircularQueue<T>.TInit GetList<T>(this ref CircularQueue<T> queue, int index) where T : unmanaged
        {
            if (index > queue.Count()) { return new CircularQueue<T>.TInit { initialized = false }; }
            return queue.data[(queue.tail + index + queue.capacity) % queue.capacity];
        }

        public static void Dispose<T>(this ref CircularQueue<T> queue) where T : unmanaged
        {
            if (queue.capacity > 0)
            {
                Marshal.FreeHGlobal((IntPtr)queue.data);
            }
            queue.capacity = 0;
            queue.head = queue.tail = 0;
        }
    }
}

