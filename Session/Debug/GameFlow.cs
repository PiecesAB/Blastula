using Blastula.VirtualVariables;
using Godot;

namespace Blastula.Debug
{
    /// <summary>
    /// Debug commands used to alter the flow of the game, such as the difficulty, rank, or speed.
    /// </summary>
    public class GameFlow
    {
        public static bool frozen = false;

        public static DebugConsole.CommandGroup commandGroup = new DebugConsole.CommandGroup
        {
            groupName = "Game Flow",
            commands = new System.Collections.Generic.List<DebugConsole.Command>()
            {
                new DebugConsole.Command
                {
                    name = "speed",
                    usageTip = "speed {new time scale}",
                    description = "Changes the time scale of the game (once it's unpaused).",
                    action = (args) =>
                    {
                        float newTimeScale = 1;
                        if (args.Count >= 2)
                        {
                            if (!float.TryParse(args[1], out newTimeScale))
                            {
                                DebugConsole.main.Print("No action: time scale must be a number within [lb]0.1, 10[rb].");
                                return;
                            }
                        }
                        if (newTimeScale != Mathf.Clamp(newTimeScale, 0.1f, 10f))
                        {
                            DebugConsole.main.Print("No action: time scale must be a number within [lb]0.1, 10[rb].");
                            return;
                        }
                        Session.main.SetTimeScale(newTimeScale);
                        DebugConsole.main.Print($"Time scale is now {newTimeScale}");
                    }
                },

                new DebugConsole.Command
                {
                    name = "freeze",
                    usageTip = "freeze {on/off}",
                    description = "When the game is frozen, all bullet behavior, firing, enemy movement, and stage progression will be stopped. " +
                                  "Player actions aren't stopped.",
                    action = (args) =>
                    {
                        frozen = !frozen;
                        if (args.Count >= 2) { DebugConsole.SetTruthValue(args[1], ref frozen); }
                        DebugConsole.main.Print($"The game is now {(frozen ? "frozen" : "unfrozen")}.");
                    }
                },

                new DebugConsole.Command
                {
                    name = "difficulty",
                    usageTip = "difficulty {number}",
                    description = "Sets game difficulty by its internally identifying number.",
                    action = (args) =>
                    {
                        if (args.Count >= 2 && int.TryParse(args[1], out int newDif))
                        {
                            Session.main.SetDifficulty(newDif);
                            DebugConsole.main.Print($"Difficulty set to {Session.main.difficulty}");
                        }
                        else
                        {
                            DebugConsole.main.Print("Difficulty not changed: invalid input.");
                        }
                    }
                },

                new DebugConsole.Command
                {
                    name = "rank",
                    usageTip = "rank {number} {freeze: on/off}",
                    description = "Sets game rank to number. You can also freeze rank at the number, using the second parameter.",
                    action = (args) =>
                    {
                        if (args.Count >= 2 && float.TryParse(args[1], out float newRank))
                        {
                            Session.main.SetRank(newRank, true);
                            bool frozen = false;
                            if (args.Count >= 3) { DebugConsole.SetTruthValue(args[2], ref frozen); }
                            Session.main.rankFrozen = frozen;
                            DebugConsole.main.Print($"Rank set to {Session.main.rank}; {(frozen ? "frozen" : "unfrozen")}");
                        }
                        else
                        {
                            DebugConsole.main.Print("Rank not changed: invalid input.");
                        }
                    }
                }
            }
        };
    }
}

