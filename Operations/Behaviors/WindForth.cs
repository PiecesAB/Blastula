using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Blastula.Wind;
using Godot;
using Godot.NativeInterop;
using System;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// Applies movement and acceleration that is produced globally by WindSource nodes. 
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/wind.png")]
    public unsafe partial class WindForth : AddBehavior
    {
        [Export] public string channel = "Main";
        /// <summary>
        /// If nonempty, a multiplier for the applied acceleration.
        /// </summary>
        [Export] public string affectStrength = "1";
        /// <summary>
        /// If nonempty, a number in Godot units per second^2 which damps the velocity.
        /// The result is a movement which is slower and more responsive to the pull of the wind field.
        /// </summary>
        [Export] public string drag = "";
        /// <summary>
        /// If nonempty, the velocity magnitude will never exceed this number. 
        /// Velocity is Godot units per second.
        /// </summary>
        [Export] public string maxSpeed = "";
        /// <summary>
        /// If nonempty, velocity will begin in the bullet structure's facing direction,
        /// with this speed in Godot units per second.
        /// </summary>
        [Export] public string initialSpeed = "";
        /// <summary>
        /// If true, the bullet is rotated so that it appears to face in this behavior's movement direction.
        /// </summary>
        [Export] public bool rotateToMatchMovement = true;

        private struct Data
        {
            public int channelNumber;
            public float affectStrength;
            public float drag;
            public float maxSpeed;
            public float initialSpeed;
            public bool needToSetInitialSpeed;
            public Vector2 currentVelocity;
            public bool rotateToMatchMovement;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            BNode* nodePtr = BNodeFunctions.masterQueue + nodeIndex;
            Data* data = (Data*)dataPtr;
            Transform2D oldWorldTransform = BulletWorldTransforms.Get(nodeIndex);
            Vector2 force = BaseWindSource.SampleForce(data->channelNumber, oldWorldTransform.Origin);
            float frameTime = stepSize / Persistent.SIMULATED_FPS;
            float velLength = data->currentVelocity.Length();
            if (data->needToSetInitialSpeed)
            {
                data->needToSetInitialSpeed = false;
                data->currentVelocity = data->initialSpeed * Vector2.FromAngle(oldWorldTransform.Rotation);
            }
            if (data->drag != 0 && velLength > 0)
            {
                float newLength = velLength;
                float dragStep = frameTime * Mathf.Abs(data->drag);
                if (data->drag > 0)
                {
                    newLength = (velLength <= dragStep) ? 0 : (velLength - dragStep);
                }
                else
                {
                    newLength = velLength + dragStep;
                }
                data->currentVelocity = (data->currentVelocity / velLength) * newLength;
                velLength = newLength;
            }
            data->currentVelocity += frameTime * data->affectStrength * force;
            if (velLength > 0 && velLength > data->maxSpeed)
            {
                data->currentVelocity = (data->currentVelocity / velLength) * data->maxSpeed;
            }
            Transform2D newWorldTransform = oldWorldTransform.Translated(frameTime * data->currentVelocity);
            if (data->rotateToMatchMovement)
            {
                newWorldTransform = newWorldTransform.RotatedLocal(data->currentVelocity.Angle() - newWorldTransform.Rotation);
            }
            BulletWorldTransforms.Set(nodeIndex, newWorldTransform);
            return new BehaviorReceipt();
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->channelNumber = BaseWindSource.GetNumberOfChannel(channel);
            dataPtr->affectStrength = 1;
            if (affectStrength != null && affectStrength != "")
            {
                dataPtr->affectStrength = Solve("affectStrength").AsSingle();
            }
            dataPtr->drag = 0;
            if (drag != null && drag != "")
            {
                dataPtr->drag = Solve("drag").AsSingle();
            }
            dataPtr->maxSpeed = float.PositiveInfinity;
            if (maxSpeed != null && maxSpeed != "")
            {
                dataPtr->maxSpeed = Solve("maxSpeed").AsSingle();
            }
            dataPtr->currentVelocity = Vector2.Zero;
            dataPtr->needToSetInitialSpeed = false;
            if (initialSpeed != null && initialSpeed != "") {
                dataPtr->initialSpeed = Solve("initialSpeed").AsSingle();
                dataPtr->needToSetInitialSpeed = true;
            }
            dataPtr->rotateToMatchMovement = rotateToMatchMovement;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        public override void _Ready()
        {
            base._Ready();
        }
    }
}