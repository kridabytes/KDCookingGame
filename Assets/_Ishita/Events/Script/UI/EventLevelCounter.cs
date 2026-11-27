using DG.Tweening;
using MultiTechStudio.Cooking2D;
using UnityEngine;
using UnityEngine.UI;

// Small pill in the level top bar: point icon + points earned in this level.
// For every served customer: "+2" pops over the customer, then strawberries fly on a curve
// (with a sparkle trail, like the ice cream) into the icon and the number counts up.
//
// Customers and the top bar are on different canvases, so - like FlyingIceCreamAnimation -
// everything is drawn on the GameFx overlay layer and works in screen pixels.
public class EventLevelCounter : MonoBehaviour
{
    private static readonly Color TrailColor = new Color(1f, 0.35f, 0.45f);

    [SerializeField] private GameObject visualRoot;        // saved hidden in the prefab
    [SerializeField] private Image pointIcon;
    [SerializeField] private Text pointsText;

    [Header("Fly animation")]
    [SerializeField] private float flyTime = 0.5f;         // time for one strawberry to reach the icon
    [SerializeField] private float delayBetween = 0.08f;   // gap between strawberries of one order
    [SerializeField] private int maxFlyingIcons = 5;       // more points than this share the icons
    [SerializeField] private float iconSize = 80f;         // strawberry size (scaled to the screen)

    private int shownPoints;
    private int targetPoints;

    public void Show(bool show, EventConfig config)
    {
        visualRoot.SetActive(show);
        shownPoints = 0;
        targetPoints = 0;
        pointsText.text = "0";

        if (show && config != null && config.pointIcon != null)
            pointIcon.sprite = config.pointIcon;
    }

    public void FlyPoint(BotMain customer, int amount, int levelTotal)
    {
        targetPoints = Mathf.Max(targetPoints, levelTotal);

        // Start above the customer (their patience bar), like the ice cream target.
        Transform from = customer == null ? null : customer.fillBar != null ? customer.fillBar.transform : customer.transform;
        if (!visualRoot.activeInHierarchy || amount <= 0 || from == null ||
            !GameFx.TryGetScreenPoint(from, out Vector2 start))
        {
            SetPoints(targetPoints);
            return;
        }

        PopAmountText(start, amount);

        // Split the points over a few strawberries, e.g. +2 -> 2 strawberries of 1 point.
        int icons = Mathf.Clamp(amount, 1, maxFlyingIcons);
        for (int i = 0; i < icons; i++)
        {
            int value = amount / icons + (i < amount % icons ? 1 : 0);
            FlyOne(start, value, i * delayBetween);
        }
    }

    // One strawberry: pop out, fly on an arc to the counter icon, small burst when it lands.
    private void FlyOne(Vector2 start, int value, float delay)
    {
        GameObject go = new GameObject("Event Strawberry", typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(GameFx.Layer, false);
        rect.SetAsLastSibling();

        Image image = go.GetComponent<Image>();
        image.sprite = pointIcon.sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        float size = iconSize * GameFx.Scale;
        rect.sizeDelta = new Vector2(size, size);
        rect.position = start;
        rect.localScale = Vector3.zero;

        float t = 0f;
        float trailTimer = 0f;

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(go);
        seq.AppendInterval(delay);
        seq.Append(rect.DOScale(1.2f, 0.12f).SetEase(Ease.OutBack));
        seq.Append(DOTween.To(() => t, value01 =>
        {
            t = value01;
            Vector2 end = IconScreenPoint(start); // follows the icon if it moves
            rect.position = Arc(start, end, t);
            rect.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.6f, t);

            trailTimer -= Time.unscaledDeltaTime;
            if (trailTimer <= 0f)
            {
                trailTimer = 0.04f;
                GameFx.TrailSparkle(rect.position, TrailColor);
            }
        }, 1f, flyTime).SetEase(Ease.InOutSine));
        seq.OnComplete(() =>
        {
            GameFx.Ring(IconScreenPoint(start), TrailColor, 20f * GameFx.Scale, 110f * GameFx.Scale, 0.25f);
            Destroy(go);
            SetPoints(Mathf.Min(shownPoints + value, targetPoints));
        });
    }

    // "+2" that pops up over the customer and fades away.
    private void PopAmountText(Vector2 position, int amount)
    {
        GameObject go = new GameObject("Event Points Text", typeof(RectTransform), typeof(Text), typeof(CanvasGroup));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(GameFx.Layer, false);
        rect.SetAsLastSibling();

        float s = GameFx.Scale;
        rect.sizeDelta = new Vector2(220f * s, 90f * s);
        rect.position = position;
        rect.localScale = Vector3.zero;

        Text text = go.GetComponent<Text>();
        text.font = pointsText.font;
        text.fontSize = Mathf.RoundToInt(56f * s);
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = "+" + amount;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.05f, 0.15f, 1f);
        outline.effectDistance = new Vector2(3f, -3f) * s;

        CanvasGroup group = go.GetComponent<CanvasGroup>();

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(go);
        seq.Append(rect.DOScale(1f, 0.18f).SetEase(Ease.OutBack));
        seq.Join(rect.DOMoveY(position.y + 90f * s, 0.7f).SetEase(Ease.OutCubic));
        seq.Insert(0.45f, group.DOFade(0f, 0.25f));
        seq.OnComplete(() => Destroy(go));
    }

    private Vector2 IconScreenPoint(Vector2 fallback)
    {
        return GameFx.TryGetScreenPoint(pointIcon.transform, out Vector2 point) ? point : fallback;
    }

    // Quadratic curve that bows upwards; higher for longer flights.
    private static Vector2 Arc(Vector2 a, Vector2 b, float t)
    {
        Vector2 middle = (a + b) * 0.5f + Vector2.up * (Vector2.Distance(a, b) * 0.3f + 60f * GameFx.Scale);
        float u = 1f - t;
        return u * u * a + 2f * u * t * middle + t * t * b;
    }

    private void SetPoints(int value)
    {
        shownPoints = Mathf.Max(shownPoints, value);
        pointsText.text = shownPoints.ToString();

        pointIcon.transform.DOKill(true);
        pointIcon.transform.localScale = Vector3.one;
        pointIcon.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 8, 0.7f).SetUpdate(true);
        pointsText.transform.DOKill(true);
        pointsText.transform.localScale = Vector3.one;
        pointsText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.7f).SetUpdate(true);
    }
}
