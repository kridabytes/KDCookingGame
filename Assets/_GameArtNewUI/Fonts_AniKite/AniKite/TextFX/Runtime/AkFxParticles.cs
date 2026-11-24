using System;
using System.Collections.Generic;

namespace AniKite.TextFx
{
    /// <summary>A particle look. Units are em (the text's font size) and seconds; dir 0 = right, 90 = up;
    /// gravity &gt; 0 pulls down. alpha: 0 fade, 1 late, 2 flicker. cells index the <see cref="AkFxSprites"/> atlas.</summary>
    public sealed class AkFxParticlePreset
    {
        public string name;
        public int[] cells;
        public bool add;
        public int count, alpha;
        public double life0, life1, speed0, speed1, dir = 90, spread, gravity, drag, size0, size1, sizeEnd;
        public double grow, spin, stretch, wobble, wobbleF, flip, jitter = 0.3, dy;
        public bool randRot;
        public string[] colors;
    }

    /// <summary>One compiled <c>particles[]</c> entry of a recipe.</summary>
    public sealed class AkFxEmitter
    {
        public int preset, on, at, count;
        public double[][] colors;
        public double size, speed, life, dir, spread, dx, dy, every, delay;
        public bool back;
        public AkFxParticlePreset Preset => AkFxParticles.Presets[preset];
    }

    /// <summary>Particle clock: the timeline plus, in DONE, the hold time when the exit ended and the seconds since.</summary>
    public struct AkFxClock
    {
        public int state;
        public double t, holdT, tail;

        /// <summary>The clock after <see cref="AkFxTimeline.SeekClip"/>(time, holdFor).</summary>
        public static AkFxClock FromClip(AkFxTimeline tl, double time, double holdFor)
        {
            if (tl.state != AkFxRecipe.DONE) return new AkFxClock { state = tl.state, t = tl.t, holdT = tl.holdT };
            double x = tl.ExitLen;
            return new AkFxClock { state = AkFxRecipe.DONE, holdT = holdFor + x, tail = Math.Max(0, time - tl.EnterLen - holdFor - x) };
        }

        /// <summary>The clock of a timeline driven by <see cref="AkFxTimeline.Step"/> (holdT already frozen in DONE).</summary>
        public static AkFxClock FromStep(AkFxTimeline tl, double doneFor) =>
            new AkFxClock { state = tl.state, t = tl.t, holdT = tl.holdT, tail = tl.state == AkFxRecipe.DONE ? doneFor : 0 };
    }

    /// <summary>A burst alive at the current clock: emitter, unit (-1 = the whole text), unit kind, key, age in seconds.</summary>
    public struct AkFxBurst { public int e, u, kind, key; public double ago; }

    /// <summary>
    /// Stateless particle schedule + motion. Every frame recomputes
    /// the live bursts from the clock, so Seek, loops and replays always show the same particles.
    /// </summary>
    public static class AkFxParticles
    {
        public static readonly string[] EmitOn = { "unitIn", "unitLand", "enter", "land", "hold", "exit", "unitOut" };
        public static readonly string[] EmitAt = { "unit", "glyphs", "center", "text" };
        public const int MaxEmitters = 6, MaxEvents = 256, MaxParticles = 600, PState = 13;
        public const int AT_UNIT = 0, AT_GLYPHS = 1, AT_CENTER = 2, AT_TEXT = 3;

        static AkFxParticlePreset P(string name, int[] cells, bool add, int count, double l0, double l1, double s0, double s1, double spread,
            double gravity, double drag, double z0, double z1, double sizeEnd, string[] colors) => new AkFxParticlePreset
        {
            name = name, cells = cells, add = add, count = count, life0 = l0, life1 = l1, speed0 = s0, speed1 = s1, spread = spread,
            gravity = gravity, drag = drag, size0 = z0, size1 = z1, sizeEnd = sizeEnd, colors = colors, alpha = 1,
        };

