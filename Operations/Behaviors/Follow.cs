using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// Causes a bullet structure to follow a Target. Can follow smoothly!
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/crosshairYellow.png")]
    public unsafe partial class Follow : AddBehavior
    {
        public enum Mode
        {
            /// <summary>
            /// Sets the transform to the target's transform directly.
            /// </summary>
            Attach,
            /// <summary>
            /// Move exactly at the maxSpeed every frame, or onto the target if close enough to get there in one frame.
            /// </summary>
            Linear,
            /// <summary>
            /// Accelerates to the target. This will overshoot it, creating an orbit or oscillation.
            /// </summary>
            Elastic
        }

        [Export] public Mode mode = Mode.Linear;
        /// <summary>
        /// If defined, follow this particular target (useful for bullets to follow multiple enemies at once.)
        /// </summary>
        [Export] public Target specificTarget;
        /// <summary>
        /// If specificTarget is not defined, the closest target with the given targetName is calculated every frame.
        /// </summary>
        [Export] public string targetName;
        /// <summary>
        /// The fastest we can move to follow something, in units per second.
        /// </summary>
        [Export] public string maxSpeed = "300";
        /// <summary>
        /// If true, the BNode is rotated in the direction of its calculated follow movement.
        /// </summary>
        [Export] public bool rotateStructure = false;
        /// <summary>
        /// If defined, it's a Vector2 with (X, Y) being the (start frame count, end frame count) of following.
        /// </summary>
        [Export] public string followingWindow = "";
        [ExportGroup("Linear")]
        /// <summary>
        /// The distance that the bullet will begin to slow down in its approach.
        /// </summary>
        [Export] public string approachRadius = "0";

        [ExportGroup("Elastic")]
        /// <summary>
        /// With elastic movement, how quickly we can change the speed? In units per second^2.
        /// </summary>
        [Export] public string accel = "300";
        [Export] public string initialVelocity = "Vector2(0, 0)";

        public struct Data
        {
            public Mode mode;
            public Transform2D* specificTarget;
            public int targetID;
            public float maxSpeed;
            public float accel;
            public bool rotateStructure;
            public Vector2 followingWindow;
            public float approachRadius;
            public float currentFrame;
            public Vector2 currentVelocity;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            BNode* nodePtr = BNodeFunctions.masterQueue + nodeIndex;
            Data* data = (Data*)dataPtr;

            if (data->currentFrame < data->followingWindow.X) { data->currentFrame += stepSize; return new BehaviorReceipt(); }
            Transform2D myWorldTransform;
            if (data->currentFrame >= data->followingWindow.Y)
            {
                // In elastic mode, BNode goes straight forward once the window is over. Slowly reaches max speed.
                if (data->mode == Mode.Elastic)
                {
                    myWorldTransform = BulletWorldTransforms.Get(nodeIndex);
                    float currVelocityLength = data->currentVelocity.Length();
                    if (currVelocityLength > data->maxSpeed)
                    {
                        data->currentVelocity = data->maxSpeed * data->currentVelocity.Normalized();
                    }
                    else if (currVelocityLength < data->maxSpeed)
                    {
                        if (currVelocityLength < 0.001f) { data->currentVelocity = 0.001f * Vector2.Right; }
                        float newSpeed = currVelocityLength + Mathf.Abs(0.1f * stepSize * data->accel / Persistent.SIMULATED_FPS);
                        data->currentVelocity = newSpeed * data->currentVelocity.Normalized();
                    }

                    BulletWorldTransforms.Set(
                        nodeIndex,
                        myWorldTransform.Translated(stepSize * data->currentVelocity / Persistent.SIMULATED_FPS)
                    );
                }
                data->currentFrame += stepSize;
                return new BehaviorReceipt();
            }
            bool followingWindowElapsed = data->currentFrame >= data->followingWindow.Y;
            Transform2D targetTransform = Transform2D.Identity;
            myWorldTransform = BulletWorldTransforms.Get(nodeIndex);
            if (data->specificTarget != null) { targetTransform = *(data->specificTarget); }
            else { targetTransform = Target.GetClosest(data->targetID, myWorldTransform.Origin); }

            Vector2 dif = targetTransform.Origin - myWorldTransform.Origin;
            switch (data->mode)
            {
                case Mode.Attach:
                    Transform2D realTargetTransform = new Transform2D(
                        data->rotateStructure ? targetTransform.Rotation : myWorldTransform.Rotation,
                        targetTransform.Origin
                    );
                    BulletWorldTransforms.Set(nodeIndex, realTargetTransform);
                    break;
                case Mode.Linear:
                    {
                        float maxFrameMovement = stepSize * data->maxSpeed / Persistent.SIMULATED_FPS;
                        if (data->approachRadius > 0)
                        {
                            float radiusRemaining = dif.Length() / data->approachRadius;
                            if (radiusRemaining < 1f) { maxFrameMovement *= radiusRemaining; }
                        }
                        if (dif.Length() > maxFrameMovement)
                        {
                            dif = dif.Normalized() * maxFrameMovement;
                        }
                        float newRotation = myWorldTransform.Rotation;
                        if (data->rotateStructure) { newRotation = dif.Angle(); }
                        BulletWorldTransforms.Set(
                            nodeIndex,
                            myWorldTransform.Translated(dif).RotatedLocal(newRotation - myWorldTransform.Rotation)
                        );
                    }
                    break;
                case Mode.Elastic:
                    {
                        float addVelMul = stepSize * data->accel / Persistent.SIMULATED_FPS;
                        data->currentVelocity += addVelMul * dif.Normalized();
                        if (data->currentVelocity.Length() > data->maxSpeed)
                        {
                            data->currentVelocity = data->maxSpeed * data->currentVelocity.Normalized();
                        }
                        float newRotation = myWorldTransform.Rotation;
                        if (data->rotateStructure) { newRotation = data->currentVelocity.Angle(); }
                        BulletWorldTransforms.Set(
                            nodeIndex,
                            myWorldTransform
                                .Translated(stepSize * data->currentVelocity / Persistent.SIMULATED_FPS)
                                .RotatedLocal(newRotation - myWorldTransform.Rotation)
                        );
                    }
                    break;
            }
            data->currentFrame += stepSize;
            return new BehaviorReceipt();
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->mode = mode;
            if (specificTarget != null)
            {
                dataPtr->specificTarget = specificTarget.GetPointerToTransform();
                dataPtr->targetID = -1;
            }
            else
            {
                dataPtr->specificTarget = null;
                dataPtr->targetID = Target.GetNumberFromID(targetName);
            }
            dataPtr->maxSpeed = Solve("maxSpeed").AsSingle();
            if (mode == Mode.Elastic)
            {
                dataPtr->accel = Solve("accel").AsSingle();
                dataPtr->currentVelocity = Solve("initialVelocity").AsVector2();
            }
            dataPtr->rotateStructure = rotateStructure;
            dataPtr->followingWindow = new Vector2(0, float.PositiveInfinity);
            if (followingWindow != null && followingWindow != "") { dataPtr->followingWindow = Solve("followingWindow").AsVector2(); }
            if (mode == Mode.Linear)
            {
                dataPtr->approachRadius = Solve("approachRadius").AsSingle();
            }
            dataPtr->currentFrame = 0;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}