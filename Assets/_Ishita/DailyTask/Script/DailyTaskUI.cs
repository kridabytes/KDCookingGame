using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MultiTechStudio.Cooking2D;
using System.Collections.Generic;
using DG.Tweening;


public class DailyTaskUI : MonoBehaviour
{
    public GameObject dailyTaskprefab;
    public Transform parentofDailyTask;
    public GameObject dailytaskpanel;
    public DailyTaskCollector taskCollector;
    [Header("Reward Fly Animation")]
    [SerializeField] private Image rewardFlyImage;
    [SerializeField] private Transform rewardFlyTarget;
    [SerializeField] private float rewardFlyDuration = 0.7f;


    public AudioSource buttonsound;

    public static DailyTaskUI instance;

    // Last fill value shown per task, so the bar animates up from where it was.
    private readonly Dictionary<DailyTaskCollector.TaskData, float> lastShownProgress =
        new Dictionary<DailyTaskCollector.TaskData, float>();

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        taskCollector.CheckDailyReset();
        DaliyTaskUI();
        dailytaskpanel.SetActive(false);
        if (rewardFlyImage != null)
            rewardFlyImage.gameObject.SetActive(false);
    }

    public void ClickDailyTask()
    {
        DaliyTaskUI();
        PopupAnimation.Show(dailytaskpanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);


    }

    public void CloseDailyTask()
    {
        PopupAnimation.Hide(dailytaskpanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }

    // Rebuild the list only when the panel is visible (e.g. progress changed while open).
    public void RefreshIfOpen()
    {
        if (dailytaskpanel != null && dailytaskpanel.activeInHierarchy)
            DaliyTaskUI();
    }

    public void DaliyTaskUI()
    {
        taskCollector.SyncProgressFromPlayerPrefs();
        foreach (Transform child in parentofDailyTask)
            Destroy(child.gameObject);

        List<DailyTaskCollector.TaskData> todayTasks =
            taskCollector.GetTodayTasks(4);
        Debug.Log("DailyTask Instantiate");
        for (int i = 0; i < todayTasks.Count; i++)
        {
            GameObject task = Instantiate(dailyTaskprefab, parentofDailyTask);
            var data = todayTasks[i];
            if (data.collected)
            {
                Destroy(task);
                continue;
            }

            Image[] image = task.GetComponentsInChildren<Image>();
            image[1].sprite = data.rewardIcon;
            RectTransform rewardImage = image[1].rectTransform;
            Text taskText = task.GetComponentInChildren<Text>();
            taskText.text = GetTaskTextWithReward(data);
            RewardText.FitText(taskText);
            Button btn = task.GetComponentInChildren<Button>();

            // Debug.Log($"Task {i} - Completed: {data.completed}, Collected: {data.collected}, Coins: {UiManager.TotalCoins}, Target: {data.targetValue}");
            bool completed = false;

            switch (data.taskType)
            {
                case DailyTaskType.EarnCoins:
                    completed = UiManager.TotalCoins >= data.targetValue;
                    break;
                default:
                    completed = data.currentValue >= data.targetValue;
                    break;
            }

            btn.interactable = completed && !data.collected;

            // Progress bar: currentProgress / targetValue
            float progress = data.GetProgress01();
            float previous;
            if (!lastShownProgress.TryGetValue(data, out previous))
                previous = progress;
            lastShownProgress[data] = progress;

            DailyTaskItemUI item = task.GetComponent<DailyTaskItemUI>();
            if (item != null)
                item.SetProgress(previous, progress);
            //btn.interactable = data.completed && !data.collected;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                CollectReward(data, rewardImage);
            });


        }
    }

    public void CollectReward(DailyTaskCollector.TaskData rewardData, RectTransform rewardImage = null)
    {
        buttonsound.Play();
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        if (!rewardData.completed || rewardData.collected)
            return;

        rewardData.collected = true;

        int amount = Mathf.Max(1, rewardData.rewardAmount);

        // Give the reward exactly once (it used to be given here AND again when the fly animation ended).
        GiveReward(rewardData, amount);
        Debug.Log($"Reward Collected: {rewardData.rewardType} x{amount}");

        // The reward picture flies from the row's reward image into the Play button.
        RewardFly.Play(rewardData.rewardIcon, rewardImage, rewardFlyTarget, rewardFlyDuration);

        DaliyTaskUI();
        taskCollector.CheckAllDailyTasksCompleted();
    }

    private static void GiveReward(DailyTaskCollector.TaskData rewardData, int amount)
    {
        switch (rewardData.rewardType)
        {
            case DailyTaskRewardType.ComboExtender:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.ComboExtender, amount);
                break;
            case DailyTaskRewardType.DoubleProfit:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.DoubleProfit, amount);
                break;
            case DailyTaskRewardType.HelpingHand:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.HelpingHand, amount);
                break;
            case DailyTaskRewardType.InstantCook:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.InstantCook, amount);
                break;
            case DailyTaskRewardType.SuperCooker:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.SuperCooker, amount);
                break;
            case DailyTaskRewardType.Diamond:
                Extensions.AddDiamonds(amount);
                break;
        }
    }

    /// <summary>"Earn 200 Coins and get Super Cooker booster x1".</summary>
    public static string GetTaskTextWithReward(DailyTaskCollector.TaskData data)
    {
        int amount = Mathf.Max(1, data.rewardAmount);
        switch (data.rewardType)
        {
            case DailyTaskRewardType.Diamond:
                return RewardText.WithReward(data.GetTaskText(), "Diamonds", false, amount);
            case DailyTaskRewardType.ComboExtender:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.ComboExtender), true, amount);
            case DailyTaskRewardType.DoubleProfit:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.DoubleProfit), true, amount);
            case DailyTaskRewardType.HelpingHand:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.HelpingHand), true, amount);
            case DailyTaskRewardType.InstantCook:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.InstantCook), true, amount);
            default:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.SuperCooker), true, amount);
        }
    }

}
