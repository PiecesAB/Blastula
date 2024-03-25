using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// Advanced adaptation. Uses the child operations of this Node as a list to apply in a pattern.
    /// </summary>
    /// <remarks>
    /// If you set "repeatMode" to random, beware of desyncing replays! 
    /// Multithreading can cause the randomness functions to be called in unknown order.
    /// To mitigate this, use StunMultithreading.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/adapt.png")]
    public unsafe partial class AdaptPattern : AddBehavior
    {
        [Export] public Repaint.PatternMode repeatMode = Repaint.PatternMode.Loop;
        /// <summary>
        /// Adaptation will occur every frame the condition is true, using the wait time as the minimum duration between triggers.
        /// </summary>
        /// <remarks>
        /// Note there is no beginning wait period: the adaptation can occur on the first frame.
        /// This evaluates to "true" when empty, unlike in Adapt.
        /// </remarks>
        [Export] public string condition = "true";
        /// <summary>
        /// The operation will apply after this amount of time.
        /// </summary>
        [Export] public string wait = "60";
        [Export] public Wait.TimeUnits waitUnits = Wait.TimeUnits.Frames;
        /// <summary>
        /// Adds to list indices when determining the pattern.
        /// </summary>
        [Export] public string startOffset = "0";
        /// <summary>
        /// Optional blastodisc from which to use local variables.
        /// </summary>
        [Export] public Blastodisc blastodisc;

        private int operationListLength = 0;
        public bool* conditionValue = null;
        public long* operationIdList = null;

        private struct Data
        {
            public bool useCondition;
            public bool* conditionValue;
            public long* opIDList;
            public int opIdListLength;
            public int opIdOffset;
            public Repaint.PatternMode repeatMode;
            public long conditionOpID;

            public int counter;
            public float wait;
            public float currentTime;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            Data* data = (Data*)dataPtr;
            data->currentTime += stepSize;
            if (data->currentTime >= data->wait) { data->currentTime = data->wait; }
            // We must ensure the operation still exists, while checking for the condition value.
            // If not then we'd dereference the freed pointer, which is potentially Very Bad.
            bool conditionMet = true;
            bool pointersValid = OperationIDExists(data->conditionOpID);
            if (data->useCondition && pointersValid)
            {
                conditionMet = *(data->conditionValue);
            }
            if (conditionMet && pointersValid && data->currentTime >= data->wait)
            {
                int index = Repaint.SolvePatternIndex(
                    data->counter + data->opIdOffset,
                    data->opIdListLength,
                    data->repeatMode
                );
                if (index >= 0 && index < data->opIdListLength) {
                    PostExecute.ScheduleOperation(nodeIndex, data->opIDList[index]);
                }
                else
                {
                    PostExecute.ScheduleOperation(nodeIndex, -1);
                }
                data->currentTime = 0;
                data->counter += 1;
            }
            return new BehaviorReceipt();
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->useCondition = false;
            if (condition != null && condition != "")
            {
                dataPtr->useCondition = true;
                dataPtr->conditionOpID = GetOperationID();
                *conditionValue = Solve("condition").AsBool();
            }
            dataPtr->conditionValue = conditionValue;
            dataPtr->opIDList = operationIdList;
            dataPtr->opIdListLength = operationListLength;
            dataPtr->opIdOffset = 0;
            dataPtr->repeatMode = repeatMode;
            if (startOffset != null && startOffset != "")
            {
                dataPtr->opIdOffset = Solve("startOffset").AsInt32();
            }
            dataPtr->wait = 0;
            if (wait != null && wait != "")
            {
                dataPtr->wait = Solve("wait").AsSingle();
                if (waitUnits == Wait.TimeUnits.Seconds) { dataPtr->wait *= 60f; }
            }
            dataPtr->counter = 0;
            dataPtr->currentTime = dataPtr->wait;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        public override void _Ready()
        {
            base._Ready();
            conditionValue = (bool*)Marshal.AllocHGlobal(sizeof(bool));
            *conditionValue = false;
            operationListLength = GetChildCount();
            operationIdList = (long*)Marshal.AllocHGlobal(sizeof(long) * operationListLength);
            for (int i = 0; i < operationListLength; ++i)
            {
                if (GetChild(i) is BaseOperation)
                {
                    operationIdList[i] = ((BaseOperation)GetChild(i)).GetOperationID();
                }
                else
                {
                    operationIdList[i] = -1;
                }
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (conditionValue != null)
            {
                Marshal.FreeHGlobal((IntPtr)conditionValue);
                conditionValue = null;
            }
            if (operationIdList != null)
            {
                Marshal.FreeHGlobal((IntPtr)operationIdList);
                operationIdList = null;
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (condition != null && condition != "")
            {
                IVariableContainer oldBD = ExpressionSolver.currentLocalContainer;
                ExpressionSolver.currentLocalContainer = blastodisc;
                *conditionValue = Solve("condition").AsBool();
                ExpressionSolver.currentLocalContainer = oldBD;
            }
        }
    }
}
