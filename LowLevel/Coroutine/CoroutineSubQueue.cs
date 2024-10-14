using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Blastula.Coroutine;

/// <summary>
/// Handles the execution of coroutines, each for a certain priority.
/// </summary>
public partial class CoroutineSubQueue : Node
{
    private class PredicateItem
    {
        public Func<bool> condition;
        public CoroutineUtility.Coroutine func;
    }

    private PriorityQueue<CoroutineUtility.Coroutine, ulong> unpausedFrameQueue = new();
    private PriorityQueue<CoroutineUtility.Coroutine, double> unpausedTimeQueue = new();
    private PriorityQueue<CoroutineUtility.Coroutine, ulong> alwaysFrameQueue = new();
    private HashSet<PredicateItem> predicateList = new();

    private void TickCoroutine(CoroutineUtility.Coroutine c)
    {
        while (true)
        {
            if (!IsInstanceValid(c.boundNode) || !c.boundNode.IsInsideTree() || !c.func.MoveNext() || c.func.Current == null)
            {
                if (c.onFinishCallback != null) c.onFinishCallback(c);
                c.finished = true;
                return;
            }
            // Else we may need to queue
            switch (c.func.Current)
            {
                case SetCancel sc:
                    c.cancel = sc.cancel;
                    continue;
                case WaitOneFrame waitOne:
                    if (waitOne.runsWhenPaused) alwaysFrameQueue.Enqueue(c, FrameCounter.realGameFrame + 1);
                    else unpausedFrameQueue.Enqueue(c, FrameCounter.stageFrame + 1);
                    return;
                case WaitFrames waitFrames:
                    if (waitFrames.frames <= 0) continue;
                    if (waitFrames.runsWhenPaused) alwaysFrameQueue.Enqueue(c, FrameCounter.realGameFrame + (ulong)waitFrames.frames);
                    else unpausedFrameQueue.Enqueue(c, FrameCounter.stageFrame + (ulong)waitFrames.frames);
                    return;
                case WaitTime waitSeconds:
                    if (waitSeconds.seconds <= 0) continue;
                    if (waitSeconds.runsWhenPaused) alwaysFrameQueue.Enqueue(c, FrameCounter.realGameFrame + (ulong)(waitSeconds.seconds * Blastula.VirtualVariables.Persistent.SIMULATED_FPS));
                    else unpausedTimeQueue.Enqueue(c, FrameCounter.stageTime + waitSeconds.seconds);
                    return;
                case WaitCondition waitUntil:
                    if (waitUntil.condition == null || waitUntil.condition()) continue;
                    predicateList.Add(new PredicateItem { condition = waitUntil.condition, func = c });
                    return;
                case IEnumerator subroutine:
                    {
                        CoroutineUtility.Coroutine subCoroutine = c.boundNode.StartCoroutine(subroutine);
                        if (subCoroutine.finished) TickCoroutine(c);
                        else subCoroutine.onFinishCallback = (_) => TickCoroutine(c);
                    }
                    return;
                case CoroutineUtility.Coroutine subroutineTemplate:
                    {
                        subroutineTemplate = CoroutineUtility.StartCoroutine(subroutineTemplate);
                        if (subroutineTemplate.finished) TickCoroutine(c);
                        else subroutineTemplate.onFinishCallback = (_) => TickCoroutine(c);
                    }
                    return;
                default:
                    throw new Exception("Can't handle what this coroutine yields.");
            }
        }
    }

    public void Clear()
    {
        while (unpausedFrameQueue.Count > 0)
        {
            var c = unpausedFrameQueue.Dequeue();
            if (c.cancel != null) c.cancel(c);
        }
        while (unpausedTimeQueue.Count > 0)
        {
            var c = unpausedTimeQueue.Dequeue();
            if (c.cancel != null) c.cancel(c);
        }
        while (alwaysFrameQueue.Count > 0)
        {
            var c = alwaysFrameQueue.Dequeue();
            if (c.cancel != null) c.cancel(c);
        }
        unpausedFrameQueue = new();
        unpausedTimeQueue = new();
        alwaysFrameQueue = new();
        foreach (var c in new List<PredicateItem>(predicateList))
        {
            if (c.func.cancel != null) c.func.cancel(c.func);
        }
        predicateList = new();
    }

    public void StartInQueue(CoroutineUtility.Coroutine c)
    {
        if (c == null || c.func == null) return;
        TickCoroutine(c);
    }

    public override void _Process(double _)
    {
        // Solve queues and crap
        while (unpausedFrameQueue.TryPeek(out CoroutineUtility.Coroutine c, out ulong frame) && frame <= FrameCounter.stageFrame)
        { unpausedFrameQueue.Dequeue(); TickCoroutine(c); }

        while (alwaysFrameQueue.TryPeek(out CoroutineUtility.Coroutine c, out ulong frame) && frame <= FrameCounter.realGameFrame)
        { alwaysFrameQueue.Dequeue(); TickCoroutine(c); }

        while (unpausedTimeQueue.TryPeek(out CoroutineUtility.Coroutine c, out double time) && time <= FrameCounter.stageTime + 0.001)
        { unpausedTimeQueue.Dequeue(); TickCoroutine(c); }

        foreach (var item in new List<PredicateItem>(predicateList))
        {
            if (item.condition())
            {
                predicateList.Remove(item); TickCoroutine(item.func);
            }
        }
    }
}
