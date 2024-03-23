using Blastula.Input;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blastula.Debug
{
    /// <summary>
    /// Console that allows the developer to have control over the game in ways
    /// that are important for debugging.
    /// </summary>
    public partial class DebugConsole : Control
    {
        public bool paused = false;
        [Export] public LineEdit inputLine;
        [Export] public RichTextLabel lastText;

        public static DebugConsole main;

        public class CommandGroup
        {
            public string groupName = "";
            public List<Command> commands = new List<Command>();
            public Command MatchName(string commandName)
            {
                foreach (Command c in commands)
                {
                    if (c.name == commandName || c.aliases.Contains(commandName)) { return c; }
                }
                return null;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append((groupName == "") ? "" : $"[color=cyan][b]{groupName}:[/b][/color] ");
                int countedCommands = 0;
                foreach (Command c in commands)
                {
                    if (countedCommands > 0) { sb.Append(", "); }
                    sb.Append(c.name);
                    countedCommands++;
                }
                return sb.ToString();
            }
        }

        public class Command
        {
            public string name;
            public List<string> aliases = new List<string>();
            public string usageTip;
            public string description;
            public Action<List<string>> action;

            public string AliasesToString()
            {
                if (aliases.Count == 0) { return ""; }
                StringBuilder sb = new StringBuilder();
                int countedAliases = 0;
                foreach (string a in aliases)
                {
                    if (countedAliases > 0) { sb.Append(", "); }
                    sb.Append(a);
                    countedAliases++;
                }
                return sb.ToString();
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[color=cyan][b]Usage:[/b][/color] ");
                if (aliases.Count > 0)
                {
                    sb.Append("[color=cyan][b]Aliases:[/b][/color] ");
                    sb.Append(AliasesToString());
                }
                sb.Append(usageTip); sb.Append("\n");
                sb.Append(description);
                return sb.ToString();
            }
        }

        private CommandGroup consoleSelfCommandGroup = new CommandGroup
        {
            groupName = "",
            commands = new List<Command>()
            {
                new Command
                {
                    name = "help",
                    usageTip = "help {command name}",
                    description = "Do you really need help with [b][i]help[/i][/b]? You just used it...\n"
                                + "Well, just in case, type [b][i]help[/i][/b] by itself to view all commands. "
                                + "Type a command name after it (example: [b][i]help time[/i][/b]) to learn about that command.",
                    action = (args) =>
                    {
                        if (args.Count == 1)
                        {
                            StringBuilder sb = new StringBuilder();
                            int lines = 0;
                            foreach (CommandGroup cg in main.commandGroupRegistry)
                            {
                                if (lines > 0) { sb.Append("\n"); }
                                sb.Append(cg.ToString());
                                lines++;
                            }
                            main.Print(sb.ToString());
                        }
                        else
                        {
                            Command c = main.GetCommandFromName(args[1]);
                            if (c == null)
                            {
                                main.Print("Sorry, I can't help (command not found).");
                                return;
                            }
                            main.Print(c.ToString());
                        }
                    }
                }
            }
        };

        private List<CommandGroup> commandGroupRegistry = new List<CommandGroup>();

        public override void _Ready()
        {
            main = this;
            commandGroupRegistry.Add(consoleSelfCommandGroup);
            commandGroupRegistry.Add(GameFlow.commandGroup);
            commandGroupRegistry.Add(DebugCollision.commandGroup);
            commandGroupRegistry.Add(StatsViews.commandGroup);
            Visible = false;
            ProcessPriority = Persistent.Priorities.PAUSE;
        }

        private void Open()
        {
            inputLine.Text = "";
            inputLine.GrabFocus();
            Visible = true;
        }

        private void Close()
        {
            inputLine.ReleaseFocus();
            Visible = false;
        }

        private static List<string> stringsLikeTrue = new List<string> { "yes", "true", "t", "y", "on", "1" };
        private static List<string> stringsLikeFalse = new List<string> { "no", "false", "f", "n", "off", "0" };

        /// <summary>
        /// If the text is true-like or false-like, set an external bool value according to that truth value.
        /// </summary>
        /// <example>
        /// "yes", "true", "t", "y", "on", "1" are true-like.
        /// </example>
        /// <example>
        /// "no", "false", "f", "n", "off", "0" are false-like.
        /// </example>
        /// <param name="text">Text to evaluate for a truth value.</param>
        /// <param name="set">reference to a bool value to set based on the string's truth value.</param>
        public static void SetTruthValue(string text, ref bool set)
        {
            text = text.ToLower();
            if (stringsLikeTrue.Contains(text.ToLower())) { set = true; }
            if (stringsLikeFalse.Contains(text.ToLower())) { set = false; }
        }

        /// <summary>
        /// Split a string into tokens delimited by spaces.
        /// </summary>
        /// <remarks>
        /// You can escape a space character using backslash.
        /// </remarks>
        public static List<string> Tokenize(string text)
        {
            List<string> tokens = new List<string>();
            string currentToken = "";
            bool escaped = false;
            foreach (char c in text)
            {
                if (escaped) { currentToken += c; escaped = false; }
                else if (c == ' ')
                {
                    if (currentToken != "") { tokens.Add(currentToken); }
                    currentToken = "";
                }
                else if (c == '\\') { escaped = true; }
                else { currentToken += c; }
            }
            if (currentToken != "") { tokens.Add(currentToken); }
            return tokens;
        }

        /// <summary>
        /// Creates a RichTextLabel to display output.
        /// </summary>
        public void Print(string text)
        {
            RichTextLabel newText = lastText.Duplicate(7) as RichTextLabel;
            lastText.AddSibling(newText);
            newText.Text = text;
            if (newText.GetParent().GetChildCount() > 60)
            {
                newText.GetParent().GetChild(0).QueueFree();
            }
            lastText = newText;
        }

        /// <summary>
        /// Can be run to close this debug console for external reasons.
        /// </summary>
        public void CloseExternal()
        {
            if (paused && Session.main.paused)
            {
                paused = false;
                WaitToUnpause();
                Close();
            }
        }

        private async void WaitToUnpause()
        {
            await this.WaitOneFrame(true);
            Session.main.Unpause();
        }

        public Command GetCommandFromName(string name)
        {
            Command c = null;
            foreach (CommandGroup cg in commandGroupRegistry)
            {
                c = cg.MatchName(name);
                if (c != null) { break; }
            }
            return c;
        }

        public void ExecuteCommand(string command)
        {
            List<string> tokens = Tokenize(command);
            Command c = GetCommandFromName(tokens[0]);
            if (c == null) { main.Print("Command not found."); return; }
            c.action(tokens);
        }

        public override void _Input(InputEvent input)
        {
            base._Input(input);
            if (!paused) { return; }
            if (input.IsPressed() && !input.IsEcho() && input is InputEventKey)
            {
                InputEventKey inputKey = input as InputEventKey;
                if (inputKey.Keycode == Key.Enter && inputLine.Text != "")
                {
                    Print($"[color=yellow]> {inputLine.Text}[/color]");
                    ExecuteCommand(inputLine.Text);
                    inputLine.Text = "";
                }
            }
        }

        public override void _Process(double delta)
        {
            if (Session.main == null) { return; }
            if (!OS.IsDebugBuild()) { return; }
            bool pausePressed = InputManager.ButtonPressedThisFrame("Debug");
            if (pausePressed)
            {
                if (paused && Session.main.paused)
                {
                    paused = false;
                    WaitToUnpause();
                    Close();
                }
                else if (!paused && !Session.main.paused)
                {
                    paused = true;
                    Session.main.Pause();
                    Open();
                }
            }
            if (paused && !inputLine.HasFocus())
            {
                inputLine.GrabFocus();
            }
        }
    }
}

