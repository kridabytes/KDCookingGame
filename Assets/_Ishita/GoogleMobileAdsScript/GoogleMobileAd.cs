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

        // Reward callbacks touch UI and start animations, which is only safe on Unity's main thread.
        // Without this, Android calls them from a background thread.
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

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
        PopupAnimation.Hide(mobileadspanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);


    }
    public void OpenneMobileADPanel()
    {
        PopupAnimation.Show(mobileadspanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }

    // ------------------- REWARD AFTER THE AD IS CLOSED -------------------
    /// <summary>
    /// Shows a rewarded ad and gives the reward only after the player has closed the ad.
    /// The "reward earned" callback arrives while the ad still covers the screen (and the game is
    /// paused behind it), so the coin / diamond fly animation and the top bar used to play hidden
    /// behind the ad and were already over when the player came back. Now the reward waits for
    /// the close, plus a short moment for the game screen to come back.
    /// </summary>
    private void ShowRewarded(RewardedAd ad, System.Action onRewarded)
    {
        bool earned = false;
        bool closed = false;
        bool given = false;

        void TryGive()
        {
            if (given || !earned || !closed)
                return;
            given = true;
            StartCoroutine(GiveRewardAfterAd(onRewarded));
        }

        ad.OnAdFullScreenContentClosed += () =>
        {
            closed = true;
            if (!earned)
                ReloadAfterSkip(ad); // closed early: no reward, but load a fresh ad for next time
            TryGive();
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to show: " + error);
            closed = true;
            if (!earned)
                ReloadAfterSkip(ad);
            TryGive();
        };

        ad.Show((Reward reward) =>
        {
            earned = true;
            TryGive();
            // Safety: if the close event never arrives, still give the reward.
            StartCoroutine(GiveRewardFallback(() => { closed = true; TryGive(); }));
        });
    }

    // A shown ad cannot be shown again, so a skipped ad must be replaced.
    private void ReloadAfterSkip(RewardedAd ad)
    {
        if (ad == rewardedAdCoin) LoadCoinAd();
        else if (ad == rewardedAdDiamond) LoadDiamondAd();
        else if (ad == rewordAdComboExtender) LoadComboExtenderAd();
    }

    private System.Collections.IEnumerator GiveRewardAfterAd(System.Action onRewarded)
    {
        // Let Unity resume and the Home screen draw again before the fly animation starts.
        yield return new WaitForSecondsRealtime(0.3f);
        onRewarded?.Invoke();
    }

    private System.Collections.IEnumerator GiveRewardFallback(System.Action giveNow)
    {
        yield return new WaitForSecondsRealtime(30f);
        giveNow();
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

        ShowRewarded(rewardedAdCoin, () =>
        {
            PopupAnimation.Hide(mobileadspanel);
            Extensions.AddCoins(50, DailyBonusUI.Instance.coinAnimation);
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

        ShowRewarded(rewardedAdDiamond, () =>
        {
            PopupAnimation.Hide(mobileadspanel);
            Extensions.AddDiamonds(1, DailyBonusUI.Instance.diamondAnimation);
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


        ShowRewarded(rewardedAdCoin, () =>
        {
            Debug.Log("Add reward coins: 100");
            Extensions.AddCoins(100);
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


        ShowRewarded(rewardedAdCoin, () =>
        {
            Debug.Log("Add reward coins: 50");
            IAPScript.Instance.CloseIAPPanel();
            PopupAnimation.Hide(adpanel);

            Extensions.AddCoins(50, DailyBonusUI.Instance.coinAnimation);
            GoogleLogin.instance.SavePlayerDataToFirebase();


            LoadCoinAd();
        });
    }
    public void ShowDiamondRewardAd_IAP()
    {
        if (AdsDisabled) return;

        if (rewardedAdDiamond == null) { LoadDiamondAd(); return; }

        ShowRewarded(rewardedAdDiamond, () =>
        {
            IAPScript.Instance.CloseIAPPanel();
            PopupAnimation.Hide(adpanel);
            Extensions.AddDiamonds(1, DailyBonusUI.Instance.diamondAnimation);
            GoogleLogin.instance.SavePlayerDataToFirebase();

            LoadDiamondAd();
        });
    }

    //-----------------------------------Free Gifts-------------------------------------------------------------

    public void FreeGiftOpen()
    {
        PopupAnimation.Show(freegiftpanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    public void CloseFreeGift()
    {
        PopupAnimation.Hide(freegiftpanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    /// <summary>
    /// Used by FreeGiftUI: shows a rewarded ad, closes the free gift panel,
    /// then runs onRewarded (which gives that gift's reward).
    /// </summary>
    /// <summary>Event x2 points on the win screen. No-ads players get the reward directly.</summary>
    public void ShowEventRewardAd(System.Action onRewarded)
    {
        if (AdsDisabled)
        {
            onRewarded?.Invoke();
            return;
        }

        if (rewardedAdCoin == null)
        {
            LoadCoinAd();
            return;
        }

        ShowRewarded(rewardedAdCoin, () =>
        {
            onRewarded?.Invoke();
            LoadCoinAd();
        });
    }

    public void ShowFreeGiftRewardAd(System.Action onRewarded)
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        if (AdsDisabled) return;

        if (rewardedAdCoin == null)
        {
            LoadCoinAd();
            return;
        }

        ShowRewarded(rewardedAdCoin, () =>
        {
            PopupAnimation.Hide(freegiftpanel);
            onRewarded?.Invoke();
            GoogleLogin.instance.SavePlayerDataToFirebase();

            LoadCoinAd();
        });
    }
    public void ShowDiamondRewardAd_FreeGift()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        if (AdsDisabled) return;

        if (rewardedAdDiamond == null) { LoadDiamondAd(); return; }

        ShowRewarded(rewardedAdDiamond, () =>
        {
            PopupAnimation.Hide(freegiftpanel);
            Extensions.AddDiamonds(1, DailyBonusUI.Instance.diamondAnimation);
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


        ShowRewarded(rewardedAdCoin, () =>
        {
            PopupAnimation.Hide(freegiftpanel);

            Debug.Log("Add reward coins: 50");
            Extensions.AddCoins(50, DailyBonusUI.Instance.coinAnimation);
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
        ShowRewarded(rewordAdComboExtender, () =>
        {
            BoosterManager.instance.UnlockGiveBooster(BoosterType.ComboExtender, 1);
            PopupAnimation.Hide(freegiftpanel);
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
        ShowRewarded(rewordAdComboExtender, () =>
        {
            BoosterManager.instance.UnlockGiveBooster(BoosterType.HelpingHand, 1);
            PopupAnimation.Hide(freegiftpanel);
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
        ShowRewarded(rewordAdComboExtender, () =>
        {
            BoosterManager.instance.UnlockGiveBooster(BoosterType.DoubleProfit, 1);
            PopupAnimation.Hide(freegiftpanel);
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
