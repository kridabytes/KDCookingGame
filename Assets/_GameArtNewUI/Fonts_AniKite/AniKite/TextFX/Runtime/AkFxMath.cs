using System;

namespace AniKite.TextFx
{
    /// <summary>AkTextFX maths: hash, easing and noise are
    /// ported from JIZURA src/01_util.js (github.com/852wa/JIZURA, MIT © 2026 hakoniwa).</summary>
    public static class AkFxMath
    {
        public const double PI = Math.PI, TAU = Math.PI * 2, DEG = Math.PI / 180;

        public static double Clamp(double x, double a = 0, double b = 1) => x < a ? a : x > b ? b : x;
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;
        public static double Inv(double a, double b, double x) => b == a ? 0 : (x - a) / (b - a);
        public static double Smooth(double a, double b, double x) { double t = Clamp(Inv(a, b, x)); return t * t * (3 - 2 * t); }

        /// <summary>JS ToInt32 (<c>x | 0</c>): NaN/∞ → 0, truncate, wrap modulo 2³².</summary>
        public static int ToInt32(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) return 0;
            double m = Math.Truncate(x) % 4294967296.0;
            if (m < 0) m += 4294967296.0;
            return unchecked((int)(uint)m);
        }

        public static uint H(double a, double b = 0, double c = 0, double d = 0, double e = 0)
        {
            unchecked
            {
                uint x = 0x9e3779b9u ^ (uint)ToInt32(a);
                x = (x ^ (x >> 16)) * 0x85ebca6bu;
                x = x + ((uint)ToInt32(b) + 0x632be5abu) * 0xc2b2ae35u;
                x = (x ^ (x >> 13)) * 0xc2b2ae35u;
                x = x + ((uint)ToInt32(c) + 0x5bd1e995u) * 0x27d4eb2fu;
                x = (x ^ (x >> 15)) * 0x165667b1u;
                x = x + ((uint)ToInt32(d) + 0x1b873593u) * 0x85ebca6bu;
                x = (x ^ (x >> 16)) * 0x27d4eb2fu;
                x = x + ((uint)ToInt32(e) + 0x68e31da4u) * 0x9e3779b1u;
                x ^= x >> 15; x *= 0x2c1b3c6du; x ^= x >> 12; x *= 0x297a2d39u; x ^= x >> 15;
                return x;
            }
        }

        public static double R(double a, double b = 0, double c = 0, double d = 0, double e = 0) => H(a, b, c, d, e) / 4294967296.0;
        public static double Rs(double a, double b = 0, double c = 0, double d = 0, double e = 0) => R(a, b, c, d, e) * 2 - 1;

        public static double Noise1(double x, double seed)
        {
            double i = Math.Floor(x), f = x - i, u = f * f * (3 - 2 * f);
            return Lerp(Rs(seed, i), Rs(seed, i + 1), u);
        }

        static double Bounce(double x)
        {
            x = Clamp(x);
            const double n1 = 7.5625, d1 = 2.75;
            if (x < 1 / d1) return n1 * x * x;
            if (x < 2 / d1) { x -= 1.5 / d1; return n1 * x * x + 0.75; }
            if (x < 2.5 / d1) { x -= 2.25 / d1; return n1 * x * x + 0.9375; }
            x -= 2.625 / d1; return n1 * x * x + 0.984375;
        }

        /// <summary>Index = ease id shared with the JS player.</summary>
        public static readonly string[] Eases =
        {
            "lin", "inQuad", "outQuad", "inCubic", "outCubic", "inOutCubic", "outQuart", "outQuint", "inOutQuart",
            "outExpo", "inExpo", "inOutExpo", "outBack", "outElastic", "inOutSine", "bounce",
        };
        public const int OutBack = 12;

        public static int EaseId(string name) => Array.IndexOf(Eases, name);

        /// <summary>p: outBack overshoot (NaN = default 1.9); ignored by the other eases.</summary>
        public static double Ease(int id, double x, double p = double.NaN)
        {
            switch (id)
            {
                case 0: return Clamp(x);
                case 1: x = Clamp(x); return x * x;
                case 2: x = Clamp(x); return 1 - (1 - x) * (1 - x);
                case 3: x = Clamp(x); return x * x * x;
                case 4: x = Clamp(x); return 1 - Math.Pow(1 - x, 3);
                case 5: x = Clamp(x); return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2;
                case 6: return 1 - Math.Pow(1 - Clamp(x), 4);
                case 7: return 1 - Math.Pow(1 - Clamp(x), 5);
                case 8: x = Clamp(x); return x < 0.5 ? 8 * x * x * x * x : 1 - Math.Pow(-2 * x + 2, 4) / 2;
                case 9: x = Clamp(x); return x >= 1 ? 1 : 1 - Math.Pow(2, -10 * x);
                case 10: x = Clamp(x); return x <= 0 ? 0 : Math.Pow(2, 10 * x - 10);
                case 11:
                    x = Clamp(x);
                    if (x <= 0 || x >= 1) return x;
                    return x < 0.5 ? Math.Pow(2, 20 * x - 10) / 2 : (2 - Math.Pow(2, -20 * x + 10)) / 2;
                case 12:
                {
                    double s = double.IsNaN(p) ? 1.9 : p;
                    x = Clamp(x); double c = s + 1;
                    return 1 + c * Math.Pow(x - 1, 3) + s * Math.Pow(x - 1, 2);
                }
                case 13:
                    x = Clamp(x);
                    if (x == 0 || x == 1) return x;
                    return Math.Pow(2, -10 * x) * Math.Sin((x * 10 - 0.75) * (TAU / 3)) + 1;
                case 14: x = Clamp(x); return -(Math.Cos(Math.PI * x) - 1) / 2;
                case 15: return Bounce(x);
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
    }
}
