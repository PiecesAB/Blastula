using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// A behavior that uses the proximity to target(s) to throttle BNode behaviors that occur after this one (or in children).
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/proximityBlocker.png")]
    public unsafe partial class ProximityBlocker : AddBehavior
    {
        /// <summary>
        /// Determines what causes the proximity blocker to cease blocking.
        /// </summary>
        public enum TriggerMode
        {
            /// <summary>
            /// Trigger condition is met when a target with the given name is close enough.
            /// </summary>
            CloseToTarget,
            /// <summary>
            /// Trigger condition is met when all targets with the given name are distant enough.
            /// </summary>
            FarFromTarget
        }

        [Export] public TriggerMode triggerMode = TriggerMode.CloseToTarget;
        /// <summary>
        /// If true, as soon as the trigger condition is true for one frame, behavior throttling ends forever.
        /// Otherwise, when the trigger condition becomes false again, throttling is resumed.
        /// </summary>
        [Export] public bool oneShot = false;
        /// <summary>
        /// Name of the relevant target(s).
        /// </summary>
        [Export] public string targetID = "Player";
        /// <summary>
        /// The relevant cutoff distance from the target(s) to determine "close" and "far".
        /// </summary>
        [Export] public string distance = "150";
        /// <summary>
        /// Determines which behaviors are affected by this blocking.
        /// </summary>
        [Export] public ThrottleMode throttleMode = ThrottleMode.Next;

        private struct Data
        {
            public TriggerMode triggerMode;
            public bool oneShot;
            public int targetNumber;
            public float distance;
            public bool triggerConditionOnceMet;
            public ThrottleMode throttleMode;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            Data* data = (Data*)dataPtr;
            Transform2D myWorldTransform = BulletWorldTransforms.Get(nodeIndex);
            Vector2 myWorldPosition = myWorldTransform.Origin;
            Transform2D closestTransform = Target.GetClosest(data->targetNumber, myWorldPosition);
            Vector2 closestPosition = closestTransform.Origin;
            float compDistance = (myWorldPosition - closestPosition).Length();
            bool triggerConditionMet = false;
            switch (data->triggerMode)
            {
                case TriggerMode.CloseToTarget:
                    triggerConditionMet = (compDistance <= data->distance);
                    break;
                case TriggerMode.FarFromTarget:
                    triggerConditionMet = (compDistance > data->distance);
                    break;
            }
            if (triggerConditionMet || (data->oneShot && data->triggerConditionOnceMet))
            {
                data->triggerConditionOnceMet = true;
                return new BehaviorReceipt();
            }
            else
            {
                return new BehaviorReceipt { throttle = 1f, throttleMode = data->throttleMode };
            }
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->triggerMode = triggerMode;
            dataPtr->oneShot = oneShot;
            dataPtr->targetNumber = Target.GetNumberFromID(targetID);
            dataPtr->distance = Solve("distance").AsSingle();
            dataPtr->triggerConditionOnceMet = false;
            dataPtr->throttleMode = throttleMode;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}


