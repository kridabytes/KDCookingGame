using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Popup UI for the ice cream machine.
    /// Shows machine storage, timer, and upgrade preview.
    /// </summary>
    public class IceCreamPopupController : MonoBehaviour
    {
        [Header("Top - Total Stored Ice Cream")]
        [SerializeField] private Text storedCountText;

        [Header("Left - Machine")]
        [SerializeField] private Image machineImage;
        [SerializeField] private Text timerText;
        [SerializeField] private Button claimButton;

        [Header("Right - Upgrade Preview - Claim Amount")]
        [SerializeField] private Text claimCurrentText;
        [SerializeField] private Text claimNextText;

        [Header("Right - Upgrade Preview - Storage")]
        [SerializeField] private Text storageCurrentText;
        [SerializeField] private Text storageNextText;

        [Header("Right - Upgrade Preview - Waiting Time")]
        [SerializeField] private Text waitingCurrentText;
        [SerializeField] private Text waitingNextText;

        [SerializeField] private Button upgradeButton;
        [SerializeField] private Text upgradeCostText;

        [Header("Currency")]
        // [SerializeField] private Text playerCoinsText;
        [SerializeField] private Text upgradePriceText;

        [Header("IAP Integration")]
        [SerializeField] private IAPScript iapScript;
        [Tooltip("Index in IAPScript.items for the first ice cream upgrade (Level 2).")]
        [SerializeField] private int level1ItemIndex = -1;
        [Tooltip("Index in IAPScript.items for the second ice cream upgrade (Level 3).")]
        [SerializeField] private int level2ItemIndex = -1;

        private bool subscribed;
        private Coroutine bindRoutine;

        private void Awake()
        {
            if (claimButton != null)
                claimButton.onClick.AddListener(OnClaimClicked);

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            // First-frame defaults before manager binds.
            if (claimButton != null)
                claimButton.interactable = false;
            if (storedCountText != null)
                storedCountText.text = "0/0";
        }

        private void OnEnable()
        {
            bindRoutine = StartCoroutine(BindWhenReady());
        }

        private void OnDisable()
        {
            if (bindRoutine != null)
            {
                StopCoroutine(bindRoutine);
                bindRoutine = null;
            }

            if (subscribed && IceCreamManager.Instance != null)
                IceCreamManager.Instance.OnStateChanged -= Refresh;

            subscribed = false;
        }

        private void Update()
        {
            // Smooth countdown update even if state event didn't fire
            RefreshTimerOnly();
        }

        private IEnumerator BindWhenReady()
        {
            while (IceCreamManager.Instance == null)
                yield return null;

            if (!subscribed)
            {
                IceCreamManager.Instance.OnStateChanged += Refresh;
                subscribed = true;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (IceCreamManager.Instance == null)
                return;

            // This text should match the gameplay HUD ice cream text exactly:
            storedCountText.text = IceCreamManager.Instance.TotalStored.ToString();

            // Right side upgrade preview
            int currentAmount = IceCreamManager.Instance.CurrentProductionAmount;
            int nextAmount = IceCreamManager.Instance.NextProductionAmount;

            int currentCap = IceCreamManager.Instance.CurrentCapacity;
            int nextCap = IceCreamManager.Instance.NextCapacity;

            int currentTime = IceCreamManager.Instance.CurrentProductionTimeMinutes;
            int nextTime = IceCreamManager.Instance.NextProductionTimeMinutes;

            // Claim amount texts
            if (claimCurrentText != null)
                claimCurrentText.text = currentAmount.ToString();
            if (claimNextText != null)
                claimNextText.text = nextAmount.ToString();

            // Storage texts
            if (storageCurrentText != null)
                storageCurrentText.text = currentCap.ToString();
            if (storageNextText != null)
                storageNextText.text = nextCap.ToString();

            // Waiting time texts
            if (waitingCurrentText != null)
                GameText.Set(waitingCurrentText, "ui_minutes_n", "{0} min", currentTime);
            if (waitingNextText != null)
                GameText.Set(waitingNextText, "ui_minutes_n", "{0} min", nextTime);

            int nextPrice = IceCreamManager.Instance.NextUpgradePrice;
            upgradeCostText.text = nextPrice > 0 ? nextPrice.ToString() : "2000";

            if (upgradePriceText != null)
                upgradePriceText.text = nextPrice > 0 ? nextPrice.ToString() : "5000";

            /*// Optional: show player coins/currency from your own system here
            if (playerCoinsText != null)
                playerCoinsText.text = string.Empty;
*/
            // Claim button rules: enabled only when a batch is ready to claim
            if (claimButton != null)
                claimButton.interactable = IceCreamManager.Instance.IsClaimReady;

            RefreshTimerOnly();
        }

        private void RefreshTimerOnly()
        {
            if (IceCreamManager.Instance == null || timerText == null)
                return;

            TimeSpan remaining = IceCreamManager.Instance.GetTimeRemainingToNextBatch();
            if (IceCreamManager.Instance.MachineStored >= IceCreamManager.Instance.MachineCapacity)
            {
                GameText.Set(timerText, "ui_full", "Full");
            }
            else
            {
                timerText.text = $"{remaining.Minutes:00}:{remaining.Seconds:00}";
            }

            // Keep claim button state correct even if only timer refreshes
            if (claimButton != null)
                claimButton.interactable = IceCreamManager.Instance.IsClaimReady;
        }

        private void OnClaimClicked()
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

            if (IceCreamManager.Instance == null)
                return;

            int claimed = IceCreamManager.Instance.ClaimAll();
            if (claimed > 0)
            {
                // Machine storage cleared; HUD will see PlayerOwned increase via event
            }
        }

        private void OnUpgradeClicked()
        {
            AudioManager.Instance.MakeVibrate(VibrationType.Low);
            if (IceCreamManager.Instance == null)
                return;

            // Try to use assigned IAPScript, otherwise fall back to singleton instance.
            if (iapScript == null)
                iapScript = IAPScript.Instance;
            if (iapScript == null)
                return;

            // Target level is current + 1 (0-based levels: 0->1, 1->2, ...)
            int currentLevel = IceCreamManager.Instance.Level;
            if (!IceCreamManager.Instance.HasNextLevel)
                return;

            int targetLevel = currentLevel + 1;

            // Map desired target level to an IAP item index
            int itemIndex = -1;
            if (targetLevel == 1)
                itemIndex = level1ItemIndex >= 0 ? level1ItemIndex : iapScript.iceCreamUpgradeItemLevel1Index;
            else if (targetLevel == 2)
                itemIndex = level2ItemIndex >= 0 ? level2ItemIndex : iapScript.iceCreamUpgradeItemLevel2Index;

            if (itemIndex < 0)
                return; // not configured

            // Use existing IAP flow: select item then start purchase
            iapScript.SelectItem(itemIndex);
            iapScript.Consumable_BTN_Pressed();
        }
    }
}