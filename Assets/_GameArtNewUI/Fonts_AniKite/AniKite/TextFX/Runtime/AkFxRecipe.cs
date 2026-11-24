using System;
using System.Collections.Generic;
using System.Globalization;

namespace AniKite.TextFx
{
    /// <summary>One compiled track. mode 0 = tween, 1 = keys, 2 = expr.</summary>
    public sealed class AkFxTrack
    {
        public int prop, mode;
        public double period = 1;
        public double[] code;
        public double[] kt, kv, kp;
        public int[] ke;
        public double from0, from1, to0, to1, start, end, easeP;
        public bool fromRange, toRange;
        public int ease, salt;
    }

    /// <summary>One compiled phase.</summary>
    public sealed class AkFxPhase
    {
        public string name;
        public bool isEnter, isExit;
        /// <summary>Enter units hidden at q &lt;= 0, exit units at q &gt;= 1; false when the recipe says "hide": false.</summary>
        public bool hide = true;
        public string[] letNames;
        public double[][] letCode;
        public int unit, order, seed;
        public double dur, perUnit, spread, fps, pivotX, pivotY, ramp;
        public double[] tint;
        public AkFxTrack[] tracks;

        public double Length(int n) => dur + perUnit * n;

        // order:random ranks for the last n asked for (main-thread player, one cache per phase)
        int randomN = -1;
        double[] randomK;

        public double OrderK(int i, int n)
        {
            if (n <= 1) return 0;
            switch (order)
            {
                case 0: return (double)i / (n - 1);
                case 1: return (double)(n - 1 - i) / (n - 1);
                case 2: return Math.Abs(i - (n - 1) / 2.0) / ((n - 1) / 2.0);
                case 3: return 1 - Math.Abs(i - (n - 1) / 2.0) / ((n - 1) / 2.0);
            }
            if (randomN != n)
            {
                // rank = position when sorted by (h(seed, i, 7777), i) — same as the JS pairwise count
                var hs = new uint[n]; var ix = new int[n];
                for (int j = 0; j < n; j++) { hs[j] = AkFxMath.H(seed, j, 7777); ix[j] = j; }
                Array.Sort(ix, (a, b) => hs[a] != hs[b] ? hs[a].CompareTo(hs[b]) : a.CompareTo(b));
                randomK = new double[n];
                for (int r = 0; r < n; r++) randomK[ix[r]] = (double)r / (n - 1);
                randomN = n;
            }
            return i >= 0 && i < n ? randomK[i] : 0;
        }
    }

    /// <summary>Per-player scratch so evaluation never allocates.</summary>
    public sealed class AkFxScratch
    {
        public readonly double[] vars = new double[AkFxExpr.Vars.Length + AkFxExpr.MaxLets];
        public readonly double[] stack = new double[AkFxExpr.MaxStack];
        public readonly double[] tmp = new double[AkFxRecipe.NP];
    }

    /// <summary>Unit index of one glyph for each phase: (i, n) under that phase's unit grouping, plus that unit's box
    /// (ux, uy, uw, uh: em from the text-box centre, y down) and the text box W, H. hasGeo = false → synthetic mono layout.
    /// gv = the 16 glyph vars wi wn li ln ci cn wc wl wx wy ww wh lx ly lw lh, set with the geometry.</summary>
    public struct AkFxUnitIndex
    {
        public int ei, en, hi, hn, xi, xn;
        public bool hasGeo;
        public double eux, euy, euw, euh, hux, huy, huw, huh, xux, xuy, xuw, xuh, W, H;
        public double[] gv;
    }

    /// <summary>A glyph's char / word / line index, plus col of ncol = its index
    /// among the glyphs of its line, wc of wl = its index among the glyphs of its word.</summary>
    public struct AkFxGlyph
    {
        public int ci, cn, wi, wn, li, ln, col, ncol, wc, wl;
        public int Index(int unit) => unit == 0 ? ci : unit == 1 ? wi : li;
        public int Count(int unit) => unit == 0 ? cn : unit == 1 ? wn : ln;
    }

