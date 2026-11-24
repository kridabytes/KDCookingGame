using System;
using UnityEngine;

namespace AniKite.TextFx
{
    /// <summary>
    /// The particle sprite atlas: 4 × 4 cells of 128 px drawn from shape functions at start-up, no texture
    /// files. Sprites are white (tinted by vertex colour) except the coin.
    /// 0 spark streak · 1 star · 2 coin · 3 confetti strip · 4 soft dot · 5 ember · 6 shard · 7 heart · 8 bubble ·
    /// 9 lightning · 10 pixel · 11 ring · 12 sparkle · 13 disc · 14 smoke puff · 15 plus.
    /// </summary>
    public static class AkFxSprites
    {
        public const int Cells = 4, CellPx = 128, Size = Cells * CellPx, SS = 3;
        public const string AdditiveShaderName = "AniKite/TextFx/UI Additive";

        static Texture2D atlas;
        static Material additive;

        public static Texture2D Atlas
        {
            get
            {
                if (atlas) return atlas;
                var px = Pixels();
                atlas = new Texture2D(Size, Size, TextureFormat.RGBA32, true)
                {
                    name = "AkTextFx Particles", hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
                };
                atlas.SetPixels32(px);
                atlas.Apply(true, true);
                return atlas;
            }
        }

        /// <summary>Additive UI material (Blend SrcAlpha One, stencil + RectMask2D aware). Null when the shader is missing.</summary>
        public static Material AdditiveMaterial
        {
            get
            {
                if (additive) return additive;
                var sh = Shader.Find(AdditiveShaderName);
                if (sh == null) return null;
                additive = new Material(sh) { name = "AkTextFx Additive", hideFlags = HideFlags.HideAndDontSave };
                return additive;
            }
        }

        /// <summary>UV rect of a cell (row 0 = top row of the atlas), inset by half a texel.</summary>
        public static Rect Uv(int cell)
        {
            int col = cell & 3, row = (cell >> 2) & 3;
            const float c = 1f / Cells, h = 0.5f / Size;
            return Rect.MinMaxRect(col * c + h, 1 - (row + 1) * c + h, (col + 1) * c - h, 1 - row * c - h);
        }

        /// <summary>RGBA pixels, Unity order (row 0 = bottom).</summary>
        public static Color32[] Pixels()
        {
            var o = new Color32[Size * Size];
            var rgb = new double[3];
            for (int cell = 0; cell < Cells * Cells; cell++)
            {
                int cx = (cell & 3) * CellPx, cyTop = (cell >> 2) * CellPx;
                for (int py = 0; py < CellPx; py++)
                    for (int px = 0; px < CellPx; px++)
                    {
                        double a = 0, r = 0, g = 0, b = 0;
                        for (int sy = 0; sy < SS; sy++)
                            for (int sx = 0; sx < SS; sx++)
                            {
                                // cell space: u, v in [-1.04, 1.04], v up
                                double u = ((px + (sx + 0.5) / SS) / CellPx * 2 - 1) * 1.04;
                                double v = -((py + (sy + 0.5) / SS) / CellPx * 2 - 1) * 1.04;
                                double c = Shape(cell, u, v, rgb);
                                a += c; r += rgb[0] * c; g += rgb[1] * c; b += rgb[2] * c;
                            }
                        double k = 1.0 / (SS * SS);
                        if (a > 0) { r /= a; g /= a; b /= a; } else { Shape(cell, 0, 0, rgb); r = rgb[0]; g = rgb[1]; b = rgb[2]; }
                        int y = Size - 1 - (cyTop + py); // flip: atlas row 0 is the top in the shape space
                        o[y * Size + cx + px] = new Color32(B(r), B(g), B(b), B(a * k));
                    }
            }
            return o;
        }

        static byte B(double x) => (byte)Math.Round(Math.Max(0, Math.Min(1, x)) * 255);

        static double Sm(double a, double b, double x) { double t = Math.Max(0, Math.Min(1, (x - a) / (b - a))); return t * t * (3 - 2 * t); }

