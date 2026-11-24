using System;
using System.Collections.Generic;
using System.Globalization;

namespace AniKite.TextFx
{
    /// <summary>AkTextFX expression language: source → flat RPN code, run on a fixed stack without allocating.</summary>
    public static class AkFxExpr
    {
        public const int CONST = 0, VAR = 1, ADD = 2, SUB = 3, MUL = 4, DIV = 5, MOD = 6, LT = 7, LE = 8, GT = 9, GE = 10, EQ = 11,
            NE = 12, AND = 13, OR = 14, NEG = 15, NOT = 16, SEL = 17, CALL = 18;

        /// <summary>ux uy uw uh W H: unit box from the text-box centre and the text box, em, y down.</summary>
        public static readonly string[] Vars = { "q", "p", "t", "T", "i", "n", "k", "seed", "PI", "TAU", "DEG", "ux", "uy", "uw", "uh", "W", "H",
            "wi", "wn", "li", "ln", "ci", "cn", "wc", "wl", "wx", "wy", "ww", "wh", "lx", "ly", "lw", "lh" };
        /// <summary>vars[VarUx .. VarUx + NGeo) = ux uy uw uh W H then the 16 glyph vars from VarGlyph (wi .. lh).</summary>
        public const int VarUx = 11, VarGlyph = 17, NGlyph = 16, NGeo = 22;
        public const int MaxStack = 64, MaxLets = 8;

        struct Fn { public string name; public int lo, hi; public Fn(string n, int l, int h) { name = n; lo = l; hi = h; } }

        static readonly Fn[] Fns = BuildFns();
        public static readonly int FnEase0 = Fns.Length - AkFxMath.Eases.Length; // = JS FN_EASE0 (FNS.length); Fns is initialised first

        static Fn[] BuildFns()
        {
            var l = new List<Fn>
            {
                new Fn("sin", 1, 1), new Fn("cos", 1, 1), new Fn("tan", 1, 1), new Fn("asin", 1, 1), new Fn("acos", 1, 1),
                new Fn("atan", 1, 1), new Fn("atan2", 2, 2), new Fn("abs", 1, 1), new Fn("sign", 1, 1), new Fn("floor", 1, 1),
                new Fn("ceil", 1, 1), new Fn("round", 1, 1), new Fn("fract", 1, 1), new Fn("sqrt", 1, 1), new Fn("exp", 1, 1),
                new Fn("log", 1, 1), new Fn("pow", 2, 2), new Fn("min", 2, 8), new Fn("max", 2, 8), new Fn("clamp", 1, 3),
                new Fn("lerp", 3, 3), new Fn("inv", 3, 3), new Fn("smooth", 3, 3), new Fn("step", 2, 2), new Fn("stg", 3, 3),
                new Fn("r", 1, 3), new Fn("rs", 1, 3), new Fn("ru", 1, 3), new Fn("noise", 2, 2), new Fn("noiseu", 2, 2), new Fn("hash", 1, 3),
            };
            for (int id = 0; id < AkFxMath.Eases.Length; id++) l.Add(new Fn(AkFxMath.Eases[id], 1, id == AkFxMath.OutBack ? 2 : 1));
            return l.ToArray();
        }

        static int FnId(string name) { for (int i = 0; i < Fns.Length; i++) if (Fns[i].name == name) return i; return -1; }
        static int VarId(string name) => Array.IndexOf(Vars, name);

        /* ---------------- tokenizer ---------------- */
        enum K { Num, Id, Op, End }
        struct Tok { public K k; public double num; public string s; }

