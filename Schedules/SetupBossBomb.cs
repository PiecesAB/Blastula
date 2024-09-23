using Blastula.Graphics;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;

namespace Blastula.Operations
{
	[GlobalClass]
	[Icon(Persistent.NODE_ICON_PATH + "/bossBomb.png")]
	public partial class SetupBossBomb : BaseSchedule
	{
		[Export] public string displayedBombName;
		/// <summary>
		/// Standard Touhou-like behavior for extra points when the player doesn't expend lives or bombs. Base score rewarded when 0 seconds remain at the attack's end.
		/// <br/>
		/// This is evaluated on execution of this schedule, casted to a double.
		/// (The resulting bonus is then casted to a BigInteger.)
		/// </summary>
		[Export] public string baseBonus = "1000000";
		/// <summary>
		/// Standard Touhou-like behavior for extra points when the player doesn't expend lives or bombs. Score rewarded for every second remaining at the attack's end.
		/// <br/>
		/// This is evaluated on execution of this schedule, casted to a double.
		/// (The resulting bonus is then casted to a BigInteger.)
		/// </summary>
		[Export] public string bonusPerExtraSecond = "10000";

		public override IEnumerator Execute(IVariableContainer source)
		{
			if (BombOverlays.main == null)
			{
				GD.PushWarning("Tried to set up a boss bomb but there was no overlay for it");
			} 
			else
			{
				BombOverlays.main.EmitSignal(
					BombOverlays.SignalName.OnBossBombOverlay, 
					displayedBombName
				);
				if (BossStandardBonusHandler.main?.IsEnabled() == true)
				{
					BossStandardBonusHandler.main.StartCalculation(
						baseBonus is null or "" ? 0 : Solve(PropertyName.baseBonus).AsDouble(), 
						bonusPerExtraSecond is null or "" ? 0 : Solve(PropertyName.bonusPerExtraSecond).AsDouble()
					);
				}
			}
			yield break;
		}
	}
}
