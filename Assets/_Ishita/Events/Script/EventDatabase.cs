using System.Collections.Generic;
using UnityEngine;

// List of all events that are in this build + the icons used for each reward type.
// Firebase (node "liveEvent") decides which one is running. If Firebase can't be reached,
// "fallbackEventId" with the dates from its config is used.
[CreateAssetMenu(fileName = "EventDatabase", menuName = "Events/Event Database")]
public class EventDatabase : ScriptableObject
{
    public string fallbackEventId = "strawberry_2026_12";
    public List<EventConfig> events = new List<EventConfig>();

    [Header("Reward icons")]
    public Sprite coinIcon;
    public Sprite diamondIcon;
    public Sprite keyIcon;
    public Sprite energyIcon;
    public Sprite superCookerIcon;
    public Sprite doubleProfitIcon;
    public Sprite instantCookIcon;
    public Sprite helpingHandIcon;
    public Sprite comboExtenderIcon;

    public EventConfig Find(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
            return null;

        foreach (EventConfig config in events)
        {
            if (config != null && config.eventId == eventId)
                return config;
        }
        return null;
    }

    public Sprite GetIcon(EventRewardType type)
    {
        switch (type)
        {
            case EventRewardType.Coins: return coinIcon;
            case EventRewardType.Diamonds: return diamondIcon;
            case EventRewardType.Key: return keyIcon;
            case EventRewardType.Energy: return energyIcon;
            case EventRewardType.SuperCooker: return superCookerIcon;
            case EventRewardType.DoubleProfit: return doubleProfitIcon;
            case EventRewardType.InstantCook: return instantCookIcon;
            case EventRewardType.HelpingHand: return helpingHandIcon;
            case EventRewardType.ComboExtender: return comboExtenderIcon;
            default: return coinIcon;
        }
    }
}
