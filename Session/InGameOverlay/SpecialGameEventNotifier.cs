using Blastula.Graphics;
using Blastula.Input;
using Blastula.Menus;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using System.Numerics;

namespace Blastula
{
    /// <summary>
    /// Handles the display that shows special game events such as getting a score extend or high score.
    /// </summary>
    public partial class SpecialGameEventNotifier : Control
    {
        public enum EventType
        {
            Extend, CaptureBonus, HighScore
        }

        [Export] public AnimationPlayer animationPlayer;
        [Export] public Label bonusScoreLabel;

        public static SpecialGameEventNotifier main { get; private set; }

        public override void _Ready()
        {
            base._Ready();
            main = this;
            // Set the animator time to something very late, as if it has already finished long ago.
            animationPlayer.Seek(1000, true);
        }

        public void ResetBonusShaderTime(string paramName)
        {
            ((ShaderMaterial)bonusScoreLabel.Material).SetShaderParameter(paramName, -10);
        }

        public void SetBonusShaderTime(string paramName)
        {
            ((ShaderMaterial)bonusScoreLabel.Material).SetShaderParameter(paramName, BulletRendererManager.GetStageTimeGlobalValue());
        }

        public void PlayCommonSFX(string sfxName)
        {
            CommonSFXManager.PlayByName(sfxName);
        }

        public static void SetBonusScore(BigInteger newBonusScore)
        {
            if (main == null) return;
            main.bonusScoreLabel.Text = ScoreDisplay.GetScoreString(newBonusScore);
        }

        public static void Trigger(EventType eventType)
        {
            if (main == null) return;
            main.animationPlayer.Play(eventType.ToString());
        }
    }
}
