using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Round event button on the home screen: event icon, countdown pill and red dot.
// Hidden when no event is live.
public class EventHomeButton : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;   // the part that is shown / hidden
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Text timerText;
    [SerializeField] private Image timerPill;
    [SerializeField] private GameObject redDot;
    [SerializeField] private EventPopupUI eventPopup;

    [Header("Look")]
    [SerializeField] private Color timerColor = new Color(0.35f, 0.15f, 0.1f);
    [SerializeField] private Color timerLastDayColor = Color.white;
    [SerializeField] private Color pillColor = Color.white;
    [SerializeField] private Color pillLastDayColor = new Color(0.95f, 0.25f, 0.25f);
    [SerializeField] private float bounceEvery = 3f;
    [SerializeField] private float refreshEvery = 0.5f;

    private float refreshTimer;
    private float bounceTimer;

    private void Awake()
    {
        button.onClick.AddListener(() => eventPopup.Open());
        visualRoot.SetActive(false);
    }

    private void Update()
    {
        EventManager manager = EventManager.Instance;
        if (manager == null)
            return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshEvery;
            Refresh(manager);
        }

        // Gentle bounce every few seconds when a reward is waiting.
        if (visualRoot.activeSelf && redDot.activeSelf)
        {
            bounceTimer -= Time.unscaledDeltaTime;
            if (bounceTimer <= 0f)
            {
                bounceTimer = bounceEvery;
                visualRoot.transform.DOKill(true);
                visualRoot.transform.DOPunchScale(Vector3.one * 0.12f, 0.5f, 5, 0.5f).SetUpdate(true);
            }
        }
    }

    private void Refresh(EventManager manager)
    {
        bool live = manager.IsLive;
        if (visualRoot.activeSelf != live)
            visualRoot.SetActive(live);
        if (!live)
            return;

        bool lastDay = manager.State == EventState.LastDay;

        icon.sprite = manager.ActiveEvent.eventIcon;
        timerText.text = EventClock.FormatTimeLeft(manager.TimeLeft);
        timerText.color = lastDay ? timerLastDayColor : timerColor;
        if (timerPill != null)
            timerPill.color = lastDay ? pillLastDayColor : pillColor;

        redDot.SetActive(manager.HasRewardToClaim());
    }
}
