using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// Add bullet behavior that reacts to being out of bounds.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/boundary.png")]
    public unsafe partial class ActOnBoundary : AddBehavior
    {
        public enum SpecialAction
        {
            None, Wrap, Reflect
        }

        /// <summary>
        /// Choose a special boundary-specific interaction.
        /// </summary>
        [Export] public SpecialAction specialAction = SpecialAction.Reflect;
        /// <summary>
        /// The boundary's name.
        /// </summary>
        [Export] public string boundaryID;
        /// <summary>
        /// Delayed operation to perform as soon as the final hit occurs.
        /// </summary>
        [Export] public BaseOperation operation;
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
            public bool reflectPerpendicular;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            Data* data = (Data*)dataPtr;
            if (data->boundInfo == null || data->hitsRemaining == 0) { return new BehaviorReceipt(); }
            Transform2D oldGlobalTransform = BulletWorldTransforms.Get(nodeIndex);
            if (Boundary.IsWithin(data->boundInfo, oldGlobalTransform.Origin, data->shrink)) { return new BehaviorReceipt(); }
            Transform2D oldLocalTransform = BNodeFunctions.masterQueue[nodeIndex].transform;
            Transform2D newGlobalTransform = oldGlobalTransform;
            Vector2 oldWorldPos = oldGlobalTransform.Origin;
            Transform2D leftGlobalToLocal = oldLocalTransform * oldGlobalTransform.AffineInverse();
            switch (data->specialAction)
            {
                case SpecialAction.None:
                default:
                    newGlobalTransform.Origin = Boundary.Clamp(data->boundInfo, oldWorldPos, data->shrink);
                    break;
                case SpecialAction.Wrap:
                    newGlobalTransform.Origin = Boundary.Wrap(data->boundInfo, oldWorldPos, data->shrink);
                    break;
                case SpecialAction.Reflect:
                    float oldRotation = oldGlobalTransform.Rotation;
                    Boundary.ReflectData rdOut = Boundary.Reflect(
                        data->boundInfo,
                        new Boundary.ReflectData { globalPosition = oldWorldPos, rotation = oldRotation },
                        data->shrink, data->reflectPerpendicular
                    );
                    newGlobalTransform.Origin = rdOut.globalPosition;
                    newGlobalTransform = newGlobalTransform.RotatedLocal(rdOut.rotation - oldRotation);
                    break;
            }
            BNodeFunctions.masterQueue[nodeIndex].transform = leftGlobalToLocal * newGlobalTransform;
            data->hitsRemaining--;
            if (data->hitsRemaining == 0 && data->opID >= 0) { 
                PostExecute.ScheduleOperation(nodeIndex, data->opID); 
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
            dataPtr->reflectPerpendicular = reflectPerpendicular;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}
