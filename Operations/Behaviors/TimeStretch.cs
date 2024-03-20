using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// This alters the speed of time itself... over time. It's weird but it may be useful.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/timeStretch.png")]
    public unsafe partial class TimeStretch : AddBehavior
    {
        [Export] public Curve curve;
        [Export] public string startTime = "0";
        [Export] public string endTime = "60";
        [Export] public Wait.TimeUnits units = Wait.TimeUnits.Frames;
        [Export] public UnsafeCurve.LoopMode loopMode = UnsafeCurve.LoopMode.Neither;
        [ExportGroup("Advanced")]
        [Export] public string curveShift = "";
        [Export] public bool reciprocated = false;
        [Export] public float stepFrames = 3f;

        private UnsafeCurve* bakedCurve;
        private bool curveHasBeenBaked = false;

        public struct Data
        {
            public UnsafeCurve* curve;
            public long curveSourceOperation;
            public float currentTime;
            public bool reciprocated;
            public Vector4 curveShift;
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

        public void BakeCurveIfNeeded()
        {
            if (curveHasBeenBaked) { return; }
            float st = Solve("startTime").AsSingle();
            float et = Solve("endTime").AsSingle();
            if (units == Wait.TimeUnits.Seconds) { st *= 60f; et *= 60f; }
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
            return new BehaviorReceipt { throttle = 1f - timeMultiplier };
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
            if (curveShift != null && curveShift != "")
            {
                float[] csl = Solve("curveShift").AsFloat32Array();
                dataPtr->curveShift = new Vector4(csl[0], csl[1], csl[2], csl[3]);
            }
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }
    }
}
