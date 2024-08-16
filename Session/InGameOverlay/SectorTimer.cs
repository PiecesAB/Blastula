using Blastula.Coroutine;
using Blastula.Input;
using Blastula.Schedules;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Threading.Tasks;

namespace Blastula
{
    /// <summary>
    /// Display for remaining seconds until the StageSector times out (ends forcibly).
    /// It turns red and makes warning sounds when the time gets low, and disappears when time is irrelevant.
    /// </summary>
    public partial class SectorTimer : Control
    {
        [Export] public Label integerPart;
        [Export] public Label fractionalPart;
        [Export] public Control mainViewHolder;

        private ulong animationIteration = 0;
        private double nextWarnSoundTime;
        private bool fadedInCompletely = false;

        private float origScale;
        private void SetScale(float scale)
        {
            Scale = origScale * scale * Vector2.One;
        }

        private int scaleTimer = 0;

        private IEnumerator Countdown()
        {
            nextWarnSoundTime = 5;
            ulong currIteration = animationIteration;
            while (currIteration == animationIteration && StageSector.GetTimeRemaining() > 0)
            {
                double timeRemaining = StageSector.GetTimeRemaining();

                if (timeRemaining <= nextWarnSoundTime)
                {
                    CommonSFXManager.PlayByName("TimeoutWarning");
                    scaleTimer = 5;
                    nextWarnSoundTime -= 1.0;
                    if (nextWarnSoundTime < 1) { nextWarnSoundTime = -99999; }
                }

                if (scaleTimer > 0)
                {
                    scaleTimer--;
                    SetScale(1f + 0.05f * scaleTimer);
                }

                // Process the time into integer and fractional portions
                string timeRemainingString = "999.99";
                if (timeRemaining < 999.99)
                {
                    timeRemainingString = timeRemaining.ToString("F2");
                }
                string integerString = timeRemainingString.Substring(0, timeRemainingString.Length - 3);
                if (integerString.Length == 1) { integerString = "0" + integerString; }
                string fractionalString = timeRemainingString.Substring(timeRemainingString.Length - 2, 2);
                integerPart.Text = integerString;
                fractionalPart.Text = fractionalString;

                // Turn the counter gradually red in the last seconds
                float redness = Mathf.Max(0, 0.1f * (8f - (float)timeRemaining));
                Modulate = new Color(1f, 1f, 1f, Modulate.A).Lerp(new Color(1f, 0f, 0f, Modulate.A), redness);

                if (fadedInCompletely)
                {
                    // Fade out if player is nearby
                    bool playerIsClose = false;
                    Vector2 apparentWorldPosition = (GlobalPosition + Size * 0.5f) - (mainViewHolder.GlobalPosition + mainViewHolder.Size * 0.5f);
                    foreach (var kv in Player.playersByControl)
                    {
                        if (kv.Value != null && (kv.Value.GlobalPosition - apparentWorldPosition).Length() < 120)
                        {
                            playerIsClose = true;
                        }
                    }
                    float newA = Modulate.A;
                    if (playerIsClose)
                    {
                        newA = Mathf.MoveToward(newA, 0.4f, 0.1f);
                    }
                    else
                    {
                        newA = Mathf.MoveToward(newA, 1f, 0.1f);
                    }
                    Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, newA);
                }

                yield return new WaitOneFrame();
            }
            if (currIteration == animationIteration) { this.StartCoroutine(FadeOut()); }
        }

        private IEnumerator FadeInAndCountdown()
        {
            ulong currIteration = ++animationIteration;
            fadedInCompletely = false;
            this.StartCoroutine(Countdown());
            SetScale(1f);
            if (Modulate.A == 1) 
            { 
                fadedInCompletely = true; 
                yield break; 
            }
            for (int i = 1; i <= 20; ++i)
            {
                if (currIteration != animationIteration) { break; }
                Modulate = new Color(1, 1, 1, i / 20f);
                yield return new WaitOneFrame();
            }
            if (currIteration == animationIteration)
            {
                fadedInCompletely = true;
            }
        }

        private IEnumerator FadeOut()
        {
            integerPart.Text = "00";
            fractionalPart.Text = "00";
            ulong currIteration = ++animationIteration;
            SetScale(1f);
            if (Modulate.A == 0) { yield break; }
            for (int i = 19; i >= 0; --i)
            {
                if (currIteration != animationIteration) { break; }
                Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, i / 20f);
                yield return new WaitOneFrame();
            }
        }

        public void StageSectorChanged(StageSector newSector)
        {
            if (StageSector.GetTimeRemaining() > 0)
            {
                this.StartCoroutine(FadeInAndCountdown());
            }
            else
            {
                this.StartCoroutine(FadeOut());
            }
        }

        public override void _Ready()
        {
            Visible = true;
            ProcessPriority = Persistent.Priorities.RENDER;
            origScale = Scale.X;
            Modulate = new Color(1, 1, 1, 0);
            if (StageSector.GetCurrentSector() != null && StageSector.GetCurrentSector().shouldUseTimer)
            {
                this.StartCoroutine(FadeInAndCountdown());
            }
            StageManager.main.Connect(
                StageManager.SignalName.StageSectorChanged, 
                new Callable(this, MethodName.StageSectorChanged)
            );
        }
    }
}
