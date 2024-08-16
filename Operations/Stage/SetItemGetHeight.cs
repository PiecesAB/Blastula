using Blastula.Coroutine;
using Blastula.Graphics;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Operations
{
    /// <summary>
    /// Changes the item get height for a player.
    /// Depends on the existence of Blastula.Graphics.ItemGetLineDisplay class to display the new height.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/itemGetLine.png")]
    public partial class SetItemGetHeight : Discrete
	{
        /// <summary>
        /// The player whose itemGetHeight is changed.
        /// </summary>
        [Export] Player.Role role = Player.Role.SinglePlayer;
        /// <summary>
        /// The new Y-position that is the item get height.
        /// </summary>
        /// <remarks>
        /// In the Godot 2D coordinate system, a lower value is higher up the screen.
        /// The starter project has been positioned such that (0, 0) is the center of the playfield.
        /// </remarks>
        [Export] public string newHeight = "-144";
        /// <summary>
        /// If true, this displays and moves the new item get height as a line in the playfield.
        /// Otherwise, the side arrow is moved and no line appears.
        /// </summary>
        [Export] public bool showLine = true;

        public override void Run()
        {
            Player player = null;
            if (Player.playersByControl.ContainsKey(role))
            {
                player = Player.playersByControl[role];
            }
            if (player == null) { return; }

            player.itemGetHeight = Solve("newHeight").AsSingle();

            ItemGetLineDisplay display = null;
            if (ItemGetLineDisplay.displaysByPlayerRole.ContainsKey(role))
            {
                display = ItemGetLineDisplay.displaysByPlayerRole[role];
            }
            if (display == null) { return; }

            if (showLine) { this.StartCoroutine(display.DisplayLine()); }
            else { this.StartCoroutine(display.SetWithoutLineDisplay()); }
        }
    }
}
