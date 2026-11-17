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
    private Vector3 rewardFlyStartPosition;
    public AudioSource buttonsound;
    void Start()
    {
        achievementpanel.SetActive(false);
        rewardFlyStartPosition = rewardFlyImage.transform.position;
        rewardFlyImage.gameObject.SetActive(false);
    }

    public void ClickAchievement()
    {
        achievementpanel.SetActive(true);
        Achievementdata();
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }

    public void CloseAchievement()
    {
        achievementpanel.SetActive(false);
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
            task.GetComponentInChildren<Text>().text = data.GetTaskText();
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
                CollectReward(data);
            });

        }
    }

    public void CollectReward(AchievementsCollector.TaskData rewardData)
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
            case AchievementRewardType.ComboExtender:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.ComboExtender, amount);
                break;

            case AchievementRewardType.DoubleProfit:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.DoubleProfit, amount);
                break;

            case AchievementRewardType.HelpingHand:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.HelpingHand, amount);
                break;

            case AchievementRewardType.InstantCook:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.InstantCook, amount);
                break;

            case AchievementRewardType.SuperCooker:
                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.SuperCooker, amount);
                break;

            case AchievementRewardType.Diamond:
                UiManager.TotalDiamond += amount;
                PersistentCanvas.instance.diamondTxt.text =
                    UiManager.TotalDiamond.ToString();
                break;
        }

        Debug.Log($"Reward Collected: {rewardData.rewardType} x{amount}");

        Achievementdata();
    }
    private void PlayRewardFlyAnimation(
    AchievementsCollector.TaskData rewardData,
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
        AchievementsCollector.TaskData rewardData,
        int amount)
    {
        switch (rewardData.rewardType)
        {
            case AchievementRewardType.ComboExtender:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.ComboExtender,
                    amount);

                break;


            case AchievementRewardType.DoubleProfit:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.DoubleProfit,
                    amount);

                break;


            case AchievementRewardType.HelpingHand:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.HelpingHand,
                    amount);

                break;


            case AchievementRewardType.InstantCook:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.InstantCook,
                    amount);

                break;


            case AchievementRewardType.SuperCooker:

                BoosterManager.instance.UnlockGiveBooster(
                    BoosterType.SuperCooker,
                    amount);

                break;


            case AchievementRewardType.Diamond:

                UiManager.TotalDiamond += amount;

                PersistentCanvas.instance.diamondTxt.text =
                    UiManager.TotalDiamond.ToString();

                break;
        }

        Debug.Log(
            $"Reward Collected: {rewardData.rewardType} x{amount}");

        Achievementdata();
    }
    public void PlayRewardFlyAnimation(Sprite rewardIcon)
    {
        if (rewardFlyImage == null || rewardFlyTarget == null)
            return;

        // Stop previous animation
        rewardFlyImage.transform.DOKill();

        // Set reward image
        rewardFlyImage.sprite = rewardIcon;

        // ALWAYS start from original position
        rewardFlyImage.transform.position = rewardFlyStartPosition;

        // Reset scale to 1
        rewardFlyImage.transform.localScale = Vector3.one;

        // Show
        rewardFlyImage.gameObject.SetActive(true);

        // Get target position
        Vector3 targetPosition = rewardFlyTarget.position;

        // Fly animation (position)
        rewardFlyImage.transform
            .DOMove(targetPosition, rewardFlyDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                rewardFlyImage.gameObject.SetActive(false);
            });

        // Shrink animation (scale)
        rewardFlyImage.transform
            .DOScale(Vector3.one * 0.3f, rewardFlyDuration)
            .SetEase(Ease.InQuad);
    }
}
