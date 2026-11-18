using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// One place for all the small "juice" effects of the game: sparkles, steam, smoke, puffs, rings.
    ///
    /// Why it exists: this game mixes canvases (Screen Space Overlay HUD, Screen Space Camera
    /// gameplay, nested World Space canvases for stoves/customers). Anything that has to travel
    /// between them, or sit on top of all of them, is hard to place and sort. So every effect is
    /// drawn on ONE Screen Space Overlay canvas that sits above everything, and positions are
    /// converted to screen pixels with the camera that really draws each object
    /// (see <see cref="TryGetScreenPoint"/>).
    ///
    /// Particles are plain UI Images taken from a pool, using one generated texture, so the
    /// whole layer is a single draw call and creates no garbage while playing. It looks the
    /// same in the Editor and on any phone because sizes are scaled to the screen.
    /// </summary>
    public class GameFx : MonoBehaviour
    {
        /// <summary>Above every gameplay and HUD canvas.</summary>
        public const int SortingOrder = 500;

        private const int MaxParticles = 220;

        private static GameFx instance;

        private RectTransform root;
        private Sprite circleSprite;
        private Sprite starSprite;

        private readonly List<Particle> live = new List<Particle>(MaxParticles);
        private readonly Stack<Particle> pool = new Stack<Particle>(MaxParticles);

        /// <summary>Canvas that every effect (and the flying ice cream) is drawn on.</summary>
        public static RectTransform Layer => Instance.root;

        /// <summary>
        /// 1 on a 1080p screen. Multiply pixel sizes/speeds by this so effects look the same on every device.
        /// </summary>
        public static float Scale => Mathf.Max(0.35f, Mathf.Min(Screen.width, Screen.height) / 1080f);

        public static Sprite Circle => Instance.circleSprite;
        public static Sprite Star => Instance.starSprite;

        public static GameFx Instance
        {
            get
            {
                if (instance == null)
                    Create();
                return instance;
            }
        }

        #region Setup

        private static void Create()
        {
            var go = new GameObject("[GameFx Layer]", typeof(RectTransform), typeof(Canvas));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameFx>();
            instance.Build();
        }

        private void Build()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            canvas.pixelPerfect = false;
            // No GraphicRaycaster: effects must never block a tap.

            root = (RectTransform)transform;

            Texture2D tex = BuildTexture();
            circleSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            starSprite = Sprite.Create(tex, new Rect(64, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            circleSprite.name = "fx_circle";
            starSprite.name = "fx_star";

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this)
                instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Nothing from the old scene should keep floating around.
            for (int i = live.Count - 1; i >= 0; i--)
                Release(i);

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.GetComponent<FlyingIceCreamAnimation>() != null)
                    Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 128x64 texture: a soft round glow on the left, a 4-point twinkle star on the right.
        /// Both in one texture so all particles batch into one draw call.
        /// </summary>
        private static Texture2D BuildTexture()
        {
            const int h = 64;
            var tex = new Texture2D(h * 2, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "GameFxTexture"
            };

            var px = new Color32[h * 2 * h];
            float c = (h - 1) * 0.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < h; x++)
                {
                    float dx = (x - c) / c;
                    float dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    // Soft circle: solid middle, smooth edge.
                    float circle = Mathf.Clamp01(1f - Mathf.SmoothStep(0.55f, 1f, d));

                    // Star: two thin crossed rays plus a bright core.
                    float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy);
                    float rays = Mathf.Max(
                        Mathf.Clamp01(1f - ax * 9f) * Mathf.Clamp01(1f - ay),
                        Mathf.Clamp01(1f - ay * 9f) * Mathf.Clamp01(1f - ax));
                    float core = Mathf.Clamp01(1f - d * 2.2f);
                    float star = Mathf.Clamp01(rays + core * core);

                    px[y * h * 2 + x] = new Color32(255, 255, 255, (byte)(circle * 255f));
                    px[y * h * 2 + x + h] = new Color32(255, 255, 255, (byte)(star * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        #endregion

        #region Screen positions

        /// <summary>
        /// Where <paramref name="target"/> is on the screen, in pixels, using the camera that
        /// actually draws it. Works for overlay, camera and world space canvases (including
        /// nested ones, whose own camera field is empty) and for normal world objects.
        /// For UI it returns the middle of the rect, not its pivot.
        /// </summary>
        public static bool TryGetScreenPoint(Transform target, out Vector2 screen)
        {
            screen = default;
            if (target == null)
                return false;

            Vector3 world = target.position;
            if (target is RectTransform rect)
                world = rect.TransformPoint(rect.rect.center);

            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Canvas rootCanvas = canvas.rootCanvas;
                if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    screen = world;
                    return true;
                }

                Camera cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
                if (cam == null)
                    return false;

                screen = RectTransformUtility.WorldToScreenPoint(cam, world);
                return true;
            }

            Camera main = Camera.main;
            if (main == null)
                return false;

            screen = main.WorldToScreenPoint(world);
            return true;
        }

        /// <summary>Height of a UI element on screen, in pixels.</summary>
        public static float ScreenHeightOf(Transform target, float fallback)
        {
            if (!(target is RectTransform rect))
                return fallback;

            Vector3 bottom = rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.yMin));
            Vector3 top = rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.yMax));

            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null)
                return fallback;

            Canvas rootCanvas = canvas.rootCanvas;
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : (rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main);

            float h = Vector2.Distance(
                RectTransformUtility.WorldToScreenPoint(cam, bottom),
                RectTransformUtility.WorldToScreenPoint(cam, top));

            return h > 1f ? h : fallback;
        }

        #endregion

        #region Ready-made effects

        private static readonly Color Gold = new Color(1f, 0.86f, 0.3f);
        private static readonly Color WarmWhite = new Color(1f, 0.98f, 0.9f);
        private static readonly Color SteamColor = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color GreySmoke = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        private static readonly Color DarkSmoke = new Color(0.18f, 0.16f, 0.15f, 0.75f);

        /// <summary>Food on the stove is done: golden twinkles, a glow ring and a puff of steam.</summary>
        public static void Cooked(Transform at)
        {
            if (!TryGetScreenPoint(at, out Vector2 p)) return;
            float s = Scale;

            Ring(p, Gold, 40f * s, 230f * s, 0.45f);
            Stars(p, 12, Gold, WarmWhite, 260f * s, 560f * s, 26f * s, 46f * s, 0.55f, 0.85f, gravity: -200f * s);
            Steam(p + new Vector2(0, 20f * s), 6);
        }

        /// <summary>A few soft white puffs rising, e.g. hot food waiting on the stove.</summary>
        public static void Steam(Vector2 p, int count)
        {
            float s = Scale;
            GameFx fx = Instance;
            for (int i = 0; i < count; i++)
            {
                fx.Spawn(fx.circleSprite,
                    p + new Vector2(Random.Range(-45f, 45f) * s, Random.Range(-10f, 15f) * s),
                    new Vector2(Random.Range(-25f, 25f) * s, Random.Range(110f, 190f) * s),
                    life: Random.Range(0.9f, 1.4f),
                    size0: Random.Range(22f, 34f) * s, size1: Random.Range(70f, 100f) * s,
                    c0: SteamColor, c1: new Color(1f, 1f, 1f, 0f),
                    gravity: 0f, drag: 0.6f, spin: 0f, fadeIn: 0.2f);
            }
        }

        public static void Steam(Transform at, int count)
        {
            if (TryGetScreenPoint(at, out Vector2 p))
                Steam(p, count);
        }

        /// <summary>Grey smoke rising: food is about to burn (dark = it burned).</summary>
        public static void Smoke(Transform at, int count, bool dark)
        {
            if (!TryGetScreenPoint(at, out Vector2 p)) return;
            float s = Scale;
            GameFx fx = Instance;
            Color c = dark ? DarkSmoke : GreySmoke;
            for (int i = 0; i < count; i++)
            {
                fx.Spawn(fx.circleSprite,
                    p + new Vector2(Random.Range(-50f, 50f) * s, Random.Range(-10f, 20f) * s),
                    new Vector2(Random.Range(-35f, 35f) * s, Random.Range(90f, 170f) * s),
                    life: Random.Range(1.0f, 1.6f),
                    size0: Random.Range(26f, 40f) * s, size1: Random.Range(90f, 130f) * s,
                    c0: c, c1: new Color(c.r, c.g, c.b, 0f),
                    gravity: 0f, drag: 0.5f, spin: 0f, fadeIn: 0.15f);
            }
        }

        /// <summary>A customer got their order: colourful twinkles and a ring.</summary>
        public static void Served(Transform at)
        {
            if (!TryGetScreenPoint(at, out Vector2 p)) return;
            float s = Scale;
            Ring(p, WarmWhite, 30f * s, 200f * s, 0.4f);
            Confetti(p, 16, 300f * s, 650f * s);
            Stars(p, 8, Gold, Color.white, 200f * s, 420f * s, 24f * s, 40f * s, 0.5f, 0.75f, gravity: -150f * s);
        }

        /// <summary>The ice cream landed on a customer's bar.</summary>
        public static void IceCreamHit(Vector2 p)
        {
            float s = Scale;
            Color pink = new Color(1f, 0.55f, 0.75f);
            Color mint = new Color(0.6f, 1f, 0.85f);
            Ring(p, pink, 30f * s, 210f * s, 0.4f);
            Ring(p, Color.white, 20f * s, 140f * s, 0.3f);
            Stars(p, 10, pink, Color.white, 250f * s, 520f * s, 24f * s, 42f * s, 0.5f, 0.8f, gravity: -250f * s);
            Stars(p, 6, mint, Color.white, 180f * s, 380f * s, 20f * s, 34f * s, 0.45f, 0.7f, gravity: -250f * s);
        }

        /// <summary>One little twinkle left behind by something flying.</summary>
        public static void TrailSparkle(Vector2 p, Color color)
        {
            float s = Scale;
            GameFx fx = Instance;
            fx.Spawn(fx.starSprite,
                p + Random.insideUnitCircle * 12f * s,
                Random.insideUnitCircle * 40f * s,
                life: Random.Range(0.3f, 0.5f),
                size0: Random.Range(22f, 34f) * s, size1: 4f * s,
                c0: color, c1: new Color(color.r, color.g, color.b, 0f),
                gravity: -60f * s, drag: 2f, spin: Random.Range(-360f, 360f), fadeIn: 0f);
        }

        /// <summary>Small dust/sparkle puff when an item lands somewhere.</summary>
        public static void LandPuff(Transform at)
        {
            if (!TryGetScreenPoint(at, out Vector2 p)) return;
            float s = Scale;
            GameFx fx = Instance;
            for (int i = 0; i < 6; i++)
            {
                float a = (i / 6f) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.5f);
                fx.Spawn(fx.circleSprite, p, dir * Random.Range(160f, 260f) * s,
                    life: Random.Range(0.3f, 0.45f),
                    size0: 18f * s, size1: 34f * s,
                    c0: new Color(1f, 1f, 1f, 0.7f), c1: new Color(1f, 1f, 1f, 0f),
                    gravity: 0f, drag: 4f, spin: 0f, fadeIn: 0f);
            }
        }

        /// <summary>Food thrown in the bin.</summary>
        public static void Trash(Transform at)
        {
            if (!TryGetScreenPoint(at, out Vector2 p)) return;
            float s = Scale;
            GameFx fx = Instance;
            Color dust = new Color(0.55f, 0.5f, 0.45f, 0.7f);
            for (int i = 0; i < 10; i++)
            {
                fx.Spawn(fx.circleSprite, p + new Vector2(0, 30f * s),
                    new Vector2(Random.Range(-260f, 260f), Random.Range(80f, 320f)) * s,
                    life: Random.Range(0.5f, 0.8f),
                    size0: Random.Range(20f, 32f) * s, size1: Random.Range(50f, 70f) * s,
                    c0: dust, c1: new Color(dust.r, dust.g, dust.b, 0f),
                    gravity: -500f * s, drag: 1.5f, spin: 0f, fadeIn: 0f);
            }
        }

        #endregion

        #region Building blocks

        public static void Ring(Vector2 p, Color color, float fromSize, float toSize, float life)
        {
            GameFx fx = Instance;
            fx.Spawn(fx.circleSprite, p, Vector2.zero, life, fromSize, toSize,
                new Color(color.r, color.g, color.b, 0.75f), new Color(color.r, color.g, color.b, 0f),
                gravity: 0f, drag: 0f, spin: 0f, fadeIn: 0f, easeOutSize: true);
        }

        public static void Stars(Vector2 p, int count, Color a, Color b, float minSpeed, float maxSpeed,
                                 float minSize, float maxSize, float minLife, float maxLife, float gravity)
        {
            GameFx fx = Instance;
            for (int i = 0; i < count; i++)
            {
                float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Color c = Color.Lerp(a, b, Random.value * 0.5f);
                fx.Spawn(fx.starSprite, p, dir * Random.Range(minSpeed, maxSpeed),
                    life: Random.Range(minLife, maxLife),
                    size0: Random.Range(minSize, maxSize), size1: 0f,
                    c0: c, c1: new Color(c.r, c.g, c.b, 0f),
                    gravity: gravity, drag: 3.2f, spin: Random.Range(-540f, 540f), fadeIn: 0f);
            }
        }

        private static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.42f, 0.42f), new Color(1f, 0.82f, 0.3f), new Color(0.45f, 0.85f, 0.45f),
            new Color(0.4f, 0.7f, 1f), new Color(0.85f, 0.5f, 1f)
        };

        public static void Confetti(Vector2 p, int count, float minSpeed, float maxSpeed)
        {
            float s = Scale;
            GameFx fx = Instance;
            for (int i = 0; i < count; i++)
            {
                float ang = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Color c = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
                fx.Spawn(fx.circleSprite, p, dir * Random.Range(minSpeed, maxSpeed),
                    life: Random.Range(0.7f, 1.0f),
                    size0: Random.Range(14f, 22f) * s, size1: Random.Range(8f, 12f) * s,
                    c0: c, c1: new Color(c.r, c.g, c.b, 0f),
                    gravity: -1300f * s, drag: 1.2f, spin: 0f, fadeIn: 0f);
            }
        }

        #endregion

        #region Particle simulation

        private class Particle
        {
            public RectTransform rect;
            public Image image;
            public Vector2 pos, vel;
            public float age, life, size0, size1, gravity, drag, spin, angle, fadeIn;
            public Color c0, c1;
            public bool easeOutSize;
        }

        private void Spawn(Sprite sprite, Vector2 pos, Vector2 vel, float life, float size0, float size1,
                           Color c0, Color c1, float gravity, float drag, float spin, float fadeIn,
                           bool easeOutSize = false)
        {
            Particle p;
            if (live.Count >= MaxParticles)
            {
                // Too many on screen (e.g. lots of stoves at once): reuse the oldest one.
                p = live[0];
                live.RemoveAt(0);
            }
            else if (pool.Count > 0)
            {
                p = pool.Pop();
                p.rect.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("fx", typeof(RectTransform), typeof(Image));
                p = new Particle { rect = (RectTransform)go.transform, image = go.GetComponent<Image>() };
                p.rect.SetParent(root, false);
                p.image.raycastTarget = false;
                p.image.maskable = false;
            }

            p.image.sprite = sprite;
            p.pos = pos;
            p.vel = vel;
            p.age = 0f;
            p.life = Mathf.Max(0.05f, life);
            p.size0 = size0;
            p.size1 = size1;
            p.c0 = c0;
            p.c1 = c1;
            p.gravity = gravity;
            p.drag = drag;
            p.spin = spin;
            p.angle = Random.Range(0f, 360f);
            p.fadeIn = fadeIn;
            p.easeOutSize = easeOutSize;

            // Particles must stay under the flying ice cream, which lives on the same canvas.
            p.rect.SetAsFirstSibling();
            Apply(p, 0f);
            live.Add(p);
        }

        private void Update()
        {
            if (live.Count == 0)
                return;

            // Unscaled so effects still finish if the game is paused mid-burst.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            for (int i = live.Count - 1; i >= 0; i--)
            {
                Particle p = live[i];
                p.age += dt;
                if (p.age >= p.life)
                {
                    Release(i);
                    continue;
                }

                p.vel.y += p.gravity * dt;
                if (p.drag > 0f)
                    p.vel *= 1f / (1f + p.drag * dt);
                p.pos += p.vel * dt;
                p.angle += p.spin * dt;

                Apply(p, p.age / p.life);
            }
        }

        private static void Apply(Particle p, float t)
        {
            float sizeT = p.easeOutSize ? 1f - (1f - t) * (1f - t) : t;
            float size = Mathf.LerpUnclamped(p.size0, p.size1, sizeT);

            Color c = Color.Lerp(p.c0, p.c1, t * t);
            if (p.fadeIn > 0f && t < p.fadeIn)
                c.a *= t / p.fadeIn;

            p.rect.position = new Vector3(p.pos.x, p.pos.y, 0f);
            p.rect.sizeDelta = new Vector2(size, size);
            p.rect.localRotation = Quaternion.Euler(0f, 0f, p.angle);
            p.image.color = c;
        }

        private void Release(int index)
        {
            Particle p = live[index];
            // Swap-remove: the list is walked backwards, so the moved item was already updated this frame.
            int last = live.Count - 1;
            live[index] = live[last];
            live.RemoveAt(last);
            p.rect.gameObject.SetActive(false);
            pool.Push(p);
        }

        #endregion
    }
}
