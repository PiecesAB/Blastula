using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
    public partial class FrameCounter : Node
    {
        /// <summary>
        /// Number of real frames since the program began, regardless of pausing.
        /// </summary>
        public static ulong realGameFrame { get; private set; }
        /// <summary>
        /// Number of unpaused frames since the stage began.
        /// </summary>
        public static ulong stageFrame { get; private set; }
        /// <summary>
        /// Number of frames since the session began, regardless of pausing.
        /// </summary>
        public static ulong realSessionFrame { get; private set; }

        public void ResetStageFrame()
        {
            stageFrame = 0;
        }

        public void ResetSessionFrame()
        {
            realSessionFrame = 0;
            ResetStageFrame();
        }

        public class Buffer
        {
            public ulong startFrame { get; private set; }
            public ulong length { get; private set; }

            // Always elapsed
            public Buffer()
            {
                startFrame = realGameFrame;
                this.length = 0;
            }

            public Buffer(ulong length)
            {
                startFrame = realGameFrame;
                this.length = length;
            }

            public bool Elapsed()
            {
                return realGameFrame - startFrame >= length;
            }

            public ulong FramesRemaining()
            {
                if (Elapsed()) { return 0; }
                return length - (realGameFrame - startFrame);
            }

            public bool BecameElapsedThisFrame()
            {
                return realGameFrame - startFrame == length;
            }

            public bool WillBecomeElapsedNextFrame()
            {
                return realGameFrame - startFrame == length - 1;
            }

            /// <summary>
            /// Restarts the buffer to this frame.
            /// </summary>
            public void Replenish()
            {
                startFrame = realGameFrame;
            }

            /// <summary>
            /// Restarts the buffer to this frame, and sets the number of frames until it elapses.
            /// </summary>
            public void Replenish(ulong newLength)
            {
                startFrame = realGameFrame;
                length = newLength;
            }

            /// <summary>
            /// Immediately makes the buffer elapsed.
            /// </summary>
            public void Deplete()
            {
                length = 0;
            }

            public void SetLength(ulong newLength)
            {
                length = newLength;
            }
        }

        // Stores results from the current frame, to avoid recalculating.
        public class Cache<T>
        {
            private bool dataExists;
            private ulong validFrame;
            public T data { get; private set; }

            public Cache()
            {
                this.dataExists = false;
                this.validFrame = realGameFrame;
                this.data = default;
            }

            public bool IsValid()
            {
                return dataExists && validFrame == realGameFrame;
            }

            public void Invalidate()
            {
                dataExists = false;
            }

            public void Update(T newData)
            {
                this.dataExists = true;
                this.validFrame = realGameFrame;
                this.data = newData;
            }
        }

        // Stores results from the current frame, to avoid recalculating.
        public class DictCache<K, V>
        {
            private System.Collections.Generic.Dictionary<K, Cache<V>> caches;

            public DictCache()
            {
                caches = new System.Collections.Generic.Dictionary<K, Cache<V>>();
            }

            public bool IsValid(K key)
            {
                return caches.ContainsKey(key) && caches[key].IsValid();
            }

            public void Update(K key, V newData)
            {
                if (!caches.ContainsKey(key)) { caches[key] = new Cache<V>(); }
                caches[key].Update(newData);
            }

            public V Get(K key)
            {
                if (!caches.ContainsKey(key)) { return default; }
                return caches[key].data;
            }
        }

        // True if at least "count" frames passed since "startFrame"
        public static bool Elapsed(ulong startFrame, ulong frameCount)
        {
            return (realGameFrame - startFrame) >= frameCount;
        }

        public static double GetStageTime()
        {
            return (double)stageFrame / Persistent.SIMULATED_FPS;
        }

        public override void _Ready()
        {
            realGameFrame = 0;
            ProcessPriority = Persistent.Priorities.FRAME_COUNTER_INCREMENT;
            BNodeFunctions.InitializeQueue();
        }

        public override void _Process(double delta)
        {
            realGameFrame++;
            realSessionFrame++;
            if (Session.main != null && !Session.main.paused)
            {
                stageFrame++;
            }
        }
    }
}
