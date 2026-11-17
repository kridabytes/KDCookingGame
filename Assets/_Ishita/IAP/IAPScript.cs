using MultiTechStudio.Cooking2D;
using System;
using UnityEngine;
using UnityEngine.Purchasing;

[Serializable]
public class ConsumbleItem
{
    public string name;
    public string id;
    public string desc;
    public float price;

    public int rewardCoins;
    public int rewardDiamonds;
}
[Serializable]
public class NonConsumbleItem
{
    public string name;
    public string id;
    public string desc;
    public float price;
}
public class IAPScript : MonoBehaviour, IStoreListener
{
    public ConsumbleItem[] items;
    public ConsumbleItem citem;
    public NonConsumbleItem ncitem;
    IStoreController m_storeController;

    [Header("UI")]
    public GameObject iapPanel;
    public GameObject coinIAPPanel;
    public GameObject diamondIAPPanel;
    public GameObject watchAdPanel;
    public GameObject watchAdCoin;
    public GameObject WatchAdDaimond;
    public GameObject offerIAP;
    public static IAPScript Instance;
    bool isIAPReady = false;
    [Header("Ice Cream Maker Upgrade Mapping")]
    [Tooltip("Index in 'items' array used for the FIRST ice cream maker upgrade (Level 2).")]
    public int iceCreamUpgradeItemLevel1Index = -1;
    [Tooltip("Index in 'items' array used for the SECOND ice cream maker upgrade (Level 3).")]
    public int iceCreamUpgradeItemLevel2Index = -1;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // If Restart / Retry happened, keep offer hidden
        if (PlayerPrefs.GetInt("HideOfferOnRestart", 0) == 1)
        {
            offerIAP.SetActive(false);
        }
        else if (PlayerPrefs.GetInt("OfferRemoved", 0) == 1)
        {
            // Offer permanently removed after purchase
            offerIAP.SetActive(false);
        }
        else
        {
            offerIAP.SetActive(true);
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

        }

