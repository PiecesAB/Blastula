using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// This alters the flow of time in further behaviors of this BNode, and behaviors of children.
    /// </summary>
    /// <remarks>
    /// It's a design decision to not have built-in direct movement by a curve.
    /// Usually, the ways we want bullets to move aren't complex enough to warrant that.
    /// This thought may change in the future.
    /// </remarks>
    /// <example>
    /// If this bullet structure has a series of behaviors as [Forth --&gt; TimeStretch --&gt; Spin] in order, you can 
    /// make time oscillate forward and backward to create an oscillating movement path.
    /// </example>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/timeStretch.png")]
    public unsafe partial class TimeStretch : AddBehavior
    {
        [Export] public Curve curve;
        /// <summary>
        /// The time at the start point of the curve, seen in the editor as the left side of the curve.
        /// </summary>
        /// <remarks>
        /// Godot only likes curves with a domain of [0, 1], which is why this variable has to exist.
        /// </remarks>
        [Export] public string startTime = "0";
        /// <summary>
        /// The time at the end point of the curve, seen in the editor as the right side of the curve.
        /// </summary>
        /// <remarks>
        /// Godot only likes curves with a domain of [0, 1], which is why this variable has to exist.
        /// </remarks>
        [Export] public string endTime = "60";
        [Export] public Wait.TimeUnits units = Wait.TimeUnits.Frames;
        [Export] public UnsafeCurve.LoopMode loopMode = UnsafeCurve.LoopMode.Neither;
        /// <summary>
        /// Determines which behaviors are affected by this time stretching.
        /// </summary>
        [Export] public ThrottleMode throttleMode = ThrottleMode.Full;
        /// <summary>
        /// An array of four numbers [a, b, c, d] that stretches or shifts the curve.
        /// Suppose that the curve is a function F. Normally, we are calculating F(t) to determine the time flow multiplier.
        /// But if curveShift is defined, we will instead determine the time flow as cF(at + b) + d.
        /// </summary>
        [ExportGroup("Advanced")]
        [Export] public string curveShift = "";
        /// <summary>
        /// Sets the time flow to 1/F(t) instead of F(t).
        /// </summary>
        /// <remarks>
        /// Be aware that because dividing by zero sucks, the time flow will remain 0 when F(t) == 0.
        /// </remarks>
        [Export] public bool reciprocated = false;
        /// <summary>
        /// Determines the resolution of the baked curve.
        /// This is approximately the interval between points which are then linearly interpolated.
        /// </summary>
        [Export] public float stepFrames = 3f;

        private UnsafeCurve* bakedCurve;
        private bool curveHasBeenBaked = false;

        private struct Data
        {
            public UnsafeCurve* curve;
            public long curveSourceOperation;
            public float currentTime;
            public bool reciprocated;
            public Vector4 curveShift;
            public ThrottleMode throttleMode;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (curveHasBeenBaked)
            {
                bakedCurve->Dispose();
                Marshal.FreeHGlobal((System.IntPtr)bakedCurve);
            }
        }

        private void BakeCurveIfNeeded()
        {
            if (curveHasBeenBaked) { return; }
            float st = Solve("startTime").AsSingle();
            float et = Solve("endTime").AsSingle();
            if (units == Wait.TimeUnits.Seconds) { st *= Persistent.SIMULATED_FPS; et *= Persistent.SIMULATED_FPS; }
            bakedCurve = UnsafeCurveFunctions.Create(curve, st, et, loopMode, stepFrames);
            curveHasBeenBaked = true;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            Data* data = (Data*)dataPtr;
            if (!OperationIDExists(data->curveSourceOperation)) { return new BehaviorReceipt(); }
            BNode* nodePtr = masterQueue + nodeIndex;
            data->currentTime += stepSize;
            float x = data->curveShift[0] * data->currentTime + data->curveShift[1];
            float timeMultiplier = data->curveShift[2] * data->curve->Evaluate(x) + data->curveShift[3];
            if (data->reciprocated && timeMultiplier != 0) { timeMultiplier = 1f / timeMultiplier; }
            return new BehaviorReceipt { throttle = 1f - timeMultiplier, throttleMode = data->throttleMode };
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            BakeCurveIfNeeded();
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->curve = bakedCurve;
            dataPtr->curveSourceOperation = GetOperationID();
            dataPtr->currentTime = 0;
            dataPtr->reciprocated = reciprocated;
            dataPtr->curveShift = new Vector4(1, 0, 1, 0);
            dataPtr->throttleMode = throttleMode;
            if (curveShift != null && curveShift != "")
            {
                float[] csl = Solve("curveShift").AsFloat32Array();
                dataPtr->curveShift = new Vector4(csl[0], csl[1], csl[2], csl[3]);
            }
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}
