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

    public void Setup(FreeGiftCollector.Gift gift, Action<FreeGiftItemUI> onWatchClicked)
    {
        Gift = gift;

        if (giftImage != null)
        {
            giftImage.sprite = gift.icon;
            giftImage.enabled = gift.icon != null;
        }

        if (giftText != null)
            giftText.text = gift.text;


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
