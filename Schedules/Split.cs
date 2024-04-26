using Blastula.VirtualVariables;
using Godot;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Run all child schedules and shots at the same time.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/scheduleSplit.png")]
    public partial class Split : BaseSchedule
    {
        public override Task Execute(IVariableContainer source)
        {
            if (base.Execute(source) == null) { return null; }
            foreach (var child in GetChildren())
            {
                _ = ExecuteOrShoot(source, child);
            }
            return Task.CompletedTask;
        }
    }
}
