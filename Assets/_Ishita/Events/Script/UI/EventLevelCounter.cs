using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Small pill in the level top bar: point icon + points earned in this level.
// A "+2" flies into it from every served customer.
public class EventLevelCounter : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Image pointIcon;
    [SerializeField] private Text pointsText;
    [SerializeField] private EventFlyingPoint flyingPointPrefab;
    [SerializeField] private float flyDuration = 0.8f;

    private int shownPoints; // visualRoot is saved hidden in the prefab

    public void Show(bool show, EventConfig config)
    {
        visualRoot.SetActive(show);
        shownPoints = 0;
        pointsText.text = "0";

        if (show && config != null)
            pointIcon.sprite = config.pointIcon;
    }

    public void FlyPoint(Vector3 customerWorldPos, int amount, int levelTotal)
    {
        if (!visualRoot.activeInHierarchy || flyingPointPrefab == null)
        {
            SetPoints(levelTotal);
            return;
        }

        Canvas root = GetComponentInParent<Canvas>().rootCanvas;
        Camera canvasCamera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;

        // Customer position in the world -> point on our canvas.
        Camera worldCamera = Camera.main;
        Vector2 screenPoint = worldCamera != null
            ? (Vector2)worldCamera.WorldToScreenPoint(customerWorldPos)
            : new Vector2(Screen.width / 2f, Screen.height / 2f);

        Vector3 start;
        RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)root.transform, screenPoint, canvasCamera, out start);

        EventFlyingPoint flyer = Instantiate(flyingPointPrefab, root.transform);
        flyer.Setup(pointIcon.sprite, amount);

        RectTransform rect = (RectTransform)flyer.transform;
        rect.position = start;
        rect.localScale = Vector3.one;

        Vector3 end = pointIcon.rectTransform.position;
        float lift = Vector3.Distance(start, end) * 0.2f;

        Sequence seq = DOTween.Sequence().SetLink(flyer.gameObject);
        seq.Append(rect.DOScale(1.2f, flyDuration * 0.2f).SetEase(Ease.OutBack));
        seq.Append(rect.DOJump(end, lift, 1, flyDuration * 0.8f).SetEase(Ease.InOutQuad));
        seq.Join(rect.DOScale(0.6f, flyDuration * 0.8f));
        seq.OnComplete(() =>
        {
            Destroy(flyer.gameObject);
            SetPoints(levelTotal);
        });
    }

    private void SetPoints(int value)
    {
        shownPoints = Mathf.Max(shownPoints, value);
        pointsText.text = shownPoints.ToString();

        pointIcon.transform.DOKill(true);
        pointIcon.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 6, 0.6f);
    }
}
