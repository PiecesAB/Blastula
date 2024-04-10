using Blastula.Graphics;
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
        [Export] public string pointItemGraphicName = "Collectible/Point";
        private int pointItemGraphicID = -1;
        [Export] public string powerItemGraphicName = "Collectible/Power";
        private int powerItemGraphicID = -1;

        public static CollectibleManager main { get; private set; } = null;

        /// <summary>
        /// Returns true if the BNode has the graphic corresponding to pointItemGraphicName
        /// (and is therefore a point item).
        /// </summary>
        public unsafe static bool IsPointItem(int bNodeIndex)
        {
            if (main == null) { return false; }
            if (bNodeIndex < 0) { return false; }
            if (main.pointItemGraphicID == -1) 
            {
                main.pointItemGraphicID = BulletRendererManager.GetIDFromName(main.pointItemGraphicName);
            }
            return BNodeFunctions.masterQueue[bNodeIndex].bulletRenderID == main.pointItemGraphicID;
        }

        /// <summary>
        /// Returns true if the BNode has the graphic corresponding to powerItemGraphicName
        /// (and is therefore a power item).
        /// </summary>
        public unsafe static bool IsPowerItem(int bNodeIndex)
        {
            if (main == null) { return false; }
            if (bNodeIndex < 0) { return false; }
            if (main.powerItemGraphicID == -1)
            {
                main.powerItemGraphicID = BulletRendererManager.GetIDFromName(main.powerItemGraphicName);
            }
            return BNodeFunctions.masterQueue[bNodeIndex].bulletRenderID == main.powerItemGraphicID;
        }

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