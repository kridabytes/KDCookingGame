using System.Collections;
using MultiTechStudio.Cooking2D;
using UnityEngine;
using UnityEngine.UI;

// The main event popup: banner, timer, points, progress bar and the scrolling reward track.
public class EventPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image tintBackground;

    [Header("Header")]
    [SerializeField] private Image bannerImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Image eventIcon;
    [SerializeField] private Text timerText;
    [SerializeField] private Color timerColor = new Color(0.35f, 0.15f, 0.1f);
    [SerializeField] private Color timerLastDayColor = new Color(0.9f, 0.1f, 0.1f);

    [Header("Points")]
    [SerializeField] private Image pointIcon;
    [SerializeField] private Text pointsText;
    [SerializeField] private Text pointNameText;
    [SerializeField] private Image progressFill;      // Image Type = Filled
    [SerializeField] private Text progressText;       // "340 / 500"

    [Header("Reward track")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;
    [SerializeField] private EventMilestoneItem milestonePrefab;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Text playText;
    [SerializeField] private Button infoButton;

    [Header("How to earn points")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Text infoTitleText;
    [SerializeField] private Text infoText;
    [SerializeField] private Button infoCloseButton;

    [Header("Claim fly")]
    [SerializeField] private float flyDuration = 0.7f;
    [SerializeField] private AudioSource clickSound;

    private EventManager manager;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        playButton.onClick.AddListener(PlayNextLevel);
        infoButton.onClick.AddListener(OpenInfo);
        infoCloseButton.onClick.AddListener(CloseInfo);

        panel.SetActive(false);
        infoPanel.SetActive(false);
    }

    private void Start()
    {
        manager = EventManager.Instance;
        if (manager != null)
            manager.OnChanged += OnEventChanged;
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.OnChanged -= OnEventChanged;
    }

    private void Update()
    {
        if (!IsOpen || manager == null)
            return;

        if (!manager.IsLive)
        {
            Close(); // event ended while the popup was open
            return;
        }

        timerText.text = EventClock.FormatTimeLeft(manager.TimeLeft);
        timerText.color = manager.State == EventState.LastDay ? timerLastDayColor : timerColor;
    }

    public void Open()
    {
        if (manager == null || !manager.IsLive)
            return;

        PlayClick();
        Refresh();
        infoPanel.SetActive(false);
        PopupAnimation.Show(panel);
        StartCoroutine(ScrollToFocus());

        EventAnalytics.Log("event_popup_open",
            "event_id", manager.ActiveEvent.eventId, "points", manager.Progress.points);
    }

    public void Close()
    {
        PlayClick();
        PopupAnimation.Hide(panel);
    }

    private void OnEventChanged()
    {
        if (IsOpen)
            Refresh();
    }

    private void Refresh()
    {
        EventConfig config = manager.ActiveEvent;
        EventProgress progress = manager.Progress;

        if (tintBackground != null) tintBackground.color = config.tintColor;
        if (bannerImage != null && config.banner != null) bannerImage.sprite = config.banner;
        if (eventIcon != null) eventIcon.sprite = config.eventIcon;
        titleText.text = config.GetDisplayName();
        pointIcon.sprite = config.pointIcon;
        pointsText.text = progress.points.ToString("N0");
        if (pointNameText != null) pointNameText.text = config.GetPointName();
        playText.text = GameText.Get("event_play", "Play");

        RefreshProgressBar(config, progress.points);
        BuildTrack(config);
    }

    // Bar from the previous milestone to the next one ("340 / 500").
    private void RefreshProgressBar(EventConfig config, int points)
    {
        int next = manager.GetNextMilestoneIndex();
        int target = config.milestones[next].pointsNeeded;
        int from = next > 0 ? config.milestones[next - 1].pointsNeeded : 0;

        float fill = target > from ? (float)(points - from) / (target - from) : 1f;
        progressFill.fillAmount = Mathf.Clamp01(fill);
        progressText.text = $"{Mathf.Min(points, target):N0} / {target:N0}";
    }

    private void BuildTrack(EventConfig config)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        for (int i = 0; i < config.milestones.Count; i++)
        {
            EventMilestoneItem item = Instantiate(milestonePrefab, content);
            item.Setup(i, config.milestones[i], manager.GetMilestoneState(i), config, manager.database, OnClaimClicked);
        }
    }

    private void OnClaimClicked(EventMilestoneItem item)
    {
        PlayClick();
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        EventMilestone milestone = manager.ActiveEvent.milestones[item.Index];
        RectTransform from = item.FirstRewardIcon;

        // Start the fly animations before the list is rebuilt by the claim.
        foreach (EventReward reward in milestone.rewards)
        {
            Transform target = EventRewardGiver.GetFlyTarget(reward.type, playButton.transform);
            RewardFly.Play(manager.database.GetIcon(reward.type), from, target, flyDuration);
        }

        manager.Claim(item.Index);
    }

    private IEnumerator ScrollToFocus()
    {
        yield return null; // wait one frame so the layout has its size
        Canvas.ForceUpdateCanvases();

        int count = manager.ActiveEvent.milestones.Count;
        if (count <= 1)
            yield break;

        float t = (float)manager.GetFocusMilestoneIndex() / (count - 1);
        scrollRect.verticalNormalizedPosition = 1f - t;
    }

    private void PlayNextLevel()
    {
        Close();
        // Same as pressing the Play button on the home screen.
        if (UiManager.Instance != null && UiManager.Instance.allCanvas.persistent.btn != null)
            UiManager.Instance.allCanvas.persistent.btn.onClick.Invoke();
    }

    private void OpenInfo()
    {
        PlayClick();
        EventConfig config = manager.ActiveEvent;
        string points = config.GetPointName();

        infoTitleText.text = GameText.Get("event_how_title", "How to earn points");
        infoText.text =
            GameText.Format("event_how_order", "Serve a customer: +{0} {1}", config.pointsPerOrder, points) + "\n" +
            GameText.Format("event_how_win", "Win a level: +{0}", config.pointsPerWin) + "\n" +
            GameText.Format("event_how_noburn", "Win without burning food: +{0}", config.bonusNoBurn) + "\n" +
            GameText.Format("event_how_firsttry", "Win on the first try: +{0}", config.bonusFirstTry) + "\n" +
            GameText.Format("event_how_stars", "Win with 3 stars: +{0}", config.bonusThreeStars) + "\n" +
            GameText.Format("event_how_ad", "Watch an ad on the win screen: x2 level points ({0} per day)", config.maxAdDoublePerDay);

        PopupAnimation.Show(infoPanel);
    }

    private void CloseInfo()
    {
        PlayClick();
        PopupAnimation.Hide(infoPanel);
    }

    private void PlayClick()
    {
        if (clickSound != null)
            clickSound.Play();
    }
}
