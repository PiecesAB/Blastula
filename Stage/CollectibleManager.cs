using Blastula.Operations;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System;

namespace Blastula
{
	public partial class CollectibleManager : Node
	{
        [Export] public Blastodisc collectibleDisc = null;

        public static CollectibleManager main { get; private set; } = null;

        /// <summary>
        /// Spawns items by the name of a child BaseOperation of this Node.
        /// </summary>
        /// <param name="amount">Sets the "item_amount" variable locally in the collectible disc.</param>
        public static void SpawnItems(string name, Vector2 globalPosition, int amount)
        {
            if (main == null) { return; }
            ((IVariableContainer)main.collectibleDisc).SetVar("item_amount", amount);
            Node childNode = main.FindChild(name);
            if (childNode == null || childNode is not BaseOperation) { return; }
            BaseOperation childOp = (BaseOperation)childNode;
            if (main.FindChild(name) != null)
            {
                main.collectibleDisc.GlobalPosition = globalPosition;
                main.collectibleDisc.Shoot(childOp);
            }
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
        }
    }
}
