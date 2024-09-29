using Blastula.VirtualVariables;
using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static Blastula.Input.InputManager;

namespace Blastula.Input
{
	/// <summary>
	/// This node is meant to be a singleton in the kernel. It handles all game input in a centralized and abstracted way.
	/// It also gathers ButtonInfos to set up the names of inputs and their keybinds from file.
	/// </summary>
	public partial class InputManager : Node
	{
		[Export] public Node buttonsHolder;

		private bool isRebinding = false;
		public bool GetIsRebinding() => isRebinding;

		public static InputManager main { get; private set; }

		private ulong currentInputState = 0;
		private ulong startedInputState = 0;
		private ulong endedInputState = 0;

		public enum CurrentSet
		{
			Default = 0, A = 1, B = 2, C = 3, Count = 4
		}

		private static CurrentSet currentSet = CurrentSet.Default;
		public static CurrentSet GetCurrentSet() => currentSet;
		private static List<string> setDevices = new();
		private static Dictionary<string, InputInfo> loadedInputInfos = new();
		public static Dictionary<string, InputInfo> GetLoadedInputInfos(bool includeDebug = true) { 
			if (includeDebug) return new Dictionary<string, InputInfo>(loadedInputInfos);
			Dictionary<string, InputInfo> nonDebugLoadedInputInfos = new();
			foreach (var kvp in loadedInputInfos)
			{
				if (!kvp.Value.isDebug) nonDebugLoadedInputInfos[kvp.Key] = kvp.Value;
			}
			return nonDebugLoadedInputInfos;
		}
		private static List<string> loadedInputNames = new();

		public const string SAVE_PATH = "user://input_bindings.csv";

		public class InputInfo
		{
			public string name;
			public ulong code;
			public bool isDebug;
			public List<InputEvent> binds;

			public static InputEvent BindFromSerializedString(string str)
			{
				try
				{
					if (str is null or "") return null;
					string[] splits = str.Split(':');
					if (splits.Length < 2) return null;
					string bindType = splits[0];
					int bindDevice = int.Parse(splits[1]);
					switch (bindType)
					{
						case "Key":
							{
								Key inputKey = System.Enum.Parse<Key>(splits[2]);
								return new InputEventKey { Device = bindDevice, Keycode = inputKey };
							}
						case "JoypadButton":
							{
								JoyButton joyButton = System.Enum.Parse<JoyButton>(splits[2]);
								return new InputEventJoypadButton { Device = bindDevice, ButtonIndex = joyButton };
							}
						case "JoypadMotion":
							{
								JoyAxis joyAxis = System.Enum.Parse<JoyAxis>(splits[2]);
								float axisValue = float.Parse(splits[3]);
								return new InputEventJoypadMotion { Device = bindDevice, Axis = joyAxis, AxisValue = axisValue };
							}
						default:
							GD.PushWarning($"Didn't parse a serialized input because its type was unrecognized. How did it get saved? You might wanna investigate.");
							return null;
					}
				}
				catch
				{
					GD.PushWarning("Uncaught error deriving bind from serialized form, returning null");
					return null;
				}
			}

			public string BindToDisplayString(int index)
			{
				if (index >= binds.Count) return null;
				InputEvent bind = binds[index];
				switch (bind)
				{
					case InputEventKey ik:
						return ik.Keycode.ToString();
					case InputEventJoypadButton ijb:
						return ijb.ButtonIndex.ToString();
					case InputEventJoypadMotion ijm:
						return (ijm.AxisValue > 0 ? "+" : "-") + ijm.Axis.ToString();
					default:
						return bind?.ToString();
				}
			}

			public static string BindToSerializedString(InputEvent @event)
			{
				switch (@event)
				{
					case InputEventKey ik and { Device: int kDevice, Keycode: Key keyCode }:
						return "Key:" + kDevice.ToString() + ":" + keyCode.ToString();
					case InputEventJoypadButton ijb and { Device: int ijbDevice, ButtonIndex: JoyButton ijbPress }:
						return "JoypadButton:" + ijbDevice.ToString() + ":" + ijbPress.ToString();
					case InputEventJoypadMotion ijm and { Device: int ijmDevice, Axis: JoyAxis ijmAxis, AxisValue: float ijmSigner }:
						return "JoypadMotion:" + ijmDevice.ToString() + ":" + ijmAxis.ToString() + ":" + Mathf.Sign(ijmSigner).ToString();
					default:
						return null;
				}
			}

