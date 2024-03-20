using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Add a trail of bullets that mimic the head bullet exactly. Also creates lasers.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/trail.png")]
    public unsafe partial class Trail : BaseOperation
    {
        /// <summary>
        /// The amount of bullets in the trail.
        /// </summary>
        [Export] public string number = "8";
        /// <summary>
        /// The number of frames between each bullet in the trail.
        /// </summary>
        [Export] public string frameDelay = "2";
        [Export] public string laserRenderName = "None";

        public struct Data
        {
            public bool initializedDefaultTransform;
            public Transform2D defaultTransform;
            public UnsafeArray<Transform2D> worldPosBuffer;
            public bool filled;
            public int currentUnscaledFrame;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            int parentIndex = masterQueue[nodeIndex].parentIndex;
            if (parentIndex < 0) { return new BehaviorReceipt(); }
            Data* data = (Data*)dataPtr;
            Transform2D parentCurrGlobalTransform = BulletWorldTransforms.Get(parentIndex);
            if (data->currentUnscaledFrame < 0)
            {
                if (!data->initializedDefaultTransform)
                {
                    if (!masterQueue[nodeIndex].worldTransformMode)
                    {
                        masterQueue[nodeIndex].transform = Transform2D.Identity;
                    }
                    BulletWorldTransforms.Invalidate(nodeIndex);
                    data->defaultTransform = masterQueue[nodeIndex].transform = BulletWorldTransforms.Get(nodeIndex);
                    masterQueue[nodeIndex].worldTransformMode = true;
                    BulletWorldTransforms.Invalidate(nodeIndex);
                    data->initializedDefaultTransform = true;
                }
                masterQueue[nodeIndex].transform = data->defaultTransform;
                data->currentUnscaledFrame = data->currentUnscaledFrame + 1;
                return new BehaviorReceipt();
            }
            int currBufferPos = data->currentUnscaledFrame % data->worldPosBuffer.count;
            if (data->filled)
            {
                masterQueue[nodeIndex].transform = data->worldPosBuffer[currBufferPos];
            }
            else
            {
                if (!data->initializedDefaultTransform)
                {
                    if (!masterQueue[nodeIndex].worldTransformMode)
                    {
                        masterQueue[nodeIndex].transform = Transform2D.Identity;
                    }
                    BulletWorldTransforms.Invalidate(nodeIndex);
                    data->defaultTransform = masterQueue[nodeIndex].transform = BulletWorldTransforms.Get(nodeIndex);
                    masterQueue[nodeIndex].worldTransformMode = true;
                    BulletWorldTransforms.Invalidate(nodeIndex);
                    data->initializedDefaultTransform = true;
                }
                masterQueue[nodeIndex].transform = data->defaultTransform;
            }
            BulletWorldTransforms.Invalidate(nodeIndex);
            data->worldPosBuffer[data->currentUnscaledFrame] = parentCurrGlobalTransform;
            data->currentUnscaledFrame = (data->currentUnscaledFrame + 1) % data->worldPosBuffer.count;
            if (data->currentUnscaledFrame == 0 && !data->filled) { data->filled = true; }
            return new BehaviorReceipt();
        }

        public static void* CloneData(void* dataPtr)
        {
            Data* oldData = (Data*)dataPtr;
            Data* newData = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            newData->currentUnscaledFrame = oldData->currentUnscaledFrame;
            newData->filled = oldData->filled;
            newData->initializedDefaultTransform = oldData->initializedDefaultTransform;
            newData->defaultTransform = oldData->defaultTransform;
            newData->worldPosBuffer = oldData->worldPosBuffer.Clone();
            return newData;
        }

        public static void DisposeData(void* dataPtr)
        {
            if (dataPtr == null) { return; }
            ((Data*)dataPtr)->worldPosBuffer.Dispose();
        }

        public BehaviorOrder CreateOrder(int frameDelay, int startFrame)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->worldPosBuffer = UnsafeArrayFunctions.Create<Transform2D>(frameDelay);
            dataPtr->initializedDefaultTransform = false;
            dataPtr->filled = false;
            dataPtr->currentUnscaledFrame = startFrame;
            return new BehaviorOrder()
            {
                data = dataPtr,
                dataSize = sizeof(Data),
                func = &Execute,
                clone = &CloneData,
                dispose = &DisposeData
            };
        }

        public override int ProcessStructure(int inStructure)
        {
            if (inStructure < 0) { return -1; }
            if (masterQueue[inStructure].children.count > 0) { return inStructure; }
            int number = Solve("number").AsInt32();
            int newBNodes = MasterQueuePopN(number);
            if (newBNodes < 0) { MasterQueuePushTree(inStructure); return -1; }
            int frameDelay = Solve("frameDelay").AsInt32();
            for (int i = 0; i < number; ++i)
            {
                int j = (newBNodes + i) % mqSize;
                BulletRenderer.SetRenderID(j, masterQueue[inStructure].bulletRenderID);
                masterQueue[j].transform = Transform2D.Identity;
                masterQueue[j].collisionLayer = masterQueue[inStructure].collisionLayer;
                masterQueue[j].collisionSleepStatus = masterQueue[inStructure].collisionSleepStatus;
                masterQueue[j].health = masterQueue[inStructure].health;
                masterQueue[j].power = masterQueue[inStructure].power;
                int prevJ = (newBNodes + i - 1) % mqSize;
                if (i == 0) { SetChild(inStructure, 0, j); }
                else { SetChild(prevJ, 0, j); }
                BNodeFunctions.AddBehavior(j, CreateOrder(frameDelay, -frameDelay * i));
            }
            int laserRenderID = LaserRendererManager.GetIDFromName(laserRenderName);
            if (laserRenderID >= 0)
            {
                LaserRenderer.SetLaserFromHead(inStructure, laserRenderID);
            }
            return inStructure;
        }
    }
}
