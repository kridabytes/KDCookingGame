using System.Collections.Generic;
using MultiTechStudio.Cooking2D;
using UnityEngine;

/// <summary>
/// Builds the Free Gift panel from a FreeGiftCollector: one freegift prefab per gift.
/// Watching the ad gives that gift's reward.
/// </summary>
public class FreeGiftUI : MonoBehaviour
{
    [SerializeField] private FreeGiftCollector giftCollector;
    [SerializeField] private FreeGiftItemUI giftPrefab;
    [Tooltip("Parent the gift rows are spawned under (e.g. an object with a Vertical Layout Group).")]
    [SerializeField] private Transform parentOfPrefab;

    [Header("Reward effects")]
    [Tooltip("Used for the booster fly animation. Coins/diamonds use DailyBonusUI's animations.")]
    [SerializeField] private AchievementUI achievementUI;
    [SerializeField] private AudioSource buttonSound;

    private readonly List<FreeGiftItemUI> spawned = new List<FreeGiftItemUI>();

    private void Start()
    {
        BuildGifts();
    }

    public void BuildGifts()
    {
        foreach (FreeGiftItemUI item in spawned)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        spawned.Clear();

        if (giftCollector == null || giftPrefab == null || parentOfPrefab == null)
        {
            Debug.LogWarning("FreeGiftUI: assign Gift Collector, Gift Prefab and Parent Of Prefab.", this);
            return;
        }

        foreach (FreeGiftCollector.Gift gift in giftCollector.gifts)
        {
            FreeGiftItemUI item = Instantiate(giftPrefab, parentOfPrefab);
            item.Setup(gift, OnWatchClicked);
            spawned.Add(item);
        }
    }

    private void OnWatchClicked(FreeGiftItemUI item)
    {
        if (buttonSound != null)
            buttonSound.Play();

        FreeGiftCollector.Gift gift = item.Gift;
        GoogleMobileAd.Instatiate.ShowFreeGiftRewardAd(() => GiveReward(gift));
    }

    private void GiveReward(FreeGiftCollector.Gift gift)
    {
        switch (gift.rewardType)
        {
            case FreeGiftRewardType.Coin:
                Extensions.AddCoins(gift.amount, DailyBonusUI.Instance != null ? DailyBonusUI.Instance.coinAnimation : null);
                break;

            case FreeGiftRewardType.Diamond:
                Extensions.AddDiamonds(gift.amount, DailyBonusUI.Instance != null ? DailyBonusUI.Instance.diamondAnimation : null);
                break;

            case FreeGiftRewardType.Booster:
                BoosterManager.instance.UnlockGiveBooster(gift.boosterType, gift.amount);
                if (achievementUI != null)
                    achievementUI.PlayRewardFlyAnimation(gift.icon);
                break;
        }

        Debug.Log($"Free gift collected: {gift.giftName} ({gift.rewardType} x{gift.amount})");
    }
}
