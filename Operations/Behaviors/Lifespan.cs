using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// After some duration, the bullet structure will be deleted.
    /// Just as organisms must die in reality, to avoid depleting the ecosystem's resources,
    /// So must bullet structures die to make room in the master queue.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/destruction.png")]
    public unsafe partial class Lifespan : AddBehavior
    {
        [Export] public string duration = "600";
        [Export] public Wait.TimeUnits units = Wait.TimeUnits.Frames;
        // Transforms visible bullets into deletion effects
        [Export] public bool deletionEffect = false;

        private struct Data
        {
            public float duration;
            public bool deletionEffect;
            public float currentTime;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            Data* data = (Data*)dataPtr;
            data->currentTime += stepSize;
            return new BehaviorReceipt { 
                delete = data->currentTime >= data->duration, 
                useDeletionEffect = data->deletionEffect 
            };
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->duration = Solve("duration").AsSingle();
            dataPtr->deletionEffect = deletionEffect;
            if (units == Wait.TimeUnits.Seconds) { dataPtr->duration *= Persistent.SIMULATED_FPS; }
            dataPtr->currentTime = 0;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        private static BehaviorOrder CreateOrderAdd(int inStructure, float duration, Wait.TimeUnits units, bool deletionEffect = false)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->duration = duration;
            dataPtr->deletionEffect = deletionEffect;
            if (units == Wait.TimeUnits.Seconds) { dataPtr->duration *= Persistent.SIMULATED_FPS; }
            dataPtr->currentTime = 0;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        public static void Add(int inStructure, float duration, Wait.TimeUnits units, bool deletionEffect = false)
        {
            if (inStructure < 0 || inStructure >= BNodeFunctions.mqSize) { return; }
            BNodeFunctions.AddBehavior(inStructure, CreateOrderAdd(inStructure, duration, units, deletionEffect));
        }
    }
}