    /// <summary>Compiled "aktextfx/1" recipe and the pose maths.
    /// No UnityEngine here.</summary>
    public sealed class AkFxRecipe
    {
        /// <summary>clipL/R/T/B: fraction of the unit box cropped from that side before the transform.</summary>
        public static readonly string[] Props = { "x", "y", "rot", "skew", "scale", "sx", "sy", "alpha", "blur", "tint", "clipL", "clipR", "clipT", "clipB" };
        public static readonly bool[] Mul = { false, false, false, false, true, true, true, true, false, false, false, false, false, false };
        public static readonly double[] Neutral = { 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0 };
        public const int NP = 14, HIDDEN = NP;
        public const int X = 0, Y = 1, ROT = 2, SKEW = 3, SCALE = 4, SX = 5, SY = 6, ALPHA = 7, BLUR = 8, TINT = 9;
        public const int CLIPL = 10, CLIPR = 11, CLIPT = 12, CLIPB = 13;
        /// <summary>Synthetic layout: 0.6 x 1 em cells, lines 1.2 em apart.</summary>
        public const double MonoW = 0.6, MonoH = 1, MonoLine = 1.2;
        public const int IDLE = 0, ENTER = 1, HOLD = 2, EXIT = 3, DONE = 4;

        static readonly string[] Units = { "char", "word", "line" };
        static readonly string[] Orders = { "forward", "reverse", "center", "edges", "random" };

        public string id, name;
        public int seed;
        public AkFxPhase enter, hold, exit;
        public Dictionary<string, object> material;
        /// <summary>Bundled font id (<see cref="AkTextFxFonts"/>), "" = keep the text's own font.</summary>
        public string font = "";
        public AkFxEmitter[] particles = Array.Empty<AkFxEmitter>();

        /* ---------------- compile ---------------- */

        public static AkFxRecipe Compile(string json) => Compile(AkFxJson.Parse(json) as Dictionary<string, object>);

        public static AkFxRecipe Compile(Dictionary<string, object> r)
        {
            if (r == null || !(Get(r, "schema") is string s) || s != "aktextfx/1") throw new FormatException("schema must be \"aktextfx/1\"");
            int seed = AkFxMath.ToInt32(Num(Get(r, "seed"), 0));
            var fx = new AkFxRecipe
            {
                id = Get(r, "id") as string ?? "",
                seed = seed,
                material = Get(r, "material") as Dictionary<string, object>,
            };
            fx.name = Truthy(Get(r, "name")) ? Get(r, "name") as string : fx.id;
            fx.enter = CompilePhase(Get(r, "enter"), "enter", seed);
            fx.hold = CompilePhase(Get(r, "hold"), "hold", seed);
            fx.exit = CompilePhase(Get(r, "exit"), "exit", seed);
            fx.font = Get(r, "font") as string ?? "";
            fx.particles = AkFxParticles.Compile(Get(r, "particles"));
            return fx;
        }

