using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula
{
    /// <summary>
    /// Async helpers that wait. 
    /// </summary>
    public static class Waiters
    {
        /// <summary>
        /// If this changes, all waits will terminate.
        /// </summary>
        public static long sceneLoadCounter { get; private set; } = 0;
        public static void IncrementSceneLoadCounter() { sceneLoadCounter++; }

        /// <summary>
        /// Wait one frame. Not affected by time scale.
        /// </summary>
        public static async Task WaitOneFrame(this Node dispatcher, bool ignorePause = false)
        {
            await dispatcher.WaitFrames(1, ignorePause);
        }

        /// <summary>
        /// Wait a number of frames. Not affected by time scale.
        /// </summary>
        public static async Task WaitFrames(this Node dispatcher, int frames, bool ignorePause = false)
        {
            long slc2 = sceneLoadCounter;
            int currFrames = 0;
            while (currFrames < frames)
            {
                if (slc2 != sceneLoadCounter) { return; }
                if ((!Session.main.paused && !Debug.GameFlow.frozen) || ignorePause) { ++currFrames; }
                if (!dispatcher.IsInsideTree()) { return; }
                await dispatcher.ToSignal(dispatcher.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        /// <summary>
        /// Wait a number of seconds. Affected by time scale.
        /// </summary>
        public static async Task WaitSeconds(this Node dispatcher, float seconds, bool ignorePause = false)
        {
            long slc2 = sceneLoadCounter;
            float currSeconds = 0;
            while (currSeconds < seconds - 0.0001f)
            {
                if (slc2 != sceneLoadCounter) { return; }
                if ((!Session.main.paused && !Debug.GameFlow.frozen) || ignorePause) { currSeconds += (float)Engine.TimeScale / Persistent.SIMULATED_FPS; }
                if (!dispatcher.IsInsideTree()) { return; }
                await dispatcher.ToSignal(dispatcher.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        /// <summary>
        /// Wait until a condition is true.
        /// </summary>
        public static async Task WaitUntil(this Node dispatcher, Func<bool> Condition)
        {
            long slc2 = sceneLoadCounter;
            while (!Condition())
            {
                if (slc2 != sceneLoadCounter) { return; }
                if (!dispatcher.IsInsideTree()) { return; }
                await dispatcher.ToSignal(dispatcher.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        /// <summary>
        /// Runs QueueFree on (deletes) the Node after frames or seconds.
        /// </summary>
        public static async void DelayedQueueFree(Node toDelete, float waitTime, Wait.TimeUnits units)
        {
            if (Session.main == null) { return; }
            if (units == Wait.TimeUnits.Seconds)
            {
                await Session.main.WaitSeconds(waitTime);
            }
            else
            {
                await Session.main.WaitFrames(Mathf.RoundToInt(waitTime));
            }
            if (toDelete != null && toDelete.IsInsideTree() && !toDelete.IsQueuedForDeletion())
            {
                toDelete.QueueFree();
            }
        }

        /// <summary>
        /// BoxedBool encapsulates a bool as a reference, so that it can be changed everywhere it's used.
        /// </summary>
        public class BoxedBool
        {
            public bool b;
            public BoxedBool() { this.b = false; }
            public BoxedBool(bool b) { this.b = b; }
            public static implicit operator bool(BoxedBool s) { return s.b; }
            public static implicit operator BoxedBool(bool s) { return new BoxedBool(s); }
        }

        /// <summary>
        /// Wait until BoxedBool is true.
        /// </summary>
        /// <example>
        /// This can be used to wait
        /// for a condition that hasn't even been determined yet.
        /// </example>
        public static async Task WaitUntilBoxedBool(this Node dispatcher, BoxedBool b)
        {
            long slc2 = sceneLoadCounter;
            while (!b)
            {
                if (slc2 != sceneLoadCounter) { return; }
                if (!dispatcher.IsInsideTree()) { return; }
                await dispatcher.ToSignal(dispatcher.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
    }
}
