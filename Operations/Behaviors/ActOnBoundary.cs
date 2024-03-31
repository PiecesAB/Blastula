using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// Add bullet behavior that reacts to being out of bounds.
    /// </summary>
    /// <remarks>
    /// At the moment, this assumes the boundary will always exist while the reacting bullets exist.
    /// So if the boundary disappears, bullets will try to reference freed memory, which is Very Bad.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/boundary.png")]
    public unsafe partial class ActOnBoundary : AddBehavior
    {
        public enum SpecialAction
        {
            None, Wrap, Reflect, Delete, DeleteWithEffect
        }

        /// <summary>
        /// The boundary's name.
        /// </summary>
        [Export] public string boundaryID;
        /// <summary>
        /// Choose a special boundary-specific interaction.
        /// </summary>
        [Export] public SpecialAction specialAction = SpecialAction.Reflect;
        /// <summary>
        /// Delayed operation to perform as soon as the final hit occurs.
        /// </summary>
        [Export] public BaseOperation operation;
        /// <summary>
        /// If the boundary is rectangular, choose which sides cause actions.
        /// </summary>
        [Export] public Boundary.ActiveSide activeSides = (Boundary.ActiveSide)15;
        /// <summary>
        /// The number of hits to execute with this boundary.
        /// </summary>
        [Export] public string hits = "1";
        /// <summary>
        /// The amount by which the boundary is shrunk. 
        /// Useful to make bullets appear to bounce at the edges instead of the center.
        /// </summary>
        [Export] public float shrink = 0;
        /// <summary>
        /// If true, reflection will make the target structure rotate perpendicularly to the wall,
        /// instead of in the natural direction.
        /// </summary>
        [Export] public bool reflectPerpendicular = false;

        private struct Data
        {
            public Boundary.LowLevelInfo* boundInfo;
            public SpecialAction specialAction;
            public int hitsRemaining;
            public long opID;
            public float shrink;
            public Boundary.ActiveSide activeSides;
            public bool reflectPerpendicular;
            public bool deletionQueued;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            Data* data = (Data*)dataPtr;
            if (data->boundInfo == null || data->hitsRemaining == 0 || data->deletionQueued) 
            { 
                return new BehaviorReceipt(); 
            }
            Transform2D oldGlobalTransform = BulletWorldTransforms.Get(nodeIndex);
            if (Boundary.IsWithin(data->boundInfo, oldGlobalTransform.Origin, data->shrink, data->activeSides)) 
            { 
                return new BehaviorReceipt(); 
            }
            Transform2D oldLocalTransform = BNodeFunctions.masterQueue[nodeIndex].transform;
            Transform2D newGlobalTransform = oldGlobalTransform;
            Vector2 oldWorldPos = oldGlobalTransform.Origin;
            Transform2D leftGlobalToLocal = oldLocalTransform * oldGlobalTransform.AffineInverse();
            switch (data->specialAction)
            {
                case SpecialAction.None:
                default:
                    newGlobalTransform.Origin = Boundary.Clamp(data->boundInfo, oldWorldPos, data->shrink, data->activeSides);
                    break;
                case SpecialAction.Wrap:
                    newGlobalTransform.Origin = Boundary.Wrap(data->boundInfo, oldWorldPos, data->shrink);
                    break;
                case SpecialAction.Reflect:
                    float oldRotation = oldGlobalTransform.Rotation;
                    Boundary.ReflectData rdOut = Boundary.Reflect(
                        data->boundInfo,
                        new Boundary.ReflectData { globalPosition = oldWorldPos, rotation = oldRotation },
                        data->shrink, data->reflectPerpendicular, data->activeSides
                    );
                    newGlobalTransform.Origin = rdOut.globalPosition;
                    newGlobalTransform = newGlobalTransform.RotatedLocal(rdOut.rotation - oldRotation);
                    break;
                case SpecialAction.Delete:
                    if (!data->deletionQueued)
                    {
                        data->deletionQueued = true;
                        PostExecute.ScheduleDeletion(nodeIndex, false);
                    }
                    break;
                case SpecialAction.DeleteWithEffect:
                    if (!data->deletionQueued)
                    {
                        data->deletionQueued = true;
                        PostExecute.ScheduleDeletion(nodeIndex, true);
                    }
                    break;
            }
            if (!data->deletionQueued)
            {
                BNodeFunctions.masterQueue[nodeIndex].transform = leftGlobalToLocal * newGlobalTransform;
                data->hitsRemaining--;
                if (data->hitsRemaining == 0 && data->opID >= 0)
                {
                    PostExecute.ScheduleOperation(nodeIndex, data->opID);
                }
            }
            return new BehaviorReceipt();
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            Boundary boundary = Boundary.boundaryFromID.ContainsKey(boundaryID) ? Boundary.boundaryFromID[boundaryID] : null;
            dataPtr->boundInfo = null;
            if (boundary != null) { dataPtr->boundInfo = boundary.lowLevelInfo; }
            dataPtr->specialAction = specialAction;
            dataPtr->hitsRemaining = Solve("hits").AsInt32();
            dataPtr->opID = operation?.GetOperationID() ?? -1;
            dataPtr->shrink = shrink;
            dataPtr->activeSides = activeSides;
            dataPtr->reflectPerpendicular = reflectPerpendicular;
            dataPtr->deletionQueued = false;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}
