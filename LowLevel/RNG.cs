using Godot;
using System.Runtime.InteropServices;


namespace Blastula
{
    /// <summary>
    /// We obviously don't want true randomness in patterns and collectibles for a bullet game - that would ruin replays!
    /// But we also don't want the same game every time.
    /// Be sure to reseed and save the seed value at the beginning of every stage!
    /// </summary>
    public static class RNG
    {
        private static uint i1 = 31415;

        public static void Reseed(uint seed)
        {
            i1 = seed;
            if (i1 == 0) { i1 = 31415; }
        }

        public static int Int(int min, int max) //includes min, but excludes max.
        {
            if (min == max) { return min; }
            if (min + 1 == max) { return min; }
            if (min > max) { var temp = max; max = min; min = temp; }
            // This is a "xorshift" algorithm.
            unchecked
            {
                i1 ^= i1 << 13;
                i1 ^= i1 >> 17;
                i1 ^= i1 << 5;
                return (int)(i1 % (max - min)) + min;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct FloatInt
        {
            [FieldOffset(0)]
            float f;
            [FieldOffset(0)]
            int i;

            public FloatInt(int _i)
            {
                f = 0f;
                i = _i;
            }

            public float GetFloat()
            {
                return f;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct DoubleLong
        {
            [FieldOffset(0)]
            double d;
            [FieldOffset(0)]
            long l;

            public DoubleLong(long _l)
            {
                d = 0.0;
                l = _l;
            }

            public double GetDouble()
            {
                return d;
            }
        }

        public static int Sign() // Returns 1 or -1. (It's just a pain to type this out.)
        {
            return Int(0, 2) * 2 - 1;
        }

        public static float Single(float min = 0f, float max = 1f) // excludes max
        {
            if (min == max) { return min; }
            if (min > max) { var temp = max; max = min; min = temp; }
            FloatInt x = new FloatInt(0x3F800000 ^ Int(0, 8388608));
            float y = x.GetFloat() - 1f;
            return (y * (max - min)) + min;
        }

        public static double Double(double min = 0f, double max = 1f) // excludes max
        {
            if (min == max) { return min; }
            if (min > max) { var temp = max; max = min; min = temp; }
            DoubleLong x = new DoubleLong((0x3FF0000000000000 ^ Int(0, 67108864)) ^ ((long)Int(0, 67108864) << 26));
            double y = x.GetDouble() - 1f;
            return (y * (max - min)) + min;
        }

        // Returns a random point in/on a circle, distributed by area.
        public static Vector2 UnitCircle(bool surfaceOnly = false)
        {
            float a = Single(0f, 6.2831853f);
            Vector2 x = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            return (surfaceOnly) ? (x) : (x * Mathf.Sqrt(Single()));
        }

        // Returns a random point in/on a sphere, distributed by volume.
        public static Vector3 UnitSphere(bool surfaceOnly = false)
        {
            float r = -2f * Single() + 1f;
            float b = Mathf.Cos(Mathf.Acos(r) - 1.5707963f);
            Vector2 c = UnitCircle(true);
            Vector3 x = new Vector3(c.X, c.Y, 0) * b + new Vector3(0, 0, -r);
            return (surfaceOnly) ? (x) : (x * Mathf.Pow(Single(), 0.33333333f));
        }

        // Normally distributed random variable.
        // This could be useful for natural-looking decay and/or item effects.
        public static float NormalDist(float mean, float sd, float min = float.NegativeInfinity, float max = float.PositiveInfinity)
        {
            return Mathf.Clamp(Sign() * MoreMath.FastInvNorm(Single()) * sd + mean, min, max);
        }

        public static Color Color(float opacitySet = 1)
        {
            return new Color(Single(), Single(), Single(), (opacitySet < 0) ? Single() : opacitySet);
        }
    }
}
