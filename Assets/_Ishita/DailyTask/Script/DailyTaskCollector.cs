using MultiTechStudio.Cooking2D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "DailyTask", menuName = "DailyTask")]
public class DailyTaskCollector : ScriptableObject
{
    public List<TaskData> Task = new List<TaskData>();
    public static DailyTaskCollector instance;
    private const string DAILY_TASK_INDEX_KEY = "DAILY_TASK_INDEXES";
    private const string DAILY_ALL_COMPLETED_KEY = "DAILY_ALL_COMPLETED_";

    private void OnEnable()
    {
        instance = this;
    }
    [System.Serializable]
   
    public class TaskData
    {
        public DailyTaskType taskType;
        public int targetValue;
        public Sprite rewardIcon;
        public DailyTaskRewardType rewardType;
        public int rewardAmount;
        public LocalizedString localizedTaskText;
        [HideInInspector] public int currentValue;

        public bool completed => IsCompleted();
        public bool collected
        {
            get => PlayerPrefs.GetInt(GetCollectedKey(), 0) == 1;
            set => PlayerPrefs.SetInt(GetCollectedKey(), value ? 1 : 0);
        }

        private string GetCollectedKey()
        {
            string today = System.DateTime.Now.ToString("yyyyMMdd");
            return $"DailyTask_{today}_{taskType}_{targetValue}_Collected";
        }

        public string GetTaskText()
        {
            localizedTaskText.Arguments = new object[] { targetValue };
            return localizedTaskText.GetLocalizedString();
        }
       /* public string GetTaskText()
        {
            switch (taskType)
            {
                case DailyTaskType.EarnCoins:
                    return $"Earn {targetValue} Coins";
                case DailyTaskType.CompleteLevel:
                    return $"Complete {targetValue} Levels";
                case DailyTaskType.NoBurnLevel:
                    return $"Complete {targetValue} Level Without Burning";
                case DailyTaskType.WinWithoutRetry:
                    return $"Win {targetValue} Level Without Retry";
                case DailyTaskType.UseBooster:
                    return $"Use any {targetValue} Booster";
                default:
                    return "Task";
            }
        }*/

        // Current progress value toward targetValue (EarnCoins reads live coins).
        public int GetCurrentProgress()
        {
            return taskType == DailyTaskType.EarnCoins ? UiManager.TotalCoins : currentValue;
        }

        // Progress as 0..1 for fill bars.
        public float GetProgress01()
        {
            if (targetValue <= 0)
                return 1f;
            return Mathf.Clamp01((float)GetCurrentProgress() / targetValue);
        }

        public bool IsCompleted()
        {
            switch (taskType)
            {
                case DailyTaskType.EarnCoins:
                    return UiManager.TotalCoins >= targetValue;
                case DailyTaskType.CompleteLevel:
                    return currentValue >= targetValue;
                case DailyTaskType.NoBurnLevel:
                    return currentValue >= targetValue;
                case DailyTaskType.WinWithoutRetry:
                    return currentValue >= targetValue;
                case DailyTaskType.UseBooster:
                    return currentValue >= targetValue;
                default:
                    return false;
            }
        }

        public void OnBoosterUsed()
        {
            if (taskType == DailyTaskType.UseBooster && !collected)
            {
                currentValue++;
                if (currentValue > targetValue)
                    currentValue = targetValue;
            }
        }
    }

    public void OnLevelCompleted()
    {
        int completedLevels = PlayerPrefs.GetInt("LevelCompletedToday", 0) + 1;
        PlayerPrefs.SetInt("LevelCompletedToday", completedLevels);

        foreach (var task in Task)
        {
            if (task.collected) continue;

            if (task.taskType == DailyTaskType.CompleteLevel)
            {
                if (task.currentValue < task.targetValue)
                    task.currentValue++;
            }

            if (task.taskType == DailyTaskType.NoBurnLevel)
            {
                if (!LevelController.instance.burnedThisLevel &&
                    task.currentValue < task.targetValue)
                {
                    task.currentValue++;
                }
            }

            if (task.taskType == DailyTaskType.WinWithoutRetry)
            {
                if (PlayerPrefs.GetInt("Tries", 0) == 0 &&
                    task.currentValue < task.targetValue)
                {
                    task.currentValue++;
                }
            }
        }

        if (DailyTaskUI.instance != null)
            DailyTaskUI.instance.RefreshIfOpen();
    }

    private void ResetDailyTasks()
    {
        foreach (var task in Task)
        {
            task.currentValue = 0;

            string key = $"DailyTaskCollected_{task.taskType}";
            PlayerPrefs.DeleteKey(key);
        }
    }

    private const string DAILY_TASK_DATE = "DAILY_TASK_DATE";

