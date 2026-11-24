using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace AniKite.TextFx
{
    /// <summary>Plays an AkTextFX recipe on a TextMeshPro text by moving its glyph quads every frame.
    /// No custom shader: blur lowers the SDF scale in uv0.w, the material block (outline / shadow / glow) goes on a
    /// private material instance that is dropped again on disable.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("AniKite/Text FX")]
    public sealed class AkTextFx : MonoBehaviour
    {
        [Tooltip("Imported recipe. Leave empty to use the JSON below.")]
        public AkTextFxPreset preset;
        [TextArea(3, 12)] public string recipeJson;

        [Header("Playback")]
        public bool playOnEnable = true;
        [Tooltip("Repeat enter → hold → exit forever (previews).")]
        public bool loop;
        [Min(0)] public float loopHold = 1.5f;
        [Min(0)] public float loopGap = 0.5f;
        [Tooltip("Seconds of hold before the exit plays by itself. < 0 = stay until PlayOut().")]
        public float autoOutAfter = -1;
        [Tooltip("Keep the text hidden until Play / PlayIn.")]
        public bool hideUntilPlayed = true;
        public bool unscaledTime = true;
        [Min(0)] public float speed = 1;

        [Header("Font")]
        [Tooltip("Swap to the recipe's bundled Unicode font while playing.")]
        public bool useRecipeFont = true;
        [SerializeField, HideInInspector] TMP_FontAsset bakedFont;
        [SerializeField, HideInInspector] string bakedFontId = "";
        [NonSerialized] bool live;

        [Header("Look")]
        [Tooltip("Colours, border, shadow and glow of this text. Each part follows the effect until you change it here.")]
        public AkTextFxLook look = new AkTextFxLook();
        AkTextFxLook cur;

        [Header("Events")]
        [Tooltip("A unit (char / word / line of the enter phase) appeared — typewriter sounds.")]
        public UnityEvent<int> onUnitShown = new UnityEvent<int>();
        public UnityEvent onEnterComplete = new UnityEvent();
        public UnityEvent onComplete = new UnityEvent();

        TMP_Text text;
        AkFxRecipe fx;
        AkFxTimeline tl;
        readonly AkFxScratch scratch = new AkFxScratch();
        readonly double[] pose = new double[AkFxRecipe.NP + 1];
        double clipTime;
        bool played, frozen, rebuilding;

        // captured mesh (per TMP sub-mesh)
        Vector3[][] origVerts = new Vector3[0][];
        Color32[][] origColors = new Color32[0][];
        Vector4[][] origUv = new Vector4[0][];

        // per glyph (non-whitespace character)
        int glyphCount;
        AkFxUnitIndex[] unitIdx = new AkFxUnitIndex[0];
        int[] gMat = new int[0], gVert = new int[0], gUnit = new int[0];
        bool[] gVisible = new bool[0];
        float[] gEm = new float[0];
        // pivot boxes per unit kind (0 char, 1 word, 2 line): centre x, centre y, width, height
        float[] boxX = new float[0], boxY = new float[0], boxW = new float[0], boxH = new float[0];
        readonly double[] geoBox = new double[12];
        // one glyph quad after the clip crop
        readonly Vector3[] qv = new Vector3[4];
        readonly Color32[] qc = new Color32[4];
        readonly Vector4[] qu = new Vector4[4];
        bool[] unitShown = new bool[0];
        // per glyph: its char / word / line index; units per kind; rest bounds of the visible glyphs (TMP local, y up)
        int[] gIdx = new int[0];
        readonly int[] unitCount = new int[3];
        Rect textBounds;

        // particle clock: clip-driven (loop / Seek) or Step-driven with the seconds spent in DONE
        bool clockFromClip;
        double clockHold, doneFor;

        Material sharedBefore, instance;
        TMP_FontAsset fontBefore;

        public AkFxRecipe Recipe => fx;
        public int State => tl != null ? tl.state : AkFxRecipe.IDLE;
        public bool IsPlaying => State == AkFxRecipe.ENTER || State == AkFxRecipe.HOLD || State == AkFxRecipe.EXIT;
        public TMP_Text Text => text ? text : text = GetComponent<TMP_Text>();

        /* ---------------- geometry for companions (particles) ---------------- */

        /// <summary>Non-whitespace characters of the text.</summary>
        public int GlyphCount => glyphCount;
        /// <summary>Units of a kind: 0 chars, 1 words, 2 lines (TMP's visual lines).</summary>
        public int UnitCount(int kind) => kind <= 0 ? glyphCount : unitCount[Math.Min(kind, 2)];
        public int GlyphUnit(int g, int kind) => gIdx[g * 3 + kind];
        /// <summary>Rest centre of glyph g, TMP local space (y up).</summary>
        public Vector2 GlyphCenter(int g) => new Vector2(boxX[g * 3], boxY[g * 3]);
        public float GlyphEm(int g) => gEm[g];
        /// <summary>Rest centre of unit u of a kind; the text centre when there is no such unit.</summary>
        public Vector2 UnitCenter(int kind, int u)
        {
            kind = Mathf.Clamp(kind, 0, 2);
            for (int g = 0; g < glyphCount; g++)
                if (gIdx[g * 3 + kind] == u) return new Vector2(boxX[g * 3 + kind], boxY[g * 3 + kind]);
            return textBounds.center;
        }
        /// <summary>1 em in TMP local units (the point size after auto size).</summary>
        public float Em => glyphCount > 0 ? gEm[0] : Text.fontSize * (Text.isOrthographic ? 1f : 0.1f);
        /// <summary>Rest bounds of the glyphs (ascender to descender of their lines).</summary>
        public Rect TextBounds => textBounds;
        public bool HasPlayed => played;
        /// <summary>The player is running: enabled in Play mode, or started by an editor preview.</summary>
        public bool IsLive => live;
        /// <summary>Clip time while looping or after Seek (enter → hold → exit), in seconds.</summary>
        public float ClipTime => (float)clipTime;
        public bool IsFrozen => frozen;
        /// <summary>Seconds of enter + <paramref name="hold"/> + exit for the current text (0 without a recipe).</summary>
        public float ClipLength(float hold) => tl == null ? 0 : (float)(tl.EnterLen + hold + tl.ExitLen);
        public float EnterLength => tl == null ? 0 : (float)tl.EnterLen;
        public float ExitLength => tl == null ? 0 : (float)tl.ExitLen;

        /// <summary>The particle clock for the current frame. False before the first Play.</summary>
        public bool TryGetClock(out AkFxClock clock)
        {
            clock = default;
            if (tl == null || !played) return false;
            clock = clockFromClip ? AkFxClock.FromClip(tl, clipTime, clockHold) : AkFxClock.FromStep(tl, doneFor);
            return true;
        }

        /* ---------------- lifecycle ---------------- */

        void Awake() { text = GetComponent<TMP_Text>(); }

        void OnEnable() => Begin();

        void OnDisable() => End();

        // OnEnable / OnDisable bodies; outside Play mode only the editor preview calls them (EditorStart / EditorStop)
        void Begin()
        {
            live = true;
            text = GetComponent<TMP_Text>();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            if (fx == null) LoadRecipe();
            RestoreMaterial();
            ApplyFont();
            ApplyMaterial();
            Rebuild();
            if (playOnEnable) PlayIn(); else Apply();
        }

        void End()
        {
            if (!live) return;
            live = false;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            RestoreMesh();
            RestoreMaterial();
            RestoreFont();
        }

        void OnDestroy() { RestoreMaterial(); RestoreFont(); }

        void LoadRecipe()
        {
            fx = null; cur = null;
            string json = preset ? preset.json : recipeJson;
            if (string.IsNullOrWhiteSpace(json)) return;
            try { fx = preset ? preset.Recipe : AkFxRecipe.Compile(json); }
            catch (Exception e) { Debug.LogError($"AkTextFx on '{name}': {e.Message}", this); }
            tl = fx != null ? new AkFxTimeline(fx, glyphCount) : null;
        }

        /* ---------------- API ---------------- */

        public void SetRecipe(AkFxRecipe recipe)
        {
            fx = recipe; preset = null; cur = null;
            tl = fx != null ? new AkFxTimeline(fx, glyphCount) : null;
            Restart();
        }

        public void SetRecipe(string json) { recipeJson = json; preset = null; LoadRecipe(); Restart(); }

        public void SetPreset(AkTextFxPreset p) { preset = p; LoadRecipe(); Restart(); }

        void Restart()
        {
            if (!live) return;
            RestoreMesh();
            RestoreMaterial();
            ApplyFont();
            ApplyMaterial();
            Rebuild();
            PlayIn();
        }

        /// <summary>Changes the text and plays the enter again.</summary>
        public void SetText(string value)
        {
            Text.text = value;
            if (!live) return;
            Rebuild();
            PlayIn();
        }

        public void Play() => PlayIn();

        public void PlayIn()
        {
            played = true; frozen = false; clipTime = 0;
            clockFromClip = loop; clockHold = loopHold; doneFor = 0;
            Array.Clear(unitShown, 0, unitShown.Length);
            tl?.PlayIn();
            Apply();
        }

        public void PlayOut() { tl?.PlayOut(); clockFromClip = false; doneFor = 0; Apply(); }

        /// <summary>Jump to an absolute clip time (enter → hold for holdFor s → exit). Freezes there until Play / PlayIn.</summary>
        public void Seek(float time, float holdFor)
        {
            if (tl == null) return;
            played = true; frozen = true;
            clockFromClip = true; clipTime = time; clockHold = holdFor;
            tl.SeekClip(time, holdFor);
            Apply();
        }

        /* ---------------- frame ---------------- */

        void LateUpdate()
        {
            if (live) Tick((unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * speed);
        }

        void Tick(float dt)
        {
            if (tl == null || !played || frozen) return;
            int before = tl.state;
            if (loop)
            {
                double L = tl.EnterLen + loopHold + tl.ExitLen + loopGap;
                clipTime += dt;
                if (clipTime >= L) { clipTime %= L; Array.Clear(unitShown, 0, unitShown.Length); }
                clockFromClip = true; clockHold = loopHold;
                tl.SeekClip(clipTime, loopHold);
            }
            else
            {
                if (clockFromClip) { clockFromClip = false; doneFor = 0; }
                if (before == AkFxRecipe.DONE) doneFor += dt;
                tl.Step(dt);
                if (tl.state == AkFxRecipe.HOLD && autoOutAfter >= 0 && tl.holdT >= autoOutAfter) tl.PlayOut();
            }
            Apply();
            if (before == AkFxRecipe.ENTER && tl.state != AkFxRecipe.ENTER) onEnterComplete.Invoke();
            if (before != AkFxRecipe.DONE && tl.state == AkFxRecipe.DONE) onComplete.Invoke();
        }

        // TMP rebuilt the mesh on its own (layout, auto size, font change): take the fresh mesh as the new original
        void OnTextChanged(UnityEngine.Object o)
        {
            if (o != text || rebuilding) return;
            Capture();
            Apply();
        }

        /* ---------------- capture ---------------- */

        // Our own rebuild: TMP fires TEXT_CHANGED inside ForceMeshUpdate, and handling it there would Apply (collapse the
        // hidden glyphs) before the capture below, which would then record the collapsed mesh as the original.
        void Rebuild()
        {
            rebuilding = true;
            try { text.ForceMeshUpdate(); }
            finally { rebuilding = false; }
            Capture();
        }

        void Capture()
        {
            var ti = text.textInfo;
            if (ti == null) return;
            int mc = ti.meshInfo.Length;
            if (origVerts.Length != mc) { origVerts = new Vector3[mc][]; origColors = new Color32[mc][]; origUv = new Vector4[mc][]; }
            for (int m = 0; m < mc; m++)
            {
                var mi = ti.meshInfo[m];
                Copy(mi.vertices, ref origVerts[m]);
                Copy(mi.colors32, ref origColors[m]);
                Copy(mi.uvs0, ref origUv[m]);
            }

            // units: same rules as AkFxRecipe.UnitsOf, but lines are TMP's visual lines (word wrap counts)
            int cc = ti.characterCount;
            var raw = new List<(int line, int word)>(cc);
            var map = new List<int>(cc);
            int word = -1, prevLine = -1; bool inWord = false;
            for (int c = 0; c < cc; c++)
            {
                var ci = ti.characterInfo[c];
                if (ci.lineNumber != prevLine) { inWord = false; prevLine = ci.lineNumber; }
                if (AkFxRecipe.IsJsSpace(ci.character)) { inWord = false; continue; }
                if (!inWord) { word++; inWord = true; }
                raw.Add((ci.lineNumber, word));
                map.Add(c);
            }
            var glyphs = AkFxRecipe.Number(raw, word + 1);
            int n = glyphs.Count;
            glyphCount = n;
            Ensure(ref unitIdx, n); Ensure(ref gMat, n); Ensure(ref gVert, n); Ensure(ref gUnit, n); Ensure(ref gVisible, n); Ensure(ref gEm, n);
            Ensure(ref boxX, n * 3); Ensure(ref boxY, n * 3); Ensure(ref boxW, n * 3); Ensure(ref boxH, n * 3); Ensure(ref gIdx, n * 3);
            for (int u = 0; u < 3; u++) unitCount[u] = n > 0 ? glyphs[0].Count(u) : 0;
            float emK = text.isOrthographic ? 1f : 0.1f;
            float bx0 = float.MaxValue, bx1 = float.MinValue, by0 = float.MaxValue, by1 = float.MinValue;
            for (int g = 0; g < n; g++)
            {
                var ci = ti.characterInfo[map[g]];
                for (int u = 0; u < 3; u++) gIdx[g * 3 + u] = glyphs[g].Index(u);
                bx0 = Mathf.Min(bx0, ci.origin); bx1 = Mathf.Max(bx1, ci.xAdvance);
                by0 = Mathf.Min(by0, ci.descender); by1 = Mathf.Max(by1, ci.ascender);
                gMat[g] = ci.materialReferenceIndex; gVert[g] = ci.vertexIndex;
                gVisible[g] = ci.isVisible && ci.materialReferenceIndex < mc && origVerts[ci.materialReferenceIndex] != null
                              && ci.vertexIndex + 3 < origVerts[ci.materialReferenceIndex].Length;
                gEm[g] = ci.pointSize * emK;
                boxX[g * 3] = (ci.origin + ci.xAdvance) * 0.5f;
                boxY[g * 3] = (ci.ascender + ci.descender) * 0.5f;
                boxW[g * 3] = ci.xAdvance - ci.origin;
                boxH[g * 3] = ci.ascender - ci.descender;
            }
            textBounds = n > 0 ? Rect.MinMaxRect(bx0, by0, bx1, by1) : new Rect(0, 0, 0, 0);
            // word / line boxes: x span of the group, y of its first glyph
            for (int u = 1; u <= 2; u++)
                for (int g = 0; g < n; g++)
                {
                    int id = glyphs[g].Index(u);
                    float x0 = float.MaxValue, x1 = float.MinValue, y = 0, hh = 0; bool first = true;
                    for (int j = 0; j < n; j++)
                    {
                        if (glyphs[j].Index(u) != id) continue;
                        var cj = ti.characterInfo[map[j]];
                        x0 = Mathf.Min(x0, cj.origin); x1 = Mathf.Max(x1, cj.xAdvance);
                        if (first) { y = boxY[j * 3]; hh = boxH[j * 3]; first = false; }
                    }
                    boxX[g * 3 + u] = (x0 + x1) * 0.5f; boxY[g * 3 + u] = y; boxW[g * 3 + u] = x1 - x0; boxH[g * 3 + u] = hh;
                }
            // unit indices + layout vars: em, from the text-box centre, y down
            if (fx != null)
                for (int g = 0; g < n; g++)
                {
                    double em = Math.Max(1e-6, gEm[g]);
                    for (int u = 0; u < 3; u++)
                    {
                        int b = g * 3 + u;
                        geoBox[u * 4] = (boxX[b] - textBounds.center.x) / em; geoBox[u * 4 + 1] = -(boxY[b] - textBounds.center.y) / em;
                        geoBox[u * 4 + 2] = boxW[b] / em; geoBox[u * 4 + 3] = boxH[b] / em;
                    }
                    unitIdx[g] = fx.UnitIndex(glyphs[g], geoBox, textBounds.width / em, textBounds.height / em);
                }
            int en = fx?.enter != null && n > 0 ? glyphs[0].Count(fx.enter.unit) : 0;
            if (unitShown.Length != en) unitShown = new bool[en];
            if (tl != null) tl.n = n;
        }

        static void Copy<T>(T[] src, ref T[] dst)
        {
            if (src == null) { dst = null; return; }
            if (dst == null || dst.Length != src.Length) dst = new T[src.Length];
            Array.Copy(src, dst, src.Length);
        }

        static void Ensure<T>(ref T[] a, int n) { if (a.Length < n) a = new T[Math.Max(n, a.Length * 2)]; }

        /* ---------------- apply ---------------- */

        static readonly double[] White = { 1, 1, 1, 1 };

        void Apply()
        {
            if (text == null || fx == null || tl == null) return;
            var ti = text.textInfo;
            if (ti == null || ti.meshInfo.Length != origVerts.Length) return;
            int state = played ? tl.state : AkFxRecipe.IDLE;
            bool showPlain = state == AkFxRecipe.IDLE && !hideUntilPlayed;
            var ph = fx.ActivePhase(state);
            int unit = ph?.unit ?? 0;
            double pivX = ph?.pivotX ?? 0, pivY = ph?.pivotY ?? 0;
            double[] tint = ph?.tint ?? White;
            bool hasFace = TryFaceColor(out Color32 face);

            for (int g = 0; g < glyphCount; g++)
            {
                if (!gVisible[g]) continue;
                int m = gMat[g], vi = gVert[g];
                var verts = ti.meshInfo[m].vertices; var cols = ti.meshInfo[m].colors32; var uvs = ti.meshInfo[m].uvs0;
                var ov = origVerts[m]; var oc = origColors[m]; var ou = origUv[m];

                if (showPlain) pose[AkFxRecipe.HIDDEN] = 0;
                else fx.EvalPose(state, tl.t, tl.holdT, unitIdx[g], pose, scratch);
                if (showPlain || (state == AkFxRecipe.HOLD && fx.hold == null))
                {
                    for (int j = 0; j < 4; j++) { verts[vi + j] = ov[vi + j]; cols[vi + j] = Base(oc[vi + j], hasFace, face); uvs[vi + j] = ou[vi + j]; }
                    continue;
                }
                if (pose[AkFxRecipe.HIDDEN] != 0)
                {
                    var c0 = (ov[vi] + ov[vi + 2]) * 0.5f;
                    for (int j = 0; j < 4; j++) { verts[vi + j] = c0; var c = cols[vi + j]; c.a = 0; cols[vi + j] = c; }
                    continue;
                }
                if (state == AkFxRecipe.ENTER && unitShown.Length > 0)
                {
                    int ui = unitIdx[g].ei;
                    if (ui >= 0 && ui < unitShown.Length && !unitShown[ui]) { unitShown[ui] = true; onUnitShown.Invoke(ui); }
                }

                // pivot (TMP local space, y up)
                int b = g * 3 + unit;
                double em = gEm[g];
                double px = boxX[b] + pivX * boxW[b];
                double py = boxY[b] - pivY * em;
                double sx = pose[AkFxRecipe.SCALE] * pose[AkFxRecipe.SX], sy = pose[AkFxRecipe.SCALE] * pose[AkFxRecipe.SY];
                double sk = Math.Tan(pose[AkFxRecipe.SKEW] * AkFxMath.DEG);
                double a = pose[AkFxRecipe.ROT] * AkFxMath.DEG, cs = Math.Cos(a), sn = Math.Sin(a);
                double tx = pose[AkFxRecipe.X] * em, ty = pose[AkFxRecipe.Y] * em;
                double alpha = AkFxMath.Clamp(pose[AkFxRecipe.ALPHA]);
                double tw = pose[AkFxRecipe.TINT];
                float uvK = (float)(1 / (1 + 8 * pose[AkFxRecipe.BLUR]));
                if (!CropQuad(ov, oc, ou, vi, b))
                {
                    for (int j = 0; j < 4; j++) { verts[vi + j] = ov[vi]; var c = cols[vi + j]; c.a = 0; cols[vi + j] = c; }
                    continue;
                }

                for (int j = 0; j < 4; j++)
                {
                    var o = qv[j];
                    // into the spec's y-down frame around the pivot
                    double x = (o.x - px) * sx, y = -(o.y - py) * sy;
                    x -= sk * y;
                    double rx = cs * x - sn * y + tx, ry = sn * x + cs * y + ty;
                    verts[vi + j] = new Vector3((float)(px + rx), (float)(py - ry), o.z);

                    var c = Base(qc[j], hasFace, face);
                    if (tw > 0)
                    {
                        c.r = (byte)Math.Round(c.r + (tint[0] * 255 - c.r) * tw);
                        c.g = (byte)Math.Round(c.g + (tint[1] * 255 - c.g) * tw);
                        c.b = (byte)Math.Round(c.b + (tint[2] * 255 - c.b) * tw);
                    }
                    c.a = (byte)Math.Round(c.a * alpha);
                    cols[vi + j] = c;

                    var uv = qu[j]; uv.w *= uvK; uvs[vi + j] = uv; // keeps the sign: negative w = bold
                }
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Uv0);
        }

        /// <summary>Glyph quad (TMP order BL, TL, TR, BR) into qv / qc / qu, cropped by the pose's clipL/R/T/B against
        /// unit box b (pre-transform). Italic quads are cut along the bottom edge's x range. False = nothing left.</summary>
        bool CropQuad(Vector3[] ov, Color32[] oc, Vector4[] ou, int vi, int b)
        {
            double cl = pose[AkFxRecipe.CLIPL], cr = pose[AkFxRecipe.CLIPR], ct = pose[AkFxRecipe.CLIPT], cb = pose[AkFxRecipe.CLIPB];
            if (cl + cr + ct + cb <= 1e-6)
            {
                for (int j = 0; j < 4; j++) { qv[j] = ov[vi + j]; qc[j] = oc[vi + j]; qu[j] = ou[vi + j]; }
                return true;
            }
            double w = boxW[b], h = boxH[b];
            double x0 = boxX[b] - w * 0.5 + cl * w, x1 = boxX[b] + w * 0.5 - cr * w;
            double yTop = boxY[b] + h * 0.5 - ct * h, yBot = boxY[b] - h * 0.5 + cb * h;
            Vector3 bl = ov[vi], tl = ov[vi + 1], br = ov[vi + 3];
            double dx = br.x - bl.x, dy = tl.y - bl.y;
            float u0 = dx > 1e-9 ? (float)AkFxMath.Clamp((x0 - bl.x) / dx) : 0f, u1 = dx > 1e-9 ? (float)AkFxMath.Clamp((x1 - bl.x) / dx) : 1f;
            float v0 = dy > 1e-9 ? (float)AkFxMath.Clamp((yBot - bl.y) / dy) : 0f, v1 = dy > 1e-9 ? (float)AkFxMath.Clamp((yTop - bl.y) / dy) : 1f;
            if (u1 <= u0 || v1 <= v0) return false;
            Corner(ov, oc, ou, vi, 0, u0, v0); Corner(ov, oc, ou, vi, 1, u0, v1);
            Corner(ov, oc, ou, vi, 2, u1, v1); Corner(ov, oc, ou, vi, 3, u1, v0);
            return true;
        }

        void Corner(Vector3[] ov, Color32[] oc, Vector4[] ou, int vi, int j, float u, float v)
        {
            // bilinear over BL(0) TL(1) TR(2) BR(3)
            qv[j] = Vector3.Lerp(Vector3.Lerp(ov[vi], ov[vi + 3], u), Vector3.Lerp(ov[vi + 1], ov[vi + 2], u), v);
            qu[j] = Vector4.Lerp(Vector4.Lerp(ou[vi], ou[vi + 3], u), Vector4.Lerp(ou[vi + 1], ou[vi + 2], u), v);
            qc[j] = Color32.Lerp(Color32.Lerp(oc[vi], oc[vi + 3], u), Color32.Lerp(oc[vi + 1], oc[vi + 2], u), v);
        }

        static Color32 Base(Color32 o, bool hasFace, Color32 face) =>
            hasFace ? new Color32(face.r, face.g, face.b, (byte)(o.a * face.a / 255)) : o;

        bool TryFaceColor(out Color32 c)
        {
            c = default;
            var l = cur ?? (cur = Resolve(look, fx));
            if (l.fill != AkTextFxLook.Fill.Custom) return false;
            c = l.fillColor;
            return true;
        }

        void RestoreMesh()
        {
            if (text == null) return;
            var ti = text.textInfo;
            if (ti == null || ti.meshInfo.Length != origVerts.Length) return;
            for (int m = 0; m < origVerts.Length; m++)
            {
                var mi = ti.meshInfo[m];
                if (origVerts[m] != null && mi.vertices != null && mi.vertices.Length == origVerts[m].Length) Array.Copy(origVerts[m], mi.vertices, origVerts[m].Length);
                if (origColors[m] != null && mi.colors32 != null && mi.colors32.Length == origColors[m].Length) Array.Copy(origColors[m], mi.colors32, origColors[m].Length);
                if (origUv[m] != null && mi.uvs0 != null && mi.uvs0.Length == origUv[m].Length) Array.Copy(origUv[m], mi.uvs0, origUv[m].Length);
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Uv0);
        }

        /* ---------------- material ---------------- */

        static object Get(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v : null;
        static float F(Dictionary<string, object> d, string k, float def) => Get(d, k) is double x && !double.IsNaN(x) && !double.IsInfinity(x) ? (float)x : def;
        static Color C(Dictionary<string, object> d, string k, Color def)
        {
            var v = AkFxRecipe.ParseColor(Get(d, k), null);
            return v == null ? def : new Color((float)v[0], (float)v[1], (float)v[2], (float)v[3]);
        }

        /// <summary>The look a recipe asks for, with every part spelled out as Off or Custom (fill: Text or Custom).</summary>
        public static AkTextFxLook EffectLook(AkFxRecipe r)
        {
            var mat = r?.material;
            var face = Get(mat, "face") as Dictionary<string, object>;
            var outline = Get(mat, "outline") as Dictionary<string, object>;
            var shadow = Get(mat, "shadow") as Dictionary<string, object>;
            var glow = Get(mat, "glow") as Dictionary<string, object>;
            const AkTextFxLook.Part Off = AkTextFxLook.Part.Off, On = AkTextFxLook.Part.Custom;
            bool fill = AkFxRecipe.ParseColor(Get(face, "color"), null) != null;
            return new AkTextFxLook
            {
                fill = fill ? AkTextFxLook.Fill.Custom : AkTextFxLook.Fill.Text,
                fillColor = C(face, "color", Color.white),
                weight = Get(face, "dilate") != null ? On : Off,
                weightValue = Mathf.Clamp(F(face, "dilate", 0), -1, 1),
                outline = outline != null && F(outline, "w", 0) > 0 ? On : Off,
                outlineColor = C(outline, "color", Color.black),
                outlineWidth = Mathf.Clamp01(F(outline, "w", 0.2f)),
                shadow = shadow != null ? On : Off,
                shadowColor = C(shadow, "color", new Color(0, 0, 0, 0.6f)),
                shadowOffset = new Vector2(Mathf.Clamp(F(shadow, "x", 0.5f), -1, 1), Mathf.Clamp(F(shadow, "y", -0.5f), -1, 1)),
                shadowSoftness = Mathf.Clamp01(F(shadow, "soft", 0.2f)),
                shadowSpread = Mathf.Clamp(F(shadow, "dilate", 0), -1, 1),
                glow = glow != null ? On : Off,
                glowColor = C(glow, "color", new Color(1, 1, 0.5f, 1)),
                glowPower = Mathf.Clamp01(F(glow, "power", 0.5f)),
                glowSize = Mathf.Clamp01(F(glow, "outer", 0.5f)),
            };
        }

        /// <summary>The recipe's look with this text's changes on top; every part comes back as Off or Custom.</summary>
        public static AkTextFxLook Resolve(AkTextFxLook mine, AkFxRecipe r)
        {
            var e = EffectLook(r);
            if (mine == null) return e;
            if (mine.fill != AkTextFxLook.Fill.Effect) { e.fill = mine.fill; e.fillColor = mine.fillColor; }
            if (mine.weight != AkTextFxLook.Part.Effect) { e.weight = mine.weight; e.weightValue = mine.weightValue; }
            if (mine.outline != AkTextFxLook.Part.Effect) { e.outline = mine.outline; e.outlineColor = mine.outlineColor; e.outlineWidth = mine.outlineWidth; }
            if (mine.shadow != AkTextFxLook.Part.Effect)
            {
                e.shadow = mine.shadow; e.shadowColor = mine.shadowColor; e.shadowOffset = mine.shadowOffset;
                e.shadowSoftness = mine.shadowSoftness; e.shadowSpread = mine.shadowSpread;
            }
            if (mine.glow != AkTextFxLook.Part.Effect) { e.glow = mine.glow; e.glowColor = mine.glowColor; e.glowPower = mine.glowPower; e.glowSize = mine.glowSize; }
            return e;
        }

        /// <summary>Shows changes to <see cref="look"/> at once while playing (or in an editor preview), without restarting.</summary>
        public void RefreshLook()
        {
            cur = null;
            if (!live || text == null || fx == null) return;
            RestoreMesh();
            ApplyMaterial();
            Rebuild();
            Apply();
        }

        void ApplyMaterial()
        {
            RestoreMaterial();
            if (fx == null) return;
            var l = cur = Resolve(look, fx);
            if (text == null || text.fontSharedMaterial == null) return;
            const AkTextFxLook.Part On = AkTextFxLook.Part.Custom;
            bool outlineOn = l.outline == On && l.outlineWidth > 0;
            // an Off part only needs work when the text's own material has it switched on
            var shared = text.fontSharedMaterial;
            bool outlineOff = l.outline != On && shared.IsKeywordEnabled(ShaderUtilities.Keyword_Outline);
            bool shadowOff = l.shadow != On && shared.IsKeywordEnabled(ShaderUtilities.Keyword_Underlay);
            bool glowOff = l.glow != On && shared.IsKeywordEnabled(ShaderUtilities.Keyword_Glow);
            bool needsInstance = outlineOn || l.shadow == On || l.glow == On || l.weight == On || outlineOff || shadowOff || glowOff;
            if (!needsInstance) return;

            sharedBefore = shared;
            instance = new Material(sharedBefore) { name = sharedBefore.name + " (AkTextFx)", hideFlags = HideFlags.DontSave };
            // Mobile SDF has no glow: use TMP's full Distance Field shader for glow recipes
            if (l.glow == On && !instance.HasProperty(ShaderUtilities.ID_GlowColor))
            {
                var full = Shader.Find("TextMeshPro/Distance Field");
                if (full != null) instance.shader = full;
            }
            if (l.weight == On) instance.SetFloat(ShaderUtilities.ID_FaceDilate, l.weightValue);
            if (outlineOn)
            {
                instance.EnableKeyword(ShaderUtilities.Keyword_Outline);
                instance.SetFloat(ShaderUtilities.ID_OutlineWidth, Mathf.Clamp01(l.outlineWidth));
                instance.SetColor(ShaderUtilities.ID_OutlineColor, l.outlineColor);
            }
            else if (outlineOff)
            {
                instance.DisableKeyword(ShaderUtilities.Keyword_Outline);
                instance.SetFloat(ShaderUtilities.ID_OutlineWidth, 0);
            }
            if (l.shadow == On)
            {
                instance.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                instance.SetColor(ShaderUtilities.ID_UnderlayColor, l.shadowColor);
                instance.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, Mathf.Clamp(l.shadowOffset.x, -1, 1));
                instance.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, Mathf.Clamp(l.shadowOffset.y, -1, 1));
                instance.SetFloat(ShaderUtilities.ID_UnderlaySoftness, Mathf.Clamp01(l.shadowSoftness));
                instance.SetFloat(ShaderUtilities.ID_UnderlayDilate, Mathf.Clamp(l.shadowSpread, -1, 1));
            }
            else if (shadowOff) instance.DisableKeyword(ShaderUtilities.Keyword_Underlay);
            if (l.glow == On && instance.HasProperty(ShaderUtilities.ID_GlowColor))
            {
                instance.EnableKeyword(ShaderUtilities.Keyword_Glow);
                instance.SetColor(ShaderUtilities.ID_GlowColor, l.glowColor);
                instance.SetFloat(ShaderUtilities.ID_GlowPower, Mathf.Clamp01(l.glowPower));
                instance.SetFloat(ShaderUtilities.ID_GlowOuter, Mathf.Clamp01(l.glowSize));
            }
            else if (glowOff) instance.DisableKeyword(ShaderUtilities.Keyword_Glow);
            ShaderUtilities.UpdateShaderRatios(instance);
            text.fontSharedMaterial = instance;
        }

        void RestoreMaterial()
        {
            if (instance == null) return;
            if (text != null && text.fontSharedMaterial == instance && sharedBefore != null) text.fontSharedMaterial = sharedBefore;
            if (Application.isPlaying) Destroy(instance); else DestroyImmediate(instance);
            instance = null; sharedBefore = null;
        }

        /* ---------------- font ---------------- */

        /// <summary>The recipe's bundled font, or null (no font, unknown id, or not baked in a build).</summary>
        TMP_FontAsset RecipeFont()
        {
            string id = fx?.font;
            if (!useRecipeFont || string.IsNullOrEmpty(id)) return null;
            if (bakedFont != null && bakedFontId == id) return bakedFont;
            var f = AkTextFxFonts.Resolve(id);
            if (f == null && !AkTextFxFonts.IsKnown(id)) Debug.LogWarning($"AkTextFx on '{name}': unknown font '{id}'", this);
            return f;
        }

        void ApplyFont()
        {
            if (text == null) return;
            var f = RecipeFont();
            if (f == null) { RestoreFont(); return; }
            if (text.font == f) return;
            if (fontBefore == null) fontBefore = text.font;
            text.font = f;
        }

        void RestoreFont()
        {
            if (fontBefore == null) return;
            if (text != null && text.font != fontBefore) text.font = fontBefore;
            fontBefore = null;
        }

#if UNITY_EDITOR
        /// <summary>Editor previews (Text FX window): run the player outside Play mode. Drive it with EditorTick or Seek,
        /// and call EditorStop before the preview scene closes (drops the material instance).</summary>
        public void EditorStart() { if (!live) Begin(); }

        public void EditorTick(float dt) { if (live) Tick(dt * speed); }

        public void EditorStop() { if (!Application.isPlaying) End(); }

        // Keep a serialized reference to the recipe's font asset so player builds include it.
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += BakeFont;
            // Play mode: look changes made in the inspector show at once
            if (live) UnityEditor.EditorApplication.delayCall += () => { if (this != null) RefreshLook(); };
        }

        /// <summary>Editor: store the recipe font asset on this component (creates the SDF asset on first use).</summary>
        public void BakeFont()
        {
            if (this == null || Application.isPlaying) return;
            string id = "";
            try
            {
                string json = preset ? preset.json : recipeJson;
                if (!string.IsNullOrWhiteSpace(json)) id = (preset ? preset.Recipe : AkFxRecipe.Compile(json))?.font ?? "";
            }
            catch (Exception) { return; } // bad JSON: the console already gets the compile error at play time
            var f = useRecipeFont && id.Length > 0 ? AkTextFxFonts.Resolve(id) : null;
            if (f == bakedFont && bakedFontId == (f != null ? id : "")) return;
            bakedFont = f; bakedFontId = f != null ? id : "";
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>
    /// This text's own look on top of the effect: fill colour, weight, border (outline), shadow and glow. Every part
    /// follows the effect until it is set to Off or Custom. Shown while the effect plays (Play mode, editor previews).
    /// </summary>
    [Serializable]
    public sealed class AkTextFxLook
    {
        /// <summary>Fill colour: the effect's, the TextMeshPro colour (gradients too), or your own.</summary>
        public enum Fill { Effect, Text, Custom }
        /// <summary>A material part: as the effect has it, turned off, or your own values.</summary>
        public enum Part { Effect, Off, Custom }

        public Fill fill = Fill.Effect;
        public Color fillColor = Color.white;

        public Part weight = Part.Effect;
        [Range(-1, 1)] public float weightValue;

        public Part outline = Part.Effect;
        public Color outlineColor = Color.black;
        [Range(0, 1)] public float outlineWidth = 0.2f;

        public Part shadow = Part.Effect;
        public Color shadowColor = new Color(0, 0, 0, 0.6f);
        public Vector2 shadowOffset = new Vector2(0.5f, -0.5f);
        [Range(0, 1)] public float shadowSoftness = 0.2f;
        [Range(-1, 1)] public float shadowSpread;

        public Part glow = Part.Effect;
        public Color glowColor = new Color(1, 1, 0.5f, 1);
        [Range(0, 1)] public float glowPower = 0.5f;
        [Range(0, 1)] public float glowSize = 0.5f;

        /// <summary>Everything follows the effect.</summary>
        public bool FollowsEffect => fill == Fill.Effect && weight == Part.Effect && outline == Part.Effect && shadow == Part.Effect && glow == Part.Effect;
    }
}
