using Blastula.VirtualVariables;
using Godot;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Blastula.Input
{
	/// <summary>
	/// This node is meant to be a singleton in the kernel. It handles all game input in a centralized and abstracted way.
	/// It also gathers ButtonInfos to set up the names of inputs and their keybinds from file.
	/// </summary>
	public partial class InputManager : Node
	{
		[Export] public Node buttonsHolder;

		private static InputManager main;

		private ulong currentInputState;
		private ulong startedInputState;
		private ulong endedInputState;

		private static Dictionary<string, InputInfo> loadedInputInfos = new();
		private static List<string> loadedInputNames = new();

		public const string SAVE_PATH = "user://input_bindings.csv";

		public class InputInfo
		{
			public string name;
			public ulong code;
			public List<InputEvent> binds;

			private static InputInfo ReadFromFile(FileAccess file)
			{
				string[] rowStrings = file.GetCsvLine();
				if (rowStrings == null || rowStrings.Length < 3) return null;
				string name = rowStrings[0];
				ulong code = ulong.Parse(rowStrings[1]);
				List<InputEvent> binds = new();
				foreach (string bindString in rowStrings[2..])
				{
					string[] split = bindString.Split(":");
					if (split.Length < 2)
					{
						GD.PushWarning($"Didn't bind input \"{name}\" because its type was absent. How did it get saved? You might wanna investigate.");
						continue;
					}
					string bindType = split[0];
					string bindId = split[1..].Join(":");
					switch (bindType)
					{
						case "Key":
							{
								try
								{
									Key inputKey = System.Enum.Parse<Key>(bindId);
									binds.Add(new InputEventKey { Keycode = inputKey });
								}
								catch (System.Exception e)
								{
									GD.PushWarning($"Couldn't bind key to input \"{name}\": {e.Message}");
								}
							}
							break;
						default:
							GD.PushWarning($"Didn't bind input \"{name}\" because its type was unrecognized. How did it get saved? You might wanna investigate.");
							break;
					}
				}

				return new InputInfo
				{
					name = name,
					code = code,
					binds = binds
				};
			}

			public static Dictionary<string, InputInfo> ReadAllFromFile(FileAccess file)
			{
				Dictionary<string, InputInfo> newDict = new();
				InputInfo currentData = null;
				while ((currentData = ReadFromFile(file)) != null)
				{
					newDict[currentData.name] = currentData;
				}
				return newDict;
			}

			public bool WriteToFile(FileAccess file)
			{
				List<string> validBindStrings = new();
				for (int i = 0; i < binds.Count; ++i)
				{
					string bindType = binds[i] switch
					{
						InputEventKey => "Key",
						_ => null
					};
					if (bindType == null)
					{
						GD.PushWarning($"Didn't save a bound input \"{name}\" because its type \"{binds[i].GetType()}\" was unrecognized.");
						continue;
					}

					string bindId = binds[i] switch
					{
						InputEventKey ik => ik.Keycode.ToString(),
						_ => null
					};
					if (bindId == null)
					{
						GD.PushWarning($"Didn't save a bound input \"{name}\" because its button ID \"{bindId}\" was unrecognized.");
						continue;
					}
					validBindStrings.Add(bindType + ":" + bindId);
				}
				if (validBindStrings.Count == 0)
				{
					OS.Alert($"Somehow, I was told to save input \"{name}\" with no bindings. " +
						$"I am instead deleting the input bindings file and quitting to preserve game control using defaults. " +
						$"Contact a developer!", "Fatal Input Binding Error");
					string path = file.GetPathAbsolute();
					file.Close();
					DirAccess.RemoveAbsolute(path);
					Persistent.GetMainScene().GetTree().Quit();
					return false;
				}
				string[] rowStrings = new string[2 + validBindStrings.Count];
				rowStrings[0] = name;
				rowStrings[1] = code.ToString();
				for (int i = 0; i < validBindStrings.Count; ++i) rowStrings[2 + i] = validBindStrings[i];
				file.StoreCsvLine(rowStrings);
				return true;
			}

			public static void WriteAllToFile(Dictionary<string, InputInfo> rows, FileAccess file)
			{
				foreach (InputInfo info in rows.Values)
				{
					try
					{
						bool succ = info.WriteToFile(file);
						if (!succ) return;
					} catch (System.Exception e)
					{
						GD.PushError($"Error saving input bindings: {e.Message}");
					}
				}
				
			}
		}

		/// <summary>
		/// Key: the button ID. value: the last frame the input was pressed or released.
		/// </summary>
		private static Dictionary<ulong, ulong> lastChangedInputFrame = new Dictionary<ulong, ulong>();

		private void AddButtonInfo(Node curr, string path, ref ulong currInputCode, ref Dictionary<string, InputInfo> defaultToWrite)
		{
			if (currInputCode == 0) { return; }
			string subName = (path == "") ? curr.Name : (path + "/" + curr.Name);
			if (curr is ButtonInfo currButtonInfo)
			{
				defaultToWrite[subName] = new InputInfo
				{
					name = subName,
					code = currInputCode,
					binds = new List<InputEvent> { new InputEventKey { Keycode = currButtonInfo.defaultKey } }
				};
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
				AddButtonInfo(child, subName, ref currInputCode, ref defaultToWrite);
			}
		}

		/// <summary>
		/// Load inputs from the buttonsHolder if input binding file if it exists.
		/// </summary>
		public void RefreshBindingsFromFile()
		{
			currentInputState = startedInputState = endedInputState = 0;
			FileAccess bindingsFile = Persistent.OpenOrCreateFile(SAVE_PATH, FileAccess.ModeFlags.Read, (file) =>
			{
				Dictionary<string, InputInfo> defaultToWrite = new();
				ulong currInputCode = 0x1;
				foreach (Node child in buttonsHolder.GetChildren())
				{
					AddButtonInfo(child, "", ref currInputCode, ref defaultToWrite);
				}
				InputInfo.WriteAllToFile(defaultToWrite, file);
			});
			loadedInputInfos = InputInfo.ReadAllFromFile(bindingsFile);
			bindingsFile.Close();
			loadedInputNames = loadedInputInfos.Keys.ToList();
			foreach (var inputInfo in loadedInputInfos.Values)
			{
				InputMap.AddAction(inputInfo.name);
				foreach (var bind in inputInfo.binds)
				{
					InputMap.ActionAddEvent(inputInfo.name, bind);
				}
			}
		}

		public override void _Ready()
		{
			ProcessPriority = Persistent.Priorities.CONSUME_INPUT;
			main = this;
			RefreshBindingsFromFile();
		}

		public override void _Input(InputEvent input)
		{
			base._Input(input);
			foreach (string inputName in loadedInputNames)
			{
				InputInfo thisInfo = loadedInputInfos[inputName];
				ulong code = thisInfo.code;
				if (input.IsAction(inputName))
				{
					if (input.IsActionPressed(inputName))
					{
						startedInputState |= code;
						currentInputState |= code;
						lastChangedInputFrame[code] = FrameCounter.realSessionFrame;
					}
					else if (input.IsActionReleased(inputName))
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

		private static ulong GetRawCurrentState()
		{
			return main?.currentInputState ?? 0;
		}

		private static ulong GetRawStartedState()
		{
			return main?.startedInputState ?? 0;
		}

		private static ulong GetRawEndedState()
		{
			return main?.endedInputState ?? 0;
		}

		private static bool ButtonPressedThisFrame(ulong comp)
		{
			if (main == null) { return false; }
			return (comp & main.startedInputState) != 0;
		}

		public static bool ButtonPressedThisFrame(string comp)
		{
			if (!loadedInputInfos.ContainsKey(comp)) { return false; }
			return ButtonPressedThisFrame(loadedInputInfos[comp].code);
		}

		private static bool ButtonReleasedThisFrame(ulong comp)
		{
			if (main == null) { return false; }
			return (comp & main.endedInputState) != 0;
		}

		public static bool ButtonReleasedThisFrame(string comp)
		{
			if (!loadedInputInfos.ContainsKey(comp)) { return false; }
			return ButtonReleasedThisFrame(loadedInputInfos[comp].code);
		}

		private static bool ButtonIsDown(ulong comp)
		{
			if (main == null) { return false; }
			return (comp & main.currentInputState) != 0;
		}

		public static bool ButtonIsDown(string comp)
		{
			if (!loadedInputInfos.ContainsKey(comp)) { return false; }
			return ButtonIsDown(loadedInputInfos[comp].code);
		}


		/// <summary>
		/// Warning: this is always false when testing multiple buttons.
		/// </summary>
		private static bool ButtonIsHeldLongEnough(ulong comp, ulong frames)
		{
			return GetButtonHeldFrames(comp) >= frames;
		}

		public static bool ButtonIsHeldLongEnough(string comp, ulong frames)
		{
			if (!loadedInputInfos.ContainsKey(comp)) { return false; }
			return ButtonIsHeldLongEnough(loadedInputInfos[comp].code, frames);
		}

		/// <summary>
		/// Warning: this is always 0 when testing multiple buttons.
		/// </summary>
		private static ulong GetButtonHeldFrames(ulong comp)
		{
			if (main == null || !ButtonIsDown(comp) || !lastChangedInputFrame.ContainsKey(comp)) { return 0; }
			return FrameCounter.realSessionFrame - lastChangedInputFrame[comp];
		}

		public static ulong GetButtonHeldFrames(string comp)
		{
			if (!loadedInputInfos.ContainsKey(comp)) { return 0; }
			return GetButtonHeldFrames(loadedInputInfos[comp].code);
		}
	}
}
