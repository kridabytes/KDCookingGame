using UnityEngine;
using UnityEngine.UI;

// One gift on top of the points fill bar (prefab). One is spawned for every milestone.
// Shows the points needed, the gift and how much of it, and a completed icon once reached.
public class EventPointGiftItem : MonoBehaviour
{
    [SerializeField] private Image icon;                // shown until the points are reached
    [SerializeField] private GameObject completedIcon;  // shown after the points are reached
    [SerializeField] private Graphic pointsText;        // "500"
    [SerializeField] private Image giftIcon;            // reward picture
    [SerializeField] private Graphic giftAmountText;    // "100"

    public RectTransform GiftIconRect => giftIcon != null ? giftIcon.rectTransform : (RectTransform)transform;

    public void Setup(EventMilestone milestone, bool reached, EventConfig config, EventDatabase database)
    {
        if (icon != null) icon.gameObject.SetActive(!reached);
        if (completedIcon != null) completedIcon.SetActive(reached);

        EventUIText.Set(pointsText, milestone.pointsNeeded.ToString("N0"));

        // Grand Chest shows the chest, other gifts show their first reward.
        EventReward first = milestone.rewards.Count > 0 ? milestone.rewards[0] : null;
        Sprite sprite = milestone.isGrandChest && config.grandChest != null
            ? config.grandChest
            : first != null ? database.GetIcon(first.type) : null;

        if (giftIcon != null)
        {
            giftIcon.sprite = sprite;
            giftIcon.enabled = sprite != null;
        }

        string amount = "";
        if (first != null && !milestone.isGrandChest)
            amount = first.amount.ToString("N0") + (milestone.rewards.Count > 1 ? "+" : "");
        EventUIText.Set(giftAmountText, amount);
    }
}
