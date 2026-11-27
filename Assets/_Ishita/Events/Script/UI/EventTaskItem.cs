using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// One event task card in the popup scroll view (prefab "eventTask").
public class EventTaskItem : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Graphic descriptionText;
    [SerializeField] private Image fillImage;            // Image Type = Filled
    [SerializeField] private Graphic progressText;       // optional "12/20"
    [SerializeField] private Image rewardIcon;
    [SerializeField] private Graphic rewardAmountText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Graphic claimText;

    public int Index { get; private set; }
    public RectTransform RewardIconRect => rewardIcon != null ? rewardIcon.rectTransform : (RectTransform)transform;

    public void Setup(int index, EventTask task, int value, bool claimed, EventDatabase database, Action<EventTaskItem> onClaim)
    {
        Index = index;
        bool completed = value >= task.target;

        if (icon != null && task.icon != null)
            icon.sprite = task.icon;
        EventUIText.Set(descriptionText, task.GetDescription());

        if (fillImage != null)
            fillImage.fillAmount = task.target > 0 ? Mathf.Clamp01((float)value / task.target) : 1f;
        EventUIText.Set(progressText, $"{Mathf.Min(value, task.target):N0}/{task.target:N0}");

        if (rewardIcon != null)
            rewardIcon.sprite = database.GetIcon(task.reward.type);
        EventUIText.Set(rewardAmountText, task.reward.amount.ToString("N0"));

        // The claim button is hidden in the prefab. It shows once the task is complete:
        // pressable until claimed, then dimmed with "Claimed".
        bool canClaim = completed && !claimed;
        claimButton.gameObject.SetActive(completed);
        claimButton.interactable = canClaim;
        CanvasGroup buttonGroup = claimButton.GetComponent<CanvasGroup>();
        if (buttonGroup == null)
            buttonGroup = claimButton.gameObject.AddComponent<CanvasGroup>();
        buttonGroup.alpha = canClaim ? 1f : 0.5f;
        EventUIText.Set(claimText, claimed
            ? GameText.Get("event_claimed", "Claimed")
            : GameText.Get("event_claim", "Claim"));

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(() => onClaim?.Invoke(this));

        claimButton.transform.DOKill();
        claimButton.transform.localScale = Vector3.one;
        if (claimButton.interactable)
        {
            claimButton.transform.DOScale(1.08f, 0.6f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (claimButton != null)
            claimButton.transform.DOKill();
    }
}
