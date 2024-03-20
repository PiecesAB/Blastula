using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;

namespace Blastula.Input
{
    /// <summary>
    /// Handles input for us in a centralized way.
    /// </summary>
    public partial class InputManager : Node
    {
        [Export] public Node buttonsHolder;

        private static InputManager main;

        private ulong currentInputState;
        private ulong startedInputState;
        private ulong endedInputState;

        private static List<string> buttonNames = new List<string>();
        private static Dictionary<string, ulong> codeFromName = new Dictionary<string, ulong>();
        private static Dictionary<string, Key> defaultKeys = new Dictionary<string, Key>();

        /// <summary>
        /// Key: the button ID. value: the last frame the input was pressed or released.
        /// </summary>
        private static Dictionary<ulong, ulong> lastChangedInputFrame = new Dictionary<ulong, ulong>();

        private void AddButtonInfo(Node curr, ref ulong currInputCode, string path = "")
        {
            if (currInputCode == 0) { return; }
            string subName = (path == "") ? curr.Name : (path + "/" + curr.Name);
            if (curr is ButtonInfo)
            {
                buttonNames.Add(subName);
                codeFromName[subName] = currInputCode;
                defaultKeys[subName] = (curr as ButtonInfo).defaultKey;
                if (currInputCode == 0x8000000000000000)
                {
                    GD.PushWarning("I only support 64 different input buttons. Having just read the 64th, I'm not looking further.");
                    currInputCode = 0;
                    return;
                }
                currInputCode <<= 1;
            }
            foreach (Node child in curr.GetChildren())
            {
                AddButtonInfo(child, ref currInputCode, subName);
            }
        }

        public void Reconfigure(Node newButtonsHolder)
        {
            if (buttonsHolder == newButtonsHolder) { return; }
            currentInputState = startedInputState = endedInputState = 0;
            foreach (var buttonName in buttonNames)
            {
                InputMap.EraseAction(buttonName);
            }
            buttonNames.Clear(); codeFromName.Clear(); defaultKeys.Clear();
            buttonsHolder = newButtonsHolder;
            Configure();
        }

        private void Configure()
        {
            ulong currInputCode = 0x1;
            foreach (Node child in buttonsHolder.GetChildren())
            {
                AddButtonInfo(child, ref currInputCode, "");
            }
            foreach (var buttonName in buttonNames)
            {
                InputEventKey defaultKeyPress = new InputEventKey { Keycode = defaultKeys[buttonName] };
                InputMap.AddAction(buttonName);
                InputMap.ActionAddEvent(buttonName, defaultKeyPress);
            }
        }

        public override void _Ready()
        {
            ProcessPriority = Persistent.Priorities.CONSUME_INPUT;
            main = this;
            Configure();
        }

        public override void _Input(InputEvent input)
        {
            base._Input(input);
            foreach (string buttonName in buttonNames)
            {
                ulong code = codeFromName[buttonName];
                if (input.IsAction(buttonName))
                {
                    if (input.IsActionPressed(buttonName))
                    {
                        startedInputState |= code;
                        currentInputState |= code;
                        lastChangedInputFrame[code] = FrameCounter.realSessionFrame;
                    }
                    else if (input.IsActionReleased(buttonName))
                    {
                        endedInputState |= code;
                        currentInputState &= ~code;
                        lastChangedInputFrame[code] = FrameCounter.realSessionFrame;
                    }
                }
            }
        }

        public override void _Process(double delta)
        {
            startedInputState = endedInputState = 0;
        }

        public static ulong GetRawCurrentState()
        {
            return main?.currentInputState ?? 0;
        }

        public static ulong GetRawStartedState()
        {
            return main?.startedInputState ?? 0;
        }

        public static ulong GetRawEndedState()
        {
            return main?.endedInputState ?? 0;
        }

        public static bool ButtonPressedThisFrame(ulong comp)
        {
            if (main == null) { return false; }
            return (comp & main.startedInputState) != 0;
        }

        public static bool ButtonPressedThisFrame(string comp)
        {
            if (!codeFromName.ContainsKey(comp)) { return false; }
            return ButtonPressedThisFrame(codeFromName[comp]);
        }

        public static bool ButtonReleasedThisFrame(ulong comp)
        {
            if (main == null) { return false; }
            return (comp & main.endedInputState) != 0;
        }

        public static bool ButtonReleasedThisFrame(string comp)
        {
            if (!codeFromName.ContainsKey(comp)) { return false; }
            return ButtonReleasedThisFrame(codeFromName[comp]);
        }

        public static bool ButtonIsDown(ulong comp)
        {
            if (main == null) { return false; }
            return (comp & main.currentInputState) != 0;
        }

        public static bool ButtonIsDown(string comp)
        {
            if (!codeFromName.ContainsKey(comp)) { return false; }
            return ButtonIsDown(codeFromName[comp]);
        }


        /// <summary>
        /// Warning: this is always false when testing multiple buttons.
        /// </summary>
        public static bool ButtonIsHeldLongEnough(ulong comp, ulong frames)
        {
            return GetButtonHeldFrames(comp) >= frames;
        }

        /// <summary>
        /// Warning: this is always 0 when testing multiple buttons.
        /// </summary>
        public static bool ButtonIsHeldLongEnough(string comp, ulong frames)
        {
            if (!codeFromName.ContainsKey(comp)) { return false; }
            return ButtonIsHeldLongEnough(codeFromName[comp], frames);
        }

        /// <summary>
        /// Warning: this is always 0 when testing multiple buttons.
        /// </summary>
        public static ulong GetButtonHeldFrames(ulong comp)
        {
            if (main == null || !ButtonIsDown(comp) || !lastChangedInputFrame.ContainsKey(comp)) { return 0; }
            return FrameCounter.realSessionFrame - lastChangedInputFrame[comp];
        }

        /// <summary>
        /// Warning: this is always 0 when testing multiple buttons.
        /// </summary>
        public static ulong GetButtonHeldFrames(string comp)
        {
            if (!codeFromName.ContainsKey(comp)) { return 0; }
            return GetButtonHeldFrames(codeFromName[comp]);
        }
    }
}