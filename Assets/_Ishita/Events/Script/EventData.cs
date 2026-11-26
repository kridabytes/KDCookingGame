using System;
using System.Collections.Generic;
using UnityEngine;

// Small data classes shared by the event system.

public enum EventRewardType
{
    Coins,
    Diamonds,
    Key,
    Energy,
    SuperCooker,
    DoubleProfit,
    InstantCook,
    HelpingHand,
    ComboExtender
}

public enum EventState
{
    None,       // no event in the build / remote, or player level too low
    Upcoming,   // event known but not started yet
    Live,       // event running
    LastDay,    // event running, less than 24 hours left (timer turns red)
    Ended       // event finished
}

public enum MilestoneState
{
    Locked,
    Ready,
    Claimed
}

[Serializable]
public class EventReward
{
    public EventRewardType type = EventRewardType.Coins;
    public int amount = 100;
}

[Serializable]
public class EventMilestone
{
    public int pointsNeeded = 50;
    public bool isGrandChest;
    public List<EventReward> rewards = new List<EventReward>();
}

// Everything we save for one event. Saved on the phone (PlayerPrefs) and in Firebase.
[Serializable]
public class EventProgress
{
    public string eventId;
    public int points;
    public List<int> claimed = new List<int>();   // milestone indexes already claimed
    public bool introSeen;
    public bool endSeen;
    public int lastLevelPoints;                   // points of the last finished level (for the x2 ad)
    public bool lastLevelDoubled;
    public string adDay;                          // yyyyMMdd of the x2 ad counter
    public int adCount;

    public bool IsClaimed(int index)
    {
        return claimed.Contains(index);
    }

    // Used when the cloud copy and the phone copy are different: keep the best of both.
    public void MergeWith(EventProgress other)
    {
        if (other == null)
            return;

        points = Mathf.Max(points, other.points);
        introSeen = introSeen || other.introSeen;
        endSeen = endSeen || other.endSeen;

        foreach (int index in other.claimed)
        {
            if (!claimed.Contains(index))
                claimed.Add(index);
        }
    }
}
