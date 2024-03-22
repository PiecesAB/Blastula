using Godot;

namespace Blastula
{
    /// <summary>
    /// Extra math functions.
    /// </summary>
    public static class MoreMath
    {
        /// <summary>
        /// Performs a % n where a / n is rounded down.
        /// </summary>
        public static int RDMod(int a, int n)
        {
            if (a >= 0) { return a % n; }
            return (n - ((n - a) % n)) % n;
        }

        /// <summary>
        /// Performs a % n where a / n is rounded down.
        /// </summary>
        public static float RDMod(float a, float n)
        {
            if (a >= 0) { return a % n; }
            return (n - ((n - a) % n)) % n;
        }

        /// <summary>
        /// Performs a logarithm with base b. Godot has DegToRad but not this?
        /// </summary>
        public static float Log(float x, float b)
        {
            return Mathf.Log(x) / Mathf.Log(b);
        }

        /// <summary>
        /// Greatest common denominator.
        /// </summary>
        public static int GCD(int a, int b)
        {
            if (b == 0) { return a; }
            return GCD(b, a % b);
        }

        /// <summary>
        /// Least common multiple.
        /// </summary>
        public static int LCM(int a, int b)
        {
            return (a * b) / GCD(a, b);
        }

        /// <summary>
        /// Returns an approximate inverse normal function; two-tailed.
        /// How many standard deviations away from the mean would the left and right sides need to be
        /// to contain (x * 100)% of the area?
        /// </summary>
        public static float FastInvNorm(float x)
        {
            float n = Mathf.Sign(x);
            x = Mathf.Abs(x);

            if (x <= 0.5f) // 99% accurate at worst when 0<x<0.5
            {
                return n * Mathf.Max(1.35f * x - 0.01f, 0f);
            }
            else if (x < 1f)// 95% accurate at worst when 0.5<x<0.9999 and the worst error is: 22% too large at x=0.9999999 (then it's basically 1.000000)
            {
                return n * (0.885f * x - 0.242f * Log(1f - x, 2));
            }
            else
            {
                return n * float.PositiveInfinity;
            }
        }

        public static float MoveTowardsAngle(float current, float goal, float maxDelta)
        {
            float dif = RDMod(goal - current, Mathf.Tau);
            if (dif >= Mathf.Pi) { dif -= Mathf.Tau; }
            if (Mathf.Abs(dif) <= maxDelta) { return goal; }
            return current + Mathf.Sign(dif) * maxDelta;
        }

        public static Transform2D Slerp(this Transform2D from, Transform2D to, float t)
        {
            (Vector2 fp, float fr, Vector2 fs) = (from.Origin, from.Rotation, from.Scale);
            (Vector2 tp, float tr, Vector2 ts) = (to.Origin, to.Rotation, to.Scale);
            (Vector2 np, Vector2 ns) = (fp.Slerp(tp, t), fs.Lerp(ts, t));
            float nr = Mathf.LerpAngle(fr, tr, t);
            return new Transform2D(nr, ns, 0, np);
        }

        public static Transform2D Lerp(this Transform2D from, Transform2D to, float t)
        {
            (Vector2 fp, float fr, Vector2 fs) = (from.Origin, from.Rotation, from.Scale);
            (Vector2 tp, float tr, Vector2 ts) = (to.Origin, to.Rotation, to.Scale);
            (Vector2 np, Vector2 ns) = (fp.Lerp(tp, t), fs.Lerp(ts, t));
            float nr = Mathf.LerpAngle(fr, tr, t);
            return new Transform2D(nr, ns, 0, np);
        }
    }
}

