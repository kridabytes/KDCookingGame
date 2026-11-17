using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Handles Unity IAP purchase flow for ice cream upgrades.
    /// You configure product IDs per level and this script will
    /// invoke a callback when the purchase succeeds.
    /// </summary>
    public class IceCreamIAPUpgradeHandler : MonoBehaviour
    #if UNITY_PURCHASING
        , IStoreListener
    #endif
    {
#if UNITY_PURCHASING
        private IStoreController storeController;
        private IExtensionProvider storeExtensionProvider;
#endif

        [Serializable]
        public class LevelProduct
        {
            public int level;          // Level index to unlock (0-based, matches IceCreamUpgradeSystem)
            public string productId;   // Unity IAP product ID for this level
        }

        [Header("IAP Products")]
        [SerializeField] private List<LevelProduct> levelProducts = new List<LevelProduct>();

        private Action<bool> pendingCallback;
        private int pendingLevel = -1;

        private void Start()
        {
#if UNITY_PURCHASING
            if (storeController == null)
            {
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

                foreach (var lp in levelProducts)
                {
                    if (!string.IsNullOrEmpty(lp.productId))
                    {
                        builder.AddProduct(lp.productId, ProductType.NonConsumable);
                    }
                }

                UnityPurchasing.Initialize(this, builder);
            }
#endif
        }

        /// <summary>
        /// Initiates purchase of the given level. On success, onComplete(true) is invoked.
        /// You should then call IceCreamManager.Instance.ApplyPurchasedUpgrade().
        /// </summary>
        public void PurchaseUpgrade(int targetLevel, Action<bool> onComplete)
        {
            pendingCallback = onComplete;
            pendingLevel = targetLevel;

#if UNITY_PURCHASING
            if (storeController == null)
            {
                onComplete?.Invoke(false);
                return;
            }

            string productId = GetProductIdForLevel(targetLevel);
            if (string.IsNullOrEmpty(productId))
            {
                onComplete?.Invoke(false);
                return;
            }

            storeController.InitiatePurchase(productId);
#else
            // Unity IAP not enabled: simulate success in editor/without IAP.
            onComplete?.Invoke(true);
#endif
        }

        private string GetProductIdForLevel(int level)
        {
            foreach (var lp in levelProducts)
            {
                if (lp.level == level)
                    return lp.productId;
            }
            return null;
        }

#if UNITY_PURCHASING
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            storeExtensionProvider = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"IceCreamIAPUpgradeHandler IAP init failed: {error}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            bool success = false;

            // Match any configured product
            foreach (var lp in levelProducts)
            {
                if (string.Equals(e.purchasedProduct.definition.id, lp.productId, StringComparison.OrdinalIgnoreCase))
                {
                    success = true;
                    break;
                }
            }

            pendingCallback?.Invoke(success);
            pendingCallback = null;
            pendingLevel = -1;

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"IceCreamIAPUpgradeHandler purchase failed: {product.definition.id} - {failureReason}");
            pendingCallback?.Invoke(false);
            pendingCallback = null;
            pendingLevel = -1;
        }
#endif
    }
}

