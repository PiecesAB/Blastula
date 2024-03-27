using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// After the wait time elapses or the condition becomes true (whichever is earlier),
    /// the bullet structure will cause an operation to be applied to itself.
    /// </summary>
    /// <remarks>
    /// It will only adapt once, and never again. For more advanced adaptive behavior, use AdaptPattern.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/adapt.png")]
    public unsafe partial class Adapt : AddBehavior
    {
        [Export] public BaseOperation operation;
        /// <summary>
        /// If a nonempty expression, this condition is updated every frame to a boolean value.
        /// The behavior will trigger when it is true.
        /// </summary>
        /// <remarks>
        /// This evaluates to "false" when empty.
        /// </remarks>
        [Export] public string condition = "";
        /// <summary>
        /// The operation will apply after this amount of time.
        /// </summary>
        [Export] public string wait = "120";
        [Export] public Wait.TimeUnits waitUnits = Wait.TimeUnits.Frames;
        /// <summary>
        /// Optional blastodisc from which to use local variables.
        /// </summary>
        [Export] public Blastodisc blastodisc;

        public bool* conditionValue = null;

        private struct Data
        {
            public bool useCondition;
            public bool* conditionValue;
            public long conditionOpID;

            public float wait;
            public float currentTime;
            public long scheduledOpID;
            public bool queued;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            Data* data = (Data*)dataPtr;
            if (data->queued) { return new BehaviorReceipt(); }
            data->currentTime += stepSize;
            // We must ensure the operation still exists, while checking for the condition value.
            // If not then we'd dereference the freed pointer, which is potentially Very Bad.
            bool conditionMet = false;
            if (data->useCondition && OperationIDExists(data->conditionOpID))
            {
                conditionMet = *(data->conditionValue);
            }
            if (data->currentTime >= data->wait || conditionMet)
            {
                PostExecute.ScheduleOperation(nodeIndex, data->scheduledOpID);
                data->queued = true;
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
            dataPtr->wait = float.PositiveInfinity;
            if (wait != null && wait != "")
            {
                dataPtr->wait = Solve("wait").AsSingle();
                if (waitUnits == Wait.TimeUnits.Seconds) { dataPtr->wait *= 60f; }
            }
            dataPtr->currentTime = 0;
            dataPtr->scheduledOpID = operation?.GetOperationID() ?? -1;
            dataPtr->queued = false;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            conditionValue = (bool*)Marshal.AllocHGlobal(sizeof(bool));
            *conditionValue = false;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (conditionValue != null)
            {
                Marshal.FreeHGlobal((System.IntPtr)conditionValue);
                conditionValue = null;
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


