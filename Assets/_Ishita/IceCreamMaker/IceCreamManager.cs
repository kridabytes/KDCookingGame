using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Central manager for ice cream production, storage, and usage.
    /// Lives across scenes via DontDestroyOnLoad.
    /// </summary>
    public class IceCreamManager : MonoBehaviour
    {
        public static IceCreamManager Instance { get; private set; }

        [Header("Upgrade Database")]
        [SerializeField] private IceCreamUpgradeDatabase upgradeDatabase;

        [Header("Boost Config")]
        [Range(0.05f, 1f)]
        [SerializeField] private float iceCreamBoostNormalized = 0.25f;

        [Header("Debug")]
        [SerializeField] private bool logDebug;

        private IceCreamStorageSystem storage;
        private IceCreamUpgradeSystem upgrade;

        // Timing
        // lastSavedUtc is used to "catch up" production when app was closed/paused.
        private double lastSavedUtc;               // seconds since Unix epoch
        private float productionProgressSeconds;   // partial progress in current cycle
        private bool batchReady;                   // timer completed; waiting for player to claim

        private bool didInitialRecalc;
        private bool dirty;                        // state changed and should be saved

        // PlayerPrefs keys
        private const string PP_LAST_PROD_UTC = "IC_LastProdUtc";
        private const string PP_MACHINE_STORED = "IC_MachineStored";
        private const string PP_PLAYER_OWNED = "IC_PlayerOwned"; // legacy (unused now)
        private const string PP_LEVEL = "IC_Level";
        private const string PP_PROGRESS = "IC_ProgressSeconds";
        private const string PP_BATCH_READY = "IC_BatchReady";

        public event Action OnStateChanged;        // For HUD / popup to refresh

        #region Unity

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstanceExists()
        {
            if (Instance != null)
                return;

            // If a scene already contains an IceCreamManager, let its Awake set Instance.
            var existing = FindFirstObjectByType<IceCreamManager>();
            if (existing != null)
                return;

            var go = new GameObject(nameof(IceCreamManager));
            go.AddComponent<IceCreamManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // If a scene contains an IceCreamManager with inspector-tuned values,
                // copy those values into the already-running singleton (often auto-created)
                // so gameplay uses the correct configuration (e.g. boost=1.0).
                Instance.CopyRuntimeConfigFrom(this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Init systems with defaults (will be overridden by load)
            upgrade = new IceCreamUpgradeSystem(upgradeDatabase, 0);
            storage = new IceCreamStorageSystem(upgrade.CurrentCapacity);

            LoadState();
            RecalculateProductionFromRealTime();
            didInitialRecalc = true;

            // UI might subscribe after Awake; they can also pull state directly.
            SafeInvokeStateChanged();
        }

        private void CopyRuntimeConfigFrom(IceCreamManager source)
        {
            if (source == null) return;

            iceCreamBoostNormalized = source.iceCreamBoostNormalized;
            logDebug = source.logDebug;

            // If the scene instance has an assigned upgrade database, use it and
            // rebuild the upgrade system so Current/Next values come from data.
            if (source.upgradeDatabase != null)
            {
                upgradeDatabase = source.upgradeDatabase;

                int currentLevel = upgrade != null ? upgrade.Level : 0;
                upgrade = new IceCreamUpgradeSystem(upgradeDatabase, currentLevel);

                if (storage != null)
                    storage.SetCapacity(upgrade.CurrentCapacity);
            }

            SafeInvokeStateChanged();
        }

        private void Update()
        {
            TickProduction(Time.deltaTime);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                SaveState(force: true);
        }

        private void OnApplicationQuit()
        {
            SaveState(force: true);
        }

        #endregion

        #region Public API

        public int MachineStored => storage.MachineStored;
        public int MachineCapacity => storage.MachineCapacity;
        public int TotalStored => storage.MachineStored;
        public bool IsClaimReady => batchReady && !IsStorageFull();

        public int CurrentProductionTimeMinutes => upgrade.CurrentProductionTimeMinutes;
        public int CurrentProductionAmount => upgrade.CurrentProductionAmount;
        public int CurrentCapacity => upgrade.CurrentCapacity;

        public int NextProductionTimeMinutes => upgrade.NextProductionTimeMinutes;
        public int NextProductionAmount => upgrade.NextProductionAmount;
        public int NextCapacity => upgrade.NextCapacity;
        public bool HasNextLevel => upgrade.HasNextLevel;
        public int Level => upgrade.Level;
        public int NextUpgradePrice => upgrade.NextUpgradePrice;

        /// <summary>
        /// Returns [0,1] progress toward next production batch.
        /// 0 if storage is full (paused).
        /// </summary>
        public float GetProductionProgress01()
        {
            if (IsStorageFull())
                return 0f;

            float cycleSeconds = upgrade.CurrentProductionTimeMinutes * 60f;
            if (cycleSeconds <= 0.01f) return 0f;

            if (batchReady)
                return 1f;

            return Mathf.Clamp01(productionProgressSeconds / cycleSeconds);
        }

        public TimeSpan GetTimeRemainingToNextBatch()
        {
            if (IsStorageFull())
                return TimeSpan.Zero;

            float cycleSeconds = upgrade.CurrentProductionTimeMinutes * 60f;
            if (batchReady)
                return TimeSpan.Zero;

            float remaining = Mathf.Max(0f, cycleSeconds - productionProgressSeconds);
            return TimeSpan.FromSeconds(remaining);
        }

        /// <summary>
        /// Called by Claim button. Adds one produced batch into total storage.
        /// </summary>
        public int ClaimAll()
        {
            if (!batchReady)
                return 0;

            int added = storage.AddToMachine(upgrade.CurrentProductionAmount);
            if (added <= 0)
                return 0;

            // Reset timer for next batch (unless full)
            batchReady = false;
            productionProgressSeconds = 0f;
            lastSavedUtc = GetUnixTimeSeconds();

            dirty = true;
            SaveState(force: true);
            SafeInvokeStateChanged();
            return added;
        }

        /// <summary>
        /// Called by HUD ice cream button to try to send 1 ice cream.
        /// If returns a target BotMain and true, caller should spawn flying ice cream.
        /// </summary>
        public bool TryUseOneForCustomer(out BotMain targetCustomer)
        {
            targetCustomer = null;

            bool wasFull = IsStorageFull();

            if (!storage.ConsumeFromMachine(1))
                return false;

            targetCustomer = FindLowestFillCustomer();
            if (targetCustomer == null)
            {
                // Refund
                storage.AddToMachine(1);
                return false;
            }

            // If we were full and now have space, restart the timer from 0.
            if (wasFull && !IsStorageFull() && !batchReady)
            {
                productionProgressSeconds = 0f;
                lastSavedUtc = GetUnixTimeSeconds();
            }

            dirty = true;
            SaveState(force: true);
            SafeInvokeStateChanged();
            return true;
        }

        /// <summary>
        /// Gives back one ice cream that was taken for a customer who left before it arrived.
        /// </summary>
        public void RefundOne()
        {
            if (storage == null || storage.AddToMachine(1) <= 0)
                return;

            dirty = true;
            SaveState(force: true);
            SafeInvokeStateChanged();
        }

        /// <summary>
        /// Called by flying ice cream when it reaches the customer.
        /// </summary>
        public void ApplyBoostToCustomer(BotMain customer)
        {
            if (customer == null) return;
            customer.ApplyIceCreamBoost(iceCreamBoostNormalized);
        }

        /// <summary>
        /// Applies the next level after a successful IAP purchase.
        /// Returns false if there is no higher level.
        /// </summary>
        public bool ApplyPurchasedUpgrade()
        {
            if (!upgrade.HasNextLevel)
                return false;

            if (!upgrade.Upgrade())
                return false;

            // Update capacity in storage and clamp stored
            storage.SetCapacity(upgrade.CurrentCapacity);

            dirty = true;
            SaveState(force: true);
            SafeInvokeStateChanged();
            return true;
        }

        #endregion

        #region Internal production

        private void TickProduction(float deltaTime)
        {
            if (IsStorageFull())
                return;
            if (batchReady)
                return;

            float cycleSeconds = upgrade.CurrentProductionTimeMinutes * 60f;
            if (cycleSeconds <= 0.01f)
                return;

            productionProgressSeconds += deltaTime;

            if (productionProgressSeconds >= cycleSeconds)
            {
                // Batch is ready to claim; do NOT auto-add to storage.
                batchReady = true;
                productionProgressSeconds = cycleSeconds;

                dirty = true;
                SaveState(force: true);
                SafeInvokeStateChanged();
            }
        }

        private void RecalculateProductionFromRealTime()
        {
            double now = GetUnixTimeSeconds();
            double elapsedSeconds = Math.Max(0.0, now - lastSavedUtc);

            float cycleSeconds = upgrade.CurrentProductionTimeMinutes * 60f;
            if (cycleSeconds <= 0.01f)
            {
                productionProgressSeconds = 0;
                return;
            }

            if (!IsStorageFull() && !batchReady)
            {
                double newProgress = productionProgressSeconds + elapsedSeconds;
                if (newProgress >= cycleSeconds)
                {
                    batchReady = true;
                    dirty = true;
                    productionProgressSeconds = cycleSeconds;
                }
                else
                {
                    productionProgressSeconds = (float)newProgress;
                }
            }
            else
            {
                productionProgressSeconds = 0f;
            }

            lastSavedUtc = now;
            SaveState(force: true);
        }

        private bool IsStorageFull()
        {
            return storage.MachineStored >= storage.MachineCapacity;
        }

        #endregion

        #region Persistence

        private void LoadState()
        {
            // Level
            int savedLevel = PlayerPrefs.GetInt(PP_LEVEL, 0);
            upgrade.SetLevel(savedLevel);

            // Capacity according to level
            storage.SetCapacity(upgrade.CurrentCapacity);

            // Stored counts
            int machineStored = PlayerPrefs.GetInt(PP_MACHINE_STORED, 0);
            storage.SetMachineStored(machineStored);

            // Timer / claim state
            productionProgressSeconds = PlayerPrefs.GetFloat(PP_PROGRESS, 0f);
            batchReady = PlayerPrefs.GetInt(PP_BATCH_READY, 0) == 1;

            // If batch is marked ready, clamp progress to full cycle.
            if (batchReady)
                productionProgressSeconds = upgrade.CurrentProductionTimeMinutes * 60f;

            string lastProdStr = PlayerPrefs.GetString(PP_LAST_PROD_UTC, string.Empty);
            if (string.IsNullOrEmpty(lastProdStr) ||
                !double.TryParse(lastProdStr, NumberStyles.Float, CultureInfo.InvariantCulture, out lastSavedUtc))
            {
                lastSavedUtc = GetUnixTimeSeconds();
            }
        }

        private void SaveState(bool force)
        {
            if (!force && !dirty)
                return;

            lastSavedUtc = GetUnixTimeSeconds();
            PlayerPrefs.SetInt(PP_LEVEL, upgrade.Level);
            PlayerPrefs.SetInt(PP_MACHINE_STORED, storage.MachineStored);
            PlayerPrefs.SetFloat(PP_PROGRESS, productionProgressSeconds);
            PlayerPrefs.SetInt(PP_BATCH_READY, batchReady ? 1 : 0);
            PlayerPrefs.SetString(PP_LAST_PROD_UTC, lastSavedUtc.ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            dirty = false;
        }

        private static double GetUnixTimeSeconds()
        {
            return (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        }

        #endregion

        #region Customer selection

        private BotMain FindLowestFillCustomer()
        {
            BotMain[] allBots = FindObjectsByType<BotMain>(FindObjectsSortMode.None);
            BotMain best = null;
            float bestFill = 1.01f; // we want the SMALLEST fill

            foreach (var bot in allBots)
            {
                if (bot == null || bot.fillBar == null)
                    continue;

                // Only customers that are standing and waiting for food (see BotMain.ChangeStatus)
                if (!bot.IsWaitingForOrder || !bot.fillBar.gameObject.activeInHierarchy)
                    continue;

                float fill = bot.fillBar.fillAmount;
                if (fill < bestFill)
                {
                    bestFill = fill;
                    best = bot;
                }
            }

            return best;
        }

        #endregion

        private void SafeInvokeStateChanged()
        {
            OnStateChanged?.Invoke();
        }
    }
}