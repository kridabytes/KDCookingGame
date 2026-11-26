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

    // Where the reward icon flies to after a claim (the matching counter in the top bar).
    public static Transform GetFlyTarget(EventRewardType type, Transform fallback)
    {
        if (UiManager.Instance == null || UiManager.Instance.allCanvas.persistent == null)
            return fallback;

        PersistentCanvas bar = UiManager.Instance.allCanvas.persistent;
        switch (type)
        {
            case EventRewardType.Coins: return bar.coinTxt != null ? bar.coinTxt.transform : fallback;
            case EventRewardType.Diamonds: return bar.diamondTxt != null ? bar.diamondTxt.transform : fallback;
            case EventRewardType.Key: return bar.keyTxt != null ? bar.keyTxt.transform : fallback;
            case EventRewardType.Energy: return bar.energyTxt != null ? bar.energyTxt.transform : fallback;
            default: return fallback;
        }
    }

    // "x200" style text under a reward icon.
    public static string AmountText(EventReward reward)
    {
        return "x" + Mathf.Max(1, reward.amount).ToString("N0");
    }
}
