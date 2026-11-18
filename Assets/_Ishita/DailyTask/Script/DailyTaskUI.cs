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
    private Vector3 rewardFlyStartPosition;


    public AudioSource buttonsound;

    public static DailyTaskUI instance;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        taskCollector.CheckDailyReset();
        DaliyTaskUI();
        dailytaskpanel.SetActive(false);
        rewardFlyStartPosition = rewardFlyImage.transform.position;
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
            task.GetComponentInChildren<Text>().text = data.GetTaskText();
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
            //btn.interactable = data.completed && !data.collected;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                CollectReward(data);
            });


        }
    }

    public void CollectReward(DailyTaskCollector.TaskData rewardData)
    {
        buttonsound.Play();
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        if (!rewardData.completed || rewardData.collected)
            return;

        rewardData.collected = true;

        int amount = Mathf.Max(1, rewardData.rewardAmount);
        PlayRewardFlyAnimation(rewardData, amount);

        switch (rewardData.rewardType)
        {
            case DailyTaskRewardType.ComboExtender:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.ComboExtender, amount);
                break;

            case DailyTaskRewardType.DoubleProfit:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.DoubleProfit, amount);
                break;

            case DailyTaskRewardType.HelpingHand:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.HelpingHand, amount);
                break;

            case DailyTaskRewardType.InstantCook:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.InstantCook, amount);
                break;

            case DailyTaskRewardType.SuperCooker:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.SuperCooker, amount);
                break;

            case DailyTaskRewardType.Diamond:
                Extensions.AddDiamonds(amount);
                break;
        }

        Debug.Log($"Reward Collected: {rewardData.rewardType} x{amount}");

        DaliyTaskUI();
        taskCollector.CheckAllDailyTasksCompleted();
    }
    private void PlayRewardFlyAnimation(
  DailyTaskCollector.TaskData rewardData,
  int amount)
    {
        if (rewardFlyImage == null || rewardFlyTarget == null)
        {
            GiveReward(rewardData, amount);
            return;
        }

        // Kill any previous tween
        rewardFlyImage.transform.DOKill();

        // Set icon
        rewardFlyImage.sprite = rewardData.rewardIcon;

        // ALWAYS reset to original start position
        rewardFlyImage.transform.position = rewardFlyStartPosition;

        // Reset scale to 1 (start full size)
        rewardFlyImage.transform.localScale = Vector3.one;

        // Show
        rewardFlyImage.gameObject.SetActive(true);

        // Target position
        Vector3 targetPosition = rewardFlyTarget.position;

        // Fly (position)
        rewardFlyImage.transform
            .DOMove(targetPosition, rewardFlyDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                rewardFlyImage.gameObject.SetActive(false);

                GiveReward(rewardData, amount);
            });

        // Shrink (scale) - same duration, target par small thay jashe
        rewardFlyImage.transform
            .DOScale(Vector3.one * 0.3f, rewardFlyDuration)
            .SetEase(Ease.InQuad);
    }
    private void GiveReward(
        DailyTaskCollector.TaskData rewardData,
        int amount)
    {
        switch (rewardData.rewardType)
        {
            case DailyTaskRewardType.ComboExtender:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.ComboExtender,
                    amount);

                break;

            case DailyTaskRewardType.DoubleProfit:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.DoubleProfit,
                    amount);

                break;

            case DailyTaskRewardType.HelpingHand:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.HelpingHand,
                    amount);

                break;

            case DailyTaskRewardType.InstantCook:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.InstantCook,
                    amount);

                break;

            case DailyTaskRewardType.SuperCooker:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.SuperCooker,
                    amount);

                break;

            case DailyTaskRewardType.Diamond:

                Extensions.AddDiamonds(amount);

                break;
        }

        Debug.Log(
            $"Daily Reward Collected: {rewardData.rewardType} x{amount}");

        DaliyTaskUI();

        taskCollector.CheckAllDailyTasksCompleted();
    }

}
