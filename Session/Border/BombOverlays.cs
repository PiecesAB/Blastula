using Blastula.Schedules;
using Blastula.Coroutine;
using Godot;
using System.Collections;

namespace Blastula.Graphics
{
	/// <summary>
	/// Idiosyncratic; manages the overlays which appear 
	/// </summary>
	public partial class BombOverlays : Node
	{
		[Export] public AnimationPlayer bossOverlayAnimator;
		[Export] public Label bossDisplayLabel;
		[Export] public Label bossBonusScoreLabel;
		[Export] public Label playerDisplayLabel;
		[Export] public AnimationPlayer playerOverlayAnimator;

		public static BombOverlays main;

		[Signal] public delegate void OnBossBombOverlayEventHandler(string displayName);
		[Signal] public delegate void OnPlayerBombOverlayEventHandler(string displayName);

		private StageSector currentBossOverlaySector;
		private StageSector currentPlayerOverlaySector;

		public IEnumerator WaitForEndBossBombSector()
		{
			StageSector refSector = currentBossOverlaySector;
			while (currentBossOverlaySector == refSector && refSector.ShouldBeExecuting())
			{
				bossBonusScoreLabel.Text = BossStandardBonusHandler.main?.GetCurrentBonus().ToString() ?? "0";
				yield return new WaitOneFrame();
			}
			if (currentBossOverlaySector == refSector) currentBossOverlaySector = null;
			bossOverlayAnimator.Play("Inactive");
		}

		public IEnumerator WaitForEndPlayerBombSector()
		{
			StageSector refSector = currentPlayerOverlaySector;
			while (currentPlayerOverlaySector == refSector && refSector.ShouldBeExecuting()) yield return new WaitOneFrame();
			if (currentPlayerOverlaySector == refSector) currentPlayerOverlaySector = null;
			playerOverlayAnimator.Play("Inactive");
		}

		public void StartBossBombOverlay(string displayName)
		{
			if (bossDisplayLabel != null) bossDisplayLabel.Text = displayName;
			StageSector stageSector = StageSector.GetCurrentSector();
			currentBossOverlaySector = stageSector;
			bossOverlayAnimator.Play("Activate");
			this.StartCoroutine(WaitForEndBossBombSector(), (c) =>
			{
				bossOverlayAnimator.Play("Inactive");
			});
		}

		public void StartPlayerBombOverlay(string displayName)
		{
			if (playerDisplayLabel != null) playerDisplayLabel.Text = displayName;
			StageSector stageSector = StageSector.GetCurrentSector();
			currentPlayerOverlaySector = stageSector;
			playerOverlayAnimator.Play("Activate");
			this.StartCoroutine(WaitForEndPlayerBombSector(), (c) =>
			{
				playerOverlayAnimator.Play("Inactive");
			});
		}

		public override void _Ready()
		{
			base._Ready();
			main = this;
		}

		public override void _Process(double delta)
		{
			base._Process(delta);
		}
	}
}
