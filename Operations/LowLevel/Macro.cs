using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;

namespace Blastula.Operations
{
    /// <summary>
    /// Opaque wrapper around a series of operations.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/macro.png")]
    public abstract partial class Macro : BaseOperation
    {
        private int subOpCurr = 0;
        private List<BaseOperation> suboperations = new List<BaseOperation>();

        protected T AddOperation<T>() where T : BaseOperation, new()
        {
            if (GetChildCount(true) > subOpCurr)
            {
                return GetChild(subOpCurr++, true) as T;
            }
            ++subOpCurr;
            T newOp = new T();
            AddChild(newOp, false, InternalMode.Front);
            suboperations.Add(newOp);
            return newOp;
        }

        private bool createdSub = false;

        protected abstract void CreateSuboperations();

        public sealed override int ProcessStructure(int inStructure)
        {
            if (inStructure < 0 || inStructure >= BNodeFunctions.mqSize) { return -1; }

            if (!createdSub)
            {
                subOpCurr = 0;
                CreateSuboperations();
                createdSub = true;
            }

            if (suboperations == null) { return inStructure; }

            int outStructure = inStructure;
            foreach (BaseOperation op in suboperations)
            {
                outStructure = op.ProcessStructure(outStructure);
                if (outStructure == -1) { break; }
            }
            return outStructure;
        }
    }
}
