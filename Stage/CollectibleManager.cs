using Blastula.Graphics;
using Blastula.Operations;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using Godot.Collections;
using System;

namespace Blastula
{
	public partial class CollectibleManager : Node
	{
        [Export] public Blastodisc collectibleDisc = null;
        /// <summary>
        /// Maps graphic names to a common collectible name (for use in other C# scripts like the Player).
        /// </summary>
        [Export] public Dictionary<string, string> itemToGraphicNames = new Dictionary<string, string>()
        {
            {"Collectible/Point", "Point"},
            {"Collectible/Point/Big", "Point"},
            {"Collectible/Power", "Power"},
            {"Collectible/Power/Big", "Power"},
            {"Collectible/Extend", "Extend"},
            {"Collectible/ExtendPiece", "Extend"},
            {"Collectible/GetBomb", "GetBomb"},
            {"Collectible/GetBombPiece" , "GetBomb"},
        };
        private Dictionary<int, string> itemNamesFromIDs = null;

        public static CollectibleManager main { get; private set; } = null;

        private void PopulateItemNamesFromIDs()
        {
            if (itemNamesFromIDs != null) { return; }
            itemNamesFromIDs = new Dictionary<int, string>();
            foreach (var kvp in itemToGraphicNames) 
            {
                string itemName = kvp.Value;
                string graphicName = kvp.Key;
                int graphicID = BulletRendererManager.GetIDFromName(graphicName);
                itemNamesFromIDs[graphicID] = itemName;
            }
        }

        /// <summary>
        /// Returns the name of the item type in main.itemToGraphicNames (or "" if it doesn't match one).
        /// Used in other C# scripts to identify the collectible and take appropriate action.
        /// </summary>
        public unsafe static string GetItemName(int bNodeIndex)
        {
            if (main == null) { return ""; }
            if (bNodeIndex < 0) { return ""; }
            int bulletRenderID = BNodeFunctions.masterQueue[bNodeIndex].bulletRenderID;
            main.PopulateItemNamesFromIDs();
            if (!main.itemNamesFromIDs.ContainsKey(bulletRenderID)) { return ""; }
            return main.itemNamesFromIDs[bulletRenderID];
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
