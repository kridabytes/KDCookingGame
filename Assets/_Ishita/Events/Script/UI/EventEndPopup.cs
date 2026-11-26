using System.Collections.Generic;
using MultiTechStudio.Cooking2D;
using UnityEngine;
using UnityEngine.UI;

// Shown once after an event ends: final points, the unclaimed rewards that were given,
// and a teaser of the next event.
public class EventEndPopup : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Image pointIcon;
    [SerializeField] private Text pointsText;
    [SerializeField] private Text rewardsTitleText;
    [SerializeField] private Transform rewardsParent;
    [SerializeField] private EventRewardItem rewardItemPrefab;
    [SerializeField] private Text noRewardsText;
    [SerializeField] private GameObject teaserRoot;
    [SerializeField] private Image teaserIcon;
    [SerializeField] private Text teaserText;
    [SerializeField] private Button okButton;
    [SerializeField] private Text okText;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        okButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    public void Show(EventConfig finished, int finalPoints, List<EventReward> givenRewards, EventConfig nextEvent)
    {
        EventDatabase database = EventManager.Instance.database;

        titleText.text = GameText.Format("event_over", "{0} is over!", finished.GetDisplayName());
        pointIcon.sprite = finished.pointIcon;
        pointsText.text = GameText.Format("event_final_points", "Final points: {0}", finalPoints.ToString("N0"));
        okText.text = GameText.Get("event_collect", "Collect");

        // Rewards that were reached but not claimed (already given by EventManager).
        foreach (Transform child in rewardsParent)
            Destroy(child.gameObject);

        foreach (EventReward reward in MergeSameType(givenRewards))
        {
            EventRewardItem item = Instantiate(rewardItemPrefab, rewardsParent);
            item.Setup(database.GetIcon(reward.type), EventRewardGiver.AmountText(reward));
        }

        bool hasRewards = givenRewards.Count > 0;
        rewardsTitleText.text = GameText.Get("event_unclaimed_rewards", "Your unclaimed rewards");
        rewardsTitleText.gameObject.SetActive(hasRewards);
        noRewardsText.gameObject.SetActive(!hasRewards);
        noRewardsText.text = GameText.Get("event_all_claimed", "You collected all your rewards. Well done!");

        teaserRoot.SetActive(nextEvent != null);
        if (nextEvent != null)
        {
            teaserIcon.sprite = nextEvent.eventIcon;
            teaserText.text = GameText.Format("event_next", "Coming soon: {0}", nextEvent.GetDisplayName());
        }

        PopupAnimation.Show(panel);
    }

    // 200 coins + 1000 coins -> one "x1,200" coin item.
    private static List<EventReward> MergeSameType(List<EventReward> rewards)
    {
        List<EventReward> merged = new List<EventReward>();
        foreach (EventReward reward in rewards)
        {
            EventReward same = merged.Find(r => r.type == reward.type);
            if (same != null)
                same.amount += reward.amount;
            else
                merged.Add(new EventReward { type = reward.type, amount = reward.amount });
        }
        return merged;
    }

    private void Close()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
        PopupAnimation.Hide(panel);
    }
}
