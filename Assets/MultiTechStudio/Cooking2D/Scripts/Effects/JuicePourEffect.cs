using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// "Juice pouring into a glass" effect, built entirely in code so it needs no extra assets.
    ///
    /// Three layers of particles:
    ///   • Stream  – a thick, glossy line of juice falling from the nozzle into the glass.
    ///   • Splash  – little drops that jump up where the stream hits the juice and fall back.
    ///   • Foam    – small light bubbles on the juice surface.
    ///
    /// The splash and foam follow the juice level while the glass fills up (they read the
    /// fill amount of the glass picture every frame), and the stream gets shorter as the
    /// glass gets fuller, so it always looks like it is landing in the juice.
    ///
    /// All sizes and speeds are in canvas pixels, because the particles inherit the glass' scale.
    /// </summary>
    public class JuicePourEffect : MonoBehaviour
    {
        // ---- Look (canvas pixels and seconds) ----
        private const float StreamSpeed = 650f;      // how fast the juice falls
        private const float StreamWidth = 13f;
        private const float StreamRate = 90f;        // drops per second; many drops overlap into a line
        private const float SplashRate = 35f;
        private const float FoamRate = 14f;

        private Image liquid;            // the glass fill picture (its fillAmount is the juice level)
        private RectTransform surface;   // moved to the juice surface every frame
        private ParticleSystem stream;
        private ParticleSystem streamShine;
        private ParticleSystem splash;
        private ParticleSystem foam;
        private bool isPouring;

        private static Material sharedMaterial;
        private static Texture2D softDot;

        private RectTransform glass;
        private Vector2 nozzleOffset;

        /// <summary>
        /// Creates the effect.
        /// </summary>
        /// <param name="owner">The machine. The effect is attached to it (not to the glass, because the
        /// glass gets hidden when it is full and the last drops should still finish falling).</param>
        /// <param name="glass">The glass the juice pours into. The stream starts at its centre and falls down.</param>
        /// <param name="liquid">The glass fill image; its fillAmount tells how full the glass is.</param>
        /// <param name="nozzleOffset">Where the stream starts, in pixels from the centre of the glass.</param>
        /// <param name="juiceColor">Colour of the juice.</param>
        public static JuicePourEffect Create(Transform owner, RectTransform glass, Image liquid, Vector2 nozzleOffset, Color juiceColor)
        {
            var root = new GameObject("JuicePourEffect", typeof(RectTransform));
            root.layer = glass.gameObject.layer; // the UI camera only draws the UI layer

            var rect = (RectTransform)root.transform;
            rect.SetParent(owner, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;

            var effect = root.AddComponent<JuicePourEffect>();
            effect.glass = glass;
            effect.nozzleOffset = nozzleOffset;
            effect.Build(liquid, juiceColor);
            return effect;
        }

        /// <summary>
        /// Keeps the juice point at the centre of the glass (plus the offset).
        /// Everything is drawn from this point downwards, never above it.
        /// </summary>
        private void FollowGlass()
        {
            Rect g = glass.rect;
            Vector3 nozzleInGlass = new Vector3(g.center.x + nozzleOffset.x, g.center.y + nozzleOffset.y, 0f);
            transform.position = glass.TransformPoint(nozzleInGlass);
        }

        public void Play()
        {
            isPouring = true;
            UpdateLevel();
            stream.Play();
            streamShine.Play();
            splash.Play();
            foam.Play();
        }

        /// <summary>Stops pouring. Drops already in the air finish falling, so it ends softly.</summary>
        public void Stop()
        {
            isPouring = false;
            stream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            streamShine.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            splash.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            foam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public void StopAndClear()
        {
            isPouring = false;
            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>(true))
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void LateUpdate()
        {
            if (isPouring)
                UpdateLevel();
        }

        /// <summary>
        /// Puts the splash and foam on the juice surface and makes the stream end there.
        /// </summary>
        private void UpdateLevel()
        {
            FollowGlass();

            RectTransform liquidRect = liquid.rectTransform;
            Rect r = liquidRect.rect;

            // Juice surface height inside the glass (fills from the bottom up).
            float fill = liquid.type == Image.Type.Filled ? liquid.fillAmount : 1f;
            Vector3 surfaceInLiquid = new Vector3(r.center.x, r.yMin + r.height * fill, 0f);
            Vector3 surfaceLocal = transform.InverseTransformPoint(liquidRect.TransformPoint(surfaceInLiquid));

            // Stream falls straight down, so keep the splash under the juice point.
            // Once the juice rises past the point, the splash stays on the point (never above it).
            surface.localPosition = new Vector3(0f, Mathf.Min(0f, surfaceLocal.y), 0f);

            // The stream lives just long enough to reach the surface.
            float fallDistance = Mathf.Max(4f, -surfaceLocal.y);
            ParticleSystem.MainModule main = stream.main;
            main.startLifetime = fallDistance / StreamSpeed;
            ParticleSystem.MainModule shineMain = streamShine.main;
            shineMain.startLifetime = fallDistance / StreamSpeed;

            // Foam spreads across most of the glass width.
            ParticleSystem.ShapeModule foamShape = foam.shape;
            foamShape.scale = new Vector3(Mathf.Max(10f, r.width * 0.7f), 2f, 1f);
        }

        // ------------------------------------------------------------------ building

        private void Build(Image liquidImage, Color juice)
        {
            liquid = liquidImage;

            var surfaceGo = new GameObject("Surface", typeof(RectTransform));
            surfaceGo.layer = gameObject.layer;
            surface = (RectTransform)surfaceGo.transform;
            surface.SetParent(transform, false);

            // Draw just above the glass picture.
            Canvas canvas = liquidImage.canvas;
            int layerId = canvas != null ? canvas.sortingLayerID : 0;
            int order = (canvas != null ? canvas.sortingOrder : 0) + 1;

            Color light = Color.Lerp(juice, Color.white, 0.45f);
            Color foamColor = Color.Lerp(juice, Color.white, 0.75f);

            stream = MakeStream("Stream", transform, juice, StreamWidth, order, layerId);
            streamShine = MakeStream("Stream Shine", transform, light, StreamWidth * 0.35f, order + 1, layerId);
            splash = MakeSplash(surface, light, order + 2, layerId);
            foam = MakeFoam(surface, foamColor, order + 2, layerId);

            StopAndClear();
        }

        private ParticleSystem MakeStream(string objectName, Transform parent, Color color, float width, int order, int layerId)
        {
            ParticleSystem ps = NewSystem(objectName, parent, order, layerId);

            ParticleSystem.MainModule main = ps.main;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(width * 0.85f, width * 1.15f);
            main.startColor = color;
            main.startLifetime = 0.15f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = StreamRate;

            // A thin line at the nozzle so the stream wobbles a tiny bit, like real liquid.
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
            shape.radius = width * 0.12f;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = 0f;
            velocity.y = -StreamSpeed;
            velocity.z = 0f;

            // Stretch each drop along its movement so the drops join into one smooth stream.
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.velocityScale = 0.035f;

            return ps;
        }

        private ParticleSystem MakeSplash(Transform parent, Color color, int order, int layerId)
        {
            ParticleSystem ps = NewSystem("Splash", parent, order, layerId);

            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(70f, 150f);
            main.startSize = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startColor = color;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = SplashRate;

            // A cone pointing up: drops jump up and out to both sides.
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 3f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            // Pull them back down into the glass.
            ParticleSystem.ForceOverLifetimeModule force = ps.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.Local;
            force.x = 0f;
            force.y = -1300f;
            force.z = 0f;

            ShrinkAndFade(ps);
            return ps;
        }

        private ParticleSystem MakeFoam(Transform parent, Color color, int order, int layerId)
        {
            ParticleSystem ps = NewSystem("Foam", parent, order, layerId);

            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 25f);
            main.startSize = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startColor = color;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = FoamRate;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 2f, 1f);
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ShrinkAndFade(ps);
            return ps;
        }

        private ParticleSystem NewSystem(string objectName, Transform parent, int order, int layerId)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy; // sizes/speeds follow the glass' scale (pixels)
            main.maxParticles = 300;
            main.gravityModifier = 0f;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetMaterial();
            renderer.sortingLayerID = layerId;
            renderer.sortingOrder = order;
            renderer.alignment = ParticleSystemRenderSpace.View;

            return ps;
        }

        private static void ShrinkAndFade(ParticleSystem ps)
        {
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f));

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;
        }

        // ------------------------------------------------------------------ coffee stream (scene particle)

        private const string CoffeeShineName = "Coffee Shine";
        private const string CoffeeSplashName = "Coffee Splash";

        /// <summary>
        /// Gives the coffee machine's hand-placed particle a proper "coffee pouring" look.
        /// Its position, direction, width (start size) and length (speed x lifetime) are kept,
        /// because those already fit the machine and the cup. Only the look changes: before it
        /// was a few separate square-ish dots in random orange/brown, now it is one smooth,
        /// glossy coffee stream with a light highlight and small drops where it hits the cup.
        /// Safe to call more than once.
        /// </summary>
        public static void StyleCoffeeStream(ParticleSystem stream, Color coffee)
        {
            if (stream == null || stream.transform.Find(CoffeeShineName) != null)
                return;

            stream.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = stream.main;
            float width = Mathf.Max(0.0001f, main.startSize.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startSize.constantMax : main.startSize.constant);
            float speed = main.startSpeed.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startSpeed.constantMax : main.startSpeed.constant;
            float lifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startLifetime.constantMax : main.startLifetime.constant;
            float length = Mathf.Abs(speed) * lifetime;

            Material material = GetMaterial();
            Color dark = Color.Lerp(coffee, Color.black, 0.18f);
            Color light = Color.Lerp(coffee, Color.white, 0.4f);

            // ---- main stream: many soft drops overlapping into one continuous line
            main.playOnAwake = false;
            main.loop = true;
            main.startSize = new ParticleSystem.MinMaxCurve(width * 0.92f, width * 1.05f);
            main.startSpeed = speed;
            main.startLifetime = lifetime;
            main.startColor = new ParticleSystem.MinMaxGradient(dark, coffee);
            main.startRotation = 0f;
            main.maxParticles = Mathf.Max(main.maxParticles, 400);

            ParticleSystem.EmissionModule emission = stream.emission;
            emission.rateOverTime = Mathf.Clamp(Mathf.Abs(speed) / (width * 0.12f), 120f, 350f);

            ParticleSystem.ShapeModule shape = stream.shape;
            if (shape.enabled)
            {
                shape.angle = 0f;          // straight down, no spray
                shape.radius = width * 0.05f;
            }

            ParticleSystem.ColorOverLifetimeModule colorOverLife = stream.colorOverLifetime;
            colorOverLife.enabled = true;
            var streamFade = new Gradient();
            streamFade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0.5f, 1f) });
            colorOverLife.color = streamFade;

            // Slightly thicker at the nozzle, a little thinner as it falls, like real liquid.
            ParticleSystem.SizeOverLifetimeModule sizeOverLife = stream.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.8f));

            ParticleSystemRenderer renderer = stream.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            int order = renderer.sortingOrder;

            // ---- highlight: a thin light line on one side of the stream (gloss)
            GameObject shineGo = Object.Instantiate(stream.gameObject, stream.transform);
            shineGo.name = CoffeeShineName;
            foreach (Transform child in shineGo.transform)
                Object.Destroy(child.gameObject);
            shineGo.transform.localRotation = Quaternion.identity;
            shineGo.transform.localScale = Vector3.one;
            // Particle sizes are world units here (the particle ignores the canvas scale), so offsets are set in world space.
            shineGo.transform.position = stream.transform.position + stream.transform.TransformDirection(Vector3.left) * width * 0.18f;
            ParticleSystem shine = shineGo.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule shineMain = shine.main;
            shineMain.startSize = new ParticleSystem.MinMaxCurve(width * 0.22f, width * 0.3f);
            shineMain.startColor = new Color(light.r, light.g, light.b, 0.55f);
            ParticleSystemRenderer shineRenderer = shine.GetComponent<ParticleSystemRenderer>();
            shineRenderer.sortingOrder = order + 1;

            // ---- small drops jumping where the stream lands in the cup
            GameObject splashGo = Object.Instantiate(stream.gameObject, stream.transform);
            splashGo.name = CoffeeSplashName;
            foreach (Transform child in splashGo.transform)
                Object.Destroy(child.gameObject);
            splashGo.transform.localRotation = Quaternion.identity;
            splashGo.transform.localScale = Vector3.one;
            // End of the stream, in the stream's own space (it falls along its emit direction).
            Vector3 fallDirection = shape.enabled
                ? Quaternion.Euler(shape.rotation) * Vector3.forward
                : Vector3.down;
            Vector3 worldFall = stream.transform.TransformDirection(fallDirection).normalized;
            splashGo.transform.position = stream.transform.position + worldFall * length * 0.95f;

            ParticleSystem splash = splashGo.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule splashMain = splash.main;
            splashMain.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            splashMain.startSpeed = new ParticleSystem.MinMaxCurve(width * 1.6f, width * 3.2f);
            splashMain.startSize = new ParticleSystem.MinMaxCurve(width * 0.18f, width * 0.35f);
            splashMain.startColor = new ParticleSystem.MinMaxGradient(coffee, light);
            ParticleSystem.EmissionModule splashEmission = splash.emission;
            splashEmission.rateOverTime = 22f;
            ParticleSystem.ShapeModule splashShape = splash.shape;
            splashShape.enabled = true;
            splashShape.shapeType = ParticleSystemShapeType.Cone;
            splashShape.angle = 40f;
            splashShape.radius = width * 0.2f;
            splashShape.rotation = new Vector3(-90f, 0f, 0f); // upwards
            ParticleSystem.ForceOverLifetimeModule force = splash.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.Local;
            force.x = 0f;
            force.y = -width * 22f;
            force.z = 0f;
            ParticleSystem.ColorOverLifetimeModule splashColor = splash.colorOverLifetime;
            splashColor.enabled = true;
            var splashFade = new Gradient();
            splashFade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            splashColor.color = splashFade;
            ParticleSystem.SizeOverLifetimeModule splashSize = splash.sizeOverLifetime;
            splashSize.enabled = true;
            splashSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f));
            ParticleSystemRenderer splashRenderer = splash.GetComponent<ParticleSystemRenderer>();
            splashRenderer.sortingOrder = order + 2;

            stream.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>One material for all juice effects: a soft round dot, alpha blended.</summary>
        private static Material GetMaterial()
        {
            if (sharedMaterial != null)
                return sharedMaterial;

            if (softDot == null)
            {
                const int size = 64;
                softDot = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Juice Dot",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                var pixels = new Color[size * size];
                float half = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half; // 0 centre, 1 edge
                        float alpha = Mathf.Clamp01((1f - distance) / 0.3f);                                     // solid with a soft rim
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
                softDot.SetPixels(pixels);
                softDot.Apply(false, true);
            }

            // "Sprites/Default" is always included in builds and uses the particle colour.
            Shader shader = Shader.Find("Sprites/Default");
            sharedMaterial = new Material(shader) { name = "Juice Pour", mainTexture = softDot };
            return sharedMaterial;
        }
    }
}
