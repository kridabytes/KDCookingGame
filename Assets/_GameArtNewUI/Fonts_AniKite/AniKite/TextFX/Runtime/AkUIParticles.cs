using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AniKite.TextFx
{
    /// <summary>
    /// One particle layer of a Text FX: a plain UGUI graphic that draws a list of rotated, tinted quads
    /// from the <see cref="AkFxSprites"/> atlas. Masks, RectMask2D and canvas sorting work like any Image. Driven by
    /// <see cref="AkTextFxParticles"/>; not meant to be added by hand.
    /// </summary>
    [AddComponentMenu("")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class AkUIParticles : MaskableGraphic
    {
        public struct Quad
        {
            public Vector2 pos;     // local px, y up
            public float half;      // half size in px
            public float rot;       // degrees, ccw
            public float sx, sy;
            public Color32 color;
            public int cell;
        }

        readonly List<Quad> quads = new List<Quad>();

        public bool Additive { get; private set; }
        public int Count => quads.Count;

        public override Texture mainTexture => AkFxSprites.Atlas;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        bool additiveSet;

        public void SetAdditive(bool on)
        {
            if (additiveSet && Additive == on) return;
            additiveSet = true;
            Additive = on;
            material = on ? AkFxSprites.AdditiveMaterial : null; // null → default UI material
        }

        public void Clear() { if (quads.Count == 0) return; quads.Clear(); SetVerticesDirty(); }
        public void Add(in Quad q) => quads.Add(q);
        public void Commit() => SetVerticesDirty();
        public void BeginFrame() => quads.Clear();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var tint = (Color32)color;
            for (int i = 0; i < quads.Count; i++)
            {
                var q = quads[i];
                if (q.half <= 0 || q.color.a == 0) continue;
                var uv = AkFxSprites.Uv(q.cell);
                float r = q.rot * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
                float hx = q.half * q.sx, hy = q.half * q.sy;
                Vector2 ax = new Vector2(c * hx, s * hx), ay = new Vector2(-s * hy, c * hy);
                var col = new Color32(
                    (byte)(q.color.r * tint.r / 255), (byte)(q.color.g * tint.g / 255),
                    (byte)(q.color.b * tint.b / 255), (byte)(q.color.a * tint.a / 255));
                int v0 = vh.currentVertCount;
                vh.AddVert(q.pos - ax - ay, col, new Vector2(uv.xMin, uv.yMin));
                vh.AddVert(q.pos - ax + ay, col, new Vector2(uv.xMin, uv.yMax));
                vh.AddVert(q.pos + ax + ay, col, new Vector2(uv.xMax, uv.yMax));
                vh.AddVert(q.pos + ax - ay, col, new Vector2(uv.xMax, uv.yMin));
                vh.AddTriangle(v0, v0 + 1, v0 + 2);
                vh.AddTriangle(v0 + 2, v0 + 3, v0);
            }
        }
    }
}
