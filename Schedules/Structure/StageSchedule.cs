using Blastula.Coroutine;
using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// A schedule item used for stages. Ensures that no local container is involved.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/scheduleStageDefault.png")]
    public abstract partial class StageSchedule : BaseSchedule
    {
        public abstract IEnumerator Execute();

        public virtual Action<CoroutineUtility.Coroutine> GetCancelMethod()
        {
            return null;
        }

        public sealed override IEnumerator Execute(IVariableContainer _)
        {
            if (!CanExecute()) { yield break; }
            yield return new CoroutineUtility.Coroutine
            {
                func = Execute(),
                boundNode = this,
                cancel = GetCancelMethod()
            };
        }
    }
}