        SetupBuilder();
    }




    void SetupBuilder()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        // builder.AddProduct(citem.id, ProductType.Consumable);
        foreach (var item in items)
        {
            builder.AddProduct(item.id, ProductType.Consumable);
        }
        builder.AddProduct(ncitem.id, ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
    }
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("Success");
        m_storeController = controller;
        isIAPReady = true;
        CheckNonConsumble(ncitem.id);
      //  GoogleMobileAd.Instatiate.InitializeAds();

    }

    public void Consumable_BTN_Pressed()
    {
        if (!isIAPReady)
        {
            Debug.LogError("IAP still initializing... please wait");
            return;
        }

        if (citem == null)
        {
            Debug.LogError("Item not selected!");
            return;
        }
        Debug.Log("Store: " + (m_storeController == null ? "NULL" : "OK"));
        Debug.Log("Item: " + (citem == null ? "NULL" : citem.id));
        m_storeController.InitiatePurchase(citem.id);
    }

    public void NonConsumable_BTN_Pressed()
    {
        m_storeController.InitiatePurchase(ncitem.id);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);


    }
    public void SelectItem(int index)
    {
        citem = items[index];   // now valid
    }
    // processing purchase
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        //retrive the purchased product
        var product = args.purchasedProduct;

        Debug.Log("purches complete" + product.definition.id);

        if (product.definition.id == citem.id)
        {
            iapPanel.SetActive(false);

            if (citem.rewardCoins > 0)
            {
                UiManager.TotalCoins += citem.rewardCoins;
                UiManager.Instance.allCanvas.persistent.coinTxt.text = UiManager.TotalCoins.ToString();
                DailyBonusUI.Instance.coinAnimation.PlayAnim();
            }

            if (citem.rewardDiamonds > 0)
            {
                UiManager.TotalDiamond += citem.rewardDiamonds;
                PersistentCanvas.instance.diamondTxt.text = UiManager.TotalDiamond.ToString();
                DailyBonusUI.Instance.diamondAnimation.PlayAnim();
            }

            int selectedIndex = Array.IndexOf(items, citem);

            if (selectedIndex == 2)
            {
                BoosterManager.instance.UnlockGiveBooster(BoosterType.ComboExtender, 1);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.DoubleProfit, 1);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.HelpingHand, 1);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.InstantCook, 1);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.SuperCooker, 1);

                Debug.Log("Booster Pack unlocked for item index 2!");
            }
            if (selectedIndex == 3)
            {
                BoosterManager.instance.UnlockGiveBooster(BoosterType.ComboExtender, 2);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.DoubleProfit, 2);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.HelpingHand, 2);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.InstantCook, 2);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.SuperCooker, 2);

                Debug.Log("Booster Pack unlocked for item index 3!");
            }
            if (selectedIndex == 1)
            {
                BoosterManager.instance.UnlockGiveBooster(BoosterType.ComboExtender, 1);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.DoubleProfit, 1);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.HelpingHand, 1);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.InstantCook, 1);
                BoosterManager.instance.UnlockGiveBooster(BoosterType.SuperCooker, 1);

                Debug.Log("Booster Pack unlocked for item index 1!");
            }
            if (selectedIndex == 9)
            {
                PlayerPrefs.SetInt("OfferRemoved", 1);
                PlayerPrefs.Save();
            }

            // Ice Cream Maker upgrade purchases
            if (selectedIndex == iceCreamUpgradeItemLevel1Index ||
                selectedIndex == iceCreamUpgradeItemLevel2Index)
            {
                if (IceCreamManager.Instance != null)
                {
                    // Each successful purchase advances one upgrade level
                    IceCreamManager.Instance.ApplyPurchasedUpgrade();
                }
            }
            GoogleLogin.instance.SavePlayerDataToFirebase();
        }
        else if (product.definition.id == ncitem.id)
        {
            RemoveAds();
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("Initialize Failed" + error);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message = null)
    {
        Debug.Log("Initialize Failed" + error + message);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log("purchase Failed" + failureReason);
    }

    void CheckNonConsumble(string id)
    {
        if (m_storeController != null)
        {
            var product = m_storeController.products.WithID(id);
            if (product != null)
            {
                if (product.hasReceipt)
                {
                    RemoveAds();
                }
                else
                {
                    ShowAds();
                }
            }
        }
    }

    public void RemoveAds()
    {
        GoogleMobileAd.AdsDisabled = true;
        GoogleMobileAd.Instatiate.watchadicon.SetActive(false);
        Debug.Log("ADS REMOVED");
    }

    public void ShowAds()
    {
        GoogleMobileAd.AdsDisabled = false;
        Debug.Log("ADS ENABLED");
    }
    //-----------------------------------------UI-----------------------------------------------------------------
    public void CloseWatchAD()
    {
        watchAdPanel.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }
    public void CloseIAPPanel()
    {
        iapPanel.SetActive(false);
        WatchAdDaimond.SetActive(false);
        watchAdCoin.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }
    public void CloseCoinIAPPanel()
    {
        iapPanel.SetActive(false);
        watchAdPanel.SetActive(true);
        watchAdCoin.SetActive(true);
        WatchAdDaimond.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }
    public void CloseDiamondIAPPanel()
    {
        iapPanel.SetActive(false);
        watchAdPanel.SetActive(true);
        WatchAdDaimond.SetActive(true);
        watchAdCoin.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }
    public void CoinIAPClick()
    {
        iapPanel.SetActive(true);

        coinIAPPanel.SetActive(true);
        diamondIAPPanel.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    public void DiamondIAPClick()
    {
        iapPanel.SetActive(true);

        diamondIAPPanel.SetActive(true);
        coinIAPPanel.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }

    public void OfferIAPClose()
    {
        offerIAP.SetActive(false);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

    }


}
