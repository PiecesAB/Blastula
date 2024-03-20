using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// Move forward. The simplest behavior for all bullets. Ubiquitious in all bullet games!
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/behaviorForth.png")]
    public unsafe partial class Forth : AddBehavior
    {
        public enum Mode
        {
            /// <summary>
            /// Apply movement after the current transform, relative to this object's own space.
            /// </summary>
            MoveAfter,
            /// <summary>
            /// Apply movement before the current transform, relative to the parent.
            /// </summary>
            MoveBefore
        }

        [Export()]
        public Mode mode = Mode.MoveAfter;
        /// <summary>
        /// Forward movement speed in Godot units per second.
        /// </summary>
        [Export()]
        public string speed = "100";
        /// <summary>
        /// Puts the BNode in world space instead of local space, which avoids matrix multiplications.
        /// Useful if you want to squeeze out ridiculous performance, but the BNode will now ignore its parent.
        /// So this hack works for only simple movement.
        /// </summary>
        [ExportGroup("Advanced")]
        [Export()]
        public bool worldSpaceHack = false;

        public struct Data
        {
            public float speed;
            public Mode mode;
            public bool useWorldSpaceHack;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            BNode* nodePtr = BNodeFunctions.masterQueue + nodeIndex;
            Data* data = (Data*)dataPtr;
            float forwardAmount = stepSize * data->speed / Persistent.SIMULATED_FPS;
            if (data->useWorldSpaceHack)
            {
                if (!nodePtr->worldTransformMode)
                {
                    BulletWorldTransforms.Invalidate(nodeIndex);
                    nodePtr->transform = BulletWorldTransforms.Get(nodeIndex);
                    nodePtr->worldTransformMode = true;
                    BulletWorldTransforms.Invalidate(nodeIndex);
                }
                nodePtr->transform.Origin += forwardAmount * Vector2.FromAngle(nodePtr->transform.Rotation);
            }
            else
            {
                switch (data->mode)
                {
                    case Mode.MoveAfter:
                        nodePtr->transform
                            = nodePtr->transform * new Transform2D(0, new Vector2(forwardAmount, 0));
                        break;
                    case Mode.MoveBefore:
                    default:
                        nodePtr->transform
                            = new Transform2D(0, new Vector2(forwardAmount, 0)) * nodePtr->transform;
                        break;
                }
            }
            return new BehaviorReceipt();
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->speed = Solve("speed").AsSingle();
            dataPtr->mode = mode;
            dataPtr->useWorldSpaceHack = worldSpaceHack;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        private static BehaviorOrder CreateOrderAdd(int inStructure, float speed, Mode mode)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->speed = speed;
            dataPtr->mode = mode;
            dataPtr->useWorldSpaceHack = false;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        public static void Add(int inStructure, float speed, Mode mode)
        {
            if (inStructure < 0 || inStructure >= BNodeFunctions.mqSize) { return; }
            BNodeFunctions.AddBehavior(inStructure, CreateOrderAdd(inStructure, speed, mode));
        }
    }
}
