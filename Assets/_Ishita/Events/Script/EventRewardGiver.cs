using MultiTechStudio.Cooking2D;
using UnityEngine;

// Gives event rewards using the same code as Daily Task (coins, diamonds, key, energy, boosters).
public static class EventRewardGiver
{
    public static void Give(EventReward reward)
    {
        int amount = Mathf.Max(1, reward.amount);

        switch (reward.type)
        {
            case EventRewardType.Coins:
                Extensions.AddCoins(amount);
                break;
            case EventRewardType.Diamonds:
                Extensions.AddDiamonds(amount);
                break;
            case EventRewardType.Key:
                Extensions.KeyAdder(amount);
                break;
            case EventRewardType.Energy:
                Extensions.EnergyAdder(amount);
                break;
            case EventRewardType.SuperCooker:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.SuperCooker, amount);
                break;
            case EventRewardType.DoubleProfit:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.DoubleProfit, amount);
                break;
            case EventRewardType.InstantCook:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.InstantCook, amount);
                break;
            case EventRewardType.HelpingHand:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.HelpingHand, amount);
                break;
            case EventRewardType.ComboExtender:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.ComboExtender, amount);
                break;
        }

        Debug.Log($"[Event] Reward given: {reward.type} x{amount}");
    }

    // Gives the reward with its animation:
    // - coins / diamonds: the game's own coin animation (the top bar comes in front of the popup)
    // - key / energy: the icon flies from the gift to its counter
    // - boosters: the icon flies from the gift to the home Play button
    public static void GiveWithAnimation(EventReward reward, Sprite icon, RectTransform from, float duration)
    {
        bool hasOwnAnimation = reward.type == EventRewardType.Coins || reward.type == EventRewardType.Diamonds;
        if (!hasOwnAnimation)
        {
            Transform target = GetFlyTarget(reward.type);
            if (target != null)
                RewardFly.Play(icon, from, target, duration);
        }

        Give(reward);
    }

    private static Transform GetFlyTarget(EventRewardType type)
    {
        if (UiManager.Instance == null || UiManager.Instance.allCanvas.persistent == null)
            return null;

        PersistentCanvas bar = UiManager.Instance.allCanvas.persistent;
        Transform playButton = bar.btn != null ? bar.btn.transform : null;

        switch (type)
        {
            case EventRewardType.Key: return bar.keyTxt != null ? bar.keyTxt.transform : playButton;
            case EventRewardType.Energy: return bar.energyTxt != null ? bar.energyTxt.transform : playButton;
            default: return playButton; // boosters
        }
    }

    // "x200" style text under a reward icon.
    public static string AmountText(EventReward reward)
    {
        return "x" + Mathf.Max(1, reward.amount).ToString("N0");
    }
}
