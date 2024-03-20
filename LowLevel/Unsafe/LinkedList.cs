using System;
using System.Runtime.InteropServices;

namespace Blastula.LowLevel
{
    /// <summary>
    /// Standard-issue doubly linked list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe struct LinkedList<T> where T : unmanaged
    {
        public struct Node
        {
            public Node* prev;
            public Node* next;
            public T data;
        }

        public Node* head;
        public Node* tail;
        public int count;
    }

    public unsafe static class LinkedListFunctions
    {
        public static LinkedList<T> Create<T>() where T : unmanaged
        {
            return new LinkedList<T>() { head = null, tail = null, count = 0 };
        }

        private static LinkedList<T>.Node* CreateNode<T>(T v) where T : unmanaged
        {
            LinkedList<T>.Node* newNode =
                (LinkedList<T>.Node*)Marshal.AllocHGlobal(sizeof(LinkedList<T>.Node));
            newNode->next = newNode->prev = null;
            newNode->data = v;
            return newNode;
        }

        public static LinkedList<T>.Node* AddHead<T>(ref this LinkedList<T> l, T v) where T : unmanaged
        {
            LinkedList<T>.Node* newNode = CreateNode(v);
            if (l.count == 0)
            {
                l.head = l.tail = newNode;
                l.count = 1;
            }
            else
            {
                l.head->prev = newNode;
                newNode->next = l.head;
                l.head = newNode;
                l.count++;
            }
            return newNode;
        }

        public static LinkedList<T>.Node* AddTail<T>(ref this LinkedList<T> l, T v) where T : unmanaged
        {
            LinkedList<T>.Node* newNode = CreateNode(v);
            if (l.count == 0)
            {
                l.head = l.tail = newNode;
                l.count = 1;
            }
            else
            {
                l.tail->next = newNode;
                newNode->prev = l.tail;
                l.tail = newNode;
                l.count++;
            }
            return newNode;
        }

        public static T GetHead<T>(ref this LinkedList<T> l) where T : unmanaged
        {
            if (l.count == 0) { return default; }
            return l.head->data;
        }

        public static T GetTail<T>(ref this LinkedList<T> l) where T : unmanaged
        {
            if (l.count == 0) { return default; }
            return l.tail->data;
        }

        public static T RemoveHead<T>(ref this LinkedList<T> l) where T : unmanaged
        {
            if (l.count == 0) { return default; }
            T ret = l.head->data;
            if (l.count == 1)
            {
                Marshal.FreeHGlobal((IntPtr)l.head);
                l.head = l.tail = null;
                l.count = 0;
            }
            else
            {
                LinkedList<T>.Node* toDelete = l.head;
                l.head->next->prev = null;
                l.head = l.head->next;
                Marshal.FreeHGlobal((IntPtr)toDelete);
                l.count--;
            }
            return ret;
        }

        public static T RemoveTail<T>(ref this LinkedList<T> l) where T : unmanaged
        {
            if (l.count == 0) { return default; }
            T ret = l.tail->data;
            if (l.count == 1)
            {
                Marshal.FreeHGlobal((IntPtr)l.tail);
                l.head = l.tail = null;
                l.count = 0;
            }
            else
            {
                LinkedList<T>.Node* toDelete = l.tail;
                l.tail->prev->next = null;
                l.tail = l.tail->prev;
                Marshal.FreeHGlobal((IntPtr)toDelete);
                l.count--;
            }
            return ret;
        }

        public static T RemoveByNode<T>(ref this LinkedList<T> l, LinkedList<T>.Node* n) where T : unmanaged
        {
            if (l.count == 0) { return default; }
            if (n == l.head) { return l.RemoveHead(); }
            if (n == l.tail) { return l.RemoveTail(); }
            T ret = n->data;
            LinkedList<T>.Node* prev = n->prev;
            LinkedList<T>.Node* next = n->next;
            prev->next = next;
            next->prev = prev;
            Marshal.FreeHGlobal((IntPtr)n);
            l.count--;
            return ret;
        }

        public static void Dispose<T>(ref this LinkedList<T> l) where T : unmanaged
        {
            if (l.count > 0)
            {
                LinkedList<T>.Node* curr = l.head;
                while (curr != null)
                {
                    LinkedList<T>.Node* toDelete = curr;
                    curr = curr->next;
                    Marshal.FreeHGlobal((IntPtr)toDelete);
                }
            }
            l.head = l.tail = null;
            l.count = 0;
        }
    }
}