        static bool Digit(char c) => c >= '0' && c <= '9';
        static bool IdStart(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_';

        static List<Tok> Tokenize(string src)
        {
            var o = new List<Tok>(); int i = 0;
            while (i < src.Length)
            {
                char c = src[i];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') { i++; continue; }
                if (Digit(c) || (c == '.' && i + 1 < src.Length && Digit(src[i + 1])))
                {
                    // ^(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?
                    int j = i;
                    if (c == '.') { j++; while (j < src.Length && Digit(src[j])) j++; }
                    else
                    {
                        while (j < src.Length && Digit(src[j])) j++;
                        if (j < src.Length && src[j] == '.') { j++; while (j < src.Length && Digit(src[j])) j++; }
                    }
                    if (j < src.Length && (src[j] == 'e' || src[j] == 'E'))
                    {
                        int e = j + 1;
                        if (e < src.Length && (src[e] == '+' || src[e] == '-')) e++;
                        if (e < src.Length && Digit(src[e])) { while (e < src.Length && Digit(src[e])) e++; j = e; }
                    }
                    o.Add(new Tok { k = K.Num, num = double.Parse(src.Substring(i, j - i), NumberStyles.Float, CultureInfo.InvariantCulture) });
                    i = j; continue;
                }
                if (IdStart(c))
                {
                    int j = i + 1;
                    while (j < src.Length && (IdStart(src[j]) || Digit(src[j]))) j++;
                    o.Add(new Tok { k = K.Id, s = src.Substring(i, j - i) }); i = j; continue;
                }
                if (i + 1 < src.Length)
                {
                    string two = src.Substring(i, 2);
                    if (two == "<=" || two == ">=" || two == "==" || two == "!=" || two == "&&" || two == "||") { o.Add(new Tok { k = K.Op, s = two }); i += 2; continue; }
                }
                if ("+-*/%<>!?:(),".IndexOf(c) >= 0) { o.Add(new Tok { k = K.Op, s = c.ToString() }); i++; continue; }
                throw new FormatException($"expr: unexpected '{c}' at {i}");
            }
            o.Add(new Tok { k = K.End });
            return o;
        }

        static bool Bin(string op, out int prec, out int code)
        {
            switch (op)
            {
                case "+": prec = 6; code = ADD; return true;
                case "-": prec = 6; code = SUB; return true;
                case "*": prec = 7; code = MUL; return true;
                case "/": prec = 7; code = DIV; return true;
                case "%": prec = 7; code = MOD; return true;
                case "<": prec = 5; code = LT; return true;
                case "<=": prec = 5; code = LE; return true;
                case ">": prec = 5; code = GT; return true;
                case ">=": prec = 5; code = GE; return true;
                case "==": prec = 4; code = EQ; return true;
                case "!=": prec = 4; code = NE; return true;
                case "&&": prec = 3; code = AND; return true;
                case "||": prec = 2; code = OR; return true;
            }
            prec = 0; code = 0; return false;
        }

        sealed class Parser
        {
            readonly List<Tok> tk; int pos; readonly IList<string> extra;
            public readonly List<double> code = new List<double>();
            public Parser(List<Tok> t, IList<string> x) { tk = t; extra = x; }
            Tok Peek => tk[pos];
            Tok Next() => tk[pos++];
            bool IsOp(Tok t, string v) => t.k == K.Op && t.s == v;
            void Expect(string v) { if (!IsOp(Next(), v)) throw new FormatException($"expr: expected '{v}'"); }

            void Primary()
            {
                var t = Next();
                if (t.k == K.Num) { code.Add(CONST); code.Add(t.num); return; }
                if (IsOp(t, "(")) { Expr(0); Expect(")"); return; }
                if (IsOp(t, "-")) { Expr(8); code.Add(NEG); return; }
                if (IsOp(t, "+")) { Expr(8); return; }
                if (IsOp(t, "!")) { Expr(8); code.Add(NOT); return; }
                if (t.k == K.Id)
                {
                    if (IsOp(Peek, "("))
                    {
                        Next();
                        int fid = FnId(t.s);
                        if (fid < 0) throw new FormatException($"expr: unknown function '{t.s}'");
                        int argc = 0;
                        if (!IsOp(Peek, ")"))
                            for (;;) { Expr(0); argc++; if (IsOp(Peek, ",")) { Next(); continue; } break; }
                        Expect(")");
                        if (argc < Fns[fid].lo || argc > Fns[fid].hi)
                            throw new FormatException($"expr: {t.s}() takes {Fns[fid].lo}..{Fns[fid].hi} args, got {argc}");
                        code.Add(CALL); code.Add(fid); code.Add(argc); return;
                    }
                    int vid = VarId(t.s);
                    if (vid < 0 && extra != null) { int j = extra.IndexOf(t.s); if (j >= 0) vid = Vars.Length + j; }
                    if (vid < 0) throw new FormatException($"expr: unknown variable '{t.s}'");
                    code.Add(VAR); code.Add(vid); return;
                }
                throw new FormatException("expr: unexpected " + (t.k == K.End ? "end" : t.s));
            }

            public void Expr(int minPrec)
            {
                Primary();
                for (;;)
                {
                    var t = Peek;
                    if (t.k != K.Op) break;
                    if (t.s == "?")
                    {
                        if (minPrec > 1) break;
                        Next(); Expr(0); Expect(":"); Expr(1);
                        code.Add(SEL); continue;
                    }
                    if (!Bin(t.s, out int prec, out int op) || prec < minPrec) break;
                    Next(); Expr(prec + 1); code.Add(op);
                }
            }

            public void End() { if (Peek.k != K.End) throw new FormatException("expr: trailing input"); }
        }

        /// <summary>extra: phase <c>let</c> names (variable ids Vars.Length + index). Throws FormatException.</summary>
        public static double[] Compile(string src, IList<string> extra = null)
        {
            var p = new Parser(Tokenize(src ?? ""), extra);
            p.Expr(0); p.End();
            return p.code.ToArray();
        }

        public struct Ctx { public int seed; public double i; }

        public static double Run(double[] code, double[] vars, Ctx ctx, double[] stack)
        {
            int sp = 0;
            for (int pc = 0; pc < code.Length;)
            {
                int op = (int)code[pc++];
                switch (op)
                {
                    case CONST: stack[sp++] = code[pc++]; break;
                    case VAR: stack[sp++] = vars[(int)code[pc++]]; break;
                    case NEG: stack[sp - 1] = -stack[sp - 1]; break;
                    case NOT: stack[sp - 1] = stack[sp - 1] == 0 ? 1 : 0; break;
                    case SEL: { double b = stack[--sp], a = stack[--sp], c = stack[--sp]; stack[sp++] = c != 0 ? a : b; break; }
                    case CALL:
                    {
                        int fid = (int)code[pc++], argc = (int)code[pc++];
                        sp -= argc;
                        stack[sp] = Call(fid, argc, stack, sp, ctx); sp++;
                        break;
                    }
                    default:
                    {
                        double b = stack[--sp], a = stack[sp - 1], v;
                        switch (op)
                        {
                            case ADD: v = a + b; break;
                            case SUB: v = a - b; break;
                            case MUL: v = a * b; break;
                            case DIV: v = a / b; break;
                            case MOD: v = a % b; break; // JS % on doubles = fmod (sign of the dividend), same as C#
                            case LT: v = a < b ? 1 : 0; break;
                            case LE: v = a <= b ? 1 : 0; break;
                            case GT: v = a > b ? 1 : 0; break;
                            case GE: v = a >= b ? 1 : 0; break;
                            case EQ: v = a == b ? 1 : 0; break;
                            case NE: v = a != b ? 1 : 0; break;
                            case AND: v = a != 0 && b != 0 ? 1 : 0; break;
                            case OR: v = a != 0 || b != 0 ? 1 : 0; break;
                            default: throw new InvalidOperationException("bad opcode " + op);
                        }
                        stack[sp - 1] = v;
                        break;
                    }
                }
            }
            return stack[0];
        }


        static double Call(int fid, int argc, double[] s, int b, Ctx ctx)
        {
            double a0 = s[b], a1 = s[b + 1], a2 = s[b + 2];
            switch (fid)
            {
                case 0: return Math.Sin(a0);
                case 1: return Math.Cos(a0);
                case 2: return Math.Tan(a0);
                case 3: return Math.Asin(a0);
                case 4: return Math.Acos(a0);
                case 5: return Math.Atan(a0);
                case 6: return Math.Atan2(a0, a1);
                case 7: return Math.Abs(a0);
                case 8: return a0 > 0 ? 1 : a0 < 0 ? -1 : 0;
                case 9: return Math.Floor(a0);
                case 10: return Math.Ceiling(a0);
                case 11: return Math.Floor(a0 + 0.5);
                case 12: return a0 - Math.Floor(a0);
                case 13: return Math.Sqrt(a0);
                case 14: return Math.Exp(a0);
                case 15: return Math.Log(a0);
                case 16: return Math.Pow(a0, a1);
                case 17: { double v = a0; for (int k = 1; k < argc; k++) if (s[b + k] < v) v = s[b + k]; return v; }
                case 18: { double v = a0; for (int k = 1; k < argc; k++) if (s[b + k] > v) v = s[b + k]; return v; }
                case 19: return argc == 1 ? AkFxMath.Clamp(a0) : argc == 2 ? AkFxMath.Clamp(a0, a1) : AkFxMath.Clamp(a0, a1, a2);
                case 20: return AkFxMath.Lerp(a0, a1, a2);
                case 21: return AkFxMath.Inv(a0, a1, a2);
                case 22: return AkFxMath.Smooth(a0, a1, a2);
                case 23: return a1 >= a0 ? 1 : 0;
                case 24: return AkFxMath.Clamp((a0 - a2 * a1) / (1 - a2));
                case 25: return AkFxMath.R(ctx.seed, ctx.i, a0, argc > 1 ? a1 : 0, argc > 2 ? a2 : 0);
                case 26: return AkFxMath.Rs(ctx.seed, ctx.i, a0, argc > 1 ? a1 : 0, argc > 2 ? a2 : 0);
                case 27: return AkFxMath.R(ctx.seed, a0, argc > 1 ? a1 : 0, argc > 2 ? a2 : 0, 0);
                case 28: return AkFxMath.Noise1(a0, AkFxMath.H(ctx.seed, ctx.i, a1, 0, 0));
                case 29: return AkFxMath.Noise1(a0, ctx.seed + a1);
                case 30: return AkFxMath.H(ctx.seed, a0, argc > 1 ? a1 : 0, argc > 2 ? a2 : 0, 0);
                default:
                {
                    int id = fid - FnEase0;
                    // JS passes the second argument straight through (a NaN overshoot stays NaN there too)
                    if (id == AkFxMath.OutBack && argc == 2) return OutBackRaw(a0, a1);
                    return AkFxMath.Ease(id, a0);
                }
            }
        }

        static double OutBackRaw(double x, double s)
        {
            x = AkFxMath.Clamp(x); double c = s + 1;
            return 1 + c * Math.Pow(x - 1, 3) + s * Math.Pow(x - 1, 2);
        }
    }
}
