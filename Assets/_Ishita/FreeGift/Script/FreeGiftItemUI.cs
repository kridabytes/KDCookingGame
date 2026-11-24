using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the freegift prefab root and drag in its gift Image, Text and Watch button.
/// FreeGiftUI calls Setup() for every gift in the FreeGiftCollector.
/// </summary>
public class FreeGiftItemUI : MonoBehaviour
{
    [SerializeField] private Image giftImage;
    [SerializeField] private Text giftText;

  
    [SerializeField] private Button watchButton;

    public FreeGiftCollector.Gift Gift { get; private set; }

    /// <summary>The gift picture on this row (start point of the reward fly animation).</summary>
    public RectTransform GiftImageRect => giftImage != null ? giftImage.rectTransform : null;

    public void Setup(FreeGiftCollector.Gift gift, Action<FreeGiftItemUI> onWatchClicked)
    {
        Gift = gift;

        if (giftImage != null)
        {
            giftImage.sprite = gift.icon;
            giftImage.enabled = gift.icon != null;
        }

        if (giftText != null)
        {
            // The row's Text has a Localize String Event that used ONE fixed entry for every row.
            // Each gift now gives it its own entry, so every row shows its own text in the
            // chosen language (and updates when the language changes).
            var localize = giftText.GetComponent<UnityEngine.Localization.Components.LocalizeStringEvent>();
            bool hasLocalized = gift.localizedText != null && !gift.localizedText.IsEmpty;
            if (localize != null && hasLocalized)
            {
                localize.enabled = true;
                localize.StringReference = gift.localizedText;
                localize.RefreshString();
            }
            else
            {
                if (localize != null)
                {
                    localize.enabled = false;
                    Destroy(localize);
                }
                giftText.text = hasLocalized ? gift.localizedText.GetLocalizedString() : gift.text;
            }
        }


        if (watchButton != null)
        {
            watchButton.onClick.RemoveAllListeners();
            watchButton.onClick.AddListener(() => onWatchClicked?.Invoke(this));
        }
        else
        {
            Debug.LogWarning($"FreeGiftItemUI on '{name}' has no Watch Button assigned.", this);
        }
    }
}
