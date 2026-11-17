using DG.Tweening;
using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// The ice cream that flies from the HUD button to a waiting customer's patience bar.
    ///
    /// 1. It pops up on the button the moment the player taps it.
    /// 2. It flies in a smooth arc to the customer's bar, drawn above everything else.
    /// 3. It shrinks into the bar, and the bar grows, glows and fills up so the
    ///    player clearly sees which customer got the ice cream.
    /// </summary>
    public class FlyingIceCreamAnimation : MonoBehaviour
    {
        [Tooltip("Seconds from the tap until the ice cream reaches the customer.")]
        [SerializeField] private float flightTime = 0.6f;

        [Tooltip("How high the arc goes, in canvas pixels.")]
        [SerializeField] private float arcHeightPixels = 180f;

        [Tooltip("Seconds for the pop-in on the button.")]
        [SerializeField] private float popTime = 0.15f;

        [Tooltip("Seconds for the ice cream to shrink into the bar after it arrives.")]
        [SerializeField] private float absorbTime = 0.15f;

        private BotMain targetBot;

        /// <summary>
        /// Starts the animation. Positions are screen positions; they are turned into
        /// local positions on <paramref name="canvasRect"/>, which is this object's parent.
        /// </summary>
        public void Init(RectTransform canvasRect, Camera canvasCamera, Vector2 startScreenPos, Vector2 endScreenPos, BotMain bot)
        {
            targetBot = bot;

            // The customers and stations sit on their own canvases with higher sort orders,
            // so without this the ice cream would fly behind them.
            FlyToTarget.DrawOnTop(gameObject);

            var rect = transform as RectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreenPos, canvasCamera, out Vector2 startLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreenPos, canvasCamera, out Vector2 endLocal);

            rect.anchoredPosition = startLocal;

            Vector3 normalScale = rect.localScale;
            rect.localScale = Vector3.zero;

            Sequence anim = DOTween.Sequence().SetLink(gameObject);

            // 1. Pop up on the button.
            anim.Append(rect.DOScale(normalScale * 1.15f, popTime).SetEase(Ease.OutBack));

            // 2. Fly: soft start, soft landing, a little bigger at the top of the arc.
            anim.Append(rect.DOJumpAnchorPos(endLocal, arcHeightPixels, 1, flightTime).SetEase(Ease.InOutSine));
            anim.Join(rect.DOScale(normalScale, flightTime).SetEase(Ease.InOutSine));

            // 3. Shrink into the bar, and start filling it at the same moment.
            anim.AppendCallback(OnArrived);
            anim.Append(rect.DOScale(Vector3.zero, absorbTime).SetEase(Ease.InBack));

            anim.OnComplete(() => Destroy(gameObject));
        }

        private void OnArrived()
        {
            if (targetBot == null || IceCreamManager.Instance == null)
                return;

            IceCreamManager.Instance.ApplyBoostToCustomer(targetBot);
            targetBot.anim.SetHappyAnim();
        }
    }
}
