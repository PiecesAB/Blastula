using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// Also known as "angular velocity". This behavior makes the bullet structure change its facing direction over time.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/behaviorSpin.png")]
    public unsafe partial class Spin : AddBehavior
    {
        public enum Mode
        {
            /// <summary>
            /// Apply rotation after the current transform, which has the effect of rotating this item in place.
            /// </summary>
            RotateAfter,
            /// <summary>
            /// Apply rotation before the current transform, which has the effect of rotating relative to the parent.
            /// </summary>
            RotateBefore
        }

        [Export()]
        public Mode mode = Mode.RotateAfter;
        /// <summary>
        /// Degrees per second.
        /// </summary>
        [Export()]
        public string speed = "60";


        private struct Data
        {
            public float speed;
            public Mode mode;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            BNode* nodePtr = BNodeFunctions.masterQueue + nodeIndex;
            Data* data = (Data*)dataPtr;
            float rotationAmount = stepSize * (MathF.PI / 180f) * data->speed / VirtualVariables.Persistent.SIMULATED_FPS;
            switch (data->mode)
            {
                case Mode.RotateAfter:
                    nodePtr->transform
                        = nodePtr->transform * new Transform2D(rotationAmount, default);
                    break;
                case Mode.RotateBefore:
                default:
                    nodePtr->transform
                        = new Transform2D(rotationAmount, default) * nodePtr->transform;
                    break;
            }
            return new BehaviorReceipt();
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->speed = Solve("speed").AsSingle();
            dataPtr->mode = mode;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}