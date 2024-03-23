using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// For a duration, children of this bullet structure will be run in the order of the child list,
    /// without any multithreading.
    /// </summary>
    /// <example>
    /// If you shoot a circle of hundreds of bullets with a "mist" effect to fade in,
    /// you may notice that the layering of bullets is abnormal and inconsistent.
    /// This is because of multithreading changing the bullet graphics in an unpredictable order.
    /// To fix this, use StunMultithreading on the circle structure, with a duration at least as long as the effect takes to complete.
    /// </example>
    [GlobalClass]
    public unsafe partial class StunMultithreading : AddBehavior
    {
        [Export] public string duration = "120";
        [Export] public Wait.TimeUnits units = Wait.TimeUnits.Frames;

        private struct Data
        {
            public float duration;
            public float currentTime;
            public bool elapsed;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            Data* data = (Data*)dataPtr;
            if (!data->elapsed) { data->currentTime += stepSize; }
            if (data->elapsed || data->currentTime >= data->duration)
            {
                data->elapsed = true;
                return new BehaviorReceipt();
            }
            return new BehaviorReceipt { noMultithreading = true };
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->duration = Solve("duration").AsSingle();
            if (units == Wait.TimeUnits.Seconds) { dataPtr->duration *= Persistent.SIMULATED_FPS; }
            dataPtr->currentTime = 0;
            dataPtr->elapsed = false;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}


