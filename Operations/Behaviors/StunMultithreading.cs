using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// After a number of frames, the structure rooted at this node will be deleted.
    /// </summary>
    [GlobalClass]
    public unsafe partial class StunMultithreading : AddBehavior
    {
        [Export] public string duration = "120";
        [Export] public Wait.TimeUnits units = Wait.TimeUnits.Frames;

        public struct Data
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


