using MultiTechStudio.Cooking2D;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AchievementUI : MonoBehaviour
{
    public GameObject achievementpanel;
    public GameObject achievementprefab;
    public Transform parentofachievement;
    public AchievementsCollector achievementsCollector;
    [Header("Reward Fly Animation")]
    [SerializeField] private Image rewardFlyImage;
    [SerializeField] private Transform rewardFlyTarget;
    [SerializeField] private float rewardFlyDuration = 0.7f;
    public AudioSource buttonsound;
    void Start()
    {
        achievementpanel.SetActive(false);
        if (rewardFlyImage != null)
            rewardFlyImage.gameObject.SetActive(false);
    }

    public void ClickAchievement()
    {
        PopupAnimation.Show(achievementpanel);
        Achievementdata();
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }

    public void CloseAchievement()
    {
        PopupAnimation.Hide(achievementpanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }

    public void Achievementdata()
    {
        achievementsCollector.SyncProgressFromPlayerPrefs();
        foreach (Transform child in parentofachievement)
            Destroy(child.gameObject);

        for (int i = 0; i < achievementsCollector.Task.Count; i++)
        {
            GameObject task = Instantiate(achievementprefab, parentofachievement);
            var data = achievementsCollector.Task[i];
            if (data.collected)
            {
                Destroy(task);
                continue;
            }

            AchievementsCollector.instance.GetNextEarnCoinsTaskIndex();
            AchievementsCollector.instance.GetNextCompleteLevelTaskIndex();
            Image[] image = task.GetComponentsInChildren<Image>();
            image[1].sprite = data.rewardIcon;
            RectTransform rewardImage = image[1].rectTransform;
            Text taskText = task.GetComponentInChildren<Text>();
            taskText.text = GetTaskTextWithReward(data);
            RewardText.FitText(taskText);
            int current = data.GetCurrentProgress();
            int target = data.targetValue;

            current = Mathf.Clamp(current, 0, target);

            // 🎯 Find Fill Image
            Image fillImage = null;

            Image[] allImages = task.GetComponentsInChildren<Image>();

            foreach (var img in allImages)
            {
                if (img.gameObject.name == "FillImage") // 👈 exact naam mukjo prefab ma
                {
                    fillImage = img;
                    break;
                }
            }

            float progress = target > 0 ? (float)current / target : 0f;

            if (fillImage != null)
            {
                fillImage.fillAmount = progress;
            }
            else
            {
                Debug.LogError("FillImage NOT found!");
            }

            // 🎯 Progress Text
            Text progressTxt = null;

            Text[] allTexts = task.GetComponentsInChildren<Text>();

            foreach (var t in allTexts)
            {
                if (t.gameObject.name == "ProgressText")
                {
                    progressTxt = t;
                    break;
                }
            }

            if (progressTxt != null)
            {
                progressTxt.text = current + "/" + target;
            }
            else
            {
                Debug.LogError("ProgressText NOT found!");
            }
            // ⭐ Stars + Cap Logic
            GameObject star1 = null, star2 = null, star3 = null;
            GameObject grayCap = null, goldenCap = null;

            Transform[] allChildren = task.GetComponentsInChildren<Transform>(true);

            foreach (var t in allChildren)
            {
                if (t.name == "Star1") star1 = t.gameObject;
                else if (t.name == "Star2") star2 = t.gameObject;
                else if (t.name == "Star3") star3 = t.gameObject;
                else if (t.name == "GrayCap") grayCap = t.gameObject;
                else if (t.name == "GoldenCap") goldenCap = t.gameObject;
            }

            // ⭐ Reset
            if (star1) star1.SetActive(false);
            if (star2) star2.SetActive(false);
            if (star3) star3.SetActive(false);

            // ⭐ Activate based on progress
            if (progress > 0f)
            {
                if (star1) star1.SetActive(true);
            }
            if (progress >= 0.34f)
            {
                if (star2) star2.SetActive(true);
            }
            if (progress >= 0.67f)
            {
                if (star3) star3.SetActive(true);
            }

            // 🎯 Cap logic
            bool isCompleted = current >= target;

            if (grayCap) grayCap.SetActive(!isCompleted);
            if (goldenCap) goldenCap.SetActive(isCompleted);
            Button btn = task.GetComponentInChildren<Button>();

            bool completed = current >= target;

            btn.interactable = completed && !data.collected;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                CollectReward(data, rewardImage);
            });

        }
    }

    public void CollectReward(AchievementsCollector.TaskData rewardData, RectTransform rewardImage = null)
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
        RewardFly.Play(rewardData.rewardIcon, rewardImage, GetFlyTarget(), rewardFlyDuration);

        Achievementdata();
    }

    private Transform GetFlyTarget()
    {
        return rewardFlyTarget;
    }

    private static void GiveReward(AchievementsCollector.TaskData rewardData, int amount)
    {
        switch (rewardData.rewardType)
        {
            case AchievementRewardType.ComboExtender:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.ComboExtender, amount);
                break;
            case AchievementRewardType.DoubleProfit:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.DoubleProfit, amount);
                break;
            case AchievementRewardType.HelpingHand:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.HelpingHand, amount);
                break;
            case AchievementRewardType.InstantCook:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.InstantCook, amount);
                break;
            case AchievementRewardType.SuperCooker:
                BoosterManager.instance.UnlockGiveBooster(BoosterType.SuperCooker, amount);
                break;
            case AchievementRewardType.Diamond:
                Extensions.AddDiamonds(amount);
                break;
        }
    }

    /// <summary>"Earn 200 Coins and get Super Cooker booster x1".</summary>
    public static string GetTaskTextWithReward(AchievementsCollector.TaskData data)
    {
        int amount = Mathf.Max(1, data.rewardAmount);
        switch (data.rewardType)
        {
            case AchievementRewardType.Diamond:
                return RewardText.WithReward(data.GetTaskText(), "Diamonds", false, amount);
            case AchievementRewardType.ComboExtender:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.ComboExtender), true, amount);
            case AchievementRewardType.DoubleProfit:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.DoubleProfit), true, amount);
            case AchievementRewardType.HelpingHand:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.HelpingHand), true, amount);
            case AchievementRewardType.InstantCook:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.InstantCook), true, amount);
            default:
                return RewardText.WithReward(data.GetTaskText(), RewardText.BoosterName(BoosterType.SuperCooker), true, amount);
        }
    }

    /// <summary>Free gift and other callers: fly a reward picture to the Play button.</summary>
    public void PlayRewardFlyAnimation(Sprite rewardIcon, RectTransform from = null)
    {
        if (from == null && rewardFlyImage != null)
            from = rewardFlyImage.rectTransform;
        RewardFly.Play(rewardIcon, from, GetFlyTarget(), rewardFlyDuration);
    }
}
