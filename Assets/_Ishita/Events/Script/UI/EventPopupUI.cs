using System.Collections;
using System.Collections.Generic;
using MultiTechStudio.Cooking2D;
using UnityEngine;
using UnityEngine.UI;

// The event popup:
// - points + fill bar, with one gift prefab per milestone placed on the bar by its points
// - gifts that are reached are given automatically (with animation) when the popup opens
// - event task cards (prefab) in the scroll view, each with its own claim button
// - timer and the player's rank
public class EventPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject panel;

    [Header("Header")]
    [SerializeField] private Graphic titleText;
    [SerializeField] private Image eventIcon;
    [SerializeField] private Graphic timerText;
    [SerializeField] private Color timerColor = new Color(0.35f, 0.15f, 0.1f);
    [SerializeField] private Color timerLastDayColor = new Color(0.9f, 0.1f, 0.1f);

    [Header("Points + gifts on the fill bar")]
    [SerializeField] private Image pointIcon;
    [SerializeField] private Graphic pointsText;
    [SerializeField] private Image progressFill;          // Image Type = Filled, Horizontal
    [SerializeField] private Graphic progressText;        // optional "340 / 500"
    [SerializeField] private RectTransform giftsParent;   // gifts are spawned here
    [SerializeField] private EventPointGiftItem giftPrefab;

    [Header("Event tasks")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;
    [SerializeField] private EventTaskItem taskPrefab;

    [Header("Rank")]
    [SerializeField] private Graphic rankText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Graphic playText;
    [SerializeField] private Button infoButton;

    [Header("How to earn points")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Graphic infoTitleText;
    [SerializeField] private Graphic infoText;
    [SerializeField] private Button infoCloseButton;

    [Header("Animation")]
    [SerializeField] private float giftGiveDelay = 0.6f;   // wait after opening before reached gifts are given
    [SerializeField] private float giftGiveGap = 0.45f;    // time between two gifts
    [SerializeField] private AudioSource clickSound;

    private EventManager manager;
    private readonly List<EventPointGiftItem> gifts = new List<EventPointGiftItem>();
    private bool givingGifts;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        if (playButton != null) playButton.onClick.AddListener(PlayNextLevel);
        if (infoButton != null) infoButton.onClick.AddListener(OpenInfo);
        if (infoCloseButton != null) infoCloseButton.onClick.AddListener(CloseInfo);

        panel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
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

        EventUIText.Set(timerText, EventClock.FormatTimeLeft(manager.TimeLeft));
        if (timerText != null)
            timerText.color = manager.State == EventState.LastDay ? timerLastDayColor : timerColor;
    }

    public void Open()
    {
        if (manager == null)
            manager = EventManager.Instance;
        if (manager == null || !manager.IsLive)
            return;

        PlayClick();
        if (infoPanel != null) infoPanel.SetActive(false);
        PopupAnimation.Show(panel);
        Refresh();
        RefreshRank();
        StartCoroutine(GiveReachedGifts());

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
        int points = manager.Progress.points;

        EventUIText.Set(titleText, config.GetDisplayName());
        if (eventIcon != null && config.eventIcon != null) eventIcon.sprite = config.eventIcon;
        if (pointIcon != null && config.pointIcon != null) pointIcon.sprite = config.pointIcon;
        EventUIText.Set(pointsText, points.ToString("N0"));
        EventUIText.Set(playText, GameText.Get("event_play", "Play Event"));

        // Gifts are evenly spaced, and the bar fills section by section between them.
        if (progressFill != null)
            progressFill.fillAmount = GetFillAmount(config, points);

        int next = manager.GetNextMilestoneIndex();
        int target = config.milestones.Count > 0 ? config.milestones[next].pointsNeeded : 0;
        EventUIText.Set(progressText, $"{Mathf.Min(points, target):N0} / {target:N0}");

        BuildGifts(config, points);
        BuildTasks(config);
    }

    #region Gifts on the fill bar

    private void BuildGifts(EventConfig config, int points)
    {
        foreach (EventPointGiftItem old in gifts)
        {
            if (old != null)
                Destroy(old.gameObject);
        }
        gifts.Clear();

        int count = config.milestones.Count;
        if (count == 0)
            return;

        // Left and right end of the fill bar, in the gifts parent space.
        RectTransform bar = progressFill.rectTransform;
        Vector3 left = giftsParent.InverseTransformPoint(bar.TransformPoint(new Vector3(bar.rect.xMin, bar.rect.center.y)));
        Vector3 right = giftsParent.InverseTransformPoint(bar.TransformPoint(new Vector3(bar.rect.xMax, bar.rect.center.y)));

        // Shrink the gifts if there are too many to fit side by side.
        float slotWidth = Mathf.Abs(right.x - left.x) / count;
        RectTransform giftRect = (RectTransform)giftPrefab.transform;
        float giftWidth = giftRect.rect.width * Mathf.Abs(giftRect.localScale.x);
        float scale = giftWidth > slotWidth && giftWidth > 0f ? slotWidth / giftWidth * 0.95f : 1f;

        for (int i = 0; i < count; i++)
        {
            EventMilestone milestone = config.milestones[i];
            EventPointGiftItem gift = Instantiate(giftPrefab, giftsParent);
            gift.gameObject.SetActive(true);
            gift.Setup(milestone, points >= milestone.pointsNeeded, config, manager.database);

            // Keep the prefab height, place it at its even spot on the bar.
            Vector3 position = gift.transform.localPosition;
            position.x = Mathf.Lerp(left.x, right.x, GiftSpot(i, count));
            gift.transform.localPosition = position;
            gift.transform.localScale = gift.transform.localScale * scale;

            gifts.Add(gift);
        }
    }

    // Even spots along the bar: with 5 gifts -> 20%, 40%, 60%, 80%, 100%.
    private static float GiftSpot(int index, int count)
    {
        return (index + 1f) / count;
    }

    // Fill that reaches each gift's spot exactly when its points are reached.
    private static float GetFillAmount(EventConfig config, int points)
    {
        int count = config.milestones.Count;
        int previousPoints = 0;

        for (int i = 0; i < count; i++)
        {
            int needed = config.milestones[i].pointsNeeded;
            if (points < needed)
            {
                float startSpot = i == 0 ? 0f : GiftSpot(i - 1, count);
                float part = needed > previousPoints ? (float)(points - previousPoints) / (needed - previousPoints) : 1f;
                return Mathf.Lerp(startSpot, GiftSpot(i, count), part);
            }
            previousPoints = needed;
        }
        return 1f;
    }

    // Reached gifts are given one by one, so the player sees each animation.
    private IEnumerator GiveReachedGifts()
    {
        if (givingGifts)
            yield break;
        givingGifts = true;

        yield return new WaitForSecondsRealtime(giftGiveDelay);

        for (int i = 0; i < manager.ActiveEvent.milestones.Count; i++)
        {
            if (!IsOpen)
                break;
            if (manager.GetMilestoneState(i) != MilestoneState.Ready)
                continue;

            RectTransform from = i < gifts.Count ? gifts[i].GiftIconRect : null;
            manager.Claim(i, from);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

            yield return new WaitForSecondsRealtime(giftGiveGap);
        }

        givingGifts = false;
    }

    #endregion

    #region Tasks

    private void BuildTasks(EventConfig config)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        for (int i = 0; i < config.tasks.Count; i++)
        {
            EventTaskItem item = Instantiate(taskPrefab, content);
            item.Setup(i, config.tasks[i], manager.Progress.GetTaskValue(i),
                manager.Progress.IsTaskClaimed(i), manager.database, OnTaskClaimClicked);
        }
    }

    private void OnTaskClaimClicked(EventTaskItem item)
    {
        PlayClick();
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
        manager.ClaimTask(item.Index, item.RewardIconRect);
    }

    #endregion

    #region Rank

    private void RefreshRank()
    {
        EventUIText.Set(rankText, "#-");
        manager.LoadRank(rank =>
        {
            if (this != null)
                EventUIText.Set(rankText, rank > 0 ? "#" + rank.ToString("N0") : "#-");
        });
    }

    #endregion

    #region Buttons

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

        EventUIText.Set(infoTitleText, GameText.Get("event_how_title", "How to earn points"));
        EventUIText.Set(infoText,
            GameText.Format("event_how_order", "Serve a customer: +{0} {1}", config.pointsPerOrder, points) + "\n" +
            GameText.Format("event_how_win", "Win a level: +{0}", config.pointsPerWin) + "\n" +
            GameText.Format("event_how_noburn", "Win without burning food: +{0}", config.bonusNoBurn) + "\n" +
            GameText.Format("event_how_firsttry", "Win on the first try: +{0}", config.bonusFirstTry) + "\n" +
            GameText.Format("event_how_stars", "Win with 3 stars: +{0}", config.bonusThreeStars) + "\n" +
            GameText.Format("event_how_ad", "Watch an ad on the win screen: x2 level points ({0} per day)", config.maxAdDoublePerDay));

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

    #endregion
}