    public void CheckDailyReset()
    {
        string today = System.DateTime.Now.ToString("yyyyMMdd");
        string savedDay = PlayerPrefs.GetString(DAILY_TASK_DATE, "");

        if (savedDay != today)
        {
            ResetDailyTasks();
            PlayerPrefs.DeleteKey(DAILY_TASK_INDEX_KEY + savedDay);
            PlayerPrefs.SetString(DAILY_TASK_DATE, today);
        }

    }
    public List<TaskData> GetTodayTasks(int count)
    {
        List<int> indexes = new List<int>();

        string today = System.DateTime.Now.ToString("yyyyMMdd");
        string saved = PlayerPrefs.GetString(DAILY_TASK_INDEX_KEY + today, "");

        if (string.IsNullOrEmpty(saved))
        {
            System.Random rng = new System.Random(today.GetHashCode());

            List<int> pool = new List<int>();
            
            int earnCoinsIndex = GetNextEarnCoinsTaskIndex();
            if (earnCoinsIndex != -1)
            {
                pool.Add(earnCoinsIndex);
               
            }

            int completeLevelIndex = GetNextCompleteLevelTaskIndex();
            if (completeLevelIndex != -1)
            {
                pool.Add(completeLevelIndex);
                
            }

            for (int i = 0; i < Task.Count; i++)
            {
                var task = Task[i];

                if (task.taskType == DailyTaskType.EarnCoins ||
                    task.taskType == DailyTaskType.CompleteLevel)
                    continue;

                if (task.collected)
                    continue;

                pool.Add(i);
            }

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int r = rng.Next(pool.Count);
                int selectedIndex = pool[r];

                if (IsTaskTypeAlreadySelected(indexes, Task[selectedIndex].taskType))
                {
                    pool.RemoveAt(r);
                    i--; 
                    continue;
                }

                indexes.Add(selectedIndex);
                pool.RemoveAt(r);
            }

            PlayerPrefs.SetString(
                DAILY_TASK_INDEX_KEY + today,
                string.Join(",", indexes)
            );
        }
        else
        {
            string[] split = saved.Split(',');
            foreach (var s in split)
                indexes.Add(int.Parse(s));
        }

        List<TaskData> result = new List<TaskData>();
        foreach (int i in indexes)
            result.Add(Task[i]);

        return result;
    }

    private int GetNextEarnCoinsTaskIndex()
    {
        int currentCoins = UiManager.TotalCoins;
        int selectedIndex = -1;
        int minTarget = int.MaxValue;

        for (int i = 0; i < Task.Count; i++)
        {
            var task = Task[i];

            if (task.taskType != DailyTaskType.EarnCoins)
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

    private int GetNextCompleteLevelTaskIndex()
    {
        int maxCompleted = 0;
        foreach (var task in Task)
        {
            if (task.taskType == DailyTaskType.CompleteLevel && !task.collected)
            {
                int completedLevels = PlayerPrefs.GetInt("LevelCompletedToday", 0); 
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
            if (task.taskType != DailyTaskType.CompleteLevel)
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
    private bool IsTaskTypeAlreadySelected(List<int> selectedIndexes, DailyTaskType type)
    {
        foreach (int index in selectedIndexes)
        {
            if (Task[index].taskType == type)
                return true;
        }
        return false;
    }
    public void SyncProgressFromPlayerPrefs()
    {
        int completedLevels = PlayerPrefs.GetInt("LevelCompletedToday", 0);

        foreach (var task in Task)
        {
            if (task.collected)
                continue;

            switch (task.taskType)
            {
                case DailyTaskType.CompleteLevel:
                    task.currentValue = Mathf.Min(completedLevels, task.targetValue);
                    break;

                case DailyTaskType.NoBurnLevel:
                case DailyTaskType.WinWithoutRetry:
                    // keep currentValue (event-based)
                    break;
            }
        }
    }
    public void CheckAllDailyTasksCompleted()
    {
        string today = System.DateTime.Now.ToString("yyyyMMdd");

        // already counted today
        if (PlayerPrefs.GetInt(DAILY_ALL_COMPLETED_KEY + today, 0) == 1)
            return;

        List<TaskData> todayTasks = GetTodayTasks(4);

        foreach (var task in todayTasks)
        {
            if (!task.collected)
                return; // still pending tasks
        }

        PlayerPrefs.SetInt(DAILY_ALL_COMPLETED_KEY + today, 1);

        AchievementsCollector.instance.OnDailyTasksCompleted();
    }

}

public enum DailyTaskRewardType
{
    ComboExtender,
    DoubleProfit,
    HelpingHand,
    InstantCook,
    SuperCooker,
    Diamond
}

public enum DailyTaskType
{
    EarnCoins,
    CompleteLevel,
    NoBurnLevel,
    WinWithoutRetry,
    UseBooster
}