using Blastula.Coroutine;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Blastula.Operations
{
    /// <summary>
    /// Shakes the camera for impact effects or otherwise.
    /// Shake effects don't stack; simultaneous shaking instances will only appear as the most intense instance.
    /// </summary>
    /// <remarks>
    /// It isn't recommended to use very vigorous shaking, because it can harm precision gameplay.
    /// Also, when using it in schedule contexts, be sure to keep in mind this will run instantly,
    /// even though the effect has a duration.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/shakeCamera.png")]
    public partial class ShakeCamera : Discrete
	{
        /// <summary>
        /// The unique name of the Camera2D in the main scene of the game. Doesn't include "%".
        /// </summary>
        [Export] public string uniqueCameraName = "Camera";
        /// <summary>
        /// The radius of the shaking repositioning.
        /// </summary>
        [Export] public string intensity = "3";
        /// <summary>
        /// The duration of the shaking.
        /// </summary>
        [Export] public string duration = "1.5";
        [Export] public Wait.TimeUnits durationUnits = Wait.TimeUnits.Seconds;
        [Export(PropertyHint.ExpEasing, "attenuation, positive_only")] public float easing = 1;

        private static Vector2 shakeValue = Vector2.Zero;
        private static float shakeCurrentIntensity = 0;
        private static ulong shakeUpdateStageFrame = 0;

        private float GetSecondsDuration()
        {
            float solvedDuration = Solve(PropertyName.duration).AsSingle();
            if (durationUnits == Wait.TimeUnits.Frames)
            {
                solvedDuration /= Persistent.SIMULATED_FPS;
            }
            return solvedDuration;
        }

        private void TryToResetShake()
        {
            if (shakeUpdateStageFrame < FrameCounter.stageFrame)
            {
                shakeUpdateStageFrame = FrameCounter.stageFrame;
                shakeCurrentIntensity = 0;
                shakeValue = Vector2.Zero;
            }
        }

        private void TryToIncreaseShake(float testIntensity)
        {
            if (testIntensity > shakeCurrentIntensity)
            {
                shakeCurrentIntensity = testIntensity;
                var r1 = (float)GD.RandRange(-testIntensity, testIntensity);
                var r2 = (float)GD.RandRange(-testIntensity, testIntensity);
                shakeValue = new Vector2(r1, r2);
            }
        }

        private long processIteration = 0;
        private IEnumerator FakeProcess(float intensity, float dur)
        {
            if (dur <= 0) { yield break; }
            Camera2D camNode = (Camera2D)Persistent.GetMainScene()?.GetNode($"%{uniqueCameraName}");
            if (camNode == null) { yield break; }
            long currProcessIter = ++processIteration;
            float t = 0;
            while (t < dur - 0.0001f && currProcessIter == processIteration)
            {
                Vector2 oldShakeValue = shakeValue;
                TryToResetShake();
                float progress = t / dur;
                float currentMultiplier = Mathf.Pow(1f - progress, easing);
                TryToIncreaseShake(currentMultiplier * intensity);
                camNode.Offset += shakeValue - oldShakeValue;
                yield return new WaitOneFrame();
                t += (float)Engine.TimeScale / Persistent.SIMULATED_FPS;
            }

            Cancel(null);
        }

        public void Cancel(CoroutineUtility.Coroutine _)
        {
            Camera2D camNode = (Camera2D)Persistent.GetMainScene()?.GetNode($"%{uniqueCameraName}");
            if (camNode == null) { return; }
            Vector2 oldShakeValue = shakeValue;
            TryToResetShake();
            camNode.Offset += shakeValue - oldShakeValue;
        }

        public override void Run()
        {
            shakeUpdateStageFrame = 0;
            var coroutine = this.StartCoroutine(FakeProcess(
                Solve(PropertyName.intensity).AsSingle(), 
                GetSecondsDuration()
            ));
            coroutine.cancel = Cancel;
        }
    }
}
