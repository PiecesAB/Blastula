using Blastula.Schedules;
using Blastula.Coroutine;
using Godot;
using System.Collections;
using Blastula.Sounds;

namespace Blastula.Graphics
{
	/// <summary>
	/// Idiosyncratic; manages the overlays which appear as the boss declares a special attack.
	/// </summary>
	public partial class BombOverlays : Node
	{
		[ExportGroup("Boss")]
		[Export] public AnimationPlayer bossOverlayAnimator;
		[Export] public Label bossDisplayLabel;
		[Export] public TextureRect bossTextureRect;
		[Export] public Label bossBonusScoreLabel;
		[ExportSubgroup("History")]
		[Export] public Label bossHistoryCodedBlocksLabel;
		[Export] public Control bossHistoryClassicVisibility;
		[Export] public Label bossHistoryClassicLabel;

		[ExportGroup("Player")]
		[Export] public Label playerDisplayLabel;
		[Export] public AnimationPlayer playerOverlayAnimator;

		public static BombOverlays main;

		[Signal] public delegate void OnBossBombOverlayEventHandler(string displayName, Texture2D texture);
		[Signal] public delegate void OnPlayerBombOverlayEventHandler(string displayName);

		private StageSector currentBossOverlaySector;
		private StageSector currentPlayerOverlaySector;

		public void PlayCommonSFX(string name)
		{
			CommonSFXManager.PlayByName(name);
		}

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

		public void StartBossBombOverlay(string displayName, Texture2D texture)
		{
			if (bossDisplayLabel != null) bossDisplayLabel.Text = displayName;
			if (bossTextureRect != null) bossTextureRect.Texture = texture;
			if (bossHistoryClassicLabel != null && bossHistoryClassicVisibility != null && bossHistoryCodedBlocksLabel != null)
			{
				var mode = HistoryHandler.main?.valueMode ?? HistoryHandler.ValueMode.Classic;
				var newText = HistoryHandler.main?.GetCurrentHistoryString() ?? "-";
				if (mode == HistoryHandler.ValueMode.Classic)
				{
					bossHistoryCodedBlocksLabel.Visible = false;
					bossHistoryClassicVisibility.Visible = true;
					bossHistoryClassicLabel.Text = newText;
				} 
				else if (mode == HistoryHandler.ValueMode.CodedBlocks)
				{
					bossHistoryCodedBlocksLabel.Visible = true;
					bossHistoryCodedBlocksLabel.Text = newText;
					bossHistoryClassicVisibility.Visible = false;
				} 
				else
				{
					throw new System.InvalidOperationException("???");
				}
			}
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
