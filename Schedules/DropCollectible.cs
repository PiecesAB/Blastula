using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Threading.Tasks;

namespace Blastula.Schedules
{
    /// <summary>
    /// Drops a collectible arrangement.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/collectible.png")]
    public partial class DropCollectible : BaseSchedule
	{
        public enum PositioningMode
        {
            /// <summary>
            /// The arrangement is placed at the source of the schedule's execution.
            /// </summary>
            Source, 
            /// <summary>
            /// The arrangement is placed at the position variable, which is a Vector2 expression.
            /// </summary>
            Direct, 
            /// <summary>
            /// The arrangement is placed at the position variable, which is a target ID.
            /// </summary>
            Target,
            /// <summary>
            /// The arrangement is placed at the position variable, which is a boss ID.
            /// </summary>
            Boss
        }

        [Export] public PositioningMode positioningMode = PositioningMode.Source;
        /// <summary>
        /// Only relevant with the right PositioningMode.
        /// </summary>
        [Export] public string position;
        /// <summary>
        /// This is a child of the Blastula.CollectibleManager singleton which produces the collectible arrangement.
        /// </summary>
        [Export] public string collectibleName;
        /// Populates the "item_amount" variable in collectible spawning sequences.
        /// For more information, see the Blastula.CollectibleManager class.
        [Export] public string collectibleAmount;

        public override IEnumerator Execute(IVariableContainer source)
        {
            Vector2 solvedPosition = Vector2.Zero;
            switch (positioningMode)
            {
                case PositioningMode.Source:
                    {
                        if (source is not Node2D) { GD.PushError("Source is not a Node2D"); yield break; }
                        solvedPosition = ((Node2D)source).GlobalPosition;
                    }
                    break;
                case PositioningMode.Direct:
                    {
                        solvedPosition = Solve(PropertyName.position).AsVector2();
                    }
                    break;
                case PositioningMode.Target:
                    {
                        solvedPosition = Target.GetClosest(position, default).Origin;
                    }
                    break;
                case PositioningMode.Boss:
                    {
                        var bossList = BossEnemy.GetBosses(position);
                        if (bossList.Count == 0) { GD.PushError("No boss with that name"); yield break; }
                        solvedPosition = bossList[0].GlobalPosition;
                    }
                    break;

            }
            int solvedAmount = Solve(PropertyName.collectibleAmount).AsInt32();
            CollectibleManager.SpawnItems(collectibleName, solvedPosition, solvedAmount);
            yield break;
        }
    }
}
