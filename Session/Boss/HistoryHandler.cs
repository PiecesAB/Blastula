using Godot;
using System;
using System.Collections.Generic;
using Blastula.Coroutine;
using Blastula.Schedules;
using System.Collections;
using Blastula.VirtualVariables;

namespace Blastula;

/// <remarks>
/// History is saved when quitting the session.
/// </remarks>
public partial class HistoryHandler : Node
{
	public enum ValueMode
	{
		/// <summary>
		/// A standard history which displays as (captured)/(attempts).
		/// </summary>
		/// <remarks>
		/// "captured" means the player didn't use a bomb or lose a life.
		/// </remarks>
		Classic, 
		/// <summary>
		/// An experimental history which displays blocks encoding the past 10 attempts.
		/// </summary>
		CodedBlocks
	}

	public abstract class HistoryValue 
	{
		private static (string, HistoryValue) ReadFromFile(FileAccess file, ValueMode valMode)
		{
			string[] rowStrings = file.GetCsvLine();
			int requiredLength = valMode switch
			{
				ValueMode.Classic => 3,
				ValueMode.CodedBlocks => 2,
				_ => throw new InvalidOperationException("???")
			};
			if (rowStrings == null || rowStrings.Length != requiredLength) return (null, null);
			switch (valMode)
			{
				case ValueMode.Classic:
					return (
						rowStrings[0],
						new ClassicValue { 
							captured = ulong.Parse(rowStrings[1]),
							attempts = ulong.Parse(rowStrings[2]) 
						}
					);
				case ValueMode.CodedBlocks:
					return (
						rowStrings[0],
						new CodedBlocksValue
						{
							blockString = rowStrings[1]
						}
					);
				default:
					throw new InvalidOperationException("???");
			}
		}

		public static Dictionary<string, HistoryValue> ReadAllFromFile(FileAccess file, ValueMode valMode) 
		{
			Dictionary<string, HistoryValue> values = new();
			(string, HistoryValue) kv = (null, null);
			while ((kv = ReadFromFile(file, valMode)) != (null, null))
			{
				values[kv.Item1] = kv.Item2;
			}
			return values;
		}

		private static void WriteToFile(FileAccess file, (string, HistoryValue) kv)
		{
			switch (kv.Item2)
			{
				case ClassicValue classic:
					file.StoreCsvLine(
						new string[] {
							kv.Item1,
							classic.captured.ToString(),
							classic.attempts.ToString(),
						}
					);
					break;
				case CodedBlocksValue codedBlocks:
					file.StoreCsvLine(
						new string[] {
							kv.Item1,
							codedBlocks.blockString,
						}
					);
					break;
				default:
					throw new InvalidOperationException("???");
			}
		}

		public static void WriteAllToFile(FileAccess file, Dictionary<string, HistoryValue> values)
		{
			foreach (var kvp in values) WriteToFile(file, (kvp.Key, kvp.Value));
		}
	}

	public class ClassicValue : HistoryValue
	{
		public ulong captured;
		public ulong attempts;
	}

	public class CodedBlocksValue : HistoryValue
	{
		// The code (displayed as blocks in a bitmap font):
		// -: It didn't happen yet.
		// A: The player neither missed nor bombed.
		// B: The player bombed at least once, but didn't miss.
		// C: The player missed at least once.
		// lowercase a,b,c: Same as above but the attack timed out (it wasn't actually defeated).
		// blockString should therefore be a string of length 10.
		public string blockString;
	}

	[Export] public ValueMode valueMode = ValueMode.CodedBlocks;

	public static HistoryHandler main;

	public const string SAVE_PATH = "user://history.csv";

	private Dictionary<string, HistoryValue> loadedValues = new Dictionary<string, HistoryValue>();

	private Player mainPlayer;
	private StageSector currentSector;
	private bool withinAttack = false;
	private bool timedOut = false;
	private bool playerStruck = false;
	private bool playerBombed = false;

	private Callable mainPlayerBombConnection;
	private Callable mainPlayerStruckConnection;

	public override void _Ready()
	{
		base._Ready();
		main = this;

		FileAccess file = null;
		try
		{
			// This node should be in the kernel. Therefore, it will load the history file into memory before any session starts.
			file = Persistent.OpenOrCreateFile(SAVE_PATH, FileAccess.ModeFlags.Read);
			loadedValues = HistoryValue.ReadAllFromFile(file, valueMode);
		} 
		catch (Exception e)
		{
			GD.PushError($"Problem loading history file: {e.Message}");
		}
		finally
		{
			if (file != null) file.Close();
		}
	}

	public void OnMainPlayerBomb()
	{
		playerBombed = true;
	}

