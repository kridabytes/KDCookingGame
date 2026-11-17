using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
        [SerializeField] private GameObject flyingIceCreamPrefab;
        [SerializeField] private Canvas mainCanvas;   // HUD canvas (for ScreenToWorldPoint ref)
        [SerializeField] private Camera uiCamera;     // camera that renders the canvas

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
            AudioManager.Instance.MakeVibrate(VibrationType.Low);

            if (IceCreamManager.Instance == null)
                return;

            if (!IceCreamManager.Instance.TryUseOneForCustomer(out BotMain targetBot) || targetBot == null)
                return;

            if (flyingIceCreamPrefab == null || mainCanvas == null)
                return;

            RectTransform canvasRect = mainCanvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            // Choose the correct camera for this canvas.
            // Screen Space Overlay: pass null
            // Screen Space Camera / World Space: use assigned uiCamera
            Camera cam = mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCamera;

            // Start: screen position of the HUD button
            RectTransform buttonRect = iceCreamButton.GetComponent<RectTransform>();
            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(cam, buttonRect.position);

            // End: screen position of the bot's fill bar UI (works for world-space and screen-space canvases)
            if (targetBot.fillBar == null)
                return;
            RectTransform targetFillRect = targetBot.fillBar.rectTransform;
            Canvas botCanvas = targetFillRect.GetComponentInParent<Canvas>();
            Camera botCam = (botCanvas != null && botCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? botCanvas.worldCamera
                : null;
            // Aim at the middle of the bar, not at its pivot (a fill bar's pivot is often on the left edge).
            Vector3 barCenter = targetFillRect.TransformPoint(targetFillRect.rect.center);
            Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(botCam, barCenter);

            // Spawn as a UI element under the same canvas so it's always visible
            GameObject go = Instantiate(flyingIceCreamPrefab, mainCanvas.transform);
            var goRect = go.transform as RectTransform;
            if (goRect != null)
            {
                goRect.anchorMin = new Vector2(0.5f, 0.5f);
                goRect.anchorMax = new Vector2(0.5f, 0.5f);
                goRect.pivot = new Vector2(0.5f, 0.5f);
                goRect.localScale = Vector3.one;
            }

            FlyingIceCreamAnimation anim = go.GetComponent<FlyingIceCreamAnimation>();
            if (anim != null)
            {
                anim.Init(canvasRect, cam, startScreen, endScreen, targetBot);
            }
        }
    }
}