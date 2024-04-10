using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.VirtualVariables;
using Godot;
using static Blastula.BNodeFunctions;

namespace Blastula.Debug
{
    /// <summary>
    /// Debug commands that mainly affect the player.
    /// </summary>
    public partial class DebugPlayer : Node2D
    {
        public static DebugConsole.CommandGroup commandGroup = new DebugConsole.CommandGroup
        {
            groupName = "Player",
            commands = new System.Collections.Generic.List<DebugConsole.Command>()
            {
                new DebugConsole.Command
                {
                    name = "god",
                    usageTip = "god {on/off} {left/right}",
                    description = "Makes a player invulnerable to enemy and enemy bullet collisions. With optional left/right argument, " +
                                  "you can choose which player in a two-player game.",
                    action = (args) =>
                    {
                        bool god = true;
                        Player.Role control = Player.Role.SinglePlayer;
                        if (args.Count >= 2) { DebugConsole.SetTruthValue(args[1], ref god); }
                        string l2 = "";
                        if (args.Count >= 3)
                        {
                            l2 = args[2].ToLower();
                            if (l2 == "left" || l2 == "l") { control = Player.Role.LeftPlayer; }
                            else if (l2 == "right" || l2 == "r") { control = Player.Role.RightPlayer; }
                            else { l2 = ""; }
                        }
                        Player player = Player.playersByControl.ContainsKey(control) ? Player.playersByControl[control] : null;
                        if (player == null)
                        {
                            DebugConsole.main.Print("No such player.");
                            return;
                        }
                        player.debugInvulnerable = god;
                        string l2f = (l2 != "") ? "(" + l2 + ") " : "";
                        DebugConsole.main.Print($"Player {l2f}is now {(god ? "invulnerable" : "vulnerable")}.");
                    }
                },

                new DebugConsole.Command
                {
                    name = "power",
                    usageTip = "power {integer} {left/right}",
                    description = "Sets a player's shot power. With optional left/right argument, you can choose which player in a two-player game. " +
                                  "[b]Note:[/b] power is always an integer, even when appearing as a decimal in the UI. " +
                                  "In the starter project, for example, the power range is 100-400, rather than \"1.00\"-\"4.00\".",
                    action = (args) =>
                    {
                        if (args.Count < 2 || !int.TryParse(args[1], out int newPower))
                        {
                            DebugConsole.main.Print("Type an integer value.");
                            return;
                        }
                        Player.Role control = Player.Role.SinglePlayer;
                        string l2 = "";
                        if (args.Count >= 3)
                        {
                            l2 = args[2].ToLower();
                            if (l2 == "left" || l2 == "l") { control = Player.Role.LeftPlayer; }
                            else if (l2 == "right" || l2 == "r") { control = Player.Role.RightPlayer; }
                            else { l2 = ""; }
                        }
                        Player player = Player.playersByControl.ContainsKey(control) ? Player.playersByControl[control] : null;
                        if (player == null)
                        {
                            DebugConsole.main.Print("No such player.");
                            return;
                        }
                        player.shotPower = Mathf.Clamp(newPower, player.GetMinPower(), player.GetMaxPower());
                        player.RecalculateShotPowerIndex();
                        string l2f = (l2 != "") ? "(" + l2 + ") " : "";
                        DebugConsole.main.Print($"Player {l2f}power is now {player.shotPower}.");
                    }
                },

                new DebugConsole.Command
                {
                    name = "lives",
                    usageTip = "lives {number} {left/right}",
                    description = "Sets the player's lives count. With optional left/right argument, you can choose which player in a two-player game. " +
                                  "[b]Note:[/b] this can be a decimal amount (like 2.4).",
                    action = (args) =>
                    {
                        if (args.Count < 2 || !float.TryParse(args[1], out float newLives))
                        {
                            DebugConsole.main.Print("Type a number value.");
                            return;
                        }
                        Player.Role control = Player.Role.SinglePlayer;
                        string l2 = "";
                        if (args.Count >= 3)
                        {
                            l2 = args[2].ToLower();
                            if (l2 == "left" || l2 == "l") { control = Player.Role.LeftPlayer; }
                            else if (l2 == "right" || l2 == "r") { control = Player.Role.RightPlayer; }
                            else { l2 = ""; }
                        }
                        Player player = Player.playersByControl.ContainsKey(control) ? Player.playersByControl[control] : null;
                        if (player == null)
                        {
                            DebugConsole.main.Print("No such player.");
                            return;
                        }
                        player.lives = Mathf.Max(newLives, 0);
                        string l2f = (l2 != "") ? "(" + l2 + ") " : "";
                        string lifeWord = (player.lives == 1) ? "life" : "lives";
                        DebugConsole.main.Print($"Player {l2f}now has {player.lives} extra {lifeWord}.");
                    }
                },

                new DebugConsole.Command
                {
                    name = "bombs",
                    usageTip = "bombs {number} {left/right}",
                    description = "Sets the player's bombs count. With optional left/right argument, you can choose which player in a two-player game. " +
                                  "[b]Note:[/b] this can be a decimal amount (like 2.4).",
                    action = (args) =>
                    {
                        if (args.Count < 2 || !float.TryParse(args[1], out float newBombs))
                        {
                            DebugConsole.main.Print("Type a number value.");
                            return;
                        }
                        Player.Role control = Player.Role.SinglePlayer;
                        string l2 = "";
                        if (args.Count >= 3)
                        {
                            l2 = args[2].ToLower();
                            if (l2 == "left" || l2 == "l") { control = Player.Role.LeftPlayer; }
                            else if (l2 == "right" || l2 == "r") { control = Player.Role.RightPlayer; }
                            else { l2 = ""; }
                        }
                        Player player = Player.playersByControl.ContainsKey(control) ? Player.playersByControl[control] : null;
                        if (player == null)
                        {
                            DebugConsole.main.Print("No such player.");
                            return;
                        }
                        player.bombs = Mathf.Max(newBombs, 0);
                        string l2f = (l2 != "") ? "(" + l2 + ") " : "";
                        string lifeWord = (player.bombs == 1) ? "bomb" : "bombs";
                        DebugConsole.main.Print($"Player {l2f}now has {player.bombs} {lifeWord}.");
                    }
                },
            }
        };
    }
}

