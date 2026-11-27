using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MultiTechStudio.Cooking2D;
using System;

public class DailyBonusUI : MonoBehaviour
{
    public GameObject coinPrefab;
    public GameObject diamondPrefab;
    public Transform parentofprefab;
    public DailyBonusCollector bonusCollector;
    public CoinAnimation coinAnimation;
    public CoinAnimation diamondAnimation;
    public GameObject dailybonusPanel;
    private const string DAILY_REWARD_DATE = "DAILY_REWARD_DATE";
    private const string CURRENT_DAY_CLIMEBTNINDEX = "CURRENT_DAY_CLIMEINDEX";

    public static DailyBonusUI Instance;
    public AudioSource buttonsound;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        // The panel is opened by HomePopupFlow (after the limited time offer).
        CheckMissedDayReset();
        dailybonusPanel.SetActive(false);
    }

    public bool IsOpen => dailybonusPanel != null && dailybonusPanel.activeInHierarchy;

    /// <summary>Opens the Daily Bonus if today's reward is not collected yet. Returns false if not shown.</summary>
    public bool TryShowDailyBonus()
    {
        if (!CanShowDailyReward())
            return false;

        PopupAnimation.Show(dailybonusPanel);
        Dailybonusui();
        return true;
    }
    void CheckMissedDayReset()
    {
        if (!PlayerPrefs.HasKey(DAILY_REWARD_DATE))
            return;

        DateTime lastDate = DateTime.ParseExact(
            PlayerPrefs.GetString(DAILY_REWARD_DATE),
            "yyyyMMdd",
            null
        );

        int daysDiff = (DateTime.UtcNow.Date - lastDate.Date).Days;

        if (daysDiff > 1)
        {
            PlayerPrefs.SetInt(CURRENT_DAY_CLIMEBTNINDEX, 0);
            PlayerPrefs.DeleteKey(DAILY_REWARD_DATE);
            PlayerPrefs.Save();
        }
    }
    public void Dailybonusui()
    {
        int lastClaimedDay = PlayerPrefs.GetInt(CURRENT_DAY_CLIMEBTNINDEX, 0);

        for (int i = 0; i < bonusCollector.days.Count; i++)
        {
            var dayData = bonusCollector.days[i];

            // ✅ Select prefab based on reward type
            GameObject prefabToUse = (dayData.rewardType == RewardType.Coin) ? coinPrefab : diamondPrefab;

            GameObject task = Instantiate(prefabToUse, parentofprefab);

            Image[] images = task.GetComponentsInChildren<Image>();
            images[2].sprite = dayData.rewardIcon;

            Text[] texts = task.GetComponentsInChildren<Text>();
            texts[0].text = dayData.rewardAmount;
            GameText.Set(texts[1], "ui_day_n", "Day {0}", i + 1);

            Button collectBtn = task.GetComponentInChildren<Button>();
            MakeWholeCardClickable(task, collectBtn);
            int index = i;

            collectBtn.onClick.AddListener(() => CollectReward(dayData, collectBtn, index));
            collectBtn.interactable = (i == lastClaimedDay);
            Transform tick = task.transform.Find("RightTick"); // name exact match hovu joie

            if (tick != null)
            {
                // show tick if already claimed
                tick.gameObject.SetActive(i < lastClaimedDay);
            }
        }
    }
    /// <summary>
    /// The collect Button sits on the card background, but the reward picture, amount and
    /// "Day N" text on top of it caught the taps, so only the bare background worked.
    /// Those pictures/texts now let taps through, so a tap anywhere on the card collects.
    /// </summary>
    private static void MakeWholeCardClickable(GameObject card, Button collectBtn)
    {
        if (collectBtn == null)
            return;

        Graphic buttonGraphic = collectBtn.targetGraphic != null ? collectBtn.targetGraphic : collectBtn.GetComponent<Graphic>();
        foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic != buttonGraphic)
                graphic.raycastTarget = false;
        }
        if (buttonGraphic != null)
            buttonGraphic.raycastTarget = true;
    }

    public void CollectReward(DailyBonusCollector.DayReward dayData, Button clickedButton, int dayIndex)
    {
        buttonsound.Play();
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
        Debug.Log("collectReword method call");
        int amount = int.Parse(dayData.rewardAmount);

        if (dayData.rewardType == RewardType.Coin)
        {
            Extensions.AddCoins(amount, coinAnimation);
            Debug.Log("Collect coins" + amount);
        }
        else if (dayData.rewardType == RewardType.Diamond)
        {
            Extensions.AddDiamonds(amount, diamondAnimation);
            Debug.Log("Collect Diamond" + amount);

        }

        PlayerPrefs.SetInt(CURRENT_DAY_CLIMEBTNINDEX, dayIndex + 1);
        PlayerPrefs.SetString(DAILY_REWARD_DATE, DateTime.UtcNow.ToString("yyyyMMdd"));
        PlayerPrefs.Save();

        clickedButton.interactable = false;
        PopupAnimation.Hide(dailybonusPanel);
    }

    bool CanShowDailyReward()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");

        if (!PlayerPrefs.HasKey(DAILY_REWARD_DATE))
            return true;

        string lastDate = PlayerPrefs.GetString(DAILY_REWARD_DATE);
        return today != lastDate;
    }

}
