using MultiTechStudio.Cooking2D;
using System;
using System.Collections.Generic;
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

    [Tooltip("How many of EACH booster (Combo Extender, Double Profit, Helping Hand, Instant Cook, Super Cooker) this pack gives. Must match the x number shown on the pack.")]
    public int boosterAmount;
}
[Serializable]
public class NonConsumbleItem
{
    public string name;
    public string id;
    public string desc;
    public float price;
}
public class IAPScript : MonoBehaviour
{
    public ConsumbleItem[] items;
    public ConsumbleItem citem;
    public NonConsumbleItem ncitem;

    [Header("UI")]
    public GameObject iapPanel;
    public GameObject coinIAPPanel;
    public GameObject diamondIAPPanel;
    public GameObject watchAdPanel;
    public GameObject watchAdCoin;
    public GameObject WatchAdDaimond;
    public GameObject offerIAP;
    public static IAPScript Instance;

    [Header("Coins / Diamond tabs (shop popup)")]
    [Tooltip("'selectIcon' image of the Coins tab, shown while the Coins list is open.")]
    public GameObject coinTabSelect;
    public TMPro.TMP_Text coinTabText;
    [Tooltip("'selectIcon' image of the Diamond tab, shown while the Diamond list is open.")]
    public GameObject diamondTabSelect;
    public TMPro.TMP_Text diamondTabText;
    public Color selectedTabTextColor = Color.white;
    private Color coinTabNormalColor = Color.white;
    private Color diamondTabNormalColor = Color.white;
    private bool tabColorsSaved;
    [Header("Ice Cream Maker Upgrade Mapping")]
    [Tooltip("Index in 'items' array used for the FIRST ice cream maker upgrade (Level 2).")]
    public int iceCreamUpgradeItemLevel1Index = -1;
    [Tooltip("Index in 'items' array used for the SECOND ice cream maker upgrade (Level 3).")]
    public int iceCreamUpgradeItemLevel2Index = -1;

    // ---- Store state lives for the whole app run, NOT per scene ----
    // The scene is reloaded after every level. The old code started a brand-new store
    // initialisation on every reload, so for a few seconds after each reload a purchase
    // failed with "IAP still initializing", and sometimes the second initialisation never
    // finished. Now the store starts once and every IAPScript instance reuses it.
    private static IStoreController storeController;
    private static StoreListener listener;
    private static bool initializing;
    private static float lastInitAttempt = -100f;
    private const float RetryInitAfterSeconds = 5f;

    // A tap that happened while the store was still starting: bought as soon as it is ready.
    private static string queuedProductId;

    // Purchases the store delivered while no IAPScript existed (e.g. during a scene load).
    private static readonly List<Product> pendingProducts = new List<Product>();

    public static bool IsReady => storeController != null;

    private void Awake()
    {
        Instance = this;
        SaveTabColors();
    }

    private void SaveTabColors()
    {
        if (tabColorsSaved)
            return;
        if (coinTabText != null) coinTabNormalColor = coinTabText.color;
        if (diamondTabText != null) diamondTabNormalColor = diamondTabText.color;
        tabColorsSaved = true;
    }

    /// <summary>Selected tab: select image on and white text. The other tab goes back to normal.</summary>
    private void SetTabSelected(bool coinSelected)
    {
        SaveTabColors();
        if (coinTabSelect != null) coinTabSelect.SetActive(coinSelected);
        if (diamondTabSelect != null) diamondTabSelect.SetActive(!coinSelected);
        if (coinTabText != null) coinTabText.color = coinSelected ? selectedTabTextColor : coinTabNormalColor;
        if (diamondTabText != null) diamondTabText.color = coinSelected ? diamondTabNormalColor : selectedTabTextColor;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        // The offer is opened by HomePopupFlow (after login and tutorial, before Daily Bonus).
        offerIAP.SetActive(false);

        EnsureInitialized();

        if (IsReady)
        {
            CheckNonConsumble(ncitem != null ? ncitem.id : null);
            DeliverPendingPurchases();
        }
    }

    public bool IsOfferOpen => offerIAP != null && offerIAP.activeInHierarchy;

    /// <summary>
    /// Opens the limited time offer once per app open (not after every level).
    /// Returns false when it is not shown (already shown this session or already bought).
    /// </summary>
    public bool TryShowStartOffer(bool firstHomeOfSession)
    {
        if (!firstHomeOfSession || PlayerPrefs.GetInt("OfferRemoved", 0) == 1)
            return false;

        PopupAnimation.Show(offerIAP);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
        return true;
    }

