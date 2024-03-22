using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// This can be used to delay the existence and function of a bullet,
    /// or add the animation of the bullet appearing to fade into existence.
    /// It can also interpolate color and custom data for Multimeshes.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/butterfly.png")]
    public unsafe partial class Morph : AddBehavior
    {
        /// <summary>
        /// The ID of the appearance during this time.
        /// Leave it empty to not change the appearance.
        /// </summary>
        [Export] public string appearance = "Mist";
        /// <summary>
        /// If true, the appearance will remain after emergence is complete.
        /// </summary>
        [Export] public bool appearancePersists = false;
        /// <summary>
        /// Duration of the wait.
        /// </summary>
        [Export] public string duration = "12";
        [Export] public Wait.TimeUnits durationUnits = Wait.TimeUnits.Frames;
        /// <summary>
        /// Multiplies the scale by this much at the animation's beginning. Shrinks to original size throughout the emergence.
        /// </summary>
        [Export] public float scaleMultiplier = 3f;
        /// <summary>
        /// How much this throttles the following behaviors. 0 = behavior proceeds as normal. 1 = behavior stops.
        /// </summary>
        [Export] public float throttle = 0.8f;
        /// <summary>
        /// We expect a Color here. Interpolates to this over the duration of the behavior. 
        /// </summary>
        [ExportGroup("Multimesh interpolation")]
        [Export] public string multimeshColor = "";
        /// <summary>
        /// We expect a Vector4 here. Interpolates to this over the duration of the behavior. 
        /// </summary>
        [Export] public string multimeshCustom = "";
        /// <summary>
        /// Whether this throttles on the last frame before it ends.
        /// </summary>
        [ExportGroup("Advanced")]
        [Export] public bool throttleOnEndFrame = false;
        

        private int storedRenderID;
        private bool mistAppearanceDirty = true;

        public enum State
        {
            Unstarted, Playing, Complete
        }

        public struct Data
        {
            public float throttle;
            public bool throttleOnEndFrame;

            public State state;
            public float duration;
            public float currentTime;

            public int tempRenderID;
            public int origRenderID;
            public bool appearancePersists;

            public float scaleMultiplier;
            public Vector2 origScale;

            public bool interpolateColor;
            public Color origColor;
            public Color targetColor;
            public bool interpolateCustom;
            public Vector4 origCustom;
            public Vector4 targetCustom;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize <= 0) { return new BehaviorReceipt(); }
            BNode* nodePtr = masterQueue + nodeIndex;
            Data* data = (Data*)dataPtr;
            switch (data->state)
            {
                case State.Unstarted:
                default:
                    // Begin the emergence.
                    data->origScale = nodePtr->transform.Scale;
                    data->origRenderID = nodePtr->bulletRenderID;
                    if (data->tempRenderID >= 0)
                    {
                        BulletRenderer.SetRenderID(nodeIndex, data->tempRenderID);
                    }
                    if (data->scaleMultiplier != 1)
                    {
                        nodePtr->transform = nodePtr->transform.ScaledLocal(
                            new Vector2(data->scaleMultiplier, data->scaleMultiplier)
                        );
                    }
                    if ((data->interpolateColor || data->interpolateCustom) && nodePtr->multimeshExtras == null)
                    {
                        nodePtr->multimeshExtras = SetMultimeshExtraData.NewPointer();
                    }
                    if (nodePtr->multimeshExtras != null)
                    {
                        data->origColor = nodePtr->multimeshExtras->color;
                        data->origCustom = nodePtr->multimeshExtras->custom;
                    }
                    data->state = State.Playing;
                    return new BehaviorReceipt { throttle = data->throttle };
                case State.Playing:
                    float progress = data->currentTime / data->duration;
                    float oldDesiredScale = Mathf.Lerp(data->scaleMultiplier, 1, progress);
                    data->currentTime += stepSize;
                    progress = data->currentTime / data->duration;
                    float newDesiredScale = Mathf.Lerp(data->scaleMultiplier, 1, progress);
                    float ratio = newDesiredScale / oldDesiredScale;
                    if (data->currentTime >= data->duration)
                    {
                        // Finish up.
                        ratio = 1 / oldDesiredScale;
                        if (data->scaleMultiplier != 1)
                        {
                            nodePtr->transform = nodePtr->transform.ScaledLocal(new Vector2(ratio, ratio));
                        }
                        if (!data->appearancePersists)
                        {
                            BulletRenderer.SetRenderID(nodeIndex, data->origRenderID);
                        }
                        if (nodePtr->multimeshExtras != null)
                        {
                            if (data->interpolateColor)
                            {
                                nodePtr->multimeshExtras->color = data->targetColor;
                            }
                            if (data->interpolateCustom)
                            {
                                nodePtr->multimeshExtras->custom = data->targetCustom;
                            }
                        }
                        data->state = State.Complete;
                        return data->throttleOnEndFrame ? new BehaviorReceipt { throttle = data->throttle } : new BehaviorReceipt();
                    }
                    else
                    {
                        // Continue interpolating.
                        if (data->scaleMultiplier != 1)
                        {
                            nodePtr->transform = nodePtr->transform.ScaledLocal(new Vector2(ratio, ratio));
                        }
                        if (nodePtr->multimeshExtras != null)
                        {
                            if (data->interpolateColor)
                            {
                                nodePtr->multimeshExtras->color = data->origColor.Lerp(data->targetColor, progress);
                            }
                            if (data->interpolateCustom)
                            {
                                nodePtr->multimeshExtras->custom = data->origCustom.Lerp(data->targetCustom, progress);
                            }
                        }
                    }
                    return new BehaviorReceipt { throttle = data->throttle };
                case State.Complete:
                    return new BehaviorReceipt();
            }
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));

            dataPtr->throttle = throttle;
            dataPtr->throttleOnEndFrame = throttleOnEndFrame;

            float duration = Solve("duration").AsSingle();
            dataPtr->state = (duration <= 0) ? State.Complete : State.Unstarted;
            dataPtr->duration = duration;
            if (durationUnits == Wait.TimeUnits.Seconds) { dataPtr->duration *= Persistent.SIMULATED_FPS; }
            dataPtr->currentTime = 0;

            if (mistAppearanceDirty)
            {
                storedRenderID = BulletRendererManager.GetIDFromName(appearance);
                mistAppearanceDirty = false;
            }
            dataPtr->tempRenderID = storedRenderID;
            dataPtr->appearancePersists = appearancePersists;

            dataPtr->scaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.001f, 1000f);

            dataPtr->interpolateColor = (multimeshColor != null && multimeshColor != "");
            if (dataPtr->interpolateColor)
            {
                dataPtr->targetColor = Solve("multimeshColor").AsColor();
            }
            dataPtr->interpolateCustom = (multimeshCustom != null && multimeshCustom != "");
            if (dataPtr->interpolateCustom)
            {
                dataPtr->targetCustom = Solve("multimeshCustom").AsVector4();
            }

            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}
