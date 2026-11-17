using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="DailyBonus", menuName ="DailyBonus/Reward")]
public class DailyBonusCollector : ScriptableObject
{
    public List<DayReward> days = new List<DayReward>();

    [System.Serializable]
    public class DayReward
    {
        public Sprite rewardIcon;
        public string rewardAmount;
        public RewardType rewardType;
    }
}

public enum RewardType
{
    Coin,
    Diamond
}