using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Blastula.Coroutine;

public static class CoroutineUtility
{
    public static Dictionary<int, CoroutineSubQueue> subqueueByPriority = new();

    public class Coroutine
    {
        public IEnumerator func;
        public Action<Coroutine> cancel;
        public Action<Coroutine> onFinishCallback; // instant callback
        public int priority;
        public bool finished = false;
        public Node boundNode;
    }

    /// <summary>
    /// Runs QueueFree on (deletes) the Node after frames or seconds.
    /// </summary>
    public static IEnumerator DelayedQueueFree(Node toDelete, float waitTime, Wait.TimeUnits units)
    {
        if (Session.main == null) yield break;
        if (units == Wait.TimeUnits.Seconds) yield return new WaitTime(waitTime);
        else yield return new WaitFrames(Mathf.RoundToInt(waitTime));
        if (toDelete != null && toDelete.IsInsideTree() && !toDelete.IsQueuedForDeletion()) toDelete.QueueFree();
    }

    private static void MakeSubQueueIfNeeded(int priority)
    {
        if (!subqueueByPriority.ContainsKey(priority))
        {
            CoroutineSubQueue newNode = new CoroutineSubQueue();
            newNode.Name = priority.ToString();
            newNode.ProcessPriority = priority;
            CoroutineQueueHolder.main.AddChild(newNode);
            subqueueByPriority[priority] = newNode;
        }
    }

    public static Coroutine StartCoroutine(Coroutine template)
    {
        if (CoroutineQueueHolder.main == null) throw new Exception("No holder!");
        int priority = template.priority;
        MakeSubQueueIfNeeded(priority);
        subqueueByPriority[priority].StartInQueue(template);
        return template;
    }

    public static Coroutine StartCoroutine(this Node dispatcher, IEnumerator item, Action<Coroutine> cancel = null, int? priorityOverride = null)
    {
        if (CoroutineQueueHolder.main == null) throw new Exception("No holder!");
        int priority = priorityOverride ?? dispatcher.ProcessPriority;
        MakeSubQueueIfNeeded(priority);
        Coroutine co = new Coroutine { func = item, boundNode = dispatcher, cancel = cancel, priority = priority };
        subqueueByPriority[priority].StartInQueue(co);
        return co;
    }

    /// <summary>
    /// Stops all coroutines everywhere.
    /// </summary>
    public static void StopAll()
    {
        foreach (CoroutineSubQueue q in subqueueByPriority.Values) q.Clear();
    }
}
