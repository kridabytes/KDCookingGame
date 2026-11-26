using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// One row of the reward track (prefab). Spawned by EventPopupUI for every milestone.
public class EventMilestoneItem : MonoBehaviour
{
    [Header("Row")]
    [SerializeField] private Image background;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private Text numberText;

    [Header("Rewards")]
    [SerializeField] private Image chestImage;              // only used by the Grand Chest row
    [SerializeField] private Transform rewardsParent;
    [SerializeField] private EventRewardItem rewardItemPrefab;

    [Header("Points needed")]
    [SerializeField] private Image pointIcon;
    [SerializeField] private Text pointsText;

    [Header("State")]
    [SerializeField] private Button claimButton;
    [SerializeField] private Text claimText;
    [SerializeField] private GameObject glow;
    [SerializeField] private GameObject claimedTick;
    [SerializeField] private GameObject lockedIcon;

    [Header("Look")]
    [SerializeField] private float normalHeight = 150f;
    [SerializeField] private float grandChestHeight = 230f;
    [SerializeField] private float lockedAlpha = 0.55f;
    [SerializeField] private float claimedAlpha = 0.7f;

    private readonly List<EventRewardItem> spawnedRewards = new List<EventRewardItem>();

    public int Index { get; private set; }

    // Where the claim fly animation starts.
    public RectTransform FirstRewardIcon
    {
        get
        {
            if (chestImage != null && chestImage.gameObject.activeSelf)
                return chestImage.rectTransform;
            return spawnedRewards.Count > 0 ? spawnedRewards[0].IconRect : (RectTransform)transform;
        }
    }

    public void Setup(int index, EventMilestone data, MilestoneState state, EventConfig config,
        EventDatabase database, Action<EventMilestoneItem> onClaim)
    {
        Index = index;

        if (numberText != null)
            numberText.text = (index + 1).ToString();

        // Grand Chest row is bigger and shows the chest picture.
        bool grand = data.isGrandChest;
        if (layoutElement != null)
            layoutElement.preferredHeight = grand ? grandChestHeight : normalHeight;
        if (chestImage != null)
        {
            chestImage.gameObject.SetActive(grand && config.grandChest != null);
            chestImage.sprite = config.grandChest;
        }

        BuildRewards(data, database);

        if (pointIcon != null)
            pointIcon.sprite = config.pointIcon;
        if (pointsText != null)
            pointsText.text = data.pointsNeeded.ToString("N0");

        if (claimText != null)
            claimText.text = GameText.Get("event_claim", "Claim");

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(() => onClaim?.Invoke(this));

        SetState(state);
    }

    private void BuildRewards(EventMilestone data, EventDatabase database)
    {
        foreach (Transform child in rewardsParent)
            Destroy(child.gameObject);
        spawnedRewards.Clear();

        foreach (EventReward reward in data.rewards)
        {
            EventRewardItem item = Instantiate(rewardItemPrefab, rewardsParent);
            item.Setup(database.GetIcon(reward.type), EventRewardGiver.AmountText(reward));
            spawnedRewards.Add(item);
        }
    }

    private void SetState(MilestoneState state)
    {
        claimButton.gameObject.SetActive(state == MilestoneState.Ready);
        if (glow != null) glow.SetActive(state == MilestoneState.Ready);
        if (claimedTick != null) claimedTick.SetActive(state == MilestoneState.Claimed);
        if (lockedIcon != null) lockedIcon.SetActive(state == MilestoneState.Locked);

        if (canvasGroup != null)
        {
            if (state == MilestoneState.Locked) canvasGroup.alpha = lockedAlpha;
            else if (state == MilestoneState.Claimed) canvasGroup.alpha = claimedAlpha;
            else canvasGroup.alpha = 1f;
        }

        // Ready rows pulse a little so the player sees them.
        Transform buttonTransform = claimButton.transform;
        buttonTransform.DOKill();
        buttonTransform.localScale = Vector3.one;
        if (state == MilestoneState.Ready)
        {
            buttonTransform.DOScale(1.08f, 0.6f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (claimButton != null)
            claimButton.transform.DOKill();
    }
}
