using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Plays the "coins fly into the wallet" effect used for coins, diamonds, smiles and likes.
    ///
    /// Each icon does three quick moves:
    ///   1. pops out from the start point and spreads a little (burst),
    ///   2. holds for a split second so the eye can catch it,
    ///   3. speeds up into the counter in the top bar and disappears.
    /// The counter icon gives a small bounce every time an icon lands.
    ///
    /// The whole thing takes well under a second. The old version waited 2 seconds and then
    /// flew for up to 3 seconds, and it applied the per-coin delay twice, which is why it felt slow.
    /// </summary>
    public class CoinAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite animSprite;
        [SerializeField] private GameObject animImage;
        [SerializeField] public Transform targetPos;
        [SerializeField] private float spreadArea = 2f;
        [SerializeField] private bool isMultiple = false;
        [SerializeField] private int multipleCount = 10;

        [Header("Timing (seconds)")]
        [Tooltip("Time for the icons to pop out around the start point.")]
        [SerializeField] private float burstTime = 0.25f;

        [Tooltip("Short pause between the burst and the flight.")]
        [SerializeField] private float holdTime = 0.08f;

        [Tooltip("Time for one icon to fly into the counter.")]
        [SerializeField] private float flyTime = 0.45f;

        [Tooltip("Gap between two icons when several are spawned.")]
        [SerializeField] private float staggerDelay = 0.04f;

        private int IconCount => isMultiple ? Mathf.Max(1, multipleCount) : 1;

        /// <summary>Seconds from PlayAnim() until the first icon reaches the counter.</summary>
        public float FirstArrivalTime => burstTime + holdTime + flyTime;

        /// <summary>Seconds between the first and the last icon reaching the counter.</summary>
        public float ArrivalSpread => staggerDelay * (IconCount - 1);

        [ContextMenu("PlayAnim")]
        public void PlayAnim(Vector3 _position = default)
        {
            if (animImage == null || targetPos == null)
                return;

            if (animSprite != null)
                animImage.GetComponent<Image>().sprite = animSprite;

            Vector3 startPos = _position == default ? transform.position : _position;

            for (int i = 0; i < IconCount; i++)
                LaunchIcon(startPos, i);
        }

        private void LaunchIcon(Vector3 startPos, int index)
        {
            GameObject icon = Instantiate(animImage, startPos, transform.rotation, transform);
            icon.SetActive(true);

            Transform iconTransform = icon.transform;
            Vector3 normalScale = iconTransform.localScale;
            iconTransform.localScale = Vector3.zero;

            // Only spread when there are several icons; a single coin goes straight up.
            Vector3 burstPos = startPos;
            if (IconCount > 1)
                burstPos += (Vector3)(Random.insideUnitCircle * spreadArea);

            Sequence flight = DOTween.Sequence().SetLink(icon);
            flight.SetDelay(index * staggerDelay);

            // 1. Burst out.
            flight.Append(iconTransform.DOMove(burstPos, burstTime).SetEase(Ease.OutCubic));
            flight.Join(iconTransform.DOScale(normalScale, burstTime).SetEase(Ease.OutBack));

            // 2. Short hold.
            flight.AppendInterval(holdTime);

            // 3. Fly into the counter, speeding up at the end so it feels snappy.
            flight.Append(iconTransform.DOMove(targetPos.position, flyTime).SetEase(Ease.InCubic));
            flight.Join(iconTransform.DOScale(normalScale * 0.6f, flyTime).SetEase(Ease.InQuad));

            flight.OnComplete(() =>
            {
                BounceCounterIcon();
                Destroy(icon);
            });
        }

        private void BounceCounterIcon()
        {
            // Complete any running bounce first so the scale always goes back to normal.
            targetPos.DOKill(true);
            targetPos.DOPunchScale(Vector3.one * 0.15f, 0.18f, 6, 0.6f);
        }
    }
}
