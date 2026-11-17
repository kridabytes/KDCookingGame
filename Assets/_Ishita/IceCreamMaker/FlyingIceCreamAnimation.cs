using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Moves a flying ice cream along a bezier curve from start to target bot.
    /// When it reaches, it boosts the customer's fill bar and destroys itself.
    /// </summary>
    public class FlyingIceCreamAnimation : MonoBehaviour
    {
        [SerializeField] private float duration = 0.6f;
        [Tooltip("Arc height in canvas local units (pixels).")]
        [SerializeField] private float arcHeight = 160f;

        private RectTransform rect;
        private RectTransform canvasRect;
        private Camera canvasCamera;

        private Vector2 startLocal;
        private Vector2 endLocal;
        private BotMain targetBot;
        private float elapsed;

        /// <summary>
        /// Initialize animation in UI space (RectTransform local position on a canvas).
        /// </summary>
        public void Init(RectTransform canvasRect, Camera canvasCamera, Vector2 startScreenPos, Vector2 endScreenPos, BotMain bot)
        {
            this.canvasRect = canvasRect;
            this.canvasCamera = canvasCamera;
            targetBot = bot;
            elapsed = 0f;

            rect = transform as RectTransform;
            if (rect == null)
                rect = gameObject.AddComponent<RectTransform>();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreenPos, canvasCamera, out startLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreenPos, canvasCamera, out endLocal);

            rect.anchoredPosition = startLocal;
        }

        private void Update()
        {
            if (targetBot == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float easedT = t * t * (3f - 2f * t);

            // Quadratic bezier curve
            Vector2 mid = (startLocal + endLocal) * 0.5f + Vector2.up * arcHeight;
            Vector2 p1 = Vector2.Lerp(startLocal, mid, easedT);
            Vector2 p2 = Vector2.Lerp(mid, endLocal, easedT);
            Vector2 pos = Vector2.Lerp(p1, p2, easedT);
            if (rect != null)
                rect.anchoredPosition = pos;

            if (t >= 1f)
            {
                OnArrived();
            }
        }

        private void OnArrived()
        {
            if (IceCreamManager.Instance != null && targetBot != null)
            {
                IceCreamManager.Instance.ApplyBoostToCustomer(targetBot);
                // Optionally trigger bot happy anim without finishing order:
                targetBot.anim.SetHappyAnim();
            }

            Destroy(gameObject);
        }
    }
}