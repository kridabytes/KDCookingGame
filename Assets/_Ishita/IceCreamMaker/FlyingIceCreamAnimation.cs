using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// The ice cream that flies from the HUD ice cream button to a waiting customer's patience bar.
    ///
    /// The button lives on a Screen Space Overlay canvas while the customers live on a
    /// Screen Space Camera canvas, so the two can't share coordinates. This flyer is drawn on the
    /// <see cref="GameFx"/> overlay layer (above everything) and works purely in screen pixels;
    /// the customer's bar is re-measured every frame through the camera that really draws it.
    ///
    /// 1. Pops out of the button.
    /// 2. Flies in a smooth arc to the bar, leaving a sparkle trail, wobbling a little.
    /// 3. Lands: sparkle burst + the bar grows, glows and fills up.
    /// If the customer leaves before it lands, the ice cream is given back.
    /// </summary>
    public class FlyingIceCreamAnimation : MonoBehaviour
    {
        private const float PopTime = 0.18f;
        private const float FlightTime = 0.7f;
        private const float AbsorbTime = 0.16f;
        private const float TrailInterval = 0.035f;

        private static readonly Color TrailColor = new Color(1f, 0.75f, 0.88f);

        private RectTransform rect;
        private Image image;
        private BotMain targetBot;
        private Transform targetBar;

        private Vector2 start;
        private Vector2 end;
        private float baseSize;
        private float age;
        private float trailTimer;
        private bool arrived;
        private bool boostApplied;
        private float wobbleSeed;

        /// <summary>
        /// Creates and starts a flying ice cream.
        /// </summary>
        /// <param name="sprite">Picture to fly (the ice cream).</param>
        /// <param name="from">The ice cream button (any canvas).</param>
        /// <param name="bot">Customer that receives it.</param>
        public static FlyingIceCreamAnimation Launch(Sprite sprite, Transform from, BotMain bot)
        {
            if (sprite == null || from == null || bot == null || bot.fillBar == null)
                return null;

            if (!GameFx.TryGetScreenPoint(from, out Vector2 startScreen))
                return null;

            var go = new GameObject("Flying IceCream", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(GameFx.Layer, false);
            rt.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var anim = go.AddComponent<FlyingIceCreamAnimation>();
            anim.Init(rt, img, startScreen, GameFx.ScreenHeightOf(from, 120f * GameFx.Scale), bot);
            return anim;
        }

        private void Init(RectTransform rt, Image img, Vector2 startScreen, float sizePixels, BotMain bot)
        {
            rect = rt;
            image = img;
            targetBot = bot;
            targetBar = bot.fillBar.transform;

            start = startScreen;
            end = GameFx.TryGetScreenPoint(targetBar, out Vector2 barScreen) ? barScreen : start;

            // Keep it readable on any phone, never comically large.
            baseSize = Mathf.Clamp(sizePixels, 70f * GameFx.Scale, 170f * GameFx.Scale);
            rect.sizeDelta = new Vector2(baseSize, baseSize);
            rect.position = start;
            rect.localScale = Vector3.zero;
            wobbleSeed = Random.value * 10f;
        }

        private void Update()
        {
            if (rect == null)
                return;

            // Real time, so it can't freeze half-way if something changes timeScale.
            age += Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            // Follow the bar in case it moves (camera shake, layout change...).
            if (!arrived && targetBot != null && targetBar != null && targetBar.gameObject.activeInHierarchy
                && GameFx.TryGetScreenPoint(targetBar, out Vector2 barScreen))
                end = barScreen;

            if (age < PopTime)
            {
                // 1. Pop out of the button.
                float t = age / PopTime;
                rect.localScale = Vector3.one * (EaseOutBack(t) * 1.2f);
                rect.position = start;
                return;
            }

            float ft = (age - PopTime) / FlightTime;
            if (ft < 1f)
            {
                // 2. Arc flight.
                float e = EaseInOut(ft);
                Vector2 pos = Arc(start, end, e);
                rect.position = pos;

                // Slightly bigger mid-air, smaller when it reaches the small bar.
                float scale = Mathf.Lerp(1.2f, 0.75f, e) + Mathf.Sin(ft * Mathf.PI) * 0.15f;
                rect.localScale = Vector3.one * scale;

                // Tilt in the flight direction plus a playful wobble.
                Vector2 ahead = Arc(start, end, Mathf.Min(1f, e + 0.05f)) - pos;
                float tilt = Mathf.Clamp(-ahead.x * 0.6f, -25f, 25f);
                float wobble = Mathf.Sin((age + wobbleSeed) * 22f) * 8f * (1f - ft);
                rect.localRotation = Quaternion.Euler(0f, 0f, tilt + wobble);

                trailTimer -= Time.unscaledDeltaTime;
                if (trailTimer <= 0f)
                {
                    trailTimer = TrailInterval;
                    GameFx.TrailSparkle(pos, TrailColor);
                }
                return;
            }

            if (!arrived)
            {
                // 3. Landed.
                arrived = true;
                rect.position = end;
                rect.localRotation = Quaternion.identity;
                OnArrived();
            }

            float at = (age - PopTime - FlightTime) / AbsorbTime;
            if (at < 1f)
            {
                rect.localScale = Vector3.one * Mathf.Lerp(0.75f, 0f, at * at);
                Color c = image.color;
                c.a = 1f - at;
                image.color = c;
                return;
            }

            Destroy(gameObject);
        }

        private void OnArrived()
        {
            if (boostApplied)
                return;
            boostApplied = true;

            bool canReceive = targetBot != null && targetBot.IsWaitingForOrder && IceCreamManager.Instance != null;
            if (!canReceive)
            {
                // The customer left or was served while the ice cream was in the air: give it back.
                if (IceCreamManager.Instance != null)
                    IceCreamManager.Instance.RefundOne();
                return;
            }

            GameFx.IceCreamHit(end);
            IceCreamManager.Instance.ApplyBoostToCustomer(targetBot);
            if (targetBot.anim != null)
                targetBot.anim.SetHappyAnim();
        }

        private void OnDestroy()
        {
            // Destroyed before landing (scene change, level closed): don't lose the ice cream.
            if (!boostApplied && IceCreamManager.Instance != null && Application.isPlaying)
            {
                boostApplied = true;
                IceCreamManager.Instance.RefundOne();
            }
        }

        /// <summary>Quadratic curve that bows upwards; higher for longer flights.</summary>
        private static Vector2 Arc(Vector2 a, Vector2 b, float t)
        {
            float dist = Vector2.Distance(a, b);
            Vector2 mid = (a + b) * 0.5f + Vector2.up * (dist * 0.35f + 60f * GameFx.Scale);
            float u = 1f - t;
            return u * u * a + 2f * u * t * mid + t * t * b;
        }

        private static float EaseInOut(float t)
        {
            // Soft start, quicker middle, soft landing.
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
