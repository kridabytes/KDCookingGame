using System.Collections.Generic;
using MultiTechStudio.Cooking2D;
using UnityEngine;

/// <summary>
/// All free gifts shown in the Free Gift panel. Add, remove or reorder gifts here;
/// the panel builds one row (freegift prefab) per entry at runtime.
/// </summary>
[CreateAssetMenu(fileName = "FreeGift", menuName = "FreeGift/Gift List")]
public class FreeGiftCollector : ScriptableObject
{
    public List<Gift> gifts = new List<Gift>();

    [System.Serializable]
    public class Gift
    {
        [Tooltip("Gift name, e.g. \"Coins\" or \"Combo Extender\".")]
        public string giftName;

        [Tooltip("Text shown on the row, e.g. \"Get 50 Coins\". Used only when Localized Text is empty.")]
        public string text;

        [Tooltip("Row text in every language (entry of a string table). Takes priority over Text.")]
        public UnityEngine.Localization.LocalizedString localizedText = new UnityEngine.Localization.LocalizedString();

        [Tooltip("Gift image shown on the row (also used for the booster fly animation).")]
        public Sprite icon;

        [Header("Reward")]
        public FreeGiftRewardType rewardType = FreeGiftRewardType.Coin;

        [Min(1)]
        public int amount = 50;

        [Tooltip("Only used when Reward Type is Booster.")]
        public BoosterType boosterType = BoosterType.ComboExtender;
    }
}

public enum FreeGiftRewardType
{
    Coin,
    Diamond,
    Booster
}