        public static readonly AkFxParticlePreset[] Presets =
        {
            With(P("spark", new[] { 0 }, true, 8, 0.25, 0.5, 3, 7, 360, 2, 3, 0.08, 0.14, 0.3, new[] { "#FFF3B0", "#FFC94A", "#FFFFFF" }), p => { p.alpha = 0; p.stretch = 0.45; }),
            With(P("star", new[] { 1, 12 }, true, 6, 0.5, 0.9, 1.5, 3.5, 360, 1.5, 2.5, 0.18, 0.32, 0, new[] { "#FFE45C", "#FFFFFF", "#FFB13B" }), p => { p.grow = 0.08; p.spin = 240; p.randRot = true; p.jitter = 0.4; }),
            With(P("coin", new[] { 2 }, false, 6, 0.7, 1, 3, 5, 70, 9, 0.5, 0.28, 0.38, 1, new[] { "#FFFFFF" }), p => { p.grow = 0.06; p.flip = 14; }),
            With(P("confetti", new[] { 3, 13 }, false, 14, 0.9, 1.4, 3, 6, 120, 6, 2.2, 0.12, 0.2, 1, new[] { "#FF4D6D", "#FFD23F", "#3BCEAC", "#4D96FF", "#B15CFF" }), p => { p.spin = 540; p.randRot = true; p.wobble = 0.15; p.wobbleF = 9; p.flip = 11; p.jitter = 0.5; }),
            With(P("dust", new[] { 4, 14 }, false, 7, 0.45, 0.8, 0.6, 1.6, 160, -0.3, 4, 0.3, 0.55, 1.8, new[] { "#D9CBB5B0", "#BFAF98B0" }), p => { p.grow = 0.1; p.spin = 60; p.randRot = true; p.alpha = 0; p.jitter = 0.6; p.dy = 0.45; }),
            With(P("embers", new[] { 5 }, true, 8, 0.8, 1.4, 0.6, 1.4, 60, -1.2, 1, 0.05, 0.1, 0.2, new[] { "#FF7A1A", "#FFB347", "#FF3D00" }), p => { p.alpha = 2; p.wobble = 0.2; p.wobbleF = 5; p.jitter = 0.8; }),
            With(P("smoke", new[] { 14, 4 }, false, 5, 0.9, 1.5, 0.3, 0.8, 50, -0.8, 1.5, 0.35, 0.6, 2.2, new[] { "#5A5F6BC0", "#3E424AC0" }), p => { p.grow = 0.15; p.spin = 40; p.randRot = true; p.alpha = 0; p.wobble = 0.1; p.wobbleF = 2; p.jitter = 0.5; }),
            With(P("shards", new[] { 6 }, false, 8, 0.4, 0.7, 4, 8, 360, 7, 2.5, 0.12, 0.24, 0.6, new[] { "#FFFFFF", "#FF5A5A", "#FFD0D0" }), p => { p.spin = 720; p.randRot = true; p.jitter = 0.2; }),
            With(P("hearts", new[] { 7 }, false, 5, 0.9, 1.3, 0.8, 1.6, 50, -1.2, 1.5, 0.22, 0.34, 1, new[] { "#FF4D7E", "#FF8FB1" }), p => { p.grow = 0.12; p.wobble = 0.15; p.wobbleF = 4; p.jitter = 0.4; }),
            With(P("bubbles", new[] { 8 }, false, 6, 1, 1.6, 0.6, 1.2, 40, -1, 1.2, 0.12, 0.26, 1.2, new[] { "#BDEBFF", "#FFFFFF" }), p => { p.grow = 0.1; p.wobble = 0.12; p.wobbleF = 6; p.jitter = 0.6; }),
            With(P("lightning", new[] { 9 }, true, 3, 0.15, 0.3, 0.2, 0.6, 360, 0, 0, 0.4, 0.7, 1, new[] { "#B9F3FF", "#FFFFFF", "#7FD8FF" }), p => { p.randRot = true; p.alpha = 2; p.jitter = 0.4; }),
            With(P("pixels", new[] { 10 }, false, 12, 0.4, 0.8, 1, 3.5, 360, 3, 2, 0.07, 0.12, 1, new[] { "#00FFC6", "#FF2E88", "#FFFFFF" }), p => { p.alpha = 2; p.jitter = 0.5; }),
            With(P("ring", new[] { 11 }, true, 1, 0.35, 0.35, 0, 0, 0, 0, 0, 0.6, 0.6, 4, new[] { "#FFFFFF" }), p => { p.alpha = 0; p.jitter = 0; }),
        };

        static AkFxParticlePreset With(AkFxParticlePreset p, Action<AkFxParticlePreset> f) { f(p); return p; }

        public static int PresetId(string name)
        {
            for (int i = 0; i < Presets.Length; i++) if (Presets[i].name == name) return i;
            return -1;
        }

        /* ---------------- compile ---------------- */

