using UnityEngine;
using UnityEngine.UI;

// Small prefab: reward icon + amount ("x200"). Spawned inside milestone rows and the end popup.
public class EventRewardItem : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text amountText;

    public RectTransform IconRect => icon != null ? icon.rectTransform : (RectTransform)transform;
    public Sprite IconSprite => icon != null ? icon.sprite : null;

    public void Setup(Sprite sprite, string amount)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (amountText != null)
            amountText.text = amount;
    }
}
