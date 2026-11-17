using UnityEngine;
using GoogleMobileAds.Api;
using MultiTechStudio.Cooking2D;

public class GoogleMobileAd : MonoBehaviour
{
    private RewardedAd rewardedAdCoin;
    private RewardedAd rewardedAdDiamond;
    private RewardedAd rewordAdComboExtender;

    private InterstitialAd interstitialAd;

    public GameObject mobileadspanel;
    public static GoogleMobileAd Instatiate;
    public static bool AdsDisabled = false;
    private string rewardedID = "ca-app-pub-3940256099942544/5224354917";     // Test Rewarded
    private string interstitialID = "ca-app-pub-3940256099942544/1033173712"; // Test Interstitial

    public GameObject watchadicon;
    public GameObject freegiftpanel;
    public GameObject adpanel;
    [SerializeField] private Sprite comboExtenderRewardSprite;
    [SerializeField] private Sprite helpingHandRewardSprite;
    [SerializeField] private Sprite doubleProfitRewardSprite;
    [SerializeField] private AchievementUI achievementUI;
    private void Awake()
    {
        Instatiate = this;
    }
    /* void Start()
     {
         MobileAds.Initialize(_ => { });

     }*/
    private void Start()
    {
        if (AdsDisabled)
            return;

        Debug.Log("Initializing Google Mobile Ads...");

        MobileAds.Initialize((InitializationStatus status) =>
        {
            Debug.Log("Google Mobile Ads Initialized Successfully");

            LoadCoinAd();
            LoadDiamondAd();
            LoadComboExtenderAd();
            LoadInterstitial();
        });
    }
    /*public void InitializeAds()
    {
        if (AdsDisabled) return;
        Debug.Log("Google Mobile Ads Initialized");
        LoadCoinAd();
        LoadDiamondAd();
        LoadComboExtenderAd();
        LoadInterstitial();
    }*/
    public void CloseMobileADPanel()
    {
        mobileadspanel.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);


    }
    public void OpenneMobileADPanel()
    {
        mobileadspanel.SetActive(true);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }

    // ------------------- REWARDED COIN -------------------
    private void LoadCoinAd()
    {
        if (AdsDisabled)
            return;

        Debug.Log("Loading Coin Rewarded Ad...");

        RewardedAd.Load(
            rewardedID,
            new AdRequest(),
            (RewardedAd ad, LoadAdError err) =>
            {
                if (err != null)
                {
                    rewardedAdCoin = null;

                    Debug.LogError(
                        "COIN REWARDED LOAD FAILED\n" +
                        "Code: " + err.GetCode() + "\n" +
                        "Domain: " + err.GetDomain() + "\n" +
                        "Message: " + err.GetMessage() + "\n" +
                        "Full Error: " + err.ToString()
                    );

                    if (err.GetResponseInfo() != null)
                    {
                        Debug.LogError(
                            "Response Info: " +
                            err.GetResponseInfo().ToString()
                        );
                    }

                    return;
                }

                rewardedAdCoin = ad;

                Debug.Log("Coin Rewarded Ad Loaded Successfully");
            });
    }
    public void ShowCoinRewardAd()
    {
        if (AdsDisabled) return;
        if (rewardedAdCoin == null) { LoadCoinAd(); return; }

        rewardedAdCoin.Show((Reward reward) =>
        {
            UiManager.TotalCoins += 50;
            mobileadspanel.SetActive(false);

            UiManager.Instance.allCanvas.persistent.coinTxt.text = UiManager.TotalCoins.ToString();
            DailyBonusUI.Instance.coinAnimation.PlayAnim();
            GoogleLogin.instance.SavePlayerDataToFirebase();

            LoadCoinAd();
        });
    }

    // ------------------- REWARDED DIAMOND -------------------
    private void LoadDiamondAd()
    {
        if (AdsDisabled)
            return;

        Debug.Log("Loading Diamond Rewarded Ad...");

        RewardedAd.Load(
            rewardedID,
            new AdRequest(),
            (RewardedAd ad, LoadAdError err) =>
            {
                if (err != null)
                {
                    rewardedAdDiamond = null;

                    Debug.LogError(
                        "DIAMOND REWARDED LOAD FAILED\n" +
                        "Code: " + err.GetCode() + "\n" +
                        "Domain: " + err.GetDomain() + "\n" +
                        "Message: " + err.GetMessage() + "\n" +
                        "Full Error: " + err.ToString()
                    );

                    return;
                }

                rewardedAdDiamond = ad;

                Debug.Log("Diamond Rewarded Ad Loaded Successfully");
            });
    }
    private void LoadComboExtenderAd()
    {
        if (AdsDisabled)
            return;

        Debug.Log("Loading Combo Rewarded Ad...");

        RewardedAd.Load(
            rewardedID,
            new AdRequest(),
            (RewardedAd ad, LoadAdError err) =>
            {
                if (err != null)
                {
                    rewordAdComboExtender = null;

                    Debug.LogError(
                        "COMBO REWARDED LOAD FAILED\n" +
                        "Code: " + err.GetCode() + "\n" +
                        "Domain: " + err.GetDomain() + "\n" +
                        "Message: " + err.GetMessage() + "\n" +
                        "Full Error: " + err.ToString()
                    );

                    return;
                }

                rewordAdComboExtender = ad;

                Debug.Log("Combo Rewarded Ad Loaded Successfully");
            });
    }
    public void ShowDiamondRewardAd()
    {
        if (AdsDisabled) return;
        if (rewardedAdDiamond == null) { LoadDiamondAd(); return; }

        rewardedAdDiamond.Show((Reward reward) =>
        {
            UiManager.TotalDiamond += 1;
            mobileadspanel.SetActive(false);

            PersistentCanvas.instance.diamondTxt.text = UiManager.TotalDiamond.ToString();
            DailyBonusUI.Instance.diamondAnimation.PlayAnim();
            GoogleLogin.instance.SavePlayerDataToFirebase();

            LoadDiamondAd();
        });
    }
    public void ShowCoinRewardAd_100()
    {
        if (AdsDisabled) return;

        if (rewardedAdCoin == null)
        {
            LoadCoinAd();
            return;
        }


        rewardedAdCoin.Show((Reward reward) =>
        {
            UiManager.TotalCoins += 100;
            Debug.Log("Add reward coins: " + reward.Amount);

            UiManager.Instance.allCanvas.persistent.coinTxt.text = UiManager.TotalCoins.ToString();
            GoogleLogin.instance.SavePlayerDataToFirebase();


            LoadCoinAd();
        });
    }
    public void ShowCoinRewardAd_IAP()
    {
        if (AdsDisabled) return;

        if (rewardedAdCoin == null)
        {
            LoadCoinAd();
            return;
        }


        rewardedAdCoin.Show((Reward reward) =>
        {
            UiManager.TotalCoins += 50;
            Debug.Log("Add reward coins in IAP: " + reward.Amount);
            IAPScript.Instance.CloseIAPPanel();
            adpanel.SetActive(false);

            DailyBonusUI.Instance.coinAnimation.PlayAnim();
            UiManager.Instance.allCanvas.persistent.coinTxt.text = UiManager.TotalCoins.ToString();
            GoogleLogin.instance.SavePlayerDataToFirebase();


            LoadCoinAd();
        });
    }
    public void ShowDiamondRewardAd_IAP()
    {
        if (AdsDisabled) return;

        if (rewardedAdDiamond == null) { LoadDiamondAd(); return; }

        rewardedAdDiamond.Show((Reward reward) =>
        {
            UiManager.TotalDiamond += 1;
            IAPScript.Instance.CloseIAPPanel();
            adpanel.SetActive(false);
            PersistentCanvas.instance.diamondTxt.text = UiManager.TotalDiamond.ToString();
            DailyBonusUI.Instance.diamondAnimation.PlayAnim();
            GoogleLogin.instance.SavePlayerDataToFirebase();

            LoadDiamondAd();
        });
    }

    //-----------------------------------Free Gifts-------------------------------------------------------------

    public void FreeGiftOpen()
    {
        freegiftpanel.SetActive(true);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    public void CloseFreeGift()
    {
        freegiftpanel.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    public void ShowDiamondRewardAd_FreeGift()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        if (AdsDisabled) return;

        if (rewardedAdDiamond == null) { LoadDiamondAd(); return; }

        rewardedAdDiamond.Show((Reward reward) =>
        {
            UiManager.TotalDiamond += 1;
            freegiftpanel.SetActive(false);

            PersistentCanvas.instance.diamondTxt.text = UiManager.TotalDiamond.ToString();
            DailyBonusUI.Instance.diamondAnimation.PlayAnim();
            GoogleLogin.instance.SavePlayerDataToFirebase();

            LoadDiamondAd();
        });
    }
    public void ShowCoinRewardAd_FreeGift()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        if (AdsDisabled) return;

        if (rewardedAdCoin == null)
        {
            LoadCoinAd();
            return;
        }


        rewardedAdCoin.Show((Reward reward) =>
        {
            UiManager.TotalCoins += 50;
            freegiftpanel.SetActive(false);

            Debug.Log("Add reward coins in IAP: " + reward.Amount);
            DailyBonusUI.Instance.coinAnimation.PlayAnim();
            UiManager.Instance.allCanvas.persistent.coinTxt.text = UiManager.TotalCoins.ToString();
            GoogleLogin.instance.SavePlayerDataToFirebase();


            LoadCoinAd();
        });
    }
    public void ShowComboExtenderRewardAD()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        if (AdsDisabled) return;

        if (rewordAdComboExtender == null)
        {
            LoadComboExtenderAd();
            return;
        }
        rewordAdComboExtender.Show((Reward reward) =>
        {
            BoosterManager.instance.UnlockGiveBooster(BoosterType.ComboExtender, 1);
            freegiftpanel.SetActive(false);
            achievementUI.PlayRewardFlyAnimation(comboExtenderRewardSprite);
            GoogleLogin.instance.SavePlayerDataToFirebase();

            LoadComboExtenderAd();
        });
    }
    public void ShowHalpingHandRewardAD()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        if (AdsDisabled) return;

        if (rewordAdComboExtender == null)
        {
            LoadComboExtenderAd();
            return;
        }
        rewordAdComboExtender.Show((Reward reward) =>
        {
            BoosterManager.instance.UnlockGiveBooster(BoosterType.HelpingHand, 1);
            freegiftpanel.SetActive(false);
            achievementUI.PlayRewardFlyAnimation(helpingHandRewardSprite);
            GoogleLogin.instance.SavePlayerDataToFirebase();

            LoadComboExtenderAd();
        });
    }
    public void ShowDubbleProfitRewardAD()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        if (AdsDisabled) return;

        if (rewordAdComboExtender == null)
        {
            LoadComboExtenderAd();
            return;
        }
        rewordAdComboExtender.Show((Reward reward) =>
        {
            BoosterManager.instance.UnlockGiveBooster(BoosterType.DoubleProfit, 1);
            freegiftpanel.SetActive(false);
            achievementUI.PlayRewardFlyAnimation(doubleProfitRewardSprite);
            GoogleLogin.instance.SavePlayerDataToFirebase();

            LoadComboExtenderAd();
        });
    }
    // ------------------- INTERSTITIAL -------------------
    private void LoadInterstitial()
    {
        if (AdsDisabled)
            return;

        Debug.Log("Loading Interstitial Ad...");

        InterstitialAd.Load(
            interstitialID,
            new AdRequest(),
            (InterstitialAd ad, LoadAdError err) =>
            {
                if (err != null)
                {
                    interstitialAd = null;

                    Debug.LogError(
                        "INTERSTITIAL LOAD FAILED\n" +
                        "Code: " + err.GetCode() + "\n" +
                        "Domain: " + err.GetDomain() + "\n" +
                        "Message: " + err.GetMessage() + "\n" +
                        "Full Error: " + err.ToString()
                    );

                    return;
                }

                interstitialAd = ad;

                Debug.Log("Interstitial Ad Loaded Successfully");
            });
    }

    public void ShowInterstitial()
    {
        if (AdsDisabled) return;
        if (interstitialAd == null)
        {
            LoadInterstitial();
            return;
        }

        interstitialAd.Show();
        LoadInterstitial();
    }

}
