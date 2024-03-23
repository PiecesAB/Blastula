using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// Aim towards a Target or fixed position. Sometimes we want bullets to actually try and hit something!!!
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/crosshair.png")]
    public unsafe partial class Aim : AddBehavior
    {
        public enum AimMode
        {
            /// <summary>
            /// The first time this behavior executes, instantly aim towards the target.
            /// </summary>
            Instant,
            /// <summary>
            /// This continuously tries to change the angle to arrive at the target.
            /// </summary>
            Homing
        }

        public enum TargetType
        {
            /// <summary>
            /// "target" is the name of a Target node. It will try to aim towards the closest one.
            /// </summary>
            TargetNode,
            /// <summary>
            /// "target" is an expression for a Vector2. It aims toward that point with the reference frame of this structure's parent.
            /// </summary>
            LocalPosition,
            /// <summary>
            /// "target" is an expression for a Vector2. It aims towards that point in world space.
            /// </summary>
            GlobalPosition
        }

        [Export] public AimMode aimMode = AimMode.Instant;
        [Export] public TargetType targetType = TargetType.TargetNode;
        [Export] public string targetName = "Player";
        /// <summary>
        /// Degrees offset, to aim around the target instead of directly toward it.
        /// </summary>
        [Export] public string angularOffset = "0";
        /// <summary>
        /// How many parents up must we go to solve a local position?
        /// </summary> 
        [ExportGroup("Local Position Settings")]
        [Export] public int parentLevel = 1;
        /// <summary>
        /// Degrees per second to rotate towards a target.
        /// </summary>
        [ExportGroup("Homing Settings")]
        [Export] public string homingSpeed = "120";
        /// <summary>
        /// If defined, it's a Vector2 with (X, Y) being the (start frame count, end frame count) of homing.
        /// </summary>
        [Export] public string homingWindow = "Vector2(0, 60)";

        private struct Data
        {
            public AimMode aimMode;

            public int targetID;
            public Vector2 targetPosition;
            public bool targetPositionIsLocal;
            public float angularOffsetRadians;
            public bool aimComplete;
            public int parentLevel;

            public float homingSpeed;
            public Vector2 homingWindow;
            public float currentFrame;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            BNode* nodePtr = BNodeFunctions.masterQueue + nodeIndex;
            Data* data = (Data*)dataPtr;
            if (stepSize == 0 && data->aimMode != AimMode.Instant) { return new BehaviorReceipt(); }
            if (data->aimMode == AimMode.Homing && data->currentFrame >= data->homingWindow.Y) { data->aimComplete = true; }
            if (data->aimComplete) { return new BehaviorReceipt(); }
            Transform2D myWorldTransform = BulletWorldTransforms.Get(nodeIndex);
            Vector2 myWorldPosition = myWorldTransform.Origin;

            bool noTarget = false;
            // Put the target position in world space.
            if (data->targetID >= 0)
            {
                noTarget = Target.GetTargetCount(data->targetID) == 0;
                data->targetPosition = Target.GetClosest(data->targetID, myWorldPosition).Origin;
            }
            else if (data->targetPositionIsLocal)
            {
                int parent = nodeIndex;
                for (int i = 0; i < data->parentLevel; ++i)
                {
                    if (parent >= 0) { parent = BNodeFunctions.masterQueue[parent].parentIndex; }
                }
                if (!nodePtr->worldTransformMode && parent >= 0)
                {
                    data->targetPosition = BulletWorldTransforms.Get(parent) * data->targetPosition;
                }
                data->targetPositionIsLocal = false;
            }

            if (noTarget) { return new BehaviorReceipt(); }

            float currentAngle = myWorldTransform.Rotation;
            float newAngle = 0;
            if (data->aimMode == AimMode.Instant)
            {
                newAngle = (data->targetPosition - myWorldPosition).Angle() + data->angularOffsetRadians;
                BulletWorldTransforms.Set(
                    nodeIndex,
                    myWorldTransform.RotatedLocal(newAngle - currentAngle)
                );
                data->aimComplete = true;
            }
            else if (data->aimMode == AimMode.Homing && data->currentFrame >= data->homingWindow.X)
            {
                float desiredAngle = (data->targetPosition - myWorldPosition).Angle() + data->angularOffsetRadians;
                newAngle = MoreMath.MoveTowardsAngle(
                    currentAngle, desiredAngle,
                    Mathf.DegToRad(data->homingSpeed) * stepSize / Persistent.SIMULATED_FPS
                );
                BulletWorldTransforms.Set(
                    nodeIndex,
                    myWorldTransform.RotatedLocal(newAngle - currentAngle)
                );
            }
            data->currentFrame += stepSize;
            return new BehaviorReceipt();
        }
        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->aimMode = aimMode;
            dataPtr->targetID = -1;
            dataPtr->angularOffsetRadians = Mathf.DegToRad(Solve("angularOffset").AsSingle());
            if (targetType == TargetType.TargetNode)
            {
                dataPtr->targetID = Target.GetNumberFromID(targetName);
            }
            dataPtr->targetPosition = Vector2.Zero;
            if (targetType == TargetType.GlobalPosition || targetType == TargetType.LocalPosition)
            {
                dataPtr->targetPosition = Solve("target").AsVector2();
                dataPtr->targetPositionIsLocal = targetType == TargetType.LocalPosition;
            }
            dataPtr->aimComplete = false;
            dataPtr->parentLevel = parentLevel;
            if (aimMode == AimMode.Homing)
            {
                dataPtr->homingSpeed = Solve("homingSpeed").AsSingle();
                dataPtr->homingWindow = Solve("homingWindow").AsVector2();
                dataPtr->currentFrame = 0;
            }
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}
