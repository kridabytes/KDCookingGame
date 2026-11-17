using MultiTechStudio.Cooking2D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Achievements", menuName = "Achievements")]
public class AchievementsCollector : ScriptableObject
{
    public List<TaskData> Task = new List<TaskData>();
    public static AchievementsCollector instance;

    private void OnEnable()
    {
        instance = this;
    }

    [System.Serializable]
    public class TaskData
    {
        public AchievementType taskType;
        public int targetValue;
        public Sprite rewardIcon;
        public AchievementRewardType rewardType;
        public int rewardAmount;
        public LocalizedString localizedTaskText;
        //  [HideInInspector] public int currentValue;
        public bool completed => IsCompleted();

        public int currentValue
        {
            get => PlayerPrefs.GetInt(GetProgressKey(), 0);
            set => PlayerPrefs.SetInt(GetProgressKey(), value);
        }

        public bool collected
        {
            get => PlayerPrefs.GetInt(GetCollectedKey(), 0) == 1;
            set => PlayerPrefs.SetInt(GetCollectedKey(), value ? 1 : 0);
        }
        private string GetProgressKey()
        {
            return $"Achievement_{taskType}_{targetValue}_Progress";
        }

        private string GetCollectedKey()
        {
            return $"Achievement_{taskType}_{targetValue}_Collected";
        }
        public string GetTaskText()
        {
            localizedTaskText.Arguments = new object[] { targetValue };
            return localizedTaskText.GetLocalizedString();
        }
        public int GetCurrentProgress()
        {
            switch (taskType)
            {
                case AchievementType.EarnCoins:
                    return UiManager.TotalCoins; // 🔥 live coins

                case AchievementType.CompleteLevel:
                    return PlayerPrefs.GetInt("AchievementsLevelCompleted", 0);

                case AchievementType.NoBurnLevel:
                    return currentValue;

                case AchievementType.OrderCompleted:
                    return currentValue;

                case AchievementType.UseBooster:
                    return currentValue;

                case AchievementType.completeDailytask:
                    return currentValue;

                default:
                    return currentValue;
            }
        }
        /* public string GetTaskText()
         {
             switch (taskType)
             {
                 case AchievementType.EarnCoins:
                     return $"Earn {targetValue} coins";

                 case AchievementType.CompleteLevel:
                     return $"Complete {targetValue} levels";

                 case AchievementType.NoBurnLevel:
                     return $"Complete {targetValue} levels without burning food";

                 case AchievementType.OrderCompleted:
                     return $"Complete {targetValue} orders";

                 case AchievementType.UseBooster:
                     return $"Use any booster {targetValue} times";

                 case AchievementType.completeDailytask:
                     return $"Complete daily tasks for {targetValue} days";

                 default:
                     return "Achievement";
             }
         }*/

        public bool IsCompleted()
        {
            switch (taskType)
            {
                case AchievementType.EarnCoins:
                    return UiManager.TotalCoins >= targetValue;
                case AchievementType.CompleteLevel:
                    return currentValue >= targetValue;
                case AchievementType.NoBurnLevel:
                    return currentValue >= targetValue;
                case AchievementType.OrderCompleted:
                    return currentValue >= targetValue;
                case AchievementType.UseBooster:
                    return currentValue >= targetValue;
                case AchievementType.completeDailytask:
                    return currentValue >= targetValue;
                default:
                    return false;
            }
        }

        public void OnBoosterUsed()
        {
            if (taskType == AchievementType.UseBooster && !collected)
            {
                currentValue++;
                if (currentValue > targetValue)
                    currentValue = targetValue;
            }
        }

    }
    public void OnOrderCompleted()
    {
        foreach (var task in Task)
        {
            if (task.collected)
                continue;

            if (task.taskType == AchievementType.OrderCompleted &&
                task.currentValue < task.targetValue)
            {
                task.currentValue++;
            }
        }
    }

    public void OnLevelCompleted()
    {
        int completedLevels = PlayerPrefs.GetInt("AchievementsLevelCompleted", 0) + 1;
        PlayerPrefs.SetInt("AchievementsLevelCompleted", completedLevels);

        foreach (var task in Task)
        {
            if (task.collected) continue;

            if (task.taskType == AchievementType.CompleteLevel)
            {
                if (task.currentValue < task.targetValue)
                    task.currentValue++;
            }

            if (task.taskType == AchievementType.NoBurnLevel)
            {
                if (!LevelController.instance.burnedThisLevel &&
                    task.currentValue < task.targetValue)
                {
                    task.currentValue++;
                }
            }


        }
    }
    public int GetNextCompleteLevelTaskIndex()
    {
        int maxCompleted = 0;
        foreach (var task in Task)
        {
            if (task.taskType == AchievementType.CompleteLevel && !task.collected)
            {
                int completedLevels = PlayerPrefs.GetInt("AchievementsLevelCompleted", 0);
                if (completedLevels >= task.targetValue)
                {
                    task.currentValue = task.targetValue;
                }
                maxCompleted = Mathf.Max(maxCompleted, task.currentValue);

                maxCompleted = Mathf.Max(maxCompleted, task.currentValue);
            }
        }

        int selectedIndex = -1;
        int minTarget = int.MaxValue;

        for (int i = 0; i < Task.Count; i++)
        {
            var task = Task[i];
            if (task.taskType != AchievementType.CompleteLevel)
                continue;

            if (task.collected)
                continue;

            if (task.targetValue > maxCompleted && task.targetValue < minTarget)
            {
                minTarget = task.targetValue;
                selectedIndex = i;
            }
        }

        return selectedIndex;
    }
    public void SyncProgressFromPlayerPrefs()
    {
        int completedLevels = PlayerPrefs.GetInt("AchievementsLevelCompleted", 0);

        foreach (var task in Task)
        {
            if (task.collected)
                continue;

            switch (task.taskType)
            {
                case AchievementType.CompleteLevel:
                    task.currentValue = Mathf.Min(completedLevels, task.targetValue);
                    break;

                case AchievementType.NoBurnLevel:
                    task.currentValue = Mathf.Min(task.currentValue, task.targetValue);
                    break;
                case AchievementType.completeDailytask:
                    task.currentValue = Mathf.Min(task.currentValue, task.targetValue);
                    break;

            }
        }
    }
    public int GetNextEarnCoinsTaskIndex()
    {
        int currentCoins = UiManager.TotalCoins;
        int selectedIndex = -1;
        int minTarget = int.MaxValue;

        for (int i = 0; i < Task.Count; i++)
        {
            var task = Task[i];

            if (task.taskType != AchievementType.EarnCoins)
                continue;

            if (task.collected)
                continue;

            if (task.targetValue > currentCoins && task.targetValue < minTarget)
            {
                minTarget = task.targetValue;
                selectedIndex = i;
            }
        }

        return selectedIndex;
    }

    public void OnDailyTasksCompleted()
    {
        foreach (var task in Task)
        {
            if (task.collected)
                continue;

            if (task.taskType == AchievementType.completeDailytask &&
                task.currentValue < task.targetValue)
            {
                task.currentValue++;
            }
        }
    }


}

public enum AchievementRewardType
{
    ComboExtender,
    DoubleProfit,
    HelpingHand,
    InstantCook,
    SuperCooker,
    Diamond
}

public enum AchievementType
{
    EarnCoins,
    CompleteLevel,
    NoBurnLevel,
    OrderCompleted,
    UseBooster,
    completeDailytask
}