    /// <summary>Starts the store once. Safe to call any number of times.</summary>
    private void EnsureInitialized()
    {
        if (IsReady || initializing)
            return;
        if (lastInitAttempt > 0f && Time.realtimeSinceStartup - lastInitAttempt < RetryInitAfterSeconds)
            return;

        initializing = true;
        lastInitAttempt = Time.realtimeSinceStartup;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        var added = new HashSet<string>();
        foreach (var item in items)
        {
            if (item != null && !string.IsNullOrEmpty(item.id) && added.Add(item.id))
                builder.AddProduct(item.id, ProductType.Consumable);
        }
        if (ncitem != null && !string.IsNullOrEmpty(ncitem.id) && added.Add(ncitem.id))
            builder.AddProduct(ncitem.id, ProductType.NonConsumable);

        if (listener == null)
            listener = new StoreListener();
        UnityPurchasing.Initialize(listener, builder);
    }

    public void Consumable_BTN_Pressed()
    {
        if (citem == null || string.IsNullOrEmpty(citem.id))
        {
            Debug.LogWarning("IAP: no item selected");
            return;
        }
        Buy(citem.id);
    }

    public void NonConsumable_BTN_Pressed()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
        if (ncitem != null && !string.IsNullOrEmpty(ncitem.id))
            Buy(ncitem.id);
    }

    /// <summary>
    /// Buys now if the store is ready; otherwise remembers the tap, (re)starts the store
    /// and opens the purchase automatically the moment it is ready.
    /// </summary>
    private void Buy(string productId)
    {
        if (IsReady)
        {
            Product product = storeController.products.WithID(productId);
            if (product == null || !product.availableToPurchase)
            {
                Debug.LogWarning("IAP: product not available in the store: " + productId);
                return;
            }
            storeController.InitiatePurchase(product);
            return;
        }

        queuedProductId = productId;
        Debug.Log("IAP: store is starting, the purchase will open when it is ready");
        lastInitAttempt = -100f; // a tap may always retry a failed start
        EnsureInitialized();
    }

    public void SelectItem(int index)
    {
        if (items != null && index >= 0 && index < items.Length)
            citem = items[index];
    }

    /// <summary>
    /// Gives the player what they bought. The item is found from the product id (not from
    /// the last selected button), so restored or delayed purchases also get the right reward.
    /// </summary>
    private void GrantPurchase(Product product)
    {
        string id = product.definition.id;
        Debug.Log("IAP: purchase complete " + id);

        if (ncitem != null && id == ncitem.id)
        {
            RemoveAds();
            return;
        }

        int selectedIndex = Array.FindIndex(items, x => x != null && x.id == id);
        if (selectedIndex < 0)
        {
            Debug.LogWarning("IAP: bought product is not in the items list: " + id);
            return;
        }
        ConsumbleItem bought = items[selectedIndex];

        PopupAnimation.Hide(iapPanel);

        if (bought.rewardCoins > 0)
            Extensions.AddCoins(bought.rewardCoins, DailyBonusUI.Instance != null ? DailyBonusUI.Instance.coinAnimation : null);

        if (bought.rewardDiamonds > 0)
            Extensions.AddDiamonds(bought.rewardDiamonds, DailyBonusUI.Instance != null ? DailyBonusUI.Instance.diamondAnimation : null);

        // Booster packs: every pack sets its own amount (of each booster) in the inspector.
        int boosterAmount = bought.boosterAmount;
        if (boosterAmount > 0 && BoosterManager.instance != null)
        {
            BoosterManager.instance.UnlockGiveBooster(BoosterType.ComboExtender, boosterAmount);
            BoosterManager.instance.UnlockGiveBooster(BoosterType.DoubleProfit, boosterAmount);
            BoosterManager.instance.UnlockGiveBooster(BoosterType.HelpingHand, boosterAmount);
            BoosterManager.instance.UnlockGiveBooster(BoosterType.InstantCook, boosterAmount);
            BoosterManager.instance.UnlockGiveBooster(BoosterType.SuperCooker, boosterAmount);
        }

        if (selectedIndex == 9)
        {
            PlayerPrefs.SetInt("OfferRemoved", 1);
            PlayerPrefs.Save();
            PopupAnimation.Hide(offerIAP);
        }

        // Ice Cream Maker upgrade purchases: each successful purchase advances one upgrade level.
        if ((selectedIndex == iceCreamUpgradeItemLevel1Index || selectedIndex == iceCreamUpgradeItemLevel2Index)
            && IceCreamManager.Instance != null)
        {
            IceCreamManager.Instance.ApplyPurchasedUpgrade();
        }

        if (GoogleLogin.instance != null)
            GoogleLogin.instance.SavePlayerDataToFirebase();
    }

    private void DeliverPendingPurchases()
    {
        if (!IsReady || pendingProducts.Count == 0)
            return;

        var toDeliver = new List<Product>(pendingProducts);
        pendingProducts.Clear();
        foreach (Product product in toDeliver)
        {
            GrantPurchase(product);
            storeController.ConfirmPendingPurchase(product);
        }
    }

    void CheckNonConsumble(string id)
    {
        if (storeController == null || string.IsNullOrEmpty(id))
            return;

        var product = storeController.products.WithID(id);
        if (product != null)
        {
            if (product.hasReceipt)
                RemoveAds();
            else
                ShowAds();
        }
    }

    public void RemoveAds()
    {
        GoogleMobileAd.AdsDisabled = true;
        if (GoogleMobileAd.Instatiate != null && GoogleMobileAd.Instatiate.watchadicon != null)
            GoogleMobileAd.Instatiate.watchadicon.SetActive(false);
        Debug.Log("ADS REMOVED");
    }

    public void ShowAds()
    {
        GoogleMobileAd.AdsDisabled = false;
        Debug.Log("ADS ENABLED");
    }

    /// <summary>
    /// The one store listener for the whole app. It is a plain object (not part of the scene),
    /// so it keeps working when the scene reloads, and passes everything to the current IAPScript.
    /// </summary>
    private class StoreListener : IStoreListener
    {
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            initializing = false;
            Debug.Log("IAP: initialized");

            IAPScript current = Instance;
            if (current == null)
                return;

            current.CheckNonConsumble(current.ncitem != null ? current.ncitem.id : null);
            current.DeliverPendingPurchases();

            if (!string.IsNullOrEmpty(queuedProductId))
            {
                string id = queuedProductId;
                queuedProductId = null;
                current.Buy(id);
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            initializing = false;
            queuedProductId = null;
            // The next tap on a shop button tries again (e.g. after the internet comes back).
            Debug.LogWarning("IAP: initialize failed " + error + " " + message);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            IAPScript current = Instance;
            if (current == null)
            {
                // No shop in the scene right now: keep it and deliver when the scene is back.
                pendingProducts.Add(args.purchasedProduct);
                return PurchaseProcessingResult.Pending;
            }

            current.GrantPurchase(args.purchasedProduct);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning("IAP: purchase failed " + (product != null ? product.definition.id : "") + " " + failureReason);
        }
    }

    //-----------------------------------------UI-----------------------------------------------------------------
    public void CloseWatchAD()
    {
        PopupAnimation.Hide(watchAdPanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    public void CloseIAPPanel()
    {
        PopupAnimation.Hide(iapPanel, () =>
        {
            WatchAdDaimond.SetActive(false);
            watchAdCoin.SetActive(false);
        });
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    // The shop popup has one Close button for both tabs, so the offer follows the open tab.
    public void CloseCoinIAPPanel()
    {
        CloseShop();
    }
    public void CloseDiamondIAPPanel()
    {
        CloseShop();
    }
    private void CloseShop()
    {
        bool coinTab = coinIAPPanel.activeSelf;
        PopupAnimation.Hide(iapPanel);
        watchAdCoin.SetActive(coinTab);
        WatchAdDaimond.SetActive(!coinTab);
        PopupAnimation.Show(watchAdPanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
    public void CoinIAPClick()
    {
        coinIAPPanel.SetActive(true);
        diamondIAPPanel.SetActive(false);
        SetTabSelected(true);
        PopupAnimation.Show(iapPanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        // Start the store early so it is ready by the time the player taps a pack.
        EnsureInitialized();
    }
    public void DiamondIAPClick()
    {
        diamondIAPPanel.SetActive(true);
        coinIAPPanel.SetActive(false);
        SetTabSelected(false);
        PopupAnimation.Show(iapPanel);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);

        EnsureInitialized();
    }

    public void OfferIAPClose()
    {
        PopupAnimation.Hide(offerIAP);
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
    }
}
