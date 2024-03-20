using Godot;
using System.Runtime.InteropServices;

namespace Blastula.LowLevel
{
    /// <summary>
    /// Represents an unmanaged baked curve. Evaluated by linear interpolation.
    /// </summary>
    public unsafe struct UnsafeCurve
    {
        public UnsafeArray<float> samples;
        public bool initialized;
        public float startTime;
        public float endTime;
        public enum LoopMode { Neither, Right, Left, Both }
        public LoopMode loopMode;

        public float Evaluate(float time)
        {
            if (!initialized) { return 0; }
            if (samples.count == 0) { return 0; }
            if (samples.count == 1) { return samples[0]; }
            if (startTime == endTime) { return samples[0]; }
            if (time >= endTime)
            {
                if (loopMode == LoopMode.Neither || loopMode == LoopMode.Left)
                {
                    return samples[samples.count - 1];
                }
                else
                {
                    return Evaluate(startTime + MoreMath.RDMod(time - startTime, endTime - startTime));
                }
            }
            if (time < startTime)
            {
                if (loopMode == LoopMode.Neither || loopMode == LoopMode.Right)
                {
                    return samples[0];
                }
                else
                {
                    return Evaluate(startTime + MoreMath.RDMod(time - startTime, endTime - startTime));
                }
            }
            float progress = (time - startTime) / (endTime - startTime);
            if (progress < 0f) { progress = 0f; }
            if (progress >= 0.99999f) { progress = 0.99999f; }
            int lowerSample = Mathf.FloorToInt(progress * (samples.count - 1));
            int higherSample = lowerSample + 1;
            float segmentSize = (endTime - startTime) / (samples.count - 1);
            float lowerSamplePosition = lowerSample * segmentSize;
            float higherSamplePosition = higherSample * segmentSize;
            float subProgress = (time - lowerSamplePosition) / (higherSamplePosition - lowerSamplePosition);
            return Mathf.Lerp(samples[lowerSample], samples[higherSample], subProgress);
        }
    }

    public unsafe static class UnsafeCurveFunctions
    {
        public static UnsafeCurve* Create(Curve realCurve, float startTime, float endTime, UnsafeCurve.LoopMode loopMode, float stepFrames)
        {
            float stepSize = stepFrames / VirtualVariables.Persistent.SIMULATED_FPS;
            if (stepSize < 0.005f) { stepSize = 0.005f; }
            int sampleCount = (int)((endTime - startTime) / stepSize);
            if (sampleCount < 2) { sampleCount = 2; }
            UnsafeArray<float> samples = UnsafeArrayFunctions.Create<float>(sampleCount);
            for (int i = 0; i < sampleCount; ++i)
            {
                samples[i] = realCurve.Sample(i / (float)(sampleCount - 1));
            }
            UnsafeCurve* newCurve = (UnsafeCurve*)Marshal.AllocHGlobal(sizeof(UnsafeCurve));
            newCurve->samples = samples;
            newCurve->startTime = startTime;
            newCurve->endTime = endTime;
            newCurve->loopMode = loopMode;
            newCurve->initialized = true;
            return newCurve;
        }

        public static void Dispose(this ref UnsafeCurve curve)
        {
            curve.samples.Dispose();
            curve.initialized = false;
        }
    }
}

