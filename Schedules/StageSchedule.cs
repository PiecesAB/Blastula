using Blastula.Operations;
using Blastula.VirtualVariables;
using Godot;
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
        public abstract Task Execute();

        public sealed override Task Execute(IVariableContainer _)
        {
            if (base.Execute(_) == null) { return null; }
            return Execute();
        }
    }
}
