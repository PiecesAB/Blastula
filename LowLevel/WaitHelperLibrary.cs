using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula
{
    public static class Waiters
    {
        public static long sceneLoadCounter { get; private set; } = 0;
        public static void IncrementSceneLoadCounter() { sceneLoadCounter++; }

        public static async Task WaitOneFrame(this Node dispatcher, bool ignorePause = false)
        {
            await dispatcher.WaitFrames(1, ignorePause);
        }

        public static async Task WaitFrames(this Node dispatcher, int frames, bool ignorePause = false)
        {
            long slc2 = sceneLoadCounter;
            int currFrames = 0;
            while (currFrames < frames)
            {
                if (slc2 != sceneLoadCounter) { return; }
                if (!Session.main.paused || ignorePause) { ++currFrames; }
                if (!dispatcher.IsInsideTree()) { return; }
                await dispatcher.ToSignal(dispatcher.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        public static async Task WaitSeconds(this Node dispatcher, float seconds, bool ignorePause = false)
        {
            long slc2 = sceneLoadCounter;
            float currSeconds = 0;
            while (currSeconds < seconds - 0.0001f)
            {
                if (slc2 != sceneLoadCounter) { return; }
                if (!Session.main.paused || ignorePause) { currSeconds += (float)Engine.TimeScale / Persistent.SIMULATED_FPS; }
                if (!dispatcher.IsInsideTree()) { return; }
                await dispatcher.ToSignal(dispatcher.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

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

        public class BoxedBool
        {
            public bool b;
            public BoxedBool() { this.b = false; }
            public BoxedBool(bool b) { this.b = b; }
            public static implicit operator bool(BoxedBool s) { return s.b; }
            public static implicit operator BoxedBool(bool s) { return new BoxedBool(s); }
        }

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
