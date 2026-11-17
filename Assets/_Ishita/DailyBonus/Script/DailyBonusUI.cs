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
        CheckMissedDayReset();
        if (CanShowDailyReward())
        {
            dailybonusPanel.SetActive(true);
            Dailybonusui();
        }
        else
        {
            dailybonusPanel.SetActive(false);
        }
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
            texts[1].text = "Day " + (i + 1);

            Button collectBtn = task.GetComponentInChildren<Button>();
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
        dailybonusPanel.SetActive(false);
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
