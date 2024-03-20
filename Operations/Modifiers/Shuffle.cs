using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Operations
{
    /// <summary>
    /// Rearrange the children of a structure in useful ways.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/shuffle.png")]
    public unsafe partial class Shuffle : Modifier
    {
        public enum Mode
        {
            /// <summary>
            /// Remove the first n children and put them at the end.
            /// </summary>
            Cut,
            /// <summary>
            /// Divides the list into n parts and interleaves them in order. Excess will appear in a shortened group.
            /// Example: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] ---(n = 2)--> [0, 6, 1, 7, 2, 8, 3, 9, 4, 10, 5, 11]<br />
            /// Example: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] ---(n = 3)--> [0, 4, 8, 1, 5, 9, 2, 6, 10, 3, 7, 11]<br />
            /// </summary>
            Weave,
            /// <summary>
            /// Leaps over n items at a time to find the new order. If we leap past the end, loop back to the beginning.
            /// If we reach a child a second time, go forward by 1.
            /// Very useful for preparing star shapes.<br />
            /// Example: [0, 1, 2, 3, 4] ---(n = 2)--> [0, 2, 4, 1, 3]<br />
            /// Example: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] ---(n = 2)--> [0, 2, 4, 6, 8, 10, 1, 3, 5, 7, 9, 11]<br />
            /// </summary>
            Leap,
            /// <summary>
            /// Puts children in backwards order. n doesn't matter.
            /// </summary>
            Reverse,
            /// <summary>
            /// Randomizes the list order. n doesn't matter.
            /// </summary>
            Randomize
        }

        [Export] public Mode mode = Mode.Leap;
        [Export] public string n;

        public override void ModifyStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= mqSize) { return; }
            if (masterQueue[inStructure].children.count <= 1) { return; }
            int n = Solve("n").AsInt32();
            if (n <= 0 && (mode == Mode.Leap || mode == Mode.Weave)) { return; }
            UnsafeArray<int> origChildren = masterQueue[inStructure].children.Clone();
            UnsafeArray<int> newChildren = origChildren.Clone();
            int gcd = MoreMath.GCD(n, origChildren.count);
            int n_gcd = origChildren.count / gcd;
            switch (mode)
            {
                case Mode.Cut:
                    for (int j = 0; j < origChildren.count; ++j)
                    {
                        newChildren[j] = origChildren[MoreMath.RDMod(j + n, origChildren.count)];
                    }
                    break;
                case Mode.Weave:
                    UnsafeArray<int> starts = UnsafeArrayFunctions.Create<int>(n);
                    int excess = origChildren.count % n;
                    int k = 0;
                    for (int j = 0; k < n; j += origChildren.count / n)
                    {
                        starts[k++] = j;
                        if (excess > 0) { --excess; ++j; }
                    }
                    k = 0;
                    for (int j = 0; j < origChildren.count / n; ++j)
                    {
                        for (int l = 0; l < n; ++l)
                        {
                            newChildren[k++] = origChildren[starts[l] + j];
                        }
                    }
                    for (int l = 0; k < origChildren.count; ++l)
                    {
                        newChildren[k++] = origChildren[starts[l] + (origChildren.count / n)];
                    }
                    starts.Dispose();
                    break;
                case Mode.Leap:
                    for (int p = 0; p < gcd; ++p)
                    {
                        for (int l = 0; l < n_gcd; ++l)
                        {
                            newChildren[p * n_gcd + l] = origChildren[(l * n + p) % origChildren.count];
                        }
                    }
                    break;
                case Mode.Reverse:
                    for (int j = 0; j < origChildren.count; ++j)
                    {
                        newChildren[j] = origChildren[origChildren.count - 1 - j];
                    }
                    break;
                case Mode.Randomize:
                    for (int j = 0; j < origChildren.count - 1; ++j)
                    {
                        int randomIndex = RNG.Int(j, origChildren.count);
                        int temp = newChildren[j];
                        newChildren[j] = newChildren[randomIndex];
                        newChildren[randomIndex] = temp;
                    }
                    break;
            }
            masterQueue[inStructure].children = newChildren;
            origChildren.Dispose();
        }
    }
}

