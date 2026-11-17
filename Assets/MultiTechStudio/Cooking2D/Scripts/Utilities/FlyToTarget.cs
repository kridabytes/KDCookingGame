using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Flies a picture of an item from one place to another in a small arc, for example
    /// raw meat from the tray to the stove, or a cooked patty from the stove to the plate.
    ///
    /// How it works: we make a temporary Image with the item's sprite on the top canvas,
    /// move it to the destination, then delete it and call <c>onLanded</c>. The real item
    /// at the destination should stay hidden until <c>onLanded</c> runs, so the player only
    /// ever sees one item travelling (use <see cref="FlyAndReveal"/> to get that for free).
    /// </summary>
    public static class FlyToTarget
    {
        public const float DefaultDuration = 0.45f;

        /// <summary>
        /// Draw order of the flying copy. The stoves, plates and customers each sit on their own
        /// canvas with a sort order between -1 and 10, so the copy needs a higher number to be seen.
        /// </summary>
        public const int FlyingSortingOrder = 100;

        /// <summary>
        /// Flies <paramref name="sprite"/> from <paramref name="from"/> to <paramref name="to"/>.
        /// By default the flying copy has the size of <paramref name="to"/>.
        /// If anything needed is missing, <paramref name="onLanded"/> runs straight away so the game never gets stuck.
        /// </summary>
        /// <param name="useSourceSize">Keep the size of <paramref name="from"/> instead (e.g. a pizza flying into a big oven).</param>
        /// <param name="landScale">Scale at the end of the flight. Below 1 makes it look like it goes "into" the target.</param>
        public static void Fly(Sprite sprite, Transform from, Transform to, Action onLanded,
                               float duration = DefaultDuration, bool useSourceSize = false, float landScale = 1f)
        {
            Canvas canvas = to != null ? to.GetComponentInParent<Canvas>() : null;
            if (sprite == null || from == null || canvas == null)
            {
                onLanded?.Invoke();
                return;
            }

            Transform canvasTransform = canvas.rootCanvas.transform;

            var flyer = new GameObject("Flying " + sprite.name, typeof(RectTransform), typeof(Image));
            // Same layer as the game UI. The UI camera only draws the "UI" layer, and a new
            // GameObject starts on "Default", so without this the copy is never drawn.
            flyer.layer = to.gameObject.layer;

            var flyerRect = (RectTransform)flyer.transform;
            flyerRect.SetParent(canvasTransform, false);
            DrawOnTop(flyer);

            var image = flyer.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // Size of the copy, measured in the top canvas' units.
            flyerRect.sizeDelta = SizeInCanvas(useSourceSize ? from : to, canvasTransform);
            flyerRect.position = from.position;
            flyerRect.localScale = Vector3.one * (useSourceSize ? 1f : 0.85f);

            // A gentle arc: the longer the distance, the higher the hop.
            float hop = Vector3.Distance(from.position, to.position) * 0.3f;

            // Soft start and soft landing (InOutSine), and the item grows a little at the
            // top of the arc then settles to its landing size, like it is being tossed.
            DOTween.Sequence()
                .Append(flyerRect.DOJump(to.position, hop, 1, duration).SetEase(Ease.InOutSine))
                .Join(flyerRect.DOScale(1.12f, duration * 0.5f).SetEase(Ease.OutSine))
                .Insert(duration * 0.5f, flyerRect.DOScale(landScale, duration * 0.5f).SetEase(Ease.InSine))
                .SetLink(flyer)
                .OnComplete(() =>
                {
                    UnityEngine.Object.Destroy(flyer);
                    onLanded?.Invoke();
                })
                .OnKill(() =>
                {
                    // If the level closes mid-flight, still clean up.
                    if (flyer != null)
                        UnityEngine.Object.Destroy(flyer);
                });
        }

        /// <summary>
        /// Hides <paramref name="target"/>, flies its picture in from <paramref name="from"/>,
        /// then shows the target with a small "plop" bounce.
        /// </summary>
        public static void FlyAndReveal(Sprite sprite, Transform from, GameObject target, float duration = DefaultDuration)
        {
            if (target == null)
                return;

            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();

            group.alpha = 0f;

            Fly(sprite, from, target.transform, () =>
            {
                if (group == null)
                    return;

                group.alpha = 1f;
                Plop(target.transform);
            }, duration);
        }

        /// <summary>
        /// Gives <paramref name="uiObject"/> its own canvas with a high sort order so it is drawn
        /// above every other canvas (stoves, plates, customers). The object must already be inside a canvas.
        /// </summary>
        public static void DrawOnTop(GameObject uiObject, int sortingOrder = FlyingSortingOrder)
        {
            Canvas canvas = uiObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = uiObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        /// <summary>Small squash bounce used when an item lands.</summary>
        public static void Plop(Transform target)
        {
            if (target == null)
                return;

            target.DOKill(true);
            target.DOPunchScale(new Vector3(0.12f, -0.08f, 0f), 0.2f, 6, 0.6f);
        }

        private static Vector2 SizeInCanvas(Transform target, Transform canvasTransform)
        {
            var rect = target as RectTransform;
            if (rect == null)
                return new Vector2(100f, 100f);

            Vector3 targetScale = rect.lossyScale;
            Vector3 canvasScale = canvasTransform.lossyScale;

            return new Vector2(
                rect.rect.width * SafeDivide(targetScale.x, canvasScale.x),
                rect.rect.height * SafeDivide(targetScale.y, canvasScale.y));
        }

        private static float SafeDivide(float a, float b)
        {
            return Mathf.Approximately(b, 0f) ? 1f : a / b;
        }
    }
}