			public string BindToSerializedString(int index)
			{
				if (index >= binds.Count) return null;
				return BindToSerializedString(binds[index]);
			}

			private static InputInfo ReadFromFile(FileAccess file)
			{
				string[] rowStrings = file.GetCsvLine();
				if (rowStrings == null || rowStrings.Length < 4) return null;
				string name = rowStrings[0];
				ulong code = ulong.Parse(rowStrings[1]);
				bool isDebug = bool.Parse(rowStrings[2]);
				List<InputEvent> binds = new();
				foreach (string bindString in rowStrings[3..])
				{
					binds.Add(BindFromSerializedString(bindString));
				}

				return new InputInfo
				{
					name = name,
					code = code,
					isDebug = isDebug,
					binds = binds
				};
			}

			public static Dictionary<string, InputInfo> ReadAllFromFile(FileAccess file)
			{
				bool loadSetSuccess = System.Enum.TryParse(file.GetLine(), true, out currentSet);
				if (!loadSetSuccess)
				{
					GD.PushWarning("Unknown current input set. Input bindings file may be invalid. Reverting to Default and continuing anyway.");
					currentSet = CurrentSet.Default;
				}

				Dictionary<string, InputInfo> newDict = new();
				InputInfo currentData = null;
				while ((currentData = ReadFromFile(file)) != null)
				{
					newDict[currentData.name] = currentData;
				}
				return newDict;
			}

			private bool WriteToFile(FileAccess file)
			{
				List<string> bindStrings = new();
				for (int i = 0; i < binds.Count; ++i)
				{
					string newBindString = BindToSerializedString(binds[i]) ?? "";
					bindStrings.Add(newBindString);
				}
				string[] rowStrings = new string[3 + bindStrings.Count];
				rowStrings[0] = name;
				rowStrings[1] = code.ToString();
				rowStrings[2] = isDebug.ToString();
				for (int i = 0; i < bindStrings.Count; ++i) rowStrings[3 + i] = bindStrings[i];
				file.StoreCsvLine(rowStrings);
				return true;
			}