        static object Get(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v : null;
        static double Num(object v, double d) => v is double x && !double.IsNaN(x) && !double.IsInfinity(x) ? x : d;
        static string Show(object v) => v == null ? "undefined" : v is double d ? d.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : v.ToString();

        public static AkFxEmitter[] Compile(object list)
        {
            if (list == null) return Array.Empty<AkFxEmitter>();
            if (!(list is List<object> l)) throw new FormatException("particles: must be an array");
            if (l.Count > MaxEmitters) throw new FormatException($"particles: at most {MaxEmitters} emitters");
            var o = new AkFxEmitter[l.Count];
            for (int i = 0; i < l.Count; i++)
            {
                string where = $"particles[{i}]";
                if (!(l[i] is Dictionary<string, object> em)) throw new FormatException(where + ": must be an object");
                int preset = Get(em, "preset") is string pn ? PresetId(pn) : -1;
                if (preset < 0) throw new FormatException($"{where}.preset: unknown '{Show(Get(em, "preset"))}'");
                object onV = Get(em, "on"), atV = Get(em, "at");
                int on = onV == null ? 1 : onV is string os ? Array.IndexOf(EmitOn, os) : -1;
                int at = atV == null ? 0 : atV is string ats ? Array.IndexOf(EmitAt, ats) : -1;
                if (on < 0) throw new FormatException($"{where}.on: unknown '{Show(onV)}'");
                if (at < 0) throw new FormatException($"{where}.at: unknown '{Show(atV)}'");
                var pr = Presets[preset];
                object cv = Get(em, "color");
                var cs = cv == null ? new List<object>(pr.colors) : cv is List<object> cl ? cl : new List<object> { cv };
                if (cs.Count == 0 || cs.Count > 8) throw new FormatException(where + ".color: 1 to 8 colours");
                var colors = new double[cs.Count][];
                for (int c = 0; c < cs.Count; c++)
                    colors[c] = AkFxRecipe.ParseColor(cs[c], null) ?? throw new FormatException($"{where}.color: bad colour '{Show(cs[c])}'");
                o[i] = new AkFxEmitter
                {
                    preset = preset, on = on, at = at, colors = colors,
                    count = (int)AkFxMath.Clamp(Math.Floor(Num(Get(em, "count"), pr.count) + 0.5), 1, 48),
                    size = AkFxMath.Clamp(Num(Get(em, "size"), 1), 0.05, 8),
                    speed = AkFxMath.Clamp(Num(Get(em, "speed"), 1), 0, 8),
                    life = AkFxMath.Clamp(Num(Get(em, "life"), 1), 0.1, 4),
                    dir = Num(Get(em, "dir"), pr.dir),
                    spread = AkFxMath.Clamp(Num(Get(em, "spread"), pr.spread), 0, 360),
                    dx = Num(Get(em, "dx"), 0), dy = Num(Get(em, "dy"), pr.dy),
                    every = Math.Max(0.05, Num(Get(em, "every"), 0.5)),
                    delay = Math.Max(0, Num(Get(em, "delay"), 0)),
                    back = Get(em, "back") is bool b && b,
                };
            }
            return o;
        }

        /* ---------------- schedule ---------------- */

        /// <summary>Bursts alive at <paramref name="clk"/>; counts = chars, words, lines. Clears and fills <paramref name="o"/>.</summary>
        public static void Events(AkFxRecipe fx, int nc, int nw, int nl, AkFxClock clk, List<AkFxBurst> o)
        {
            o.Clear();
            var ps = fx.particles; int s = clk.state;
            if (ps == null || ps.Length == 0 || s == AkFxRecipe.IDLE) return;
            int n = nc;
            double E = fx.enter != null ? fx.enter.Length(n) : 0, X = fx.exit != null ? fx.exit.Length(n) : 0;
            double A = s == AkFxRecipe.ENTER ? clk.t : s == AkFxRecipe.DONE ? E + clk.holdT + clk.tail : E + clk.holdT;
            double B = s == AkFxRecipe.EXIT ? clk.t : s == AkFxRecipe.DONE ? X + clk.tail : double.NegativeInfinity;
            double Hstop = s == AkFxRecipe.HOLD ? clk.holdT : s == AkFxRecipe.EXIT ? clk.holdT - clk.t : s == AkFxRecipe.DONE ? clk.holdT - X : double.NegativeInfinity;
            double Hreal = s == AkFxRecipe.DONE ? clk.holdT + clk.tail : clk.holdT;
            for (int e = 0; e < ps.Length; e++)
            {
                var em = ps[e];
                double life = em.Preset.life1 * em.life;
                void Push(int u, int kind, int key, double ago)
                {
                    if (ago >= 0 && ago < life && o.Count < MaxEvents) o.Add(new AkFxBurst { e = e, u = u, kind = kind, key = key, ago = ago });
                }
                switch (em.on)
                {
                    case 0: case 1:
                    {
                        var ph = fx.enter; if (ph == null) break;
                        int nu = ph.unit == 0 ? nc : ph.unit == 1 ? nw : nl; double T = ph.Length(nu);
                        for (int i = 0; i < nu; i++)
                        {
                            double k = ph.OrderK(i, nu);
                            Push(i, ph.unit, i, A - ((em.on == 0 ? ph.spread * k : ph.spread * k + 1 - ph.spread) * T + em.delay));
                        }
                        break;
                    }
                    case 2: Push(-1, 0, 0, A - em.delay); break;
                    case 3: Push(-1, 0, 0, A - E - em.delay); break;
                    case 4:
                    {
                        if (double.IsNegativeInfinity(Hstop)) break;
                        double jmax = Math.Floor((Hstop - em.delay) / em.every + 1e-9);
                        if (jmax < 0) break;
                        for (double j = Math.Max(0, jmax - Math.Ceiling(life / em.every)); j <= jmax; j++) Push(-1, 0, (int)j, Hreal - em.delay - j * em.every);
                        break;
                    }
                    case 5: Push(-1, 0, 0, B - em.delay); break;
                    case 6:
                    {
                        var ph = fx.exit; if (ph == null || double.IsNegativeInfinity(B)) break;
                        int nu = ph.unit == 0 ? nc : ph.unit == 1 ? nw : nl; double T = ph.Length(nu);
                        for (int i = 0; i < nu; i++) Push(i, ph.unit, i, B - (ph.spread * ph.OrderK(i, nu) * T + em.delay));
                        break;
                    }
                }
            }
        }

        /* ---------------- one particle ---------------- */

        /// <summary>Particle p of burst ev at <paramref name="age"/> s; sub = glyph + 1 for at:"glyphs", else 0.
        /// o[PState]: x, y (em from the burst origin, y up), size (em), rot (deg, ccw), sx, sy, alpha, r, g, b, cell, fx, fy.
        /// Returns false when the particle is dead.</summary>
        public static bool State(AkFxRecipe fx, AkFxBurst ev, int sub, int p, double age, double[] o)
        {
            var em = fx.particles[ev.e]; var pr = em.Preset;
            double a0 = fx.seed, b0 = ev.e * 4096 + sub, c0 = ev.key;
            double R(int salt) => AkFxMath.R(a0, b0, c0, p, salt);
            double life = AkFxMath.Lerp(pr.life0, pr.life1, R(1)) * em.life;
            if (!(age >= 0 && age < life)) return false;
            double ang = (em.dir + (R(2) - 0.5) * em.spread) * AkFxMath.DEG;
            double sp = AkFxMath.Lerp(pr.speed0, pr.speed1, R(3)) * em.speed;
            double vx0 = Math.Cos(ang) * sp, vy0 = Math.Sin(ang) * sp, g = pr.gravity, d = pr.drag;
            double x, y, vx, vy;
            if (d > 1e-6)
            {
                double ed = Math.Exp(-d * age), f = (1 - ed) / d;
                x = vx0 * f; y = vy0 * f - g * (age - f) / d; vx = vx0 * ed; vy = vy0 * ed - g * (1 - ed) / d;
            }
            else { x = vx0 * age; y = vy0 * age - 0.5 * g * age * age; vx = vx0; vy = vy0 - g * age; }
            double ph = R(10) * AkFxMath.TAU;
            if (pr.wobble > 0) x += pr.wobble * Math.Sin(age * pr.wobbleF + ph);
            x += (R(8) - 0.5) * pr.jitter + em.dx;
            y += (R(9) - 0.5) * pr.jitter - em.dy;
            double u = age / life;
            double size = AkFxMath.Lerp(pr.size0, pr.size1, R(4)) * em.size * AkFxMath.Lerp(1, pr.sizeEnd, u);
            if (pr.grow > 0) size *= AkFxMath.Smooth(0, pr.grow, age);
            double rot = (pr.randRot ? R(5) * 360 : 0) + pr.spin * (R(6) * 2 - 1) * age, sx = 1, sy = 1;
            if (pr.stretch > 0) { rot = Math.Atan2(vy, vx) / AkFxMath.DEG; sx = 1 + pr.stretch * Math.Sqrt(vx * vx + vy * vy); }
            if (pr.flip > 0) sy = Math.Cos(age * pr.flip + ph);
            double al = pr.alpha == 0 ? 1 - u : 1 - AkFxMath.Smooth(0.6, 1, u);
            if (pr.alpha == 2) al *= 0.6 + 0.4 * Math.Cos(age * 37 + ph);
            var col = em.colors[Math.Min(em.colors.Length - 1, (int)Math.Floor(R(7) * em.colors.Length))];
            int cell = pr.cells[Math.Min(pr.cells.Length - 1, (int)Math.Floor(R(11) * pr.cells.Length))];
            o[0] = x; o[1] = y; o[2] = size; o[3] = rot; o[4] = sx; o[5] = sy; o[6] = al * col[3];
            o[7] = col[0]; o[8] = col[1]; o[9] = col[2]; o[10] = cell;
            o[11] = em.at == AT_TEXT ? R(12) - 0.5 : 0; o[12] = em.at == AT_TEXT ? R(13) - 0.5 : 0;
            return true;
        }
    }
}