        static object Get(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v : null;
        static bool Has(Dictionary<string, object> d, string k) => d.ContainsKey(k);
        static bool Truthy(object v) => v is string s ? s.Length > 0 : v != null && !(v is bool b && !b);

        /// <summary>JS <c>num(v, d)</c>: a finite number, else the default.</summary>
        static double Num(object v, double d) => v is double x && !double.IsNaN(x) && !double.IsInfinity(x) ? x : d;

        static string JsString(object v)
        {
            if (v is string s) return s;
            if (v is double d) return d.ToString("R", CultureInfo.InvariantCulture);
            if (v is bool b) return b ? "true" : "false";
            return v == null ? "null" : v.ToString();
        }

        public static double[] ParseColor(object v, double[] fallback)
        {
            if (!(v is string s) || s.Length == 0 || s[0] != '#') return fallback;
            string x = s.Substring(1);
            if (x.Length == 3 || x.Length == 4) { var c = new char[x.Length * 2]; for (int i = 0; i < x.Length; i++) c[i * 2] = c[i * 2 + 1] = x[i]; x = new string(c); }
            if (x.Length == 6) x += "FF";
            if (x.Length != 8) return fallback;
            foreach (char ch in x) if (!Uri.IsHexDigit(ch)) return fallback;
            var o = new double[4];
            for (int k = 0; k < 4; k++) o[k] = int.Parse(x.Substring(k * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return o;
        }

        static bool IsIdent(string k)
        {
            if (string.IsNullOrEmpty(k)) return false;
            for (int i = 0; i < k.Length; i++)
            {
                char c = k[i];
                bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_' || (i > 0 && c >= '0' && c <= '9');
                if (!ok) return false;
            }
            return true;
        }

        static int EaseOf(object name, string where)
        {
            string n = Truthy(name) ? JsString(name) : "lin";
            int id = AkFxMath.EaseId(n);
            if (id < 0) throw new FormatException($"{where}: unknown ease '{n}'");
            return id;
        }

        static AkFxPhase CompilePhase(object o, string name, int seed)
        {
            if (o == null) return null;
            if (!(o is Dictionary<string, object> ph)) throw new FormatException(name + ": must be an object");
            object u = Get(ph, "unit"), od = Get(ph, "order");
            int unit = Array.IndexOf(Units, Truthy(u) ? u as string : "char");
            int order = Array.IndexOf(Orders, Truthy(od) ? od as string : "forward");
            if (unit < 0) throw new FormatException($"{name}.unit: unknown '{JsString(u)}'");
            if (order < 0) throw new FormatException($"{name}.order: unknown '{JsString(od)}'");
            var pv = Get(ph, "pivot") as List<object>;

            var letNames = new List<string>(); var letCode = new List<double[]>();
            if (Get(ph, "let") is Dictionary<string, object> lets)
                foreach (var kv in lets)
                {
                    if (!IsIdent(kv.Key) || Array.IndexOf(AkFxExpr.Vars, kv.Key) >= 0 || letNames.Contains(kv.Key))
                        throw new FormatException($"{name}.let: bad name '{kv.Key}'");
                    if (letNames.Count >= AkFxExpr.MaxLets) throw new FormatException($"{name}.let: at most {AkFxExpr.MaxLets} names");
                    try { letCode.Add(AkFxExpr.Compile(JsString(kv.Value), letNames)); }
                    catch (FormatException e) { throw new FormatException($"{name}.let.{kv.Key}: {e.Message}"); }
                    letNames.Add(kv.Key);
                }

            var p = new AkFxPhase
            {
                name = name, isEnter = name == "enter", isExit = name == "exit",
                letNames = letNames.ToArray(), letCode = letCode.ToArray(),
                unit = unit, order = order,
                seed = AkFxMath.ToInt32(Num(Get(ph, "seed"), seed)),
                dur = Math.Max(0.01, Num(Get(ph, "dur"), 0.5)),
                perUnit = Math.Max(0, Num(Get(ph, "perUnit"), 0)),
                spread = AkFxMath.Clamp(Num(Get(ph, "spread"), 0.4), 0, 0.95),
                fps = Math.Max(0, Num(Get(ph, "fps"), 0)),
                pivotX = pv != null && pv.Count > 0 ? Num(pv[0], 0) : 0,
                pivotY = pv != null && pv.Count > 1 ? Num(pv[1], 0) : 0,
                ramp = Math.Max(0, Num(Get(ph, "ramp"), 0.25)),
                hide = !(Get(ph, "hide") is bool hb && !hb),
                tint = ParseColor(Get(ph, "tint"), new double[] { 1, 1, 1, 1 }),
            };
            var tl = Get(ph, "tracks") as List<object>;
            p.tracks = new AkFxTrack[tl?.Count ?? 0];
            for (int i = 0; i < p.tracks.Length; i++) p.tracks[i] = CompileTrack(tl[i] as Dictionary<string, object>, i, name, letNames);
            return p;
        }

        static AkFxTrack CompileTrack(Dictionary<string, object> tr, int ti, string phase, List<string> lets)
        {
            string where = $"{phase}.tracks[{ti}]";
            if (tr == null) throw new FormatException(where + ": must be an object");
            int pid = Array.IndexOf(Props, Get(tr, "prop") as string);
            if (pid < 0) throw new FormatException($"{where}: unknown prop '{JsString(Get(tr, "prop"))}'");
            int modes = (Has(tr, "expr") ? 1 : 0) + (Has(tr, "keys") ? 1 : 0) + (Has(tr, "to") || Has(tr, "from") ? 1 : 0);
            if (modes != 1) throw new FormatException($"{where}: needs exactly one of expr / keys / from+to");
            var o = new AkFxTrack { prop = pid, period = Num(Get(tr, "period"), 1) };
            if (Has(tr, "expr"))
            {
                o.mode = 2;
                try { o.code = AkFxExpr.Compile(JsString(Get(tr, "expr")), lets); }
                catch (FormatException e) { throw new FormatException($"{where}: {e.Message}"); }
                return o;
            }
            if (Has(tr, "keys"))
            {
                if (!(Get(tr, "keys") is List<object> keys) || keys.Count == 0) throw new FormatException($"{where}: keys must be a non-empty array");
                o.mode = 1;
                int m = keys.Count;
                o.kt = new double[m]; o.kv = new double[m]; o.ke = new int[m]; o.kp = new double[m];
                double last = double.NegativeInfinity;
                for (int j = 0; j < m; j++)
                {
                    if (!(keys[j] is List<object> k) || k.Count < 2) throw new FormatException($"{where}: key must be [t, v, ease?]");
                    double t = Num(k[0], double.NaN), v = Num(k[1], double.NaN);
                    if (double.IsNaN(t) || double.IsNaN(v) || t < last) throw new FormatException($"{where}: keys need increasing numeric t");
                    last = t;
                    o.kt[j] = t; o.kv[j] = v;
                    o.ke[j] = EaseOf(k.Count > 2 ? k[2] : null, where);
                    o.kp[j] = Num(k.Count > 3 ? k[3] : null, double.NaN);
                }
                return o;
            }
            double nd = Neutral[pid];
            Range(Get(tr, "from"), nd, out o.from0, out o.from1, out o.fromRange);
            Range(Get(tr, "to"), nd, out o.to0, out o.to1, out o.toRange);
            o.start = Num(Get(tr, "start"), 0); o.end = Num(Get(tr, "end"), 1);
            o.ease = EaseOf(Get(tr, "ease"), where);
            o.easeP = Num(Get(tr, "easeP"), double.NaN);
            o.salt = 100 + ti * 2;
            return o;
        }

        static void Range(object v, double d, out double a, out double b, out bool isRange)
        {
            if (v is List<object> l) { a = Num(l.Count > 0 ? l[0] : null, d); b = Num(l.Count > 1 ? l[1] : null, d); isRange = true; }
            else { a = b = Num(v, d); isRange = false; }
        }

        /* ---------------- evaluation ---------------- */

        public static double TrackValue(AkFxTrack tr, double x, double[] vars, AkFxExpr.Ctx ctx, double[] stack)
        {
            if (tr.mode == 2) return AkFxExpr.Run(tr.code, vars, ctx, stack);
            if (tr.mode == 1)
            {
                double[] kt = tr.kt, kv = tr.kv; int m = kt.Length;
                if (x <= kt[0]) return kv[0];
                if (x >= kt[m - 1]) return kv[m - 1];
                int j = 1; while (j < m - 1 && x > kt[j]) j++;
                double u = kt[j] == kt[j - 1] ? 1 : (x - kt[j - 1]) / (kt[j] - kt[j - 1]);
                return AkFxMath.Lerp(kv[j - 1], kv[j], AkFxMath.Ease(tr.ke[j], u, tr.kp[j]));
            }
            double f = tr.fromRange ? AkFxMath.Lerp(tr.from0, tr.from1, AkFxMath.R(ctx.seed, ctx.i, tr.salt)) : tr.from0;
            double to = tr.toRange ? AkFxMath.Lerp(tr.to0, tr.to1, AkFxMath.R(ctx.seed, ctx.i, tr.salt + 1)) : tr.to0;
            double uu = tr.end == tr.start ? (x >= tr.end ? 1 : 0) : (x - tr.start) / (tr.end - tr.start);
            return AkFxMath.Lerp(f, to, AkFxMath.Ease(tr.ease, AkFxMath.Clamp(uu), tr.easeP));
        }

        /// <summary>One phase's props (pre-weight) into out[0..NP). False = unit hidden. t = seconds into the phase.
        /// Geometry = n mono cells on one line, one word.</summary>
        public static bool EvalPhase(AkFxPhase ph, bool isHold, double t, int i, int n, double[] o, AkFxScratch sc) =>
            EvalPhase(ph, isHold, t, i, n, o, sc, (i + 0.5 - n / 2.0) * MonoW, 0, MonoW, MonoH, n * MonoW, MonoH, null);

        /// <summary>As above with the unit box (ux, uy, uw, uh), the text box (W, H), em, y down, and the 16 glyph vars
        /// (<see cref="AkFxUnitIndex.gv"/>); gv null = one word on one line, i of n.</summary>
        public static bool EvalPhase(AkFxPhase ph, bool isHold, double t, int i, int n, double[] o, AkFxScratch sc,
            double ux, double uy, double uw, double uh, double W, double H, double[] gv)
        {
            for (int k = 0; k < NP; k++) o[k] = Neutral[k];
            double T = isHold ? 0 : ph.Length(n);
            if (ph.fps > 0 && (isHold || t < T)) t = Math.Floor(t * ph.fps + 1e-9) / ph.fps;
            double p = isHold ? 0 : AkFxMath.Clamp(t / T);
            double kk = ph.OrderK(i, n);
            double q = isHold ? 0 : AkFxMath.Clamp((p - ph.spread * kk) / (1 - ph.spread));
            if (!isHold && ph.hide)
            {
                if (ph.isEnter && q <= 0) return false;
                if (ph.isExit && q >= 1) return false;
            }
            var vars = sc.vars;
            vars[0] = q; vars[1] = p; vars[2] = t; vars[3] = T; vars[4] = i; vars[5] = n; vars[6] = kk; vars[7] = ph.seed;
            vars[8] = AkFxMath.PI; vars[9] = AkFxMath.TAU; vars[10] = AkFxMath.DEG;
            int g0 = AkFxExpr.VarUx;
            vars[g0] = ux; vars[g0 + 1] = uy; vars[g0 + 2] = uw; vars[g0 + 3] = uh; vars[g0 + 4] = W; vars[g0 + 5] = H;
            int gg = AkFxExpr.VarGlyph;
            if (gv != null) for (int j = 0; j < AkFxExpr.NGlyph; j++) vars[gg + j] = gv[j];
            else
            {
                vars[gg] = 0; vars[gg + 1] = 1; vars[gg + 2] = 0; vars[gg + 3] = 1; vars[gg + 4] = i; vars[gg + 5] = n; vars[gg + 6] = i; vars[gg + 7] = n;
                for (int b = gg + 8; b < gg + 16; b += 4) { vars[b] = 0; vars[b + 1] = 0; vars[b + 2] = W; vars[b + 3] = H; }
            }
            var ctx = new AkFxExpr.Ctx { seed = ph.seed, i = i };
            int nv = AkFxExpr.Vars.Length;
            for (int j = 0; j < ph.letCode.Length; j++) vars[nv + j] = AkFxExpr.Run(ph.letCode[j], vars, ctx, sc.stack);
            for (int j = 0; j < ph.tracks.Length; j++)
            {
                var tr = ph.tracks[j];
                double x = isHold ? (tr.period > 0 ? t / tr.period - Math.Floor(t / tr.period) : 0) : q;
                double v = TrackValue(tr, x, vars, ctx, sc.stack);
                if (Mul[tr.prop]) o[tr.prop] *= v; else o[tr.prop] += v;
            }
            return true;
        }

        /// <summary>Full pose: out[0..NP) props, out[NP] = 1 when hidden. out must hold NP + 1.</summary>
        public void EvalPose(int state, double t, double holdT, AkFxUnitIndex iu, double[] o, AkFxScratch sc)
        {
            for (int k = 0; k < NP; k++) o[k] = Neutral[k];
            o[HIDDEN] = 0;
            if (state == IDLE || state == DONE) { o[HIDDEN] = 1; return; }
            var tmp = sc.tmp;
            if (state == ENTER && enter != null)
            {
                bool vis = iu.hasGeo ? EvalPhase(enter, false, t, iu.ei, iu.en, tmp, sc, iu.eux, iu.euy, iu.euw, iu.euh, iu.W, iu.H, iu.gv)
                                     : EvalPhase(enter, false, t, iu.ei, iu.en, tmp, sc);
                if (!vis) { o[HIDDEN] = 1; return; }
                Combine(o, tmp, 1);
            }
            if (state == EXIT && exit != null)
            {
                bool vis = iu.hasGeo ? EvalPhase(exit, false, t, iu.xi, iu.xn, tmp, sc, iu.xux, iu.xuy, iu.xuw, iu.xuh, iu.W, iu.H, iu.gv)
                                     : EvalPhase(exit, false, t, iu.xi, iu.xn, tmp, sc);
                if (!vis) { o[HIDDEN] = 1; return; }
                Combine(o, tmp, 1);
            }
            if ((state == HOLD || state == EXIT) && hold != null)
            {
                double w = hold.ramp > 0 ? AkFxMath.Smooth(0, hold.ramp, holdT) : 1;
                if (state == EXIT) w *= hold.ramp > 0 ? 1 - AkFxMath.Smooth(0, hold.ramp, t) : 0;
                if (w > 0)
                {
                    if (iu.hasGeo) EvalPhase(hold, true, holdT, iu.hi, iu.hn, tmp, sc, iu.hux, iu.huy, iu.huw, iu.huh, iu.W, iu.H, iu.gv);
                    else EvalPhase(hold, true, holdT, iu.hi, iu.hn, tmp, sc);
                    Combine(o, tmp, w);
                }
            }
            o[BLUR] = Math.Max(0, o[BLUR]);
            for (int k = TINT; k < NP; k++) o[k] = AkFxMath.Clamp(o[k]);
        }

        static void Combine(double[] o, double[] v, double w)
        {
            for (int k = 0; k < NP; k++)
            {
                double e = Neutral[k] + w * (v[k] - Neutral[k]);
                if (Mul[k]) o[k] *= e; else o[k] += e;
            }
        }

        public AkFxUnitIndex UnitIndex(AkFxGlyph g) => new AkFxUnitIndex
        {
            ei = enter != null ? g.Index(enter.unit) : 0, en = enter != null ? g.Count(enter.unit) : 1,
            hi = hold != null ? g.Index(hold.unit) : 0, hn = hold != null ? g.Count(hold.unit) : 1,
            xi = exit != null ? g.Index(exit.unit) : 0, xn = exit != null ? g.Count(exit.unit) : 1,
        };

        /// <summary>UnitIndex plus geometry: box[kind * 4 ..] = ux, uy, uw, uh of this glyph's char / word / line box.
        /// Allocates gv once per glyph (call on text / layout change, not per frame).</summary>
        public AkFxUnitIndex UnitIndex(AkFxGlyph g, double[] box, double W, double H)
        {
            var iu = UnitIndex(g);
            iu.hasGeo = true; iu.W = W; iu.H = H;
            iu.gv = new double[] { g.wi, g.wn, g.li, g.ln, g.col, g.ncol, g.wc, g.wl, box[4], box[5], box[6], box[7], box[8], box[9], box[10], box[11] };
            if (enter != null) { int b = enter.unit * 4; iu.eux = box[b]; iu.euy = box[b + 1]; iu.euw = box[b + 2]; iu.euh = box[b + 3]; }
            if (hold != null) { int b = hold.unit * 4; iu.hux = box[b]; iu.huy = box[b + 1]; iu.huw = box[b + 2]; iu.huh = box[b + 3]; }
            if (exit != null) { int b = exit.unit * 4; iu.xux = box[b]; iu.xuy = box[b + 1]; iu.xuw = box[b + 2]; iu.xuh = box[b + 3]; }
            return iu;
        }

        /// <summary>The phase whose unit grouping / pivot / tint drives the current state.</summary>
        public AkFxPhase ActivePhase(int state) => state == ENTER ? enter : state == EXIT ? exit : hold;

        /* ---------------- text → units ---------------- */

        /// <summary>JS <c>/\s/</c>.</summary>
        public static bool IsJsSpace(int c) =>
            c == ' ' || (c >= '\t' && c <= '\r') || c == 0xa0 || c == 0x1680 || (c >= 0x2000 && c <= 0x200a) ||
            c == 0x2028 || c == 0x2029 || c == 0x202f || c == 0x205f || c == 0x3000 || c == 0xfeff;

        /// <summary>Code points, '\n' = new line, whitespace splits words, empty lines dropped.</summary>
        public static List<AkFxGlyph> UnitsOf(string text)
        {
            var raw = new List<(int line, int word)>();
            int line = 0, word = -1; bool inWord = false;
            text = text ?? "";
            for (int i = 0; i < text.Length; i++)
            {
                int cp = text[i];
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) { cp = char.ConvertToUtf32(text[i], text[i + 1]); i++; }
                if (cp == '\n') { line++; inWord = false; continue; }
                if (IsJsSpace(cp)) { inWord = false; continue; }
                if (!inWord) { word++; inWord = true; }
                raw.Add((line, word));
            }
            return Number(raw, word + 1);
        }

        /// <summary>Builds glyph indices from (line, word) pairs in reading order; line ids are renumbered densely.</summary>
        public static List<AkFxGlyph> Number(List<(int line, int word)> raw, int wordCount)
        {
            int n = raw.Count;
            var lineIds = new List<int>();
            foreach (var g in raw) if (!lineIds.Contains(g.line)) lineIds.Add(g.line);
            var perLine = new Dictionary<int, int>(); var perWord = new Dictionary<int, int>();
            var o = new List<AkFxGlyph>(n);
            for (int i = 0; i < n; i++)
            {
                var (line, word) = raw[i];
                perLine.TryGetValue(line, out int col); perLine[line] = col + 1;
                perWord.TryGetValue(word, out int wc); perWord[word] = wc + 1;
                o.Add(new AkFxGlyph { ci = i, cn = n, wi = word, wn = wordCount, li = lineIds.IndexOf(line), ln = lineIds.Count, col = col, wc = wc });
            }
            for (int i = 0; i < n; i++) { var g = o[i]; g.ncol = perLine[raw[i].line]; g.wl = perWord[raw[i].word]; o[i] = g; }
            return o;
        }
    }

    /// <summary>Playback state machine.</summary>
    public sealed class AkFxTimeline
    {
        public AkFxRecipe fx;
        public int n, state;
        public double t, holdT;

        public AkFxTimeline(AkFxRecipe fx, int n) { this.fx = fx; this.n = n; }

        public double EnterLen => fx.enter != null ? fx.enter.Length(n) : 0;
        public double ExitLen => fx.exit != null ? fx.exit.Length(n) : 0;

        public void PlayIn() { state = fx.enter != null ? AkFxRecipe.ENTER : AkFxRecipe.HOLD; t = 0; holdT = 0; }

        public void PlayOut()
        {
            if (state == AkFxRecipe.DONE || state == AkFxRecipe.IDLE) return;
            state = fx.exit != null ? AkFxRecipe.EXIT : AkFxRecipe.DONE; t = 0;
        }

        public void Step(double dt)
        {
            if (state == AkFxRecipe.ENTER)
            {
                t += dt;
                if (t >= EnterLen) { holdT = t - EnterLen; state = AkFxRecipe.HOLD; t = 0; }
            }
            else if (state == AkFxRecipe.HOLD) holdT += dt;
            else if (state == AkFxRecipe.EXIT)
            {
                t += dt; holdT += dt;
                if (t >= ExitLen) { state = AkFxRecipe.DONE; t = 0; }
            }
        }

        /// <summary>Absolute clip time: enter → hold (holdFor s) → exit. For previews.</summary>
        public void SeekClip(double time, double holdFor)
        {
            double e = EnterLen, x = ExitLen;
            if (time < e) { state = AkFxRecipe.ENTER; t = time; holdT = 0; }
            else if (time < e + holdFor || fx.exit == null) { state = AkFxRecipe.HOLD; t = 0; holdT = time - e; }
            else if (time < e + holdFor + x) { state = AkFxRecipe.EXIT; t = time - e - holdFor; holdT = time - e; }
            else { state = AkFxRecipe.DONE; t = 0; holdT = time - e; }
        }
    }
}