			public static void WriteAllToFile(Dictionary<string, InputInfo> rows, FileAccess file)
			{
				file.StoreLine(currentSet.ToString());
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
					isDebug = currButtonInfo.debugOnly,
					binds = new List<InputEvent> { new InputEventKey { Keycode = currButtonInfo.defaultKey }, null, null, null }
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
		private void RefreshBindingsFromFile()
		{
			FileAccess bindingsFile = Persistent.OpenOrCreateFile(SAVE_PATH, FileAccess.ModeFlags.Read, (file) =>
			{
				currentSet = CurrentSet.Default;
				setDevices = new List<string> { "", "", "", "" };
				Dictionary<string, InputInfo> defaultToWrite = new();
				ulong currInputCode = 0x1;
				foreach (Node child in buttonsHolder.GetChildren())
				{
					AddButtonInfo(child, "", ref currInputCode, ref defaultToWrite);
				}
				InputInfo.WriteAllToFile(defaultToWrite, file);
			});
			var fullLoadedInputInfos = InputInfo.ReadAllFromFile(bindingsFile);
			bindingsFile.Close();
			loadedInputInfos = new Dictionary<string, InputInfo>();
			foreach (var kvp in fullLoadedInputInfos) 
			{ 
				loadedInputInfos[kvp.Key] = kvp.Value;
			}
			loadedInputNames = loadedInputInfos.Keys.ToList();
			foreach (var inputInfo in loadedInputInfos.Values)
			{
				InputMap.AddAction(inputInfo.name);
			}
			bool swapSucc = SwapControlToInputSet(currentSet);
			if (!swapSucc) SwapControlToInputSet(CurrentSet.Default);
		}

		public void SaveFile()
		{
			FileAccess bindingsFile = Persistent.OpenOrCreateFile(SAVE_PATH, FileAccess.ModeFlags.Write);
			InputInfo.WriteAllToFile(loadedInputInfos, bindingsFile);
			bindingsFile.Close();
		}

		public override void _Ready()
		{
			ProcessPriority = Persistent.Priorities.CONSUME_INPUT;
			main = this;
			RefreshBindingsFromFile();
		}

		// I'm sorry you have to see this. (It's a bunch of rebind state crap.)
		public enum RebindCancelReason { Unknown, AlreadyUsedInput, TimeOut }
		private Action<InputEvent, string> afterRebindInputCallback; // parameters: the input which just happened and was bound; next input name
		private Action<RebindCancelReason> onRebindCancelCallback;
		private Action onRebindCompleteCallback;
		private CurrentSet rebindSet;
		private Dictionary<string, InputEvent> rebindNewInputColumn = new();
		private List<string> rebindList = new List<string>();
		private int rebindCurrentIndex = 0;
		private int rebindFramesCooldown = 0;
		private const int rebindTimeoutFrames = 600;
		private InputEvent previousRebindInput = null; 

		public string GetRebindProgressString() => $"{rebindCurrentIndex + 1}/{rebindList.Count}";
		public float GetProgressBeforeTimeout() => Mathf.Min(rebindTimeoutFrames, rebindTimeoutFrames + rebindFramesCooldown) / (float)rebindTimeoutFrames;

		private string GetNextRebindInputName() => isRebinding ? (rebindCurrentIndex < rebindList.Count ? rebindList[rebindCurrentIndex] : null) : null;

		public void StartRebinding(CurrentSet set, Action<InputEvent, string> afterInputCallback, Action<RebindCancelReason> onCancelCallback, Action onCompleteCallback)
		{
			if (isRebinding) { GD.PushWarning("Tried to start input rebinding, but I was already doing that."); return; }
			if (set == CurrentSet.Default) { GD.PushWarning("Tried to start rebinding the default inputs. I'm afraid I can't let you do that."); return; }
			isRebinding = true;
			rebindSet = set;
			rebindNewInputColumn = new();
			Dictionary<string, InputInfo> nonDebugLoadedInputInfos = new();
			foreach (string k in loadedInputInfos.Keys)
			{
				if (!loadedInputInfos[k].isDebug) nonDebugLoadedInputInfos[k] = loadedInputInfos[k];
			}
			rebindList = nonDebugLoadedInputInfos.Keys.ToList();
			rebindCurrentIndex = 0;
			rebindFramesCooldown = 6;
			previousRebindInput = null;
			afterRebindInputCallback = afterInputCallback;
			onRebindCancelCallback = onCancelCallback;
			onRebindCompleteCallback = onCompleteCallback;
			afterRebindInputCallback(null, GetNextRebindInputName());
		}

		private void CancelRebinding(RebindCancelReason reason)
		{
			currentInputState = startedInputState = endedInputState = 0;
			isRebinding = false;
			onRebindCancelCallback(reason);
		}

		private void ApplyAndCompleteRebinding()
		{
			currentInputState = startedInputState = endedInputState = 0;
			foreach (var kvp in rebindNewInputColumn)
			{
				loadedInputInfos[kvp.Key].binds[(int)rebindSet] = kvp.Value;
			}
			bool swapSucc = SwapControlToInputSet(rebindSet);
			if (!swapSucc)
			{
				OS.Alert("Tried to swap to the new input set, but it was somehow incomplete, so I'm reverting to defaults. " +
							"This is a highly abnormal game state, and you may want to tell a developer.", "Input Error");
				SwapControlToInputSet(CurrentSet.Default);
			}
			isRebinding = false;
		}

		private void ThrowFatalSwapError()
		{
			OS.Alert("Tried to swap to the default or current set, but it was somehow incomplete. " +
							$"Your input file is likely corrupted. " +
							$"Navigate to {ProjectSettings.GlobalizePath(SAVE_PATH)} and delete it to repopulate defaults on the next game load. " +
							$"Contact a developer if you still see this error.", "Fatal Input Error");
			GetTree().Quit(1);
			throw new Exception("Fatal Input Error");
		}

		/// <returns>True if the swap was successful.</returns>
		public bool SwapControlToInputSet(CurrentSet set)
		{
			CurrentSet oldSet = currentSet;
			foreach (var inputInfo in loadedInputInfos.Values)
			{
				InputMap.ActionEraseEvents(inputInfo.name);
			}
			foreach (var inputInfo in loadedInputInfos.Values)
			{
				if (inputInfo.isDebug)
				{
					if (inputInfo.binds.Count < (int)CurrentSet.Default || inputInfo.binds[(int)CurrentSet.Default] == null)
					{
						ThrowFatalSwapError();
						return false;
					}
					InputMap.ActionAddEvent(inputInfo.name, inputInfo.binds[(int)CurrentSet.Default]);
				} else
				{
					if ((int)set >= inputInfo.binds.Count || inputInfo.binds[(int)set] == null)
					{
						if (set == CurrentSet.Default || set == oldSet) ThrowFatalSwapError();
						SwapControlToInputSet(oldSet);
						return false;
					}
					InputMap.ActionAddEvent(inputInfo.name, inputInfo.binds[(int)set]);
				}
			}
			currentSet = set;
			return true;
		}

		public override void _Input(InputEvent input)
		{
			base._Input(input);
			if (isRebinding)
			{
				if (input.IsEcho()) return;
				if (rebindFramesCooldown > 0) return;
				if (!input.IsPressed() || input.IsReleased()) return;
				if (previousRebindInput is InputEventJoypadMotion pjm && input is InputEventJoypadMotion cjm
					&& pjm.Device == cjm.Device && pjm.Axis == cjm.Axis && Mathf.Sign(pjm.AxisValue) == Mathf.Sign(cjm.AxisValue))
				{
					return;
				}
				string serialized = InputInfo.BindToSerializedString(input);
				if (serialized == null) return;
				rebindNewInputColumn[GetNextRebindInputName()] = input;
				rebindCurrentIndex++;
				afterRebindInputCallback(input, GetNextRebindInputName());
				rebindFramesCooldown = 6;
				previousRebindInput = input;
				if (rebindCurrentIndex >= rebindList.Count)
				{
					ApplyAndCompleteRebinding();
					onRebindCompleteCallback();
				}
			}
			else
			{
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
		}

		public override void _Process(double delta)
		{
			if (isRebinding)
			{
				rebindFramesCooldown--;
				if (rebindFramesCooldown < -rebindTimeoutFrames) { CancelRebinding(RebindCancelReason.TimeOut); return; } // 12 seconds: timeout
			}
			else
			{
				startedInputState = endedInputState = 0;
			}
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
			if (main == null || main.isRebinding) { return false; }
			return (comp & main.startedInputState) != 0;
		}

		public static bool ButtonPressedThisFrame(string comp)
		{
			if (!loadedInputInfos.ContainsKey(comp)) { return false; }
			return ButtonPressedThisFrame(loadedInputInfos[comp].code);
		}

		private static bool ButtonReleasedThisFrame(ulong comp)
		{
			if (main == null || main.isRebinding) { return false; }
			return (comp & main.endedInputState) != 0;
		}

		public static bool ButtonReleasedThisFrame(string comp)
		{
			if (!loadedInputInfos.ContainsKey(comp)) { return false; }
			return ButtonReleasedThisFrame(loadedInputInfos[comp].code);
		}

		private static bool ButtonIsDown(ulong comp)
		{
			if (main == null || main.isRebinding) { return false; }
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
			if (main == null || main.isRebinding || !ButtonIsDown(comp) || !lastChangedInputFrame.ContainsKey(comp)) { return 0; }
			return FrameCounter.realSessionFrame - lastChangedInputFrame[comp];
		}

		public static ulong GetButtonHeldFrames(string comp)
		{
			if (!loadedInputInfos.ContainsKey(comp)) { return 0; }
			return GetButtonHeldFrames(loadedInputInfos[comp].code);
		}
	}
}
