using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AniKite.TextFx
{
    /// <summary>
    /// Draws the recipe's "particles" around a UGUI Text FX. Nothing is simulated: every frame the live
    /// bursts and each particle's position come from the text's clock, so Seek, loops and replays always match the
    /// web preview. Four <see cref="AkUIParticles"/> layers are made at run time and never saved: behind the text
    /// (siblings drawn before it) and in front (children), each with an alpha and an additive graphic.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AkTextFx))]
    [DefaultExecutionOrder(100)]
    [AddComponentMenu("AniKite/Text FX Particles")]
    public sealed class AkTextFxParticles : MonoBehaviour
    {
        [Tooltip("Multiplies every particle's size.")]
        [Min(0)] public float sizeScale = 1;
        [Tooltip("Multiplies every particle's colour.")]
        public Color tint = Color.white;

        AkTextFx fx;
        // 0 back alpha, 1 back additive, 2 front alpha, 3 front additive
        readonly AkUIParticles[] layers = new AkUIParticles[4];
        readonly List<AkFxBurst> bursts = new List<AkFxBurst>();
        readonly double[] st = new double[AkFxParticles.PState];
        readonly bool[] used = new bool[4];
        bool warnedWorld;

        public int LiveParticles { get; private set; }

        void OnEnable() { fx = GetComponent<AkTextFx>(); }

        void OnDisable() { DestroyLayers(); }

        void OnDestroy() { DestroyLayers(); }

        void LateUpdate() { Render(); }

        /* ---------------- layers ---------------- */

        AkUIParticles Layer(int i, RectTransform textRt)
        {
            var l = layers[i];
            if (l == null)
            {
                bool back = i < 2;
                var go = new GameObject(back ? (i == 0 ? "FX Back" : "FX Back +") : (i == 2 ? "FX Front" : "FX Front +"),
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(AkUIParticles), typeof(LayoutElement));
                go.hideFlags = HideFlags.DontSave;
                // new objects land in the active scene; editor previews live in a preview scene
                if (go.scene != gameObject.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
                go.layer = gameObject.layer;
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                var rt = (RectTransform)go.transform;
                rt.SetParent(back && textRt.parent != null ? textRt.parent : textRt, false);
                l = layers[i] = go.GetComponent<AkUIParticles>();
                l.raycastTarget = false;
                l.SetAdditive(i == 1 || i == 3);
            }
            var lt = l.rectTransform;
            if (lt.parent == textRt)
            {
                // front: same local frame as the text
                lt.anchorMin = lt.anchorMax = lt.pivot = textRt.pivot;
                lt.anchoredPosition = Vector2.zero; lt.sizeDelta = Vector2.zero;
                lt.localRotation = Quaternion.identity; lt.localScale = Vector3.one;
            }
            else
            {
                // back: a sibling that copies the text's rect, so its local frame is the text's
                lt.anchorMin = textRt.anchorMin; lt.anchorMax = textRt.anchorMax; lt.pivot = textRt.pivot;
                lt.anchoredPosition3D = textRt.anchoredPosition3D; lt.sizeDelta = textRt.sizeDelta;
                lt.localRotation = textRt.localRotation; lt.localScale = textRt.localScale;
            }
            return l;
        }

        // draw order: [back alpha][back additive][text … its children … front alpha][front additive]
        void Order(RectTransform textRt)
        {
            Transform before = textRt;
            for (int i = 1; i >= 0; i--)
            {
                if (layers[i] == null || layers[i].transform.parent != textRt.parent) continue;
                PlaceBefore(layers[i].transform, before);
                before = layers[i].transform;
            }
            for (int i = 2; i < 4; i++)
            {
                if (layers[i] == null) continue;
                var t = layers[i].transform;
                if (t.GetSiblingIndex() != t.parent.childCount - 1) t.SetAsLastSibling();
            }
        }

        static void PlaceBefore(Transform t, Transform target)
        {
            int ti = target.GetSiblingIndex(), i = t.GetSiblingIndex();
            if (i == ti - 1) return;
            t.SetSiblingIndex(i < ti ? ti - 1 : ti);
        }

        void DestroyLayers()
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null)
                {
                    var go = layers[i].gameObject;
                    if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
                }
                layers[i] = null;
            }
            LiveParticles = 0;
        }

        void ClearLayers()
        {
            for (int i = 0; i < layers.Length; i++) if (layers[i] != null) layers[i].Clear();
            LiveParticles = 0;
        }

        /* ---------------- frame ---------------- */

        /// <summary>Rebuilds the particle quads for the text's current clock (called every LateUpdate).</summary>
        public void Render()
        {
            if (fx == null) fx = GetComponent<AkTextFx>();
            var recipe = fx != null ? fx.Recipe : null;
            if (recipe == null || recipe.particles == null || recipe.particles.Length == 0 || !fx.IsLive
                || !fx.TryGetClock(out var clk) || fx.GlyphCount == 0)
            {
                ClearLayers();
                return;
            }
            if (!(fx.Text is TextMeshProUGUI))
            {
                if (!warnedWorld) { warnedWorld = true; Debug.LogWarning($"AkTextFxParticles on '{name}': particles need a UI text (TextMeshProUGUI).", this); }
                return;
            }

            var textRt = (RectTransform)fx.transform;
            var used = this.used;
            System.Array.Clear(used, 0, 4);
            foreach (var em in recipe.particles) used[(em.back ? 0 : 2) + (em.Preset.add ? 1 : 0)] = true;
            for (int i = 0; i < 4; i++)
            {
                if (used[i]) Layer(i, textRt).BeginFrame();
                else if (layers[i] != null) layers[i].Clear();
            }
            Order(textRt);

            int nc = fx.GlyphCount, nw = fx.UnitCount(1), nl = fx.UnitCount(2);
            AkFxParticles.Events(recipe, nc, nw, nl, clk, bursts);
            float em0 = fx.Em * sizeScale;
            var bounds = fx.TextBounds;
            int live = 0;

            for (int b = 0; b < bursts.Count && live < AkFxParticles.MaxParticles; b++)
            {
                var ev = bursts[b];
                var emitter = recipe.particles[ev.e];
                var layer = layers[(emitter.back ? 0 : 2) + (emitter.Preset.add ? 1 : 0)];
                if (emitter.at == AkFxParticles.AT_GLYPHS)
                {
                    for (int g = 0; g < nc && live < AkFxParticles.MaxParticles; g++)
                    {
                        if (ev.u >= 0 && fx.GlyphUnit(g, ev.kind) != ev.u) continue;
                        live = Emit(recipe, ev, g + 1, emitter.count, fx.GlyphCenter(g), fx.GlyphEm(g) * sizeScale, bounds, layer, live);
                    }
                }
                else
                {
                    Vector2 origin = emitter.at == AkFxParticles.AT_UNIT && ev.u >= 0 ? fx.UnitCenter(ev.kind, ev.u) : bounds.center;
                    live = Emit(recipe, ev, 0, emitter.count, origin, em0, bounds, layer, live);
                }
            }

            for (int i = 0; i < 4; i++) if (used[i]) layers[i].Commit();
            LiveParticles = live;
        }

        int Emit(AkFxRecipe recipe, AkFxBurst ev, int sub, int count, Vector2 origin, float em, Rect bounds, AkUIParticles layer, int live)
        {
            for (int p = 0; p < count && live < AkFxParticles.MaxParticles; p++)
            {
                if (!AkFxParticles.State(recipe, ev, sub, p, ev.ago, st)) continue;
                // at "text": a random point of the text box (fx, fy in -0.5..0.5, fy down like the web preview)
                float ox = origin.x + (float)st[11] * bounds.width, oy = origin.y - (float)st[12] * bounds.height;
                float a = (float)st[6] * tint.a;
                if (a <= 0.002f) continue;
                layer.Add(new AkUIParticles.Quad
                {
                    pos = new Vector2(ox + (float)st[0] * em, oy + (float)st[1] * em),
                    half = (float)st[2] * em * 0.5f,
                    rot = (float)st[3],
                    sx = (float)st[4], sy = (float)st[5],
                    color = new Color(Mathf.Clamp01((float)st[7] * tint.r), Mathf.Clamp01((float)st[8] * tint.g), Mathf.Clamp01((float)st[9] * tint.b), Mathf.Clamp01(a)),
                    cell = (int)st[10],
                });
                live++;
            }
            return live;
        }
    }
}
