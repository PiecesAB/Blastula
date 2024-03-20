using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula.Schedules.EnemySchedules
{
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/yellowCross.png")]
    public partial class TriggerSpawner : EnemySchedule
    {
        public override Task Execute(IVariableContainer source)
        {
            if (source is not Spawner) { return Task.CompletedTask; }
            ((Spawner)source).Spawn();
            return Task.CompletedTask;
        }
    }
}