        static bool Poly(double[] p, double u, double v)
        {
            bool inside = false;
            for (int i = 0, j = p.Length / 2 - 1; i < p.Length / 2; j = i++)
            {
                double xi = p[i * 2], yi = p[i * 2 + 1], xj = p[j * 2], yj = p[j * 2 + 1];
                if ((yi > v) != (yj > v) && u < (xj - xi) * (v - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside;
        }

        static readonly double[] Star = MakeStar();
        static double[] MakeStar()
        {
            var p = new double[20];
            for (int k = 0; k < 10; k++)
            {
                double a = (90 + k * 36) * Math.PI / 180, rad = k % 2 == 0 ? 0.95 : 0.4;
                p[k * 2] = Math.Cos(a) * rad; p[k * 2 + 1] = Math.Sin(a) * rad;
            }
            return p;
        }
        static readonly double[] Shard = { 0.05, 0.95, -0.8, -0.75, 0.9, -0.4 };
        static readonly double[] Bolt = { 0.2, 0.98, -0.55, -0.08, -0.05, -0.08, -0.3, -0.98, 0.55, 0.18, 0.05, 0.18, 0.4, 0.98 };
        static readonly double[] Puffs = { -0.38, -0.15, 0.5, 0.35, -0.2, 0.48, 0, 0.22, 0.58, 0.05, -0.35, 0.45 };

        /// <summary>Coverage 0..1 of one sample; rgb gets the sample colour (white for all but the coin).</summary>
        public static double Shape(int cell, double u, double v, double[] rgb)
        {
            rgb[0] = rgb[1] = rgb[2] = 1;
            double r = Math.Sqrt(u * u + v * v), t;
            switch (cell)
            {
                case 0: t = 1 - (u / 0.95) * (u / 0.95) - (v / 0.2) * (v / 0.2); return t > 0 ? Math.Min(1, t * 1.6) : 0;
                case 1: return Poly(Star, u, v) ? 1 : 0;
                case 2:
                    if (r >= 0.92) return 0;
                    if ((u + 0.28) * (u + 0.28) + (v - 0.32) * (v - 0.32) < 0.03) { rgb[0] = 1; rgb[1] = 0.95; rgb[2] = 0.72; }
                    else if (r > 0.76) { rgb[0] = 0.85; rgb[1] = 0.59; rgb[2] = 0.12; }
                    else if (Math.Abs(u) < 0.16 && Math.Abs(v) < 0.46) { rgb[0] = 1; rgb[1] = 0.89; rgb[2] = 0.47; }
                    else { rgb[0] = 0.96; rgb[1] = 0.76; rgb[2] = 0.26; }
                    return 1;
                case 3: return Math.Abs(u) < 0.92 && Math.Abs(v) < 0.5 ? 1 : 0;
                case 4: t = Math.Max(0, 1 - r / 0.95); return t * t * (3 - 2 * t);
                case 5: t = Math.Max(0, 1 - r / 0.95); return Math.Min(1, Math.Pow(t, 1.5) * 1.4);
                case 6: return Poly(Shard, u, v) ? 1 : 0;
                case 7:
                {
                    double x = u * 1.2, y = v * 1.25 + 0.2, q = x * x + y * y - 1;
                    return q * q * q - x * x * y * y * y <= 0 ? 1 : 0;
                }
                case 8:
                {
                    double ring = 1 - Sm(0.04, 0.08, Math.Abs(r - 0.84));
                    double fill = r < 0.84 ? 0.18 : 0;
                    double hi = (u + 0.35) * (u + 0.35) + (v - 0.38) * (v - 0.38) < 0.04 ? 0.95 : 0;
                    return Math.Max(ring, Math.Max(fill, hi));
                }
                case 9: return Poly(Bolt, u, v) ? 1 : 0;
                case 10: return Math.Abs(u) < 0.85 && Math.Abs(v) < 0.85 ? 1 : 0;
                case 11: return 1 - Sm(0.05, 0.12, Math.Abs(r - 0.8));
                case 12:
                {
                    double s = Math.Sqrt(Math.Abs(u)) + Math.Sqrt(Math.Abs(v));
                    double core = s <= Math.Sqrt(0.95) ? 1 : 0;
                    return Math.Max(core, 0.35 * Math.Max(0, 1 - r / 0.6));
                }
                case 13: return r < 0.9 ? 1 : 0;
                case 14:
                {
                    double a = 0;
                    for (int i = 0; i < Puffs.Length; i += 3)
                    {
                        double dx = u - Puffs[i], dy = v - Puffs[i + 1], d = Math.Sqrt(dx * dx + dy * dy), rr = Puffs[i + 2];
                        a = Math.Max(a, 1 - Sm(rr * 0.55, rr, d));
                    }
                    return a * 0.9;
                }
                case 15: return (Math.Abs(u) < 0.25 && Math.Abs(v) < 0.85) || (Math.Abs(v) < 0.25 && Math.Abs(u) < 0.85) ? 1 : 0;
            }
            return 0;
        }
    }
}
