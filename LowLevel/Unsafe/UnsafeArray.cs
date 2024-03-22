using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Blastula.LowLevel
{
    /// <summary>
    /// Stores a list of VALUES (it copies them.)
    /// </summary>
    /// <typeparam name="T">
    /// Unmanaged type of the array.
    /// </typeparam>
    public unsafe struct UnsafeArray<T> where T : unmanaged
    {
        public T* array;
        public int count;

        public T this[int i]
        {
            get { return array[i]; }
            set { this.Set(i, value); }
        }
    }

    public unsafe static class UnsafeArrayFunctions
    {
        public static UnsafeArray<T> Create<T>(int count) where T : unmanaged
        {
            T* newArray = (count > 0) ? (T*)Marshal.AllocHGlobal(count * sizeof(T)) : null;
            return new UnsafeArray<T>() { array = newArray, count = count };
        }

        public static UnsafeArray<T> Create<T>(List<T> arr) where T : unmanaged
        {
            T* newArray = arr.Count > 0 ? (T*)Marshal.AllocHGlobal(arr.Count * sizeof(T)) : null;
            for (int i = 0; i < arr.Count; ++i) { newArray[i] = arr[i]; }
            return new UnsafeArray<T>() { array = newArray, count = arr.Count };
        }

        public static UnsafeArray<T> Clone<T>(this ref UnsafeArray<T> a) where T : unmanaged
        {
            UnsafeArray<T> clone = Create<T>(a.count);
            int c = sizeof(T) * a.count;
            if (a.count > 0) { Buffer.MemoryCopy(a.array, clone.array, c, c); }
            return clone;
        }

        public static UnsafeArray<BehaviorOrder> CloneBehaviorOrder(this ref UnsafeArray<BehaviorOrder> a)
        {
            UnsafeArray<BehaviorOrder> clone = Create<BehaviorOrder>(a.count);
            for (int i = 0; i < a.count; i++)
            {
                clone[i] = BehaviorOrderFunctions.Clone(a.array + i);
            }
            return clone;
        }

        public static bool Set<T>(this ref UnsafeArray<T> a, int index, T newValue) where T : unmanaged
        {
            if (index < 0 || index > a.count) { return false; }
            a.array[index] = newValue;
            return true;
        }

        public static void Expand<T>(this ref UnsafeArray<T> a, int howMany, T filler = default) where T : unmanaged
        {
            if (howMany <= a.count) { return; }
            T* oldMem = a.array;
            int st = sizeof(T);
            T* newMem = (T*)Marshal.AllocHGlobal(st * howMany);
            if (a.count > 0) { Buffer.MemoryCopy(oldMem, newMem, st * howMany, st * a.count); }
            for (int i = a.count; i < howMany; ++i) { newMem[i] = filler; }
            Marshal.FreeHGlobal((IntPtr)oldMem);
            a.array = newMem;
            a.count = howMany;
        }

        public static void Dispose<T>(this ref UnsafeArray<T> a) where T : unmanaged
        {
            if (a.count > 0) { Marshal.FreeHGlobal((IntPtr)a.array); }
            a.array = null;
            a.count = 0;
        }

        public static void DisposeBehaviorOrder(this ref UnsafeArray<BehaviorOrder> a)
        {
            if (a.count > 0)
            {
                for (int i = 0; i < a.count; ++i) { BehaviorOrderFunctions.Dispose(a.array + i); }
                Marshal.FreeHGlobal((IntPtr)a.array);
            }
            a.array = null;
            a.count = 0;
        }
    }
}
