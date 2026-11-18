using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// HUD button + text for total player-owned ice cream.
    /// Also spawns flying ice cream toward best customer.
    /// </summary>
    public class IceCreamHUDController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button iceCreamButton;
        [SerializeField] private TextMeshProUGUI totalIceCreamText;

        [Header("Flying Ice Cream")]
        [Tooltip("Only its picture is used. The flight itself is drawn on the GameFx overlay, so it is visible above every canvas.")]
        [SerializeField] private GameObject flyingIceCreamPrefab;
        [Tooltip("Optional: picture to fly instead of the prefab's picture.")]
        [SerializeField] private Sprite flyingSprite;

        private bool subscribed;
        private Coroutine bindRoutine;

        private void Awake()
        {
            if (iceCreamButton != null)
                iceCreamButton.onClick.AddListener(OnIceCreamButtonClicked);

            // Ensure a sensible first-frame UI even before manager binds.
            if (totalIceCreamText != null)
                totalIceCreamText.text = "0";
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
            {
                IceCreamManager.Instance.OnStateChanged -= Refresh;
            }
            subscribed = false;
        }

        private IEnumerator BindWhenReady()
        {
            // Wait until IceCreamManager is created (first scene load order can vary)
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
            if (IceCreamManager.Instance == null || totalIceCreamText == null)
                return;

            totalIceCreamText.text = IceCreamManager.Instance.TotalStored.ToString();
        }

        private void OnIceCreamButtonClicked()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.MakeVibrate(VibrationType.Low);

            if (IceCreamManager.Instance == null || iceCreamButton == null)
                return;

            Transform buttonTransform = iceCreamButton.transform;

            if (!IceCreamManager.Instance.TryUseOneForCustomer(out BotMain targetBot) || targetBot == null)
            {
                // Nothing in stock, or nobody waiting: a small "no" shake.
                buttonTransform.DOKill(true);
                buttonTransform.DOShakePosition(0.3f, new Vector3(12f, 0f, 0f), 18, 0f).SetLink(gameObject);
                return;
            }

            // Button squish so the tap feels responsive.
            buttonTransform.DOKill(true);
            buttonTransform.DOPunchScale(new Vector3(-0.15f, -0.15f, 0f), 0.25f, 8, 0.7f).SetLink(gameObject);

            Sprite sprite = GetFlyingSprite();
            if (FlyingIceCreamAnimation.Launch(sprite, buttonTransform, targetBot) == null)
            {
                // Could not animate (e.g. missing picture): still give the customer the ice cream.
                IceCreamManager.Instance.ApplyBoostToCustomer(targetBot);
            }
        }

        /// <summary>The ice cream picture: from the flying prefab, or the button's own image.</summary>
        private Sprite GetFlyingSprite()
        {
            if (flyingSprite != null)
                return flyingSprite;

            if (flyingIceCreamPrefab != null)
            {
                Image prefabImage = flyingIceCreamPrefab.GetComponentInChildren<Image>(true);
                if (prefabImage != null && prefabImage.sprite != null)
                    return prefabImage.sprite;
            }

            Image buttonImage = iceCreamButton.image != null ? iceCreamButton.image : iceCreamButton.GetComponent<Image>();
            return buttonImage != null ? buttonImage.sprite : null;
        }
    }
}