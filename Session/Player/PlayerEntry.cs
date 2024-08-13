using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula
{
    /// <summary>
    /// Information about a player, and a reference to the player scene for spawning.
    /// </summary>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/player.png")]
    public partial class PlayerEntry : Node
    {
        /// <summary>
        /// The player's full name as displayed in the game.
        /// </summary>
        [Export] public string fullName = "The Player One";
        /// <summary>
        /// A possible short name (ex. first name or nickname) as displayed in the game.
        /// </summary>
        [Export] public string shortName = "Player";
        /// <summary>
        /// A rich-text description of the player for use in selection.
        /// </summary>
        [Export(PropertyHint.MultilineText)] public string description = "That person who does the thing with the bullets";
        /// <summary>
        /// This scene contains the player. The root should be of Player class.
        /// </summary>
        [Export] public PackedScene playerScene;

        public async Task SpawnPlayer()
        {
            while (PlayerManager.main == null) { await this.WaitOneFrame(); }
            Player newPlayer = (Player)playerScene.Instantiate();
            Node2D mainScene = Persistent.GetMainScene();
            while (mainScene == null)
            {
                await this.WaitOneFrame();
                mainScene = Persistent.GetMainScene();
            }
            Node2D spawn = (Node2D)mainScene.GetNode("%PlayerHome");
            while (spawn == null)
            {
                await this.WaitOneFrame();
                spawn = (Node2D)mainScene.GetNode("%PlayerHome");
            }
            spawn.GetParent().CallDeferred(MethodName.AddChild, newPlayer);
            newPlayer.GlobalPosition = spawn.GlobalPosition;
            newPlayer.entry = this;
        }
    }
}
