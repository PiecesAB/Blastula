using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Connects the children of a structure with lines of same bullets. Literally "connect the dots"!
    /// Also interpolates rotation and scale.<br />
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/connect.png")]
    public unsafe partial class Connect : BaseOperation
    {
        public enum Structure
        {
            /// <summary>
            /// The result has the same depth.<br />
            /// It is continuous in the order of connection.
            /// </summary>
            Flat,
            /// <summary>
            /// The result is a deeper structure.<br />
            /// Each line is a child of the result, and includes the start and end points.<br />
            /// This means most or all of the original points are doubled up.
            /// </summary>
            SeparateLines
        }

        public enum LineType
        {
            /// <summary>
            /// The normal way of connecting the dots, with straight lines.
            /// </summary>
            Line,
            /// <summary>
            /// The circular/polar way of connecting the dots, lerping radius and angle.
            /// </summary>
            Scrimble
        }

        [Export] public Structure structure = Structure.Flat;
        [Export] public LineType lineType = LineType.Line;
        /// <summary>
        /// The number of bullets that fill out each line.
        /// </summary>
        [Export] public string number = "8";
        /// <summary>
        /// If true, the last item will be connected to the first.
        /// </summary>
        [Export] public bool circular;

        private Transform2D LerpTransforms(Transform2D a, Transform2D b, float t)
        {
            switch (lineType)
            {
                case LineType.Line:
                default:
                    return a.Lerp(b, t);
                case LineType.Scrimble:
                    return a.Slerp(b, t);
            }
        }

        private void EmergencyDelete(UnsafeArray<int> children, int root)
        {
            for (int j = 0; j < children.count; ++j)
            {
                if (children[j] < 0 || children[j] >= mqSize) { continue; }
                MasterQueuePushTree(children[j]);
            }
            // It's ok to free some children twice. There's a failsafe...
            MasterQueuePushTree(root);
        }

        private int ProcessFlat(int number, int inStructure)
        {
            if (number == 1) { return inStructure; }
            UnsafeArray<int> origChildren = masterQueue[inStructure].children.Clone();
            for (int j = 0; j < origChildren.count; ++j) { SetChild(inStructure, j, -1); }
            int newChildCount = circular ? (origChildren.count * number) : (1 + ((origChildren.count - 1) * number));
            MakeSpaceForChildren(inStructure, newChildCount);
            for (int j = 0; j < origChildren.count - (circular ? 0 : 1); ++j)
            {
                int startIndex = origChildren[j];
                int endIndex = origChildren[(j + 1) % origChildren.count];
                if (startIndex < 0 || startIndex >= mqSize) { continue; }
                if (endIndex < 0 || endIndex >= mqSize) { continue; }
                Transform2D startT = masterQueue[startIndex].transform;
                Transform2D endT = masterQueue[endIndex].transform;
                int newChildOffset = j * number;
                SetChild(inStructure, newChildOffset, startIndex);
                int childClones = CloneN(startIndex, number - 1);
                if (childClones < 0) { origChildren.Dispose(); EmergencyDelete(origChildren, inStructure); return -1; }
                for (int k = 1; k < number; ++k)
                {
                    int cloneIndex = (childClones + k - 1) % mqSize;
                    masterQueue[cloneIndex].transform = LerpTransforms(startT, endT, k / (float)number);
                    SetChild(inStructure, newChildOffset + k, cloneIndex);
                }
            }
            if (!circular)
            {
                SetChild(inStructure, (origChildren.count - 1) * number, origChildren[origChildren.count - 1]);
            }
            origChildren.Dispose();
            return inStructure;
        }

        private int ProcessSeparateLines(int number, int inStructure)
        {
            int outStructure = MasterQueuePopOne();
            if (outStructure < 0) { MasterQueuePushTree(inStructure); return -1; }
            SetTransform2D(outStructure, masterQueue[inStructure].transform);
            if (number == 1) { return inStructure; }
            UnsafeArray<int> origChildren = masterQueue[inStructure].children.Clone();
            if (origChildren.count == 1) { return inStructure; }
            for (int j = 0; j < origChildren.count; ++j) { SetChild(inStructure, j, -1); }
            MasterQueuePushTree(inStructure);
            int lineCount = circular ? origChildren.count : (origChildren.count - 1);
            MakeSpaceForChildren(outStructure, lineCount);
            for (int j = 0; j < lineCount; ++j)
            {
                int startIndex = origChildren[j];
                int endIndex = origChildren[(j + 1) % origChildren.count];
                if (startIndex < 0 || startIndex >= mqSize) { continue; }
                if (endIndex < 0 || endIndex >= mqSize) { continue; }
                Transform2D startT = masterQueue[startIndex].transform;
                Transform2D endT = masterQueue[endIndex].transform;
                int subStructure = MasterQueuePopOne();
                if (subStructure < 0) { origChildren.Dispose(); EmergencyDelete(origChildren, outStructure); return -1; }
                SetChild(outStructure, j, subStructure);
                MakeSpaceForChildren(subStructure, number);
                SetChild(subStructure, 0, startIndex);
                int childClones = CloneN(startIndex, number - 1);
                if (childClones < 0) { origChildren.Dispose(); EmergencyDelete(origChildren, outStructure); return -1; }
                for (int k = 1; k < number; ++k)
                {
                    int cloneIndex = (childClones + k - 1) % mqSize;
                    masterQueue[cloneIndex].transform = LerpTransforms(startT, endT, k / (float)(number - 1));
                    SetChild(subStructure, k, cloneIndex);
                }
            }
            if (!circular)
            {
                MasterQueuePushTree(origChildren[origChildren.count - 1]);
            }
            origChildren.Dispose();
            return outStructure;
        }

        public override int ProcessStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { MasterQueuePushTree(inStructure); return -1; }
            if (masterQueue[inStructure].children.count == 0) { return inStructure; }
            int number = Solve("number").AsInt32();
            if (number <= 0) { DeleteAllChildren(inStructure); return inStructure; }
            switch (structure)
            {
                case Structure.Flat:
                default:
                    return ProcessFlat(number, inStructure);
                case Structure.SeparateLines:
                    return ProcessSeparateLines(number, inStructure);
            }
        }
    }
}

