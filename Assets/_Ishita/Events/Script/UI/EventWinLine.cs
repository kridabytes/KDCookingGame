using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Line on the win screen: "+55" event points and the x2 rewarded ad button.
public class EventWinLine : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Image pointIcon;
    [SerializeField] private Text pointsText;
    [SerializeField] private Button doubleButton;
    [SerializeField] private Text doubleText;

    private int points;

    private void Awake()
    {
        // visualRoot is saved hidden in the prefab. Don't hide it here: Show() can run
        // before Awake when the win screen turns on.
        doubleButton.onClick.AddListener(OnDoubleClicked);
    }

    public void Show(int levelPoints, bool canDouble, Sprite icon)
    {
        points = levelPoints;
        visualRoot.SetActive(true);

        pointIcon.sprite = icon;
        pointsText.text = "+" + points;
        doubleText.text = GameText.Get("event_x2", "x2");
        doubleButton.gameObject.SetActive(canDouble);

        visualRoot.transform.localScale = Vector3.zero;
        visualRoot.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(0.4f).SetUpdate(true);
    }

    private void OnDoubleClicked()
    {
        doubleButton.interactable = false;

        if (GoogleMobileAd.Instatiate == null)
        {
            OnAdRewarded();
            return;
        }
        GoogleMobileAd.Instatiate.ShowEventRewardAd(OnAdRewarded);
    }

    private void OnAdRewarded()
    {
        if (EventManager.Instance == null || !EventManager.Instance.DoubleLastLevel())
        {
            doubleButton.interactable = true;
            return;
        }

        points *= 2;
        pointsText.text = "+" + points;
        doubleButton.gameObject.SetActive(false);
        pointsText.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 0.6f).SetUpdate(true);
    }
}
