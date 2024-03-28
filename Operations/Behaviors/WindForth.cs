using Blastula.LowLevel;
using Blastula.VirtualVariables;
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
        public int channel = 0;
        public string affectStrength = "1";
        public string affectRotation = "0";
        public string drag = "";
        public string maxSpeed = "";
        public string initialSpeed = "";

        private struct Data
        {
            public float strengthMultiplier;
            public float directionOffset;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            if (stepSize == 0) { return new BehaviorReceipt(); }
            BNode* nodePtr = BNodeFunctions.masterQueue + nodeIndex;
            Data* data = (Data*)dataPtr;
            return new BehaviorReceipt();
        }

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            dataPtr->strengthMultiplier = Solve("strengthMultiplier").AsSingle();
            dataPtr->directionOffset = Solve("directionOffset").AsSingle();
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        public override void _Ready()
        {
            base._Ready();
        }
    }
}