	/// <summary>
	/// Warning: for simplicity, a deathbomb counts as a miss.
	/// </summary>
	public void OnMainPlayerStruck()
	{
		playerStruck = true;
	}

	public string GetCurrentHistoryString()
	{
		if (currentSector == null) return null;
		string sectorId = currentSector.GetUniqueId();
		loadedValues.TryGetValue(sectorId, out HistoryValue hv);
		switch (hv)
		{
			case ClassicValue classic:
				{
					if (classic.captured > 99) return "master";
					string caps = classic.captured.ToString().PadZeros(2);
					string total = classic.attempts >= 100 ? "99+" : classic.attempts.ToString().PadZeros(2);
					return $"{caps}/{total}";
				}
			case CodedBlocksValue codedBlocks:
				return codedBlocks.blockString;
			default:
				if (valueMode == ValueMode.Classic) return "00/00";
				else if (valueMode == ValueMode.CodedBlocks) return "----------";
				else throw new InvalidOperationException("???");
		}
	}

	public IEnumerator Calculate()
	{
		if (withinAttack)
		{
			GD.PushWarning(
				"Tried to listen for history purposes while already listening; not re-entering. " +
				"This is likely a symptom of mishandling/nesting boss attacks."
			);
			yield break;
		}
		withinAttack = true;
		timedOut = false;
		playerStruck = false;
		playerBombed = false;

		mainPlayer.Connect(
			Player.SignalName.OnBombBegan,
			mainPlayerBombConnection = new Callable(this, MethodName.OnMainPlayerBomb)
		);

		mainPlayer.Connect(
			Player.SignalName.OnStruck,
			mainPlayerStruckConnection = new Callable(this, MethodName.OnMainPlayerStruck)
		);

		while (currentSector.ShouldBeExecuting())
		{
			yield return new WaitOneFrame();
		}

		if (currentSector.HasBeenTimedOut())
		{
			timedOut = true;
		}

		string sectorId = currentSector.GetUniqueId();
		// mutate history state
		switch (valueMode)
		{
			case ValueMode.Classic:
				{
					HistoryValue currVal;
					if (!loadedValues.TryGetValue(sectorId, out currVal))
					{
						currVal = new ClassicValue() { attempts = 0, captured = 0 };
					}
					((ClassicValue)currVal).attempts++;
					if (!timedOut && !playerStruck && !playerBombed) ((ClassicValue)currVal).captured++;
					loadedValues[sectorId] = currVal;
				}
				break;
			case ValueMode.CodedBlocks:
				{
					HistoryValue currVal;
					if (!loadedValues.TryGetValue(sectorId, out currVal))
					{
						currVal = new CodedBlocksValue() { blockString = "----------" };
					}
					string myNewLetter = "A";
					if (playerStruck) myNewLetter = "C";
					else if (playerBombed) myNewLetter = "B";
					if (timedOut) myNewLetter = myNewLetter.ToLowerInvariant();
					((CodedBlocksValue)currVal).blockString = ((CodedBlocksValue)currVal).blockString.Substring(1) + myNewLetter;
					loadedValues[sectorId] = currVal;
				}
				break;
			default:
				throw new InvalidOperationException("???");
		}

		mainPlayer.Disconnect(Player.SignalName.OnBombBegan, mainPlayerBombConnection);
		mainPlayer.Disconnect(Player.SignalName.OnStruck, mainPlayerStruckConnection);

		withinAttack = false;
	}

	public void StartCalculation()
	{
		if (!Player.playersByControl.ContainsKey(Player.Role.SinglePlayer))
		{
			GD.PushError("This action currently makes no sense; I'm looking for a SinglePlayer to detect bonus condition.");
			return;
		}
		mainPlayer = Player.playersByControl[Player.Role.SinglePlayer];
		currentSector = StageSector.GetCurrentSector();
		this.StartCoroutine(Calculate(), (c) =>
		{
			// Note: this should only be cancelled due to quitting the game session.
			// Don't save history information, under the assumption it's incomplete. Particularly, it might always time out.
			withinAttack = false;
		});
	}

	public static void Save()
	{
		if (main == null)
		{
			GD.Print("There is no HistoryHandler. Not saving history");
			return;
		}

		FileAccess file = null;
		try
		{
			file = Persistent.OpenOrCreateFile(SAVE_PATH, FileAccess.ModeFlags.Write);
			HistoryValue.WriteAllToFile(file, main.loadedValues);
		}
		catch (Exception e)
		{
			GD.PushError($"Problem saving history file: {e.Message}");
		}
		finally
		{
			if (file != null) file.Close();
		}
	}
}

