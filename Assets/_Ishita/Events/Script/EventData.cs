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

public enum EventTaskType
{
    ServeOrders,      // serve N customers
    CompleteLevels,   // win N levels
    WinWithoutBurn,   // win N levels without burning food
    WinFirstTry,      // win N levels on the first try
    EarnCoins,        // earn N coins in levels
    UseBoosters,      // use N boosters
    EarnPoints        // collect N event points
}

[Serializable]
public class EventTask
{
    public EventTaskType taskType = EventTaskType.ServeOrders;
    public int target = 20;
    public Sprite icon;
    [Tooltip("{0} = target. Example: Serve {0} customers")]
    public string description = "Serve {0} customers";
    [Tooltip("Key in the ChooseLanguage table. Empty = use Description.")]
    public string descriptionKey;
    public EventReward reward = new EventReward();

    public string GetDescription()
    {
        string format = string.IsNullOrEmpty(descriptionKey) ? description : GameText.Get(descriptionKey, description);
        try { return string.Format(format, target); }
        catch (FormatException) { return format; }
    }
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
    public List<int> taskValues = new List<int>(); // progress of each event task (same index as config.tasks)
    public List<int> taskClaimed = new List<int>(); // task indexes already claimed

    public bool IsClaimed(int index)
    {
        return claimed.Contains(index);
    }

    public int GetTaskValue(int index)
    {
        return index < taskValues.Count ? taskValues[index] : 0;
    }

    public void SetTaskValue(int index, int value)
    {
        while (taskValues.Count <= index)
            taskValues.Add(0);
        taskValues[index] = value;
    }

    public bool IsTaskClaimed(int index)
    {
        return taskClaimed.Contains(index);
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

        for (int i = 0; i < other.taskValues.Count; i++)
            SetTaskValue(i, Mathf.Max(GetTaskValue(i), other.taskValues[i]));

        foreach (int index in other.taskClaimed)
        {
            if (!taskClaimed.Contains(index))
                taskClaimed.Add(index);
        }
    }
}